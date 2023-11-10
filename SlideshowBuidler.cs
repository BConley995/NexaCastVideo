using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using NexaCastVideo;

public class SlideshowBuilder
{
    private readonly string _generationDirectory;

    public SlideshowBuilder(string generationDirectory)
    {
        _generationDirectory = generationDirectory ?? throw new ArgumentNullException(nameof(generationDirectory));
        Directory.CreateDirectory(generationDirectory); // Ensure the directory exists.
    }

    public async Task<bool> CreateSlideshow(List<string> imagePaths, List<string> voiceoverPaths, string musicPath, string outputPath)
    {
        try
        {
            if (imagePaths.Count != voiceoverPaths.Count)
                throw new ArgumentException("The number of images and voiceovers must match.");

            var fileList = new List<string>();
            var filterComplex = new List<string>();

            for (int i = 0; i < imagePaths.Count; i++)
            {
                var durationInSeconds = GetAudioDuration(voiceoverPaths[i]);
                var tempOutput = Path.Combine(_generationDirectory, $"temp_video_{i}.mp4");

                await ExecuteFFmpegCommand($"-loop 1 -i \"{imagePaths[i]}\" -i \"{voiceoverPaths[i]}\" -c:v libx264 -t {durationInSeconds} -pix_fmt yuv420p -vf scale=1280:720 -c:a aac -strict experimental -shortest \"{tempOutput}\"");

                fileList.Add($"file '{tempOutput}'");
                filterComplex.Add($"[{i}:v:0] [{i}:a:0]");
            }

            var concatListFile = Path.Combine(_generationDirectory, "concat_list.txt");
            await File.WriteAllLinesAsync(concatListFile, fileList);

            await ExecuteFFmpegCommand($"-f concat -safe 0 -i \"{concatListFile}\" -c copy \"{outputPath}\"");

            // Cleanup temporary files
            foreach (var file in fileList)
            {
                File.Delete(file.Replace("file '", "").Replace("'", ""));
            }
            File.Delete(concatListFile);

            // Add music to the final video if a music path is provided
            if (!string.IsNullOrWhiteSpace(musicPath))
            {
                var finalOutputWithMusic = Path.Combine(_generationDirectory, "final_output_with_music.mp4");
                await ExecuteFFmpegCommand($"-i \"{outputPath}\" -i \"{musicPath}\" -filter_complex \"amix=inputs=2:duration=first:dropout_transition=2\" -c:v copy -c:a aac -strict experimental -shortest \"{finalOutputWithMusic}\"");

                // Replace original video with the one with music
                File.Delete(outputPath);
                File.Move(finalOutputWithMusic, outputPath);
            }

            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error creating slideshow: {ex.Message}");
            return false;
        }
    }

    private double GetAudioDuration(string audioFilePath)
    {
        using (var reader = new NAudio.Wave.AudioFileReader(audioFilePath))
        {
            return reader.TotalTime.TotalSeconds;
        }
    }

    private async Task ExecuteFFmpegCommand(string arguments)
    {
        using (var process = new Process())
        {
            process.StartInfo.FileName = "ffmpeg";
            process.StartInfo.Arguments = arguments;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;

            process.Start();

            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();
            if (process.ExitCode != 0)
            {
                Logger.LogError($"FFmpeg failed with error: {error}");
                throw new Exception($"FFmpeg failed with error: {error}");
            }
        }
    }
}
