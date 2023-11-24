using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

public class VideoCompiler
{
    private string projectFolder;
    private string outputDirectory;
<<<<<<< HEAD
    private string generationDirectory;

    public VideoCompiler(string generationDirectory)
    {
        this.generationDirectory = generationDirectory;
        outputDirectory = Path.Combine(this.generationDirectory, "Generated Video");
=======

    public VideoCompiler(string generationDirectory)
    {
        projectFolder = generationDirectory;
        outputDirectory = Path.Combine(projectFolder, "Generated Video");
>>>>>>> 11e65ae839e18e5bf191660fc19839b2140e4a37

        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }
    }

    private List<string> GetVoiceoverFiles(string folderPath, string excludedFile)
    {
        return new DirectoryInfo(folderPath)
            .GetFiles("*.mp3")
            .Where(f => f.FullName != excludedFile)
            .Select(f => f.FullName)
            .ToList();
    }

    public async Task CompileIndividualSegments(List<string> imageFiles, List<string> voiceoverFiles)
    {
<<<<<<< HEAD
        int imageCount = imageFiles.Count;
        int voiceoverCount = voiceoverFiles.Count;
        int segmentCount = Math.Min(imageCount, voiceoverCount);

        for (int i = 0; i < segmentCount; i++)
        {
            string imageFile = imageFiles[i];
            string voiceoverFile = voiceoverFiles[i];  // Already a full path

            Console.WriteLine($"Creating segment {i}:");
            Console.WriteLine($" - Image File: {imageFile}");
            Console.WriteLine($" - Voiceover File: {voiceoverFile}");

            CreateVideoSegment(imageFile, voiceoverFile, $"segment_{i}.mp4");
        }

        if (voiceoverCount > imageCount)
        {
            Console.WriteLine("More voiceover files than image files. Reusing images for extra voiceovers.");
            // Additional logic to handle extra voiceovers, if needed
        }
        else if (imageCount > voiceoverCount)
        {
            Console.WriteLine("More image files than voiceover files. Some images will not be used.");
            // Additional logic if needed
        }

    }


    private string ConstructFFmpegCommand(string imageFile, string voiceoverFile, string outputVideoFile)
    {
        return $"-i \"{imageFile}\" -i \"{voiceoverFile}\" -c:v libx264 -c:a aac \"{outputVideoFile}\"";
=======
        if (imageFiles.Count != voiceoverFiles.Count)
        {
            throw new InvalidOperationException("The number of image files must match the number of voiceover files.");
        }

        for (int i = 0; i < imageFiles.Count; i++)
        {
            string imageFile = imageFiles[i];
            string voiceoverFile = voiceoverFiles[i];
            string outputVideoFile = Path.Combine(outputDirectory, $"segment_{i}.mp4");

            string ffmpegCommand = ConstructFFmpegCommand(imageFile, voiceoverFile, outputVideoFile);
            await ExecuteFFmpegCommand(ffmpegCommand);
        }
    }

    private string ConstructFFmpegCommand(string imageFile, string voiceoverFile, string outputVideoFile)
    {
        return $"-i {imageFile} -i {voiceoverFile} -c:v libx264 -c:a aac {outputVideoFile}";
>>>>>>> 11e65ae839e18e5bf191660fc19839b2140e4a37
    }

    private void CreateVideoSegment(string imageFile, string voiceoverFile, string outputSegmentFileName)
    {
        string outputFilePath = Path.Combine(outputDirectory, outputSegmentFileName);

<<<<<<< HEAD
        TimeSpan duration = GetAudioDuration(voiceoverFile);
        string ffmpegCommand = $"-loop 1 -i \"{imageFile}\" -i \"{voiceoverFile}\" -t {duration.TotalSeconds} -c:v libx264 -pix_fmt yuv420p -vf scale=1280:720 -c:a aac -b:a 192k \"{outputFilePath}\"";

        ExecuteFFmpegCommand(ffmpegCommand).Wait();
=======
        // Get the duration of the MP3 file
        TimeSpan duration = GetAudioDuration(voiceoverFile);

        string ffmpegCommand = $"-loop 1 -i \"{imageFile}\" -i \"{voiceoverFile}\" -t {duration.TotalSeconds} -c:v libx264 -pix_fmt yuv420p -vf scale=1280:720 -c:a aac -b:a 192k \"{outputFilePath}\"";
        Console.WriteLine($"Executing FFmpeg command: {ffmpegCommand}");

        using (var process = new Process())
        {
            process.StartInfo.FileName = "ffmpeg";
            process.StartInfo.Arguments = ffmpegCommand;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;

            process.Start();

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Read output and error in real-time
            process.OutputDataReceived += (sender, args) => Console.WriteLine(args.Data);
            process.ErrorDataReceived += (sender, args) => Console.WriteLine(args.Data);

            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"FFmpeg command failed with exit code {process.ExitCode}");
            }
        }
