using NexaCastVideo;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using NexaCastVideo;

public class Uploader
{
    private readonly string _uploadEndpoint;
    private readonly string _generationDirectory;

    public Uploader(string uploadEndpoint, string generationDirectory)
    {
        _uploadEndpoint = uploadEndpoint ?? throw new ArgumentNullException(nameof(uploadEndpoint));
        _generationDirectory = generationDirectory ?? throw new ArgumentNullException(nameof(generationDirectory));

        // Check if generation directory exists, if not, create it.
        if (!Directory.Exists(_generationDirectory))
        {
            Directory.CreateDirectory(_generationDirectory);
        }
    }

    public async Task<bool> UploadVideoAsync(string videoPath)
    {
        try
        {
            using (HttpClient client = new HttpClient())
            using (var content = new MultipartFormDataContent())
            using (var fileStream = new FileStream(videoPath, FileMode.Open))
            {
                var fileContent = new StreamContent(fileStream);

                fileContent.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("form-data")
                {
                    Name = "file",
                    FileName = Path.GetFileName(videoPath)
                };

                content.Add(fileContent);

                HttpResponseMessage response = await client.PostAsync(_uploadEndpoint, content);
                if (response.IsSuccessStatusCode)
                {
                    Logger.LogInfo("Video uploaded successfully!");

                    // Optionally, save a copy of the uploaded video to the generation directory.
                    string destinationPath = Path.Combine(_generationDirectory, Path.GetFileName(videoPath));
                    File.Copy(videoPath, destinationPath, overwrite: true);

                    return true;
                }
                else
                {
                    string errorMessage = $"Failed to upload video. Server responded with status code: {response.StatusCode}";
                    Logger.LogError(errorMessage);
                    return false;
                }
            }
        }
        catch (Exception ex)
        {
            string errorMessage = $"An error occurred during the upload process: {ex.Message}\nType: {ex.GetType().FullName}\nStackTrace: {ex.StackTrace}";
            Logger.LogError(errorMessage);
            return false;
        }
    }
}
