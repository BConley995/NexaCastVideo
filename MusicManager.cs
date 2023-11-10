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

public class MusicManager
{
    private readonly DriveService _driveService;
    private readonly string _folderId = "1rOX3z7rSz5YZN0aEhzS6wMC98P5RrJd3";

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

    public async Task<string> AddBackgroundMusicWithFade(string videoPath, List<string> musicPaths)
    {
        if (musicPaths == null || musicPaths.Count == 0)
        {
            var errorMessage = "No music files provided.";
            Logger.LogError(errorMessage);
            throw new ArgumentException(errorMessage);
        }

        try
        {
            double videoDuration = await GetVideoDurationAsync(videoPath);

            var random = new Random();
            var selectedMusicPath = musicPaths[random.Next(musicPaths.Count)];
            var outputVideoPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "output_video_with_music.mp4");

            var ffmpegProcess = new Process();
            ffmpegProcess.StartInfo.FileName = "ffmpeg";
            ffmpegProcess.StartInfo.Arguments = $"-i {videoPath} -i {selectedMusicPath} -filter_complex [0:a][1:a]amix=inputs=2:duration=first[aout];[aout]afade=t=out:st={videoDuration - 3}:d=3[vout] -map [vout] -map 0:v -c:v copy {outputVideoPath}";
            ffmpegProcess.StartInfo.UseShellExecute = false;
            ffmpegProcess.StartInfo.RedirectStandardOutput = true;
            ffmpegProcess.Start();
            await ffmpegProcess.WaitForExitAsync();

            return outputVideoPath;
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to add background music: {ex.Message}");
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
            ffprobeProcess.StartInfo.Arguments = $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 {videoPath}";
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


