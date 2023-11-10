using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

public class VideoCompiler
{
    private readonly string _ffmpegPath;
    private readonly string _generationDirectory;

    public VideoCompiler(string ffmpegPath, string generationDirectory)
    {
        _ffmpegPath = ffmpegPath ?? throw new ArgumentNullException(nameof(ffmpegPath));
        _generationDirectory = generationDirectory ?? throw new ArgumentNullException(nameof(generationDirectory));

        if (!Directory.Exists(_generationDirectory))
            Directory.CreateDirectory(_generationDirectory);
    }

    public async Task CompileIndividualSegments(string imageDirectory, string voiceoverDirectory)
    {
        var imageFiles = Directory.GetFiles(imageDirectory, "image_*.jpg");
        var voiceoverFiles = Directory.GetFiles(voiceoverDirectory, "voiceover_*.mp3");

        for (int i = 0; i < imageFiles.Length && i < voiceoverFiles.Length; i++)
        {
            string imageFile = imageFiles[i];
            string voiceoverFile = voiceoverFiles[i];
            string outputVideoFile = Path.Combine(_generationDirectory, $"video_{i + 1}.mp4");

            string command = $"-loop 1 -framerate 1 -i \"{imageFile}\" -i \"{voiceoverFile}\" -c:v libx264 -tune stillimage -c:a aac -strict experimental -b:a 192k -pix_fmt yuv420p -shortest -y \"{outputVideoFile}\"";

            await ExecuteFFmpegCommand(command);
        }
    }

    private async Task ExecuteFFmpegCommand(string arguments)
    {
        using (var process = new Process())
        {
            process.StartInfo.FileName = _ffmpegPath;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;

            process.Start();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                string error = await process.StandardError.ReadToEndAsync();
                Console.WriteLine($"FFmpeg Error: {error}");
                throw new ApplicationException($"FFmpeg failed with error: {error}");
            }
        }
    }
}
