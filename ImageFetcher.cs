using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Png;
using System.Threading.Tasks;

namespace NexaCastVideo
{
    public class ImageFetcher
    {
        private readonly string _dalleApiEndpoint = "https://api.openai.com/v1/images/generations";
        private readonly string _apiKey;
        private readonly string _generationDirectory;
        private int _imageCounter = 1;
        private const int MaxRetries = 3;

        public ImageFetcher(string apiKey, string generationDirectory)
        {
            _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
            _generationDirectory = generationDirectory ?? throw new ArgumentNullException(nameof(generationDirectory));
        }

        public async Task<List<string>> FetchAndDownloadImages()
        {
            var promptsFilePath = Path.Combine(_generationDirectory, "DallePrompts.txt");
            var imagePaths = new List<string>();

            if (!File.Exists(promptsFilePath))
            {
                Logger.LogInfo($"Prompts file not found at path: {promptsFilePath}");
                return imagePaths;
            }

            var prompts = File.ReadAllLines(promptsFilePath);

            foreach (var prompt in prompts)
            {
                bool success = false;
                int retryCount = 0;
                while (!success && retryCount < MaxRetries)
                {
                    var imageFilePath = await GenerateAndSaveImage(prompt);
                    if (!string.IsNullOrEmpty(imageFilePath))
                    {
                        imagePaths.Add(imageFilePath);
                        success = true;
                    }
                    else
                    {
                        retryCount++;
                        Logger.LogInfo($"Retry {retryCount} for prompt: {prompt}");
                        await Task.Delay(5000);
                    }
                }

                if (!success)
                {
                    Logger.LogError($"Prompt \"{prompt}\" failed after {MaxRetries} retries.");
                }

                await Task.Delay(15000);
            }

            return imagePaths;
        }

        private async Task<string> GenerateAndSaveImage(string prompt)
        {
            try
            {
                var requestBody = new
                {
                    model = "dall-e-3",
                    prompt = prompt,
                    n = 1,
                    size = "1792x1024", // dall-e-3 uses 1792x1024 - dall-e-2 uses 1024x1024
                    response_format = "b64_json"
                };

                var data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(requestBody));

                var request = (HttpWebRequest)WebRequest.Create(_dalleApiEndpoint);
                request.Method = "POST";
                request.ContentType = "application/json";
                request.Headers.Add("Authorization", "Bearer " + _apiKey);

                using (var stream = await request.GetRequestStreamAsync())
                {
                    stream.Write(data, 0, data.Length);
                }

                using (var response = (HttpWebResponse)await request.GetResponseAsync())
                {
                    Logger.LogInfo($"Response Status Code: {response.StatusCode}");
                    var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
                    var responseObject = JsonConvert.DeserializeObject<DalleResponseWrapper>(responseString);

                    if (responseObject?.Data != null && responseObject.Data.Count > 0)
                    {
                        var base64Data = responseObject.Data[0].B64_Json;
                        return SaveImage(base64Data, _imageCounter++, _generationDirectory);
                    }
                    else
                    {
                        Logger.LogInfo("No image data found in API response.");
                    }
                }
            }
            catch (WebException webEx) when (webEx.Response is HttpWebResponse response)
            {
                Logger.LogError($"HTTP Error: {response.StatusCode}");
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    var responseContent = await reader.ReadToEndAsync();
                    Logger.LogError($"Error content: {responseContent}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Exception occurred during image generation and saving: {ex.Message}");
            }

            return string.Empty;
        }

        private string SaveImage(string base64String, int imageNumber, string directoryPath)
        {
            byte[] imageBytes = Convert.FromBase64String(base64String);
            string filename = $"image_{imageNumber}.png";
            string filePath = Path.Combine(directoryPath, filename);

            Directory.CreateDirectory(directoryPath);

            using (var stream = new MemoryStream(imageBytes))
            {
                using (var image = Image.Load<Rgba32>(stream))
                {
                    image.Save(filePath, new PngEncoder());
                    Logger.LogInfo($"Downloaded and saved image to {filePath}");
                }
            }

            return filePath;
        }

        public class DalleResponseWrapper
        {
            public long Created { get; set; }
            public List<DalleData> Data { get; set; }
        }

        public class DalleData
        {
            public string Url { get; set; }
            public string B64_Json { get; set; }
        }
    }
}
