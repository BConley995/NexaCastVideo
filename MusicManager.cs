using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using NexaCastVideo;

// MusicManager is responsible for managing music-related operations.
public class MusicManager
{
    private readonly DriveService _driveService;
    private readonly string _folderId = "1rOX3z7rSz5YZN0aEhzS6wMC98P5RrJd3";

    // Allows for injecting an external DriveService instance.
    public MusicManager()
    {
        try
        {
            string relativePath = @"..\..\..\SECURE\nexacastvideo-838db759dbe4.json";
            GoogleCredential credential = GoogleCredential.FromFile(relativePath)
                                                          .CreateScoped(DriveService.Scope.Drive);

            _driveService = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "nexacastvideo",
            });
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to initialize DriveService: {ex.Message}");
            throw;
        }
    }

    public MusicManager(DriveService driveService)
    {
        _driveService = driveService ?? throw new ArgumentNullException(nameof(driveService));
    }

    // Methods for handling music files can be added without altering existing code.
    public async Task<string> GetRandomMusicFileUrlAsync()
    {
        try
        {
            var request = _driveService.Files.List();
            request.Q = $"'{_folderId}' in parents";
            var fileList = await request.ExecuteAsync();

            if (fileList.Files != null && fileList.Files.Count > 0)
            {
                var random = new Random();
                var randomFile = fileList.Files[random.Next(fileList.Files.Count)];
                string shareableLink = $"https://drive.google.com/file/d/{randomFile.Id}/view";
                return shareableLink;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to fetch random music file URL: {ex.Message}");
        }
        return string.Empty;
    }

    // Each method does one thing - downloading, calculating duration, or adding music.
    public async Task<string> AddBackgroundMusicWithFade(string videoPath, List<string> musicPaths, string generationDirectory)
    {
        Logger.LogInfo("Entering AddBackgroundMusicWithFade method.");

        if (musicPaths == null || musicPaths.Count == 0)
        {
            var errorMessage = "No music files provided.";
            Logger.LogError(errorMessage);
            throw new ArgumentException(errorMessage);
        }

        try
        {
            Logger.LogInfo($"Video path: {videoPath}");
            double videoDuration = await GetVideoDurationAsync(videoPath);
            Logger.LogInfo($"Video duration: {videoDuration} seconds.");

            var random = new Random();
            var selectedMusicPath = musicPaths[random.Next(musicPaths.Count)];
            Logger.LogInfo($"Selected music path: {selectedMusicPath}");

            double musicDuration = await GetAudioDurationAsync(selectedMusicPath);
            Logger.LogInfo($"Music duration: {musicDuration} seconds.");

            var outputVideoPath = Path.Combine(generationDirectory, "Generated Video", "output_with_music.mp4");
            Logger.LogInfo($"Output video path: {outputVideoPath}");

            int loopCount = (int)Math.Ceiling(videoDuration / musicDuration);
            Logger.LogInfo($"Music loop count: {loopCount}");

            var ffmpegProcess = new Process();
            ffmpegProcess.StartInfo.FileName = "ffmpeg";
            ffmpegProcess.StartInfo.Arguments = $"-stream_loop {loopCount} -i \"{selectedMusicPath}\" -i \"{videoPath}\" -filter_complex \"[0:a]volume=0.14,aloop=loop=-1:size=2e+09[a1];[1:a][a1]amix=inputs=2:duration=first,afade=t=out:st={videoDuration - 0.5}:d=0.5[aout]\" -map 1:v -map \"[aout]\" -c:v copy -shortest \"{outputVideoPath}\"";
            ffmpegProcess.StartInfo.UseShellExecute = false;
            ffmpegProcess.StartInfo.RedirectStandardOutput = true;

            Logger.LogInfo("Starting FFmpeg process for adding background music.");
            ffmpegProcess.Start();
            await ffmpegProcess.WaitForExitAsync();
            Logger.LogInfo("FFmpeg process completed.");

            if (ffmpegProcess.ExitCode != 0)
            {
                Logger.LogError($"FFmpeg process exited with non-zero exit code: {ffmpegProcess.ExitCode}");
            }

            return outputVideoPath;
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to add background music: {ex.Message}");
            throw;
        }
        finally
        {
            Logger.LogInfo("Exiting AddBackgroundMusicWithFade method.");
        }
    }


    private async Task<double> GetAudioDurationAsync(string audioPath)
    {
        try
        {
            // Ensuring the path is correctly enclosed in quotes to handle spaces and special characters
            string formattedAudioPath = $"\"{audioPath}\"";

            // Constructing the ffprobe command
            string ffprobeArguments = $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 {formattedAudioPath}";
            Logger.LogInfo($"Executing ffprobe with arguments: {ffprobeArguments}");

            var ffprobeProcess = new Process();
            ffprobeProcess.StartInfo.FileName = "ffprobe";
            ffprobeProcess.StartInfo.Arguments = ffprobeArguments;
            ffprobeProcess.StartInfo.UseShellExecute = false;
            ffprobeProcess.StartInfo.RedirectStandardOutput = true;
            ffprobeProcess.Start();

            string output = await ffprobeProcess.StandardOutput.ReadToEndAsync();
            await ffprobeProcess.WaitForExitAsync();

            Logger.LogInfo($"ffprobe output: {output}");

            if (double.TryParse(output, NumberStyles.Number, CultureInfo.InvariantCulture, out double duration))
            {
                Logger.LogInfo($"Parsed audio duration: {duration} seconds.");
                return duration;
            }
            else
            {
                var errorMessage = "Failed to obtain or parse audio duration.";
                Logger.LogError(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error while obtaining audio duration: {ex.Message}");
            throw;
        }
    }


    public async Task<string> DownloadRandomMusicFileAsync(string saveDirectory)
    {
        try
        {
            // List files from Google Drive
            var request = _driveService.Files.List();
            request.Q = $"'{_folderId}' in parents";
            var fileList = await request.ExecuteAsync();

            if (fileList.Files != null && fileList.Files.Count > 0)
            {
                // Pick a random file
                var random = new Random();
                var randomFile = fileList.Files[random.Next(fileList.Files.Count)];

                // Download the file
                var stream = new MemoryStream();
                var getRequest = _driveService.Files.Get(randomFile.Id);
                await getRequest.DownloadAsync(stream);

                // Save the file locally in the specified directory
                string localPath = Path.Combine(saveDirectory, randomFile.Name);
                using (var fileStream = new FileStream(localPath, FileMode.Create, FileAccess.Write))
                {
                    stream.WriteTo(fileStream);
                }

                return localPath;
            }
            else
            {
                Logger.LogError("No files found in the specified folder.");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to download random music file: {ex.Message}");
        }

        return string.Empty;
    }

    private async Task<double> GetVideoDurationAsync(string videoPath)
    {
        try
        {
            var ffprobeProcess = new Process();
            ffprobeProcess.StartInfo.FileName = "ffprobe";
            ffprobeProcess.StartInfo.Arguments = $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"{videoPath}\""; // Note the quotes around {videoPath}
            ffprobeProcess.StartInfo.UseShellExecute = false;
            ffprobeProcess.StartInfo.RedirectStandardOutput = true;
            ffprobeProcess.Start();

            string output = await ffprobeProcess.StandardOutput.ReadToEndAsync();
            await ffprobeProcess.WaitForExitAsync();

            if (double.TryParse(output, NumberStyles.Number, CultureInfo.InvariantCulture, out double duration))
            {
                return duration;
            }
            else
            {
                var errorMessage = "Failed to obtain video duration.";
                Logger.LogError(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error while obtaining video duration: {ex.Message}");
            throw;
        }
    }
}


