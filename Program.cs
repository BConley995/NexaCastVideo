﻿using NexaCastVideo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;
using Polly;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Security.Cryptography.X509Certificates;

class Program
{
    static async Task Main(string[] args)
    {
        string logFileName = "log_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt";
        string logDirectory = Path.Combine(Environment.CurrentDirectory, "NexaCastVideo", "Errors");

        string generationDirectory = null;

        // Ensure the directory exists
        if (!Directory.Exists(logDirectory))
        {
            Directory.CreateDirectory(logDirectory);
        }

        string logFilePath = Path.Combine(logDirectory, logFileName);

        Logger.Setup(logFilePath);

        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            Logger.LogError($"Unhandled exception: {(e.ExceptionObject as Exception)?.Message}");
        };


        try
        {
            Logger.LogInfo("Initializing...");

            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\..\\");
            Environment.SetEnvironmentVariable("PATH", Environment.GetEnvironmentVariable("PATH") + ";" + path);

            Logger.LogInfo("Loading configurations...");
            ConfigManager.LoadConfigurations();

            var gpt4ApiKey = ConfigManager.GetAPIKey("Gpt4ApiKey");
            var elevenLabsApiKey = ConfigManager.GetAPIKey("ElevenLabsApiKey");
            var cloudStorageUrl = ConfigManager.GetAppSetting("CloudStorageUrl");
            var uploadEndpoint = ConfigManager.GetAppSetting("UploadEndpoint");
            Console.WriteLine("Please enter a short description of what you want the video to be about:");
            string userRequest = Console.ReadLine();

            string formattedUserRequest = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(userRequest);
            string safeUserRequestFolderName = string.Concat(formattedUserRequest.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));

            // Initialize generationDirectory
            generationDirectory = Path.Combine("NexaCastVideo", "Generation", safeUserRequestFolderName);

            // Debugging & Sanity Check
            Console.WriteLine($"Attempting to create directory: {generationDirectory}");
            Directory.CreateDirectory(generationDirectory);
            if (!Directory.Exists(generationDirectory))
            {
                Console.WriteLine($"Failed to create directory: {generationDirectory}");
            }

            string subtitlePath = Path.Combine(generationDirectory, "GeneratedSubtitles.srt");


            ContentCreator contentCreator = new ContentCreator(generationDirectory);
            string script = await contentCreator.GenerateScriptAsync(userRequest);

            using (var spinner = new Spinner())
            {
                Console.Write("  Generating Script... ");
                spinner.Start();

                var subtopicGenerator = new TopicGenerator(generationDirectory);

                var retryPolicy = Policy
                    .Handle<Exception>()
                    .WaitAndRetryAsync(
                        retryCount: 3,
                        sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                        onRetry: (exception, timeSpan, retryCount, context) =>
                        {
                            Logger.LogWarning($"Attempt {retryCount} failed - delaying for {timeSpan.TotalSeconds}sec");
                        }
                    );

                try
                {
                    await retryPolicy.ExecuteAsync(async () =>
                    {
                        script = await subtopicGenerator.GenerateScriptFromInput(userRequest);
                    });
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Exception: {ex.Message}\nType: {ex.GetType().FullName}\nStackTrace: {ex.StackTrace}");
                    throw;
                }

                spinner.Stop();
                Console.WriteLine("Done!");
            }

            Logger.LogInfo($"Generated Script: {script}");
            await contentCreator.SaveScript(script);

            // Call to GenerateSubtitles
            TopicGenerator topicGenerator = new TopicGenerator(generationDirectory);
            topicGenerator.GenerateSubtitles(script);

            // Call the new GenerateDallePromptsFromScript method to generate prompts
            TopicGenerator topicGeneratorForDalle = new TopicGenerator(generationDirectory);
            var dallePrompts = await topicGeneratorForDalle.GenerateDallePromptsFromScript(script);

