using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using NexaCastVideo;
using System.Globalization;

public class SlideshowBuilder
{
    private string _outputPath;
    private string _generationDirectory;

    public SlideshowBuilder(string outputPath, string generationDirectory)
    {
        _outputPath = outputPath;
        _generationDirectory = generationDirectory;
        Directory.CreateDirectory(generationDirectory); // Ensure the directory exists.
    }

    public string GetOutputPath()
    {
        return _outputPath;
    }

    public class MediaFile
    {
        public string Filename { get; set; }

        public MediaFile() { }

        public MediaFile(string filename)
        {
            this.Filename = filename;
        }
    }

    public async Task<bool> CreateSlideshow(List<string> imagePaths, List<string> voiceoverPaths, string musicPath)
    {
        try
        {
            if (imagePaths.Count != voiceoverPaths.Count)
                throw new ArgumentException("The number of images and voiceovers must match.");

            var tempVideoFiles = new List<string>();

            for (int i = 0; i < imagePaths.Count; i++)
            {
                var durationInSeconds = await GetAudioDurationAsync(voiceoverPaths[i]);
                var tempImageVideo = Path.Combine(_generationDirectory, Path.GetRandomFileName() + ".mp4");
                var tempFinalVideo = Path.Combine(_generationDirectory, Path.GetRandomFileName() + ".mp4");

                var inputImage = new MediaFile { Filename = imagePaths[i] };
                var outputImageVideo = new MediaFile { Filename = tempImageVideo };

                await ExecuteFFmpegCommandAsync($"-i {inputImage.Filename} -t {durationInSeconds} {outputImageVideo.Filename}");

                var inputVoiceover = new MediaFile { Filename = voiceoverPaths[i] };
                var outputFinalVideo = new MediaFile { Filename = tempFinalVideo };

                await ExecuteFFmpegCommandAsync($"-i {tempImageVideo} -i {voiceoverPaths[i]} -c:v copy -c:a aac -strict experimental {tempFinalVideo}");

                tempVideoFiles.Add(tempFinalVideo);
            }

            var concatArg = string.Join("|", tempVideoFiles);
            var finalOutput = new MediaFile { Filename = _outputPath };
            await ExecuteFFmpegCommandAsync($"-i \"concat:{concatArg}\" -c copy {_outputPath}");

            foreach (var temp in tempVideoFiles)
            {
                File.Delete(temp);
            }

            // Overlay the music on top of the slideshow video and add fade out
            double videoDuration = await GetVideoDurationAsync(_outputPath); // assuming method is present
            string finalOutputWithMusic = Path.Combine(_generationDirectory, "final_output_with_music.mp4");

            // Construct FFmpeg command with music overlay and fade out
            await ExecuteFFmpegCommandAsync($"-i {_outputPath} -i {musicPath} -filter_complex [1:a]afade=t=out:st={videoDuration - 5}:d=5[aout] -map 0:v -map [aout] -c:v copy -c:a aac -shortest {finalOutputWithMusic}");

            // Optional: Replace original video with the one with music, or use a different name
            File.Delete(_outputPath);
            File.Move(finalOutputWithMusic, _outputPath);

            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error creating slideshow: {ex.Message}");
            return false;
        }
    }

    private async Task<double> GetVideoDurationAsync(string videoPath)
    {
        try
        {
            // Start FFprobe process to get video duration
            using (var process = new Process())
            {
                process.StartInfo.FileName = "ffprobe";
                process.StartInfo.Arguments = $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 {videoPath}";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;

                process.Start();

                // Read process standard output to get video duration
                string output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (double.TryParse(output, NumberStyles.Float, CultureInfo.InvariantCulture, out double duration))
                {
                    return duration;
                }
                else
                {
                    Logger.LogError("Failed to parse video duration from FFprobe output.");
                    throw new InvalidOperationException("Failed to parse video duration from FFprobe output.");
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error while obtaining video duration: {ex.Message}");
            throw;
        }
    }

    private async Task<double> GetAudioDurationAsync(string audioFilePath)
    {
        using (var reader = new AudioFileReader(audioFilePath))
        {
            return reader.TotalTime.TotalSeconds;
        }
    }

    private async Task ExecuteFFmpegCommandAsync(string arguments)
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

            await process.WaitForExitAsync();
            if (process.ExitCode != 0)
            {
                string errorOutput = await process.StandardError.ReadToEndAsync();
                Logger.LogError($"FFmpeg failed with error: {errorOutput}");
                throw new Exception($"FFmpeg failed with error: {errorOutput}");
            }
        }
    }
}

