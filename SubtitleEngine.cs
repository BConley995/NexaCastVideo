using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NexaCastVideo;

public class SubtitleEngine
{
    private string _generationDirectory;

    public SubtitleEngine(string generationDirectory)
    {
        _generationDirectory = generationDirectory;
        Directory.CreateDirectory(generationDirectory); // Ensure the directory exists.
    }

    public string GenerateSRT(List<string> lines, List<double> durations)
    {
        try
        {
            StringBuilder srtBuilder = new StringBuilder();
            double currentTime = 0.0;

            for (int i = 0; i < lines.Count; i++)
            {
                srtBuilder.AppendLine((i + 1).ToString());
                srtBuilder.AppendLine($"{FormatTime(currentTime)} --> {FormatTime(currentTime + durations[i])}");
                srtBuilder.AppendLine(lines[i]);
                srtBuilder.AppendLine();

                currentTime += durations[i];
            }

            return srtBuilder.ToString();
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error generating SRT: {ex.Message}");
            return string.Empty;
        }
    }

    public void SaveSRTToFile(string srtContent, string outputPath)
    {
        try
        {
            File.WriteAllText(outputPath, srtContent);
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error saving SRT file to '{outputPath}': {ex.Message}");
        }
    }

    public void SaveSRTToGenerationDirectory(string srtContent, string fileName)
    {
        try
        {
            string filePath = Path.Combine(_generationDirectory, fileName);
            File.WriteAllText(filePath, srtContent);
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error saving SRT file to generation directory: {ex.Message}");
        }
    }

    private string FormatTime(double totalSeconds)
    {
        try
        {
            TimeSpan time = TimeSpan.FromSeconds(totalSeconds);
            return $"{time.Hours:D2}:{time.Minutes:D2}:{time.Seconds:D2},{time.Milliseconds:D3}";
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error formatting time: {ex.Message}");
            return "00:00:00,000";  
        }
    }
}