            // Save the prompts to DallePrompts.txt
            var dallePromptsFilePath = Path.Combine(generationDirectory, "DallePrompts.txt");
            await File.WriteAllLinesAsync(dallePromptsFilePath, dallePrompts);

            // Process the script for TTS
            string ttsScript = ProcessTextForTts(script, true);
            ttsScript = RemoveNarrationLabels(ttsScript);

            static string RemoveNarrationLabels(string input)
            {
                string[] patterns = { "Sub-point:", "Narrator:", "Point:", "Main Point:" };

                foreach (var pattern in patterns)
                {
                    input = Regex.Replace(input, pattern, "", RegexOptions.IgnoreCase);
                }

                return input.Trim();
            }

            Logger.LogInfo("Generating and downloading voiceovers...");
            VoiceoverGenerator voiceoverGenerator = new VoiceoverGenerator(generationDirectory, elevenLabsApiKey);
            var scriptSegments = ttsScript.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
            var voiceoverFiles = await voiceoverGenerator.GenerateVoiceovers(new List<string>(scriptSegments));
            
            // Create a list to store the tasks for downloading voiceovers
            var downloadTasks = new List<Task<string>>();

            foreach (var voiceoverUrl in voiceoverFiles)
            {
                Logger.LogInfo($"Attempting to download voiceover from URL: {voiceoverUrl}");

                // Generate a local path based on the safeUserRequestFolderName
                string localVoiceoverPath = Path.Combine(generationDirectory, "Voiceovers", Path.GetFileName(voiceoverUrl));

                // Start downloading the voiceover asynchronously and add it to the downloadTasks list
                downloadTasks.Add(DownloadFileAsync(voiceoverUrl, localVoiceoverPath));
            }

            // Wait for all voiceover downloads to complete asynchronously
            await Task.WhenAll(downloadTasks);

            List<string> downloadedVoiceoverFiles = downloadTasks.Select(task => task.Result).ToList();
            // Check if any downloads failed
            if (downloadTasks.Any(task => task.IsFaulted))
            {
                // Handle the error as needed
                Logger.LogError("Some voiceover downloads failed.");
                return;
            }

            Logger.LogInfo("Fetching and downloading images...");
            ImageFetcher imageFetcher = new ImageFetcher(generationDirectory);
            var imageUrls = await imageFetcher.GenerateImagesForScriptAsync(scriptSegments.ToList());

            // Concurrent image download tasks
            var imageDownloadTasks = imageUrls.Select(url => DownloadFileAsync(url, Path.Combine(generationDirectory, "Image" + (imageUrls.IndexOf(url) + 1) + ".jpg"))).ToList();

            // Await all the downloads
            var localImagePaths = await Task.WhenAll(imageDownloadTasks);

            // Check if any downloads failed
            if (imageDownloadTasks.Any(task => task.IsFaulted))
            {
                // Handle the error as needed
                Logger.LogError("Some image downloads failed.");
                return;
            }

            List<string> imageFiles = new List<string>();

            imageFiles.AddRange(localImagePaths);

            foreach (var scriptSegment in scriptSegments)
            {
                var imageUrl = await imageFetcher.GenerateImageForScriptLineAsync(scriptSegment);

                Logger.LogInfo($"Attempting to download image from URL: {imageUrl}");

                var localImagePath = await DownloadFileAsync(imageUrl, Path.Combine(generationDirectory, Path.GetFileName(imageUrl)));
                imageFiles.Add(localImagePath);
            }

            int voiceoverCount = downloadedVoiceoverFiles.Count;
            int imageCount = imageFiles.Count;

