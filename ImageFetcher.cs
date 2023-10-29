using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using NexaCastVideo;

public class ImageFetcher
{
    private readonly string _dalleApiEndpoint = "https://api.openai.com/v1/images/generations";
    private readonly string _apiKey;
    private readonly string _generationDirectory;
    private int _imageCounter;

    private const int MaxRetries = 3;

    public ImageFetcher(string generationDirectory)
    {
        _apiKey = ConfigManager.GetAPIKey("DalleApiKey");
        _generationDirectory = generationDirectory ?? throw new ArgumentNullException(nameof(generationDirectory));
        _imageCounter = 1;
    }

    private async Task<string> FetchImageURLAsync(string description)
    {
        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
            client.DefaultRequestHeaders.Add("User-Agent", "NexaCast");

            var requestBody = new
            {
                prompt = description,
                n = 1,
                size = "1024x1024"
            };

            for (int retry = 0; retry < MaxRetries; retry++)
            {
                try
                {
                    var response = await client.PostAsync(_dalleApiEndpoint, new StringContent(JsonSerializer.Serialize(requestBody), System.Text.Encoding.UTF8, "application/json"));

                    if (response.IsSuccessStatusCode)
                    {
                        var responseBody = await response.Content.ReadAsStringAsync();
                        var responseObject = JsonSerializer.Deserialize<DalleResponseWrapper>(responseBody);

                        if (responseObject?.Data != null && responseObject.Data.Count > 0)
                        {
                            var fullUrl = responseObject.Data[0]?.Url ?? string.Empty;
                            return fullUrl;
                        }
                        else
                        {
                            Logger.LogError($"Unexpected API response: {responseBody}");
                            return string.Empty;
                        }
                    }
                    else
                    {
                        var errorBody = await response.Content.ReadAsStringAsync();
                        Logger.LogError($"Error calling DALL·E API. Status Code: {response.StatusCode}. Reason: {response.ReasonPhrase}. Error Details: {errorBody}");
                        return string.Empty;
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error calling DALL·E API (Attempt {retry + 1}/{MaxRetries}): {ex.Message}");

                    if (retry == MaxRetries - 1)
                    {
                        return string.Empty;
                    }

                    await Task.Delay(1000); // wait for a second before retrying
                }
            }

            return string.Empty;
        }
    }

    public async Task<string> GenerateImageForScriptLineAsync(string scriptLine)
    {
        var imageUrl = await FetchImageURLAsync(scriptLine);

        if (string.IsNullOrEmpty(imageUrl))
        {
            Logger.LogError($"Failed to generate image for script line: {scriptLine}");
            return string.Empty;
        }

        try
        {
            var filename = $"Image{_imageCounter++}.png";
            var destinationPath = Path.Combine(_generationDirectory, filename);

            await DownloadImageAsync(imageUrl, destinationPath);
            return destinationPath;
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error processing image: {ex.Message}");
            return string.Empty;
        }
    }

    private async Task DownloadImageAsync(string url, string destinationPath)
    {
        using HttpClient client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

        for (int retry = 0; retry < MaxRetries; retry++)
        {
            try
            {
                var responseBytes = await client.GetByteArrayAsync(url);
                await File.WriteAllBytesAsync(destinationPath, responseBytes);
                return;
            }
            catch (HttpRequestException ex)
            {
                Logger.LogError($"Error during file download (Attempt {retry + 1}/{MaxRetries}): {ex.Message}");
            }

            await Task.Delay(1000); // waiting a second before retrying
        }

        throw new Exception($"Failed to download image from {url} after {MaxRetries} attempts.");
    }

    public async Task<List<string>> GenerateImagesForScriptAsync(List<string> scriptLines)
    {
        var imageUrls = new List<string>();

        foreach (var line in scriptLines)
        {
            if (!ShouldGenerateImageForLine(line))
            {
                imageUrls.Add(string.Empty);  // Add an empty string or null to maintain line order, if necessary.
                continue;
            }

            var imageUrl = await GenerateImageForScriptLineAsync(line);
            imageUrls.Add(imageUrl);
        }

        return imageUrls;
    }

    private bool ShouldGenerateImageForLine(string line)
    {
        var ignoredPhrases = new List<string>
    {
        "[Upbeat music playing]",
        "[Transition scene]",
        "[Background music fades in]",
        "[Background music fades out]",
        "[Upbeat music starts playing]"
    };

        foreach (var phrase in ignoredPhrases)
        {
            if (line.Contains(phrase, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    public class DalleResponseWrapper
    {
        public List<DalleData> Data { get; set; }
    }

    public class DalleData
    {
        public string Url { get; set; }
    }
}
