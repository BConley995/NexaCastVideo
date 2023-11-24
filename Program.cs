using NexaCastVideo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Globalization;
using System.Text.RegularExpressions;
<<<<<<< HEAD
=======
using Google.Apis.YouTube.v3.Data;
>>>>>>> 11e65ae839e18e5bf191660fc19839b2140e4a37

class Program
{
    static async Task Main(string[] args)
    {
<<<<<<< HEAD

        //static void TestFilePathGeneration()
        //{
        //    // Simulate user input or dynamic values
        //    string userInput = "History Of Thanksgiving";
        //    string generationDirectory = Path.Combine("NexaCastVideo", "Generation", userInput);

        //    // Simulate the generation of file names
        //    List<string> voiceoverFiles = new List<string> { "voiceover_0.mp3", "voiceover_1.mp3", "voiceover_2.mp3" };

        //    foreach (var fileName in voiceoverFiles)
        //    {
        //        string fullPath = Path.Combine(generationDirectory, fileName);
        //        Console.WriteLine($"Generated path: {fullPath}");

        //        // Check if the file exists (optional)
        //        if (File.Exists(fullPath))
        //        {
        //            Console.WriteLine("File exists.");
        //        }
        //        else
        //        {
        //            Console.WriteLine("File does not exist.");
        //        }
        //    }
        //}

        //TestFilePathGeneration();

        //Console.WriteLine("\nPress any key to exit...");
        //Console.ReadKey();

=======
>>>>>>> 11e65ae839e18e5bf191660fc19839b2140e4a37
        string logFileName = "log_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt";
        string logDirectory = Path.Combine(Environment.CurrentDirectory, "NexaCastVideo", "Logs");
        string generationDirectory = "";

<<<<<<< HEAD
        Console.WriteLine("\nPress any key to continue...");
        Console.ReadKey();

=======
>>>>>>> 11e65ae839e18e5bf191660fc19839b2140e4a37
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

<<<<<<< HEAD
        Console.WriteLine("Please enter a short description of what you want the video to be about:");
        string userRequest = Console.ReadLine();

        string formattedUserRequest = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(userRequest);
        string safeUserRequestFolderName = string.Concat(formattedUserRequest.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        generationDirectory = Path.Combine("NexaCastVideo", "Generation", safeUserRequestFolderName);

        if (!Directory.Exists(generationDirectory))
        {
            Console.WriteLine($"Attempting to create directory: {generationDirectory}");
            Directory.CreateDirectory(generationDirectory);
        }

        TopicGenerator topicGenerator = new TopicGenerator(generationDirectory);

        //await TestPathConstruction(generationDirectory);

        //Console.WriteLine("\nPress any key to exit...");
        //Console.ReadKey();

        bool apiIsAvailable = await topicGenerator.TestOpenAIAPIAvailability();
        if (!apiIsAvailable)
        {
            Console.WriteLine("OpenAI API is not responding. Choose an option:");
            Console.WriteLine("1. Attempt to continue");
            Console.WriteLine("2. Cancel");
            Console.WriteLine("3. Check again");

            string userChoice = Console.ReadLine();
            if (userChoice == "2")
            {
                Console.WriteLine("Cancelling program.");
                return;
            }
            else if (userChoice == "3")
            {
                Console.WriteLine("Rechecking API status...");
                await Main(args);
                return;
            }
        }

        using (Spinner spinner = new Spinner())
        {
            spinner.Start();

            MusicManager musicManager = new MusicManager();
            Logger.LogInfo("Downloading random royalty-free music...");
            string musicFilePath = await musicManager.DownloadRandomMusicFileAsync(generationDirectory);

            if (string.IsNullOrEmpty(musicFilePath))
            {
                Logger.LogError("Failed to download a music file. Aborting process.");
                return;
            }

            Logger.LogInfo($"Music file downloaded successfully: {musicFilePath}");

            string script = await topicGenerator.GenerateScriptFromInput(userRequest);
            await topicGenerator.SaveScript(script);

            // Split the script into segments for voiceover generation and convert to List<string>
            var scriptSegmentsList = new List<string>(script.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries));

            await topicGenerator.GenerateDallePromptsFromScript(script);

            string promptsFilePath = Path.Combine(generationDirectory, "DallePrompts.txt");
            if (new FileInfo(promptsFilePath).Length == 0)
            {
                Logger.LogError("DallePrompts.txt is empty. No prompts generated.");
                return;
            }

            Logger.LogInfo("Fetching and downloading images...");
            ImageFetcher imageFetcher = new ImageFetcher(ConfigManager.GetAPIKey("DalleApiKey"), generationDirectory);
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
            List<string> voiceoverFiles = await voiceoverGenerator.GenerateVoiceovers(scriptSegmentsList);

            VideoCompiler videoCompiler = new VideoCompiler(generationDirectory);
            await videoCompiler.CompileIndividualSegments(imageFiles, voiceoverFiles);

            Logger.LogInfo("Concatenating video segments...");
            await videoCompiler.ConcatenateSegments();

            string finalVideoPath = Path.Combine(generationDirectory, "Generated Video", "final_video.mp4");
            //Logger.LogInfo("Concatenation of video segments completed. Proceeding to add background music.");
            //string outputWithMusic = await musicManager.AddBackgroundMusicWithFade(finalVideoPath, new List<string> { musicFilePath }, generationDirectory);
            //Logger.LogInfo($"Background music added. Final video with music located at: {outputWithMusic}");

            Logger.LogInfo("Video compilation completed.");

            spinner.Stop();
        }
    }


    //static async Task TestPathConstruction(string generationDirectory)
    //{
    //    Console.WriteLine($"Generation directory: {generationDirectory}");

    //    string testImagePath = Path.Combine(generationDirectory, "test_image.png");
    //    string testVoiceoverPath = Path.Combine(generationDirectory, "test_voiceover.mp3");

    //    Console.WriteLine($"Test image path: {testImagePath}");
    //    Console.WriteLine($"Test voiceover path: {testVoiceoverPath}");

    //    // Create fake files for testing
    //    File.Create(testImagePath).Dispose();
    //    File.Create(testVoiceoverPath).Dispose();

    //    // Instantiate and test VideoCompiler
    //    VideoCompiler videoCompiler = new VideoCompiler(generationDirectory);
    //    await videoCompiler.CompileIndividualSegments(new List<string> { testImagePath }, new List<string> { testVoiceoverPath });

    //    // Simulate creating fake segment files
    //    string fakeSegmentPath = Path.Combine(generationDirectory, "segment_0.mp4");
    //    File.Create(fakeSegmentPath).Dispose();
    //    Console.WriteLine($"Fake segment file created at: {fakeSegmentPath}");

    //    // Call ConcatenateSegments for testing
    //    await videoCompiler.ConcatenateSegments();

    //    // Clean up: delete fake files
    //    File.Delete(testImagePath);
    //    File.Delete(testVoiceoverPath);
    //    File.Delete(fakeSegmentPath);
    //}



=======
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

>>>>>>> 11e65ae839e18e5bf191660fc19839b2140e4a37
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
