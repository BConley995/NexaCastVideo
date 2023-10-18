using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using NexaCastVideo;

public class ContentCreator
{
    private readonly string _openAIApiEndpoint = "https://api.openai.com/v2/engines/davinci/completions";
    private readonly string _apiKey;
    private readonly string _generationDirectory = ".\\GeneratedContent";

    public ContentCreator(string generationDirectory = ".\\GeneratedContent")
    {
        _apiKey = ConfigManager.GetAPIKey("Gpt4ApiKey");
        _generationDirectory = generationDirectory;
    }

    private void SaveGeneratedContent(string content, string fileType, string filePath)
    {
        try
        {
            Directory.CreateDirectory(_generationDirectory);
            File.WriteAllText(filePath, content);
            Logger.LogInfo($"{fileType} has been saved to: {filePath}");
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to save generated {fileType}: {ex.Message}");
        }
    }

    private async Task<string> CallOpenAIAsync(string prompt, string fileType)
    {
        try
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
                client.DefaultRequestHeaders.Add("User-Agent", "NexaCast");

                var requestBody = new
                {
                    prompt = prompt,
                    max_tokens = 500 // Limiting response length.
                };

                var response = await client.PostAsync(_openAIApiEndpoint, new StringContent(JsonSerializer.Serialize(requestBody), System.Text.Encoding.UTF8, "application/json"));
                var responseBody = await response.Content.ReadAsStringAsync();

                var responseObject = JsonSerializer.Deserialize<OpenAIResponse>(responseBody);

                string responseText = responseObject?.Choices?[0]?.Text?.Trim() ?? string.Empty;

                // Save Sctipt_   content
                // string filePath = Path.Combine(_generationDirectory, $"{fileType}_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                // SaveGeneratedContent(responseText, fileType, filePath);
                // Logger.LogInfo($"Saving {fileType} to: {Path.GetFullPath(filePath)}");

                return responseText;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error in calling OpenAI API: {ex.Message}");
            return string.Empty;
        }
    }

    public async Task<string> GenerateTitleAsync(string topic)
    {
        return await CallOpenAIAsync($"Generate a catchy video title about {topic}", "Title");
    }

    public async Task<string> GenerateDescriptionAsync(string topic)
    {
        return await CallOpenAIAsync($"Write a brief video description about {topic}", "Description");
    }

    public async Task<string> GenerateScriptAsync(string topic)
    {
        return await CallOpenAIAsync($"Draft a video script on the topic of {topic}", "Script");
    }

    public async Task SaveScript(string script)
    {
        string scriptFilePath = Path.Combine(_generationDirectory, "GeneratedScript.txt");

        try
        {
            await File.WriteAllTextAsync(scriptFilePath, script);
            Logger.LogInfo($"Script saved successfully to: {scriptFilePath}");
        }
        catch (Exception e)
        {
            Logger.LogError($"Error saving script to {scriptFilePath}: {e.Message}");
        }
    }


}

public class OpenAIResponse
{
    public Choice[] Choices { get; set; }
}

public class Choice
{
    public string Text { get; set; }
}
