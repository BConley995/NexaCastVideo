using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Globalization;

public class VideoCompiler
{
    private readonly string _ffmpegPath;
    private readonly string _generationDirectory;

    public VideoCompiler(string ffmpegPath, string generationDirectory)
    {
        _ffmpegPath = ffmpegPath ?? throw new ArgumentNullException(nameof(ffmpegPath));
        _generationDirectory = generationDirectory ?? throw new ArgumentNullException(nameof(generationDirectory));

        // Ensure the generation directory exists, if not, create it.
        if (!Directory.Exists(_generationDirectory))
        {
            Directory.CreateDirectory(_generationDirectory);
        }
    }

    public async Task<bool> CompileVideoFromSegments(
    string imageDirectory,
    string voiceoverDirectory,
    string musicPath,
    string subtitlePath,
    string outputPath)
    {
        // This FFmpeg command is indicative and may need to be adjusted
        string command = $@"-framerate 1 -i {imageDirectory}\image_%03d.jpg -i {voiceoverDirectory}\voiceover_%03d.mp3 -i {musicPath} -vf ""subtitles={subtitlePath}, fade=out:st=[START_TIME]:d=[DURATION]"" -shortest {outputPath}";

        var processStartInfo = new ProcessStartInfo
        {
            FileName = _ffmpegPath,
            Arguments = command,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using (var process = new Process { StartInfo = processStartInfo })
        {
            process.Start();

            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();

            process.WaitForExit();

            // Log output and error if they are not empty.
            if (!string.IsNullOrEmpty(output) || !string.IsNullOrEmpty(error))
            {
                string logFileName = $"FFmpegLog_{DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture)}.txt";
                string logFilePath = Path.Combine(_generationDirectory, logFileName);

                string logContent = $"{DateTime.Now} - Output: {output}, Error: {error}\n";
                await File.AppendAllTextAsync(logFilePath, logContent);
            }

            // Check if an error occurred during the process.
            if (!string.IsNullOrEmpty(error))
            {
                Console.WriteLine($"FFmpeg Error: {error}");
                return false; // Return false to indicate the video compilation was not successful.
            }

            // Copy the generated video file to the generation directory if it was successful.
            if (File.Exists(outputPath))
            {
                string destinationPath = Path.Combine(_generationDirectory, Path.GetFileName(outputPath));
                File.Copy(outputPath, destinationPath, overwrite: true);
            }

            return true; // Return true to indicate the video compilation was successful.
        }
    }


    public async Task<bool> CompileVideo(
        string voiceoverDirectory,
        string imageDirectory,
        string musicPath,
        string subtitlePath,
        string outputPath)
    {
        string command = $@"-framerate 1 -i {imageDirectory}\image_%03d.jpg -i {voiceoverDirectory}\voiceover_%03d.mp3 -i {musicPath} -vf subtitles={subtitlePath} -shortest {outputPath}";

        var processStartInfo = new ProcessStartInfo
        {
            FileName = _ffmpegPath,
            Arguments = command,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using (var process = new Process { StartInfo = processStartInfo })
        {
            process.Start();

            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();

            process.WaitForExit();

            // Log output and error if they are not empty.
            if (!string.IsNullOrEmpty(output) || !string.IsNullOrEmpty(error))
            {
                string logFileName = $"FFmpegLog_{DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture)}.txt";
                string logFilePath = Path.Combine(_generationDirectory, logFileName);

                string logContent = $"{DateTime.Now} - Output: {output}, Error: {error}\n";
                await File.AppendAllTextAsync(logFilePath, logContent);
            }

            // Copy the generated video file to the generation directory, if the compilation was successful.
            if (string.IsNullOrEmpty(error) && File.Exists(outputPath))
            {
                string destinationPath = Path.Combine(_generationDirectory, Path.GetFileName(outputPath));
                File.Copy(outputPath, destinationPath, overwrite: true);
            }
            else if (!string.IsNullOrEmpty(error))
            {
                Console.WriteLine($"FFmpeg Error: {error}");
                return false;
            }

            return true;
        }
    }
}
