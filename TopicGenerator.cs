using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NexaCastVideo;
using System.Linq;

public class TopicGenerator
{
    private const string OpenAIApiUrl = "https://api.openai.com/v1/chat/completions";
    private readonly HttpClient _client;
    private readonly string _generationDirectory;

    public TopicGenerator(string generationDirectory)
    {
        string apiKey = ConfigManager.GetAPIKey("Gpt4ApiKey");

        _client = new HttpClient();
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        _generationDirectory = generationDirectory ?? throw new ArgumentNullException(nameof(generationDirectory));

        if (!Directory.Exists(_generationDirectory))
        {
            Directory.CreateDirectory(_generationDirectory);
        }
    }

    private async Task<string> GenerateSegment(string context, string prompt, int maxTokens)
    {
        var messages = new object[]
        {
            new { role = "system", content = "You are a script generator for educational YouTube videos." },
            new { role = "user", content = context + prompt }
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
            throw new Exception($"OpenAI API call failed. Status: {response.StatusCode}. Error: {errorContent}");
        }
    }

    private string GeneratePromptForDalle(string content)
    {
        var keywords = new List<string> { "history", "origin", "ancient", "dynasty", "China", "royalty", "culture", "folklore" };
        var foundKeywords = keywords.Where(k => content.Contains(k)).ToList();

        if (foundKeywords.Count == 0)
            return "[General illustration]";  // default prompt

        return $"[Illustration of {string.Join(", ", foundKeywords)}]";
    }

    public async Task<string> GenerateScriptFromInput(string topic)
    {
        var script = new StringBuilder();
        string context = "";

        try
        {
            // DALL·E prompt for topic
            script.AppendLine($"[{topic} Illustration]");

            var introduction = await GenerateSegment(context, $"Create an engaging introduction discussing {topic}.", 100);
            context += introduction;

            // DALL·E prompt for introduction
            script.AppendLine(GeneratePromptForDalle(introduction));
            script.AppendLine(introduction);
            script.AppendLine();

            for (int i = 1; i <= 2; i++)
            {
                var mainPoint = await GenerateSegment(context, $"Provide a main point about {topic}.", 100);
                context += mainPoint;

                // DALL·E prompt for the main point
                script.AppendLine(GeneratePromptForDalle(mainPoint));
                script.AppendLine(mainPoint);
                script.AppendLine();

                var subPoint = await GenerateSegment(context, $"Elaborate with a sub-point regarding the above point.", 100);
                context += subPoint;

                // DALL·E prompt for the sub-point
                script.AppendLine(GeneratePromptForDalle(subPoint));
                script.AppendLine(subPoint);
                script.AppendLine();
            }

            var conclusion = await GenerateSegment(context, "Conclude the discussion by summarizing the key points mentioned.", 100);
            context += conclusion;

            // DALL·E prompt for the conclusion
            script.AppendLine(GeneratePromptForDalle(conclusion));
            script.AppendLine(conclusion);
            script.AppendLine();

            script.AppendLine("Don’t forget to subscribe, and thank you for enjoying a byte-sized bit with NexaCast, from thought to YouTube.");

            // Cleaning the script
            string cleanedScript = ScriptCleaner(script.ToString());

            // Subtitle Generation
            GenerateSubtitles(cleanedScript);

            return cleanedScript;
        }
        catch (Exception e)
        {
            Logger.LogError($"Error generating script: {e.Message}");
            throw;
        }
    }

    private void GenerateSubtitles(string script)
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
            var srtContent = subtitleEngine.GenerateSRT(lines.ToList(), durations);  // Convert array to list here

            // Save the generated SRT content to file.
            string srtFileName = "GeneratedSubtitles.srt";
            subtitleEngine.SaveSRTToGenerationDirectory(srtContent, srtFileName);

            Logger.LogInfo($"Subtitle file saved successfully to: {_generationDirectory}/{srtFileName}");
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
        // Correct missing spaces after punctuation
        script = Regex.Replace(script, @"([a-zA-Z])\.([a-zA-Z])", "$1. $2");  // Periods
        script = Regex.Replace(script, @"([a-zA-Z])\,([a-zA-Z])", "$1, $2");  // Commas
        script = Regex.Replace(script, @"([a-zA-Z])\!([a-zA-Z])", "$1! $2");  // Exclamation points
        script = Regex.Replace(script, @"([a-zA-Z])\?([a-zA-Z])", "$1? $2");  // Question marks
        script = Regex.Replace(script, @"([a-zA-Z])\:([a-zA-Z])", "$1: $2");  // Colons
        script = Regex.Replace(script, @"([a-zA-Z])\;([a-zA-Z])", "$1; $2");  // Semicolons

        // Correct multiple spaces to a single space
        script = Regex.Replace(script, @" +", " ");

        return script;
    }
}