>>>>>>> 11e65ae839e18e5bf191660fc19839b2140e4a37

        if (!File.Exists(outputFilePath) || new FileInfo(outputFilePath).Length < 1024)
        {
            throw new InvalidOperationException($"Output file {outputSegmentFileName} did not generate correctly.");
        }

        Console.WriteLine($"Completed segment: {outputSegmentFileName}");
    }

<<<<<<< HEAD

private TimeSpan GetAudioDuration(string filePath)
{
    Console.WriteLine($"Getting audio duration for file: {filePath}");

    if (new FileInfo(filePath).Length == 0)
    {
        Console.WriteLine("Skipping duration calculation for empty file.");
        return TimeSpan.Zero; // Returning zero duration for empty files
    }

    try
=======
    private TimeSpan GetAudioDuration(string filePath)
>>>>>>> 11e65ae839e18e5bf191660fc19839b2140e4a37
    {
        using (var reader = new NAudio.Wave.Mp3FileReader(filePath))
        {
            return reader.TotalTime;
        }
    }
<<<<<<< HEAD
    catch (Exception ex)
    {
        Console.WriteLine($"Error reading MP3 file: {ex.Message}");
        return TimeSpan.Zero; // Handling other exceptions
    }
}


    public async Task ConcatenateSegments()
    {
        Console.WriteLine("Starting concatenation of video segments...");

        // Find all segment files
        var segmentFiles = Directory.GetFiles(outputDirectory, "segment_*.mp4")
                                    .Select(Path.GetFileName) // Get only the file names
                                    .ToList();

        // Creating a file list for FFmpeg
        string fileListPath = Path.Combine(outputDirectory, "filelist.txt");
        Console.WriteLine("Creating a file list for FFmpeg to read...");

        // Write relative paths to filelist.txt
        File.WriteAllLines(fileListPath, segmentFiles.Select(s => $"file '{s}'"));

=======

    public async Task ConcatenateSegments(List<string> videoSegmentFileNames)
    {
        Console.WriteLine("Starting concatenation of video segments...");

        // Creating a file list for FFmpeg
        string fileListPath = Path.Combine(outputDirectory, "filelist.txt");
        Console.WriteLine("Creating a file list for FFmpeg to read...");
        File.WriteAllLines(fileListPath, videoSegmentFileNames.Select(s => $"file '{Path.Combine(outputDirectory, s)}'"));
>>>>>>> 11e65ae839e18e5bf191660fc19839b2140e4a37
        Console.WriteLine("File list created.");

        // Defining the output file path
        string outputVideo = Path.Combine(outputDirectory, "final_video.mp4");
        Console.WriteLine($"Output video will be saved as: {outputVideo}");

        // Constructing the FFmpeg command for concatenation
        string concatCommand = $"-f concat -safe 0 -i \"{fileListPath}\" -c copy \"{outputVideo}\"";
        Console.WriteLine("FFmpeg command for concatenation constructed. Executing the command...");

        // Executing the FFmpeg command
        await ExecuteFFmpegCommand(concatCommand);

        // Verifying if the final video file is created
        if (File.Exists(outputVideo))
        {
            Console.WriteLine("Concatenation of video segments completed successfully.");
        }
        else
        {
            Console.WriteLine("Error: Concatenation failed. Final video file not found.");
        }
    }

<<<<<<< HEAD
=======
    public async Task OverlayBackgroundMusicAndSubtitles(string finalVideoFileName, string musicFile)
    {
        string finalVideoFile = Path.Combine(outputDirectory, finalVideoFileName);
        string outputFile = Path.Combine(outputDirectory, "output_with_music.mp4");

        string overlayCommand = $"-i \"{finalVideoFile}\" -i \"{musicFile}\" -filter_complex \"[1:a]volume=0.08[a1]; [0:a][a1]amerge=inputs=2[aout]\" -map 0:v -map \"[aout]\" -c:v copy -c:a aac -shortest \"{outputFile}\"";
        await ExecuteFFmpegCommand(overlayCommand);
    }

>>>>>>> 11e65ae839e18e5bf191660fc19839b2140e4a37
    private string FindLargestMp3File(string folderPath)
    {
        return new DirectoryInfo(folderPath)
            .GetFiles("*.mp3")
            .OrderByDescending(f => f.Length)
            .First().FullName;
    }

    private List<string> ReadScriptSegments(string scriptPath)
    {
        var segments = new List<string>();
        var currentSegment = new List<string>();
        bool isSegmentStarted = false;

        foreach (var line in File.ReadAllLines(scriptPath))
        {
            if (line.StartsWith("Narrator:"))
            {
                if (isSegmentStarted)
                {
                    segments.Add(string.Join(Environment.NewLine, currentSegment));
                    currentSegment.Clear();
                }
                isSegmentStarted = true;
            }

            if (isSegmentStarted)
            {
                currentSegment.Add(line);
            }
        }

        if (currentSegment.Count > 0)
        {
            segments.Add(string.Join(Environment.NewLine, currentSegment));
        }

        return segments;
    }

    private List<string> ReadImageFiles(string folderPath)
    {
        var imageFiles = new List<string>();

        Console.WriteLine("Reading files from: " + folderPath);
        var files = Directory.GetFiles(folderPath, "*.png");

        foreach (var file in files)
        {
            imageFiles.Add(file);
        }

        return imageFiles;
    }

<<<<<<< HEAD
    private List<string> ReadVoiceoverFiles(string folderPath)
    {
        var voiceoverFiles = new List<string>();

        Console.WriteLine("Reading files from: " + folderPath);
        var files = Directory.GetFiles(folderPath, "*.mp3");

        foreach (var file in files)
        {
            voiceoverFiles.Add(file);
        }

        return voiceoverFiles;
    }

    private async Task GenerateDurationsFile(List<string> mp3Files, List<string> mp4Files, string fileName)
    {
        string filePath = Path.Combine(generationDirectory, fileName);
=======
    private async Task GenerateDurationsFile(List<string> mp3Files, List<string> mp4Files, string fileName)
    {
        string filePath = Path.Combine(outputDirectory, fileName);
>>>>>>> 11e65ae839e18e5bf191660fc19839b2140e4a37
        using (StreamWriter file = new StreamWriter(filePath))
        {
            foreach (var mp3File in mp3Files)
            {
                TimeSpan mp3Duration = GetAudioDuration(mp3File);
                file.WriteLine($"MP3 File: {mp3File}, Duration: {mp3Duration}");
            }

            foreach (var mp4File in mp4Files)
            {
<<<<<<< HEAD
                string fullMp4Path = Path.Combine(generationDirectory, mp4File);
=======
                string fullMp4Path = Path.Combine(outputDirectory, mp4File);
>>>>>>> 11e65ae839e18e5bf191660fc19839b2140e4a37
                TimeSpan mp4Duration = await GetVideoDuration(fullMp4Path);
                file.WriteLine($"MP4 File: {fullMp4Path}, Duration: {mp4Duration}");
            }
        }
    }

    private async Task<TimeSpan> GetVideoDuration(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"File not found: {filePath}");
                return new TimeSpan();
            }

            var mediaInfo = await Xabe.FFmpeg.FFmpeg.GetMediaInfo(filePath);
            return mediaInfo.Duration;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting video duration for file {filePath}: {ex.Message}");
            return new TimeSpan();
        }
    }


    private async Task ExecuteFFmpegCommand(string arguments)
    {
        using (var process = new Process())
        {
            process.StartInfo.FileName = "ffmpeg";
            process.StartInfo.Arguments = arguments;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
<<<<<<< HEAD
=======
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
>>>>>>> 11e65ae839e18e5bf191660fc19839b2140e4a37

            Console.WriteLine($"Starting FFmpeg with arguments: {arguments}");
            process.Start();

<<<<<<< HEAD
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Read output and error in real-time
            process.OutputDataReceived += (sender, args) => Console.WriteLine(args.Data);
            process.ErrorDataReceived += (sender, args) => Console.WriteLine(args.Data);

            await Task.Run(() => process.WaitForExit());
=======
            // Reading output/error in separate tasks
            var readOutputTask = ReadStreamAsync(process.StandardOutput);
            var readErrorTask = ReadStreamAsync(process.StandardError);

            // Wait for the process to exit and for the output/error tasks to complete
            await Task.WhenAll(Task.Run(() => process.WaitForExit()), readOutputTask, readErrorTask);
>>>>>>> 11e65ae839e18e5bf191660fc19839b2140e4a37

            if (process.ExitCode != 0)
            {
                Console.WriteLine("FFmpeg command failed with exit code: " + process.ExitCode);
<<<<<<< HEAD
=======
                Console.WriteLine("FFmpeg Output: " + output);
                Console.WriteLine("FFmpeg Error: " + error);
>>>>>>> 11e65ae839e18e5bf191660fc19839b2140e4a37
                throw new InvalidOperationException("FFmpeg command failed.");
            }
            else
            {
                Console.WriteLine("FFmpeg process completed successfully.");
            }
        }
    }

<<<<<<< HEAD


=======
>>>>>>> 11e65ae839e18e5bf191660fc19839b2140e4a37
    private async Task ReadStreamAsync(StreamReader stream)
    {
        string line;
        while ((line = await stream.ReadLineAsync()) != null)
        {
            Console.WriteLine(line);
        }
    }
}
