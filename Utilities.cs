using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

public static class Utilities
{
    /// <summary>
    /// Downloads a file from a given URL and saves it to the specified path.
    /// </summary>
    public static async Task DownloadFileAsync(string url, string outputPath)
    {
        using (HttpClient client = new HttpClient())
        using (HttpResponseMessage response = await client.GetAsync(url))
        using (Stream stream = await response.Content.ReadAsStreamAsync())
        using (FileStream fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await stream.CopyToAsync(fileStream);
        }
    }

    /// <summary>
    /// Checks if a directory exists, and if not, creates it.
    /// </summary>
    public static void EnsureDirectoryExists(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
    }

    /// <summary>
    /// Generates a random filename with the specified extension.
    /// </summary>
    public static string GenerateRandomFilename(string extension)
    {
        return $"{Guid.NewGuid()}.{extension}";
    }

    /// <summary>
    /// Deletes a file safely, capturing any potential exceptions.
    /// </summary>
    public static void SafeDelete(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to delete file: {filePath}. Error: {ex.Message}");
        }
    }
}
