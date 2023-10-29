using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NexaCastVideo;
using System.Linq;
using Google.Apis.Http;

public class TopicGenerator
{
    private const string OpenAIApiUrl = "https://api.openai.com/v1/chat/completions";
    private readonly HttpClient _client;
    private readonly string _generationDirectory;
    private readonly SubtitleEngine _subtitleEngine;

    public TopicGenerator(string generationDirectory)
    {
        string apiKey = ConfigManager.GetAPIKey("Gpt4ApiKey");

        _subtitleEngine = new SubtitleEngine(generationDirectory);
        _client = new HttpClient();
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        _generationDirectory = generationDirectory ?? throw new ArgumentNullException(nameof(generationDirectory));

        if (!Directory.Exists(_generationDirectory))
        {
            Directory.CreateDirectory(_generationDirectory);
        }
    }

    private async Task<string> MakeAPICall(string messageRole, string messageContent, int maxTokens)
    {
        var messages = new object[]
        {
            new { role = "system", content = messageRole },
            new { role = "user", content = messageContent }
        };

        var requestBody = new
        {
            model = "gpt-3.5-turbo",
            messages = messages,
            max_tokens = maxTokens
        };

        var requestContent = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

        var response = await _client.PostAsync(OpenAIApiUrl, requestContent).ConfigureAwait(false);

        if (response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            var responseObject = JObject.Parse(responseBody);
            return responseObject["choices"][0]["message"]["content"].ToString().Trim();
        }
        else
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"OpenAI API call failed. Status: {response.StatusCode}. Error: {errorContent}");
        }
    }

    private async Task<string> GenerateSegment(string context, string prompt, int maxTokens)
    {
        return await MakeAPICall("You are a script generator for educational YouTube videos.", context + prompt, maxTokens);
    }

    private async Task<string> GenerateDetailedPromptForDalle(string content, string topic)
    {
        var promptMessage = $"Describe a detailed DALL·E illustration based on this content: \"{content}\"";

        var messages = new object[]
        {
        new { role = "system", content = "You are an assistant capable of generating detailed DALL·E prompts." },
        new { role = "user", content = promptMessage }
        };

        var requestBody = new
        {
            model = "gpt-3.5-turbo",
            messages = messages,
            max_tokens = 200  // limit to get more concise responses
        };

        var requestContent = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

        var response = await _client.PostAsync(OpenAIApiUrl, requestContent).ConfigureAwait(false);

        if (response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            var responseObject = JObject.Parse(responseBody);
            return responseObject["choices"][0]["message"]["content"].ToString().Trim();
        }
        else
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"OpenAI API call failed. Status: {response.StatusCode}. Error: {errorContent}");
        }
    }

    public async Task<string> GenerateScriptFromInput(string topic)
    {
        var script = new StringBuilder();
        string context = "";

        try
        {
            var introduction = await GenerateSegment(context, $"Create an engaging introduction discussing {topic}.", 300);
            context += introduction;
            script.AppendLine($"Narrator: {introduction}");
            script.AppendLine();

            for (int i = 1; i <= 2; i++)
            {
                var mainPoint = await GenerateSegment(context, $"Provide a main point about {topic}.", 500);
                context += mainPoint;
                script.AppendLine($"Narrator: {mainPoint}");
                script.AppendLine();

                var subPoint = await GenerateSegment(context, $"Elaborate with a sub-point regarding the above point.", 200);
                context += subPoint;
                script.AppendLine($"Narrator: {subPoint}");
                script.AppendLine();
            }

            var conclusion = await GenerateSegment(context, "Conclude the discussion with an interesting face.", 200);
            context += conclusion;
            script.AppendLine($"Narrator: {conclusion}");
            script.AppendLine();

            script.AppendLine("Don’t forget to subscribe, and thank you for enjoying a byte-sized bit with NexaCast, from thought to YouTube.");

            string cleanedScript = ScriptCleaner(script.ToString());

            GenerateSubtitles(cleanedScript);

            return cleanedScript;
        }
        catch (Exception e)
        {
            Logger.LogError($"Error generating script: {e.Message}");
            throw;
        }
    }

    public async Task<List<string>> GenerateDallePromptsFromScript(string script)
    {
        Logger.LogInfo($"Received script for DALL·E prompts generation:\n{script}");

        var prompts = new List<string>();
        try
        {
            var paragraphs = script.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                                .Where(line => line.StartsWith("Narrator:")).ToList();
            Logger.LogInfo($"Extracted paragraphs:\n{string.Join("\n", paragraphs)}");

            foreach (var paragraph in paragraphs)
            {
                var content = paragraph.Replace("Narrator: ", "").Trim();
                var rawPrompt = await GenerateDetailedPromptForDalle(content, "");

                // Remove any leading words followed by a colon
                rawPrompt = Regex.Replace(rawPrompt, @"^\w+:\s+", "");

                // Wrap prompt in brackets [ ]
                var bracketedPrompt = $"[{rawPrompt}]";

                prompts.Add(bracketedPrompt);
                Logger.LogInfo($"Generated prompt for paragraph:\n{paragraph}\nPrompt:\n{bracketedPrompt}");
            }

            string promptsFilePath = Path.Combine(_generationDirectory, "DallePrompts.txt");

            if (!prompts.Any())
            {
                Logger.LogError("No prompts generated for DALL·E.");
                throw new Exception("No prompts generated for DALL·E.");
            }

            await File.WriteAllLinesAsync(promptsFilePath, prompts);

            Logger.LogInfo($"DALL·E prompts saved successfully to: {promptsFilePath}");
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error generating DALL·E prompts: {ex.Message}");
            throw;
        }
        return prompts;
    }



    public void GenerateSubtitles(string script)
    {
        try
        {
            // Assume each line takes approximately 3 seconds to speak.
            List<double> durations = new List<double>();
            var lines = script.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            foreach (var line in lines)
            {
                durations.Add(3.0); // Adding 3 seconds for each line.
            }

            // Initialize SubtitleEngine and generate the SRT content.
            var subtitleEngine = new SubtitleEngine(_generationDirectory);
            var srtContent = subtitleEngine.GenerateSRT(lines.ToList(), durations);

            // Save the generated SRT content to file.
            string srtFileName = Path.Combine(_generationDirectory, "GeneratedSubtitles.srt");
            _subtitleEngine.SaveSRTToGenerationDirectory(srtContent, "GeneratedSubtitles.srt");

            Logger.LogInfo($"Subtitle file saved successfully to: {srtFileName}");
        }
        catch (Exception e)
        {
            Logger.LogError($"Error generating or saving subtitles: {e.Message}");
        }
    }


    public async Task SaveScript(string script)
    {
        string scriptFilePath = Path.Combine(_generationDirectory, "GeneratedScript.txt");

        if (string.IsNullOrWhiteSpace(script))
        {
            Logger.LogError("Generated script is empty. Aborting save.");
            return;
        }

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
    private string ScriptCleaner(string script)
    {
        // Remove content within square brackets
        script = Regex.Replace(script, @"\[.*?\]", string.Empty);

        // Replace "word one" with "word two"
        script = script.Replace("Host:", "Narrator:");
        script = script.Replace("Presenter:", "Narrator:");
        script = script.Replace("Narrator:Narrator:", "Narrator:");
        script = script.Replace("Narrator: Narrator:", "Narrator:");
        script = script.Replace("Wrap-up:", "");
        script = script.Replace("Wrap-Up:", "");
        script = script.Replace("Details:", "");
        script = script.Replace("Sub-point:", "");
        script = script.Replace("Main-point", "");
        script = script.Replace("Sub-Point:", "");
        script = script.Replace("Main-Point", "");
        script = script.Replace("- ", "");
        script = script.Replace("Sub point:", "");
        script = script.Replace("Main point", "");
        script = script.Replace("Sub Point:", "");
        script = script.Replace("Main Point", "");

        // Correct missing spaces after punctuation
        script = Regex.Replace(script, @"([a-zA-Z])\.([a-zA-Z])", "$1. $2");  // Periods
        script = Regex.Replace(script, @"([a-zA-Z])\,([a-zA-Z])", "$1, $2");  // Commas
        script = Regex.Replace(script, @"([a-zA-Z])\!([a-zA-Z])", "$1! $2");  // Exclamation points
        script = Regex.Replace(script, @"([a-zA-Z])\?([a-zA-Z])", "$1? $2");  // Question marks
        script = Regex.Replace(script, @"([a-zA-Z])\:([a-zA-Z])", "$1: $2");  // Colons
        script = Regex.Replace(script, @"([a-zA-Z])\;([a-zA-Z])", "$1; $2");  // Semicolons

        // Correct multiple spaces to a single space
        script = Regex.Replace(script, @" +", " ");

        // Remove "Narrator:" occurrences that have no content following them
        script = Regex.Replace(script, @"Narrator:\s*\r\n", "");

        return script;
    }
}
