using NexaCastVideo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Globalization;
using System.Text.RegularExpressions;
using Google.Apis.YouTube.v3.Data;

class Program
{
    static async Task Main(string[] args)
    {
        string logFileName = "log_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt";
        string logDirectory = Path.Combine(Environment.CurrentDirectory, "NexaCastVideo", "Logs");
        string generationDirectory = "";

        Logger.LogInfo($"Attempting to create directory at {logDirectory}");
        if (!Directory.Exists(logDirectory))
        {
            Directory.CreateDirectory(logDirectory);
        }
        Logger.LogInfo($"Directory created at {logDirectory}");

        string logFilePath = Path.Combine(logDirectory, logFileName);
        Logger.Setup(logFilePath);

        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            Logger.LogError($"Unhandled exception: {(e.ExceptionObject as Exception)?.Message}");
        };

        try
        {
            Logger.LogInfo("Initializing...");
            Logger.LogInfo("Loading configurations...");
            ConfigManager.LoadConfigurations();
            string apiKey = ConfigManager.GetAPIKey("DalleApiKey");

            Console.WriteLine("Please enter a short description of what you want the video to be about:");
            string userRequest = Console.ReadLine();

            using (Spinner spinner = new Spinner())
            {
                spinner.Start();

                string formattedUserRequest = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(userRequest);
                string safeUserRequestFolderName = string.Concat(formattedUserRequest.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
                generationDirectory = Path.Combine("NexaCastVideo", "Generation", safeUserRequestFolderName);

                Console.WriteLine($"Attempting to create directory: {generationDirectory}");
                Directory.CreateDirectory(generationDirectory);

                if (Directory.Exists(generationDirectory))
                {
                    Logger.LogInfo($"Directory {generationDirectory} created successfully.");
                }
                else
                {
                    Logger.LogError($"Failed to create directory {generationDirectory}.");
                    return;
                }

                MusicManager musicManager = new MusicManager();
                Logger.LogInfo("Downloading random royalty-free music...");
                string musicFilePath = await musicManager.DownloadRandomMusicFileAsync(generationDirectory);

                if (string.IsNullOrEmpty(musicFilePath))
                {
                    Logger.LogError("Failed to download a music file. Aborting process.");
                    return;
                }
                else
                {
                    Logger.LogInfo($"Music file downloaded successfully: {musicFilePath}");
                }

                TopicGenerator topicGenerator = new TopicGenerator(generationDirectory);
                string script = await topicGenerator.GenerateScriptFromInput(userRequest);
                await topicGenerator.SaveScript(script);

                string subtitlePath = Path.Combine(generationDirectory, "GeneratedSubtitles.srt");
                topicGenerator.GenerateSubtitles(script);
                await topicGenerator.GenerateDallePromptsFromScript(script);

                string promptsFilePath = Path.Combine(generationDirectory, "DallePrompts.txt");
                if (new FileInfo(promptsFilePath).Length == 0)
                {
                    Logger.LogError("DallePrompts.txt is empty. No prompts generated.");
                    return;
                }

                Logger.LogInfo("Fetching and downloading images...");
                ImageFetcher imageFetcher = new ImageFetcher(apiKey, generationDirectory);
                List<string> imageFiles = await imageFetcher.FetchAndDownloadImages();

                if (imageFiles.Count == 0)
                {
                    Logger.LogError("No images were downloaded.");
                    return;
                }

                string ttsScript = ProcessTextForTts(script, true);
                ttsScript = RemoveNarrationLabels(ttsScript);

                Logger.LogInfo("Generating and downloading voiceovers...");
                VoiceoverGenerator voiceoverGenerator = new VoiceoverGenerator(generationDirectory, ConfigManager.GetAPIKey("ElevenLabsApiKey"));
                var scriptSegments = ttsScript.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
                var voiceoverFiles = await voiceoverGenerator.GenerateVoiceovers(new List<string>(scriptSegments));

                List<string> downloadedVoiceoverFiles = new List<string>();
                foreach (var voiceoverUrl in voiceoverFiles)
                {
                    Logger.LogInfo($"Attempting to download voiceover from URL: {voiceoverUrl}");
                    string localVoiceoverPath = Path.Combine(generationDirectory, "Voiceovers", Path.GetFileName(voiceoverUrl));
                    downloadedVoiceoverFiles.Add(await DownloadFileAsync(voiceoverUrl, localVoiceoverPath));
                }

                List<string> videoSegments = new List<string>();

                // After downloading voiceovers
                VideoCompiler videoCompiler = new VideoCompiler(generationDirectory);

                // Compile individual segments
                await videoCompiler.CompileIndividualSegments(imageFiles, downloadedVoiceoverFiles);

                // Path for the final concatenated video
                string finalOutputVideo = Path.Combine(generationDirectory, "final_video.mp4");

                // Concatenate segments
                await videoCompiler.ConcatenateSegments(videoSegments);

                // Overlay background music
                string backgroundMusicFile = musicFilePath; // Path to background music file
                await videoCompiler.OverlayBackgroundMusicAndSubtitles(finalOutputVideo, backgroundMusicFile);

                spinner.Stop();
            }
        }
        finally
        {
            if (!string.IsNullOrEmpty(generationDirectory))
            {
                // placeholder for cleanup directory
            }

            Logger.LogInfo("Application has terminated.");
        }
    }

    private static string ProcessTextForTts(string input, bool ignoreParenthesesContent)
    {
        if (ignoreParenthesesContent)
        {
            input = Regex.Replace(input, @"\([^)]*\)", "");
        }
        return input;
    }

    private static string RemoveNarrationLabels(string input)
    {
        string[] patterns = { "Sub-point:", "Narrator:", "Point:", "Main Point:" };
        foreach (var pattern in patterns)
        {
            input = Regex.Replace(input, pattern, "", RegexOptions.IgnoreCase);
        }
        return input.Trim();
    }

    private static async Task<string> DownloadFileAsync(string fileUrl, string destinationPath)
    {
        using (HttpClient client = new HttpClient())
        {
            client.Timeout = TimeSpan.FromSeconds(200);
            byte[] bytes = null;
            try
            {
                var response = await client.GetAsync(fileUrl);
                response.EnsureSuccessStatusCode();
                bytes = await response.Content.ReadAsByteArrayAsync();
            }
            catch (HttpRequestException httpEx)
            {
                Logger.LogError($"HTTP Request Error in DownloadFileAsync: {httpEx.Message}");
            }
            catch (Exception ex)
            {
                Logger.LogError($"General Error in API call in DownloadFileAsync: {ex.Message}");
            }

            if (bytes != null)
            {
                await File.WriteAllBytesAsync(destinationPath, bytes);
            }
            else
            {
                Logger.LogError($"Bytes are null, skipping file writing for {fileUrl}");
            }

            return destinationPath;
        }
    }
}