            if (voiceoverCount != imageCount)
            {
                Logger.LogError("Mismatch between the number of voiceovers and images.");

                if (voiceoverCount > imageCount)
                {
                    int excessCount = voiceoverCount - imageCount;
                    downloadedVoiceoverFiles.RemoveRange(imageCount, excessCount);
                    Logger.LogInfo($"Removed {excessCount} excess voiceovers.");
                }
                else
                {
                    int excessImages = imageCount - voiceoverCount;
                    Logger.LogInfo($"Removing {excessImages} excess images...");
                    imageFiles.RemoveRange(voiceoverCount, excessImages);
                }

                voiceoverCount = downloadedVoiceoverFiles.Count;
                imageCount = imageFiles.Count;

                if (voiceoverCount != imageCount)
                {
                    Logger.LogError("Failed to rectify the mismatch between voiceovers and images.");
                    return;
                }
                else
                {
                    Logger.LogInfo("Mismatch handled. The counts now match.");
                }
            }

            Logger.LogInfo("Building slideshow...");
            MusicManager musicManager1 = new MusicManager();
            string outputPath = Path.Combine(generationDirectory, "FileName.mp4");
            SlideshowBuilder slideshowBuilder = new SlideshowBuilder(outputPath, generationDirectory);

            string ffmpegPath = @"path_to_ffmpeg";
            VideoCompiler compiler = new VideoCompiler(ffmpegPath, generationDirectory);

            MusicManager musicManager = new MusicManager();
            string musicFileUrl = await musicManager.GetRandomMusicFileUrlAsync();

            // Ideally, you'd use a method to download the file and get a local path.
            string musicFilePath = await musicManager.DownloadRandomMusicFileAsync();

            await compiler.CompileVideoFromSegments(string.Join(",", downloadedVoiceoverFiles), string.Join(",", imageFiles), musicFilePath, subtitlePath, outputPath);



            Logger.LogInfo($"Video created at: {outputPath}");
        }
        finally
        {
            CleanupDirectories(new List<string> { Path.Combine(generationDirectory, "Voiceovers"), Path.Combine(generationDirectory, "Images"), Path.Combine(generationDirectory, "Subtitles") });

            Logger.LogInfo("Application has terminated.");
        }
    }

    private static void CreateDirectories(List<string> directories)
    {
        foreach (var directory in directories)
        {
            Directory.CreateDirectory(directory);
        }
    }

    private static void CleanupDirectories(List<string> directories)
    {
        foreach (var directory in directories)
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }

    private static string ToTitleCase(string input)
    {
        TextInfo ti = CultureInfo.CurrentCulture.TextInfo;
        return ti.ToTitleCase(input.ToLower());
    }

    private static string ProcessTextForTts(string input, bool ignoreParenthesesContent)
    {
        if (ignoreParenthesesContent)
        {
            return System.Text.RegularExpressions.Regex.Replace(input, @"\([^)]*\)", "");
        }
        return input;
    }
    private static async Task<string> DownloadFileAsync(string fileUrl, string destinationPath)
    {
        using (HttpClient client = new HttpClient())
        {
            byte[] bytes = null;
            try
            {
                // API Call: Handling exception for client.GetAsync
                var response = await client.GetAsync(fileUrl);
                response.EnsureSuccessStatusCode();
                bytes = await response.Content.ReadAsByteArrayAsync();
            }
            catch (HttpRequestException httpEx)
            {
                // You may want to add specific handling for HTTP request exceptions here.
                Logger.LogError($"HTTP Request Error in DownloadFileAsync: {httpEx.Message}");
            }
            catch (Exception ex)
            {
                Logger.LogError($"General Error in API call in DownloadFileAsync: {ex.Message}");
            }

            // File I/O: Handling exception for File.WriteAllBytesAsync
            try
            {
                if (bytes != null)
                {
                    await File.WriteAllBytesAsync(destinationPath, bytes);
                }
                else
                {
                    Logger.LogError($"Bytes are null, skipping file writing for {fileUrl}");
                }
            }
            catch (IOException ioEx)
            {
                // Handling exceptions related to file I/O operations.
                Logger.LogError($"File I/O Error in DownloadFileAsync: {ioEx.Message}");
            }
            catch (Exception ex)
            {
                Logger.LogError($"General Error in file operation in DownloadFileAsync: {ex.Message}");
            }

            return destinationPath;
        }
    }

}
