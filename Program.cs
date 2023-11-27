using NexaCastVideo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Globalization;
using System.Text.RegularExpressions;

class Program
{
    //  Main handles the program flow and coordinates various functionalities.
    static async Task Main(string[] args)
    {

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

        // Setting up logging
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

        // Handle uncaught exceptions
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            Logger.LogError($"Unhandled exception: {(e.ExceptionObject as Exception)?.Message}");
        };

        // Capturing user input
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

            // Music management
            MusicManager musicManager = new MusicManager();
            Logger.LogInfo("Downloading random royalty-free music...");
            string musicFilePath = await musicManager.DownloadRandomMusicFileAsync(generationDirectory);

            if (string.IsNullOrEmpty(musicFilePath))
            {
                Logger.LogError("Failed to download a music file. Aborting process.");
                return;
            }

            Logger.LogInfo($"Music file downloaded successfully: {musicFilePath}");

            // Script generation and processing
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

            // Image fetching
            Logger.LogInfo("Fetching and downloading images...");
            ImageFetcher imageFetcher = new ImageFetcher(ConfigManager.GetAPIKey("DalleApiKey"), generationDirectory);
            List<string> imageFiles = await imageFetcher.FetchAndDownloadImages();

            if (imageFiles.Count == 0)
            {
                Logger.LogError("No images were downloaded.");
                return;
            }

            // Voiceover generation
            string ttsScript = ProcessTextForTts(script, true);
            ttsScript = RemoveNarrationLabels(ttsScript);

            Logger.LogInfo("Generating and downloading voiceovers...");
            VoiceoverGenerator voiceoverGenerator = new VoiceoverGenerator(generationDirectory, ConfigManager.GetAPIKey("ElevenLabsApiKey"));
            List<string> voiceoverFiles = await voiceoverGenerator.GenerateVoiceovers(scriptSegmentsList);

            // Video compilation
            VideoCompiler videoCompiler = new VideoCompiler(generationDirectory);
            await videoCompiler.CompileIndividualSegments(imageFiles, voiceoverFiles);

            Logger.LogInfo("Concatenating video segments...");
            await videoCompiler.ConcatenateSegments();

            string finalVideoPath = Path.Combine(generationDirectory, "Generated Video", "final_video.mp4");
            Logger.LogInfo($"Final video path: {finalVideoPath}");
            Logger.LogInfo($"Music file path: {string.Join(", ", new List<string> { musicFilePath })}");
            Logger.LogInfo("Concatenation of video segments completed. Proceeding to add background music.");
            string outputWithMusic = await musicManager.AddBackgroundMusicWithFade(finalVideoPath, new List<string> { musicFilePath }, generationDirectory);
            Logger.LogInfo($"Background music added. Final video with music located at: {outputWithMusic}");

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


    // Method to process text for Text-to-Speech conversion
    private static string ProcessTextForTts(string input, bool ignoreParenthesesContent)
    {
        if (ignoreParenthesesContent)
        {
            input = Regex.Replace(input, @"\([^)]*\)", "");
        }
        return input;
    }

    // Method to remove narration labels from the script
    private static string RemoveNarrationLabels(string input)
    {
        string[] patterns = { "Sub-point:", "Narrator:", "Point:", "Main Point:" };
        foreach (var pattern in patterns)
        {
            input = Regex.Replace(input, pattern, "", RegexOptions.IgnoreCase);
        }
        return input.Trim();
    }

    // Method to download files asynchronously
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
