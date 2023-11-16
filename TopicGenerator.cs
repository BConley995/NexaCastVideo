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
using Polly;

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
            // model = "gpt-4-1106-preview",
            model = "gpt-3.5-turbo",
            messages = messages,
            max_tokens = maxTokens
        };

        var requestContent = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

        var response = await _client.PostAsync(OpenAIApiUrl, requestContent).ConfigureAwait(false);

        var responseBody = await response.Content.ReadAsStringAsync();
        Logger.LogInfo($"Raw API response: {responseBody}");

        if (response.IsSuccessStatusCode)
        {
            var responseObject = JObject.Parse(responseBody);
            return responseObject["choices"][0]["message"]["content"].ToString().Trim();
        }
        else
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"OpenAI API call failed. Status: {response.StatusCode}. Error: {errorContent}. Please try again.");
        }
    }

    private async Task<string> GenerateSegment(string context, string prompt, int maxTokens)
    {
        var generatedSegment = await MakeAPICall("You are a script generator for educational YouTube videos.", context + prompt, maxTokens);
        var completeSegment = await EnsureCompleteSentence(generatedSegment);
        return completeSegment;
    }

    private async Task<string> EnsureCompleteSentence(string segment)
    {
        if (!segment.EndsWith(".") && !segment.EndsWith("?") && !segment.EndsWith("!"))
        {
            // The last sentence seems incomplete. Generate the completion.
            string completionPrompt = "Complete the following sentence: " + segment.Split('.').Last();
            var completion = await MakeAPICall("You are a script generator for educational YouTube videos.", completionPrompt, 50);
            return segment + completion;
        }
        return segment; // Return the original segment if it ends with a complete sentence.
    }

    private async Task<string> GenerateDetailedPromptForDalle(string content, string topic)
    {
        // Adjust the instruction to ask for a simple and clear description suitable for image generation.
        var promptMessage = $"Generate a simple and concise DALL·E prompt for an illustration based on: \"{content}\"";

        var messages = new object[]
        {
        new { role = "system", content = "You are an assistant capable of generating simple DALL·E prompts." },
        new { role = "user", content = promptMessage }
        };

        var requestBody = new
        {
            model = "gpt-3.5-turbo", 
            messages = messages,
            max_tokens = 60  // Reduce max_tokens to encourage brevity
        };

        var requestContent = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

        var response = await _client.PostAsync(OpenAIApiUrl, requestContent).ConfigureAwait(false);

        if (response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            var responseObject = JObject.Parse(responseBody);
            // Simplify and extract the core idea of the generated prompt.
            var generatedPrompt = responseObject["choices"][0]["message"]["content"].ToString().Trim();
            // Post-process the prompt if necessary to ensure simplicity.
            return SimplifyPrompt(generatedPrompt);
        }
        else
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"OpenAI API call failed. Status: {response.StatusCode}. Error: {errorContent}");
        }
    }

    private string SimplifyPrompt(string prompt)
    { 
        return prompt;
    }


    public async Task<string> GenerateScriptFromInput(string topic)
    {
        var script = new StringBuilder();
        string context = "";

        try
        {
            // Generate a concise introduction
            var introduction = await GenerateSegment(context, $"Create a very brief introduction include a quick statement that content is AI generated and the narrator is an AI voice. Than discus a quick into to {topic}.", 100);
            if (!string.IsNullOrWhiteSpace(introduction))
            {
                script.AppendLine($"Narrator: {introduction}");
                script.AppendLine();
            }

            // Limit the number of main points and sub-points
            for (int i = 1; i <= 1; i++)
            {
                // Generate a main point
                var mainPoint = await GenerateSegment(context, $"Provide a concise main point about {topic}.", 150);
                if (!string.IsNullOrWhiteSpace(mainPoint))
                {
                    script.AppendLine($"Narrator: {mainPoint}");
                    script.AppendLine();
                }

                // Generate a sub-point
                var subPoint = await GenerateSegment(context, $"Briefly elaborate on the above point.", 100);
                if (!string.IsNullOrWhiteSpace(subPoint))
                {
                    script.AppendLine($"Narrator: {subPoint}");
                    script.AppendLine();
                }
            }

            // Generate a brief conclusion
            var conclusion = await GenerateSegment(context, "Conclude the discussion with a short fact.", 100);
            if (!string.IsNullOrWhiteSpace(conclusion))
            {
                script.AppendLine($"Narrator: {conclusion}");
                script.AppendLine();
            }

            // Add the final call to action
            script.AppendLine("Don’t forget to subscribe. Thank you for watching NexaCast.");

            string finalScript = ScriptCleaner(script.ToString());

            return finalScript;
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
            // Split the script into paragraphs
            var paragraphs = script.Split(new string[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries);

            Logger.LogInfo($"Extracted paragraphs:\n{string.Join("\n", paragraphs)}");

            // Define a retry policy for transient fault handling
            var retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(
                    3, // Retry count
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // Exponential back-off
                    onRetry: (exception, timespan, retryCount, context) =>
                    {
                        Logger.LogWarning($"Retry {retryCount} due to {exception.Message}. Waiting {timespan.TotalSeconds} seconds before next retry.");
                    });

            foreach (var paragraph in paragraphs)
            {
                var content = paragraph.Trim();
                if (!string.IsNullOrEmpty(content))
                {
                    // Generate DALL-E prompt for each paragraph
                    var rawPrompt = await retryPolicy.ExecuteAsync(() => GenerateDetailedPromptForDalle(content, ""));
                    var bracketedPrompt = $"[{Regex.Replace(rawPrompt, @"^\w+:\s+", "")}]";
                    prompts.Add(bracketedPrompt);
                }
            }

            string promptsFilePath = Path.Combine(_generationDirectory, "DallePrompts.txt");
            if (prompts.Any())
            {
                await File.WriteAllLinesAsync(promptsFilePath, prompts);
                Logger.LogInfo($"DALL·E prompts saved successfully to: {promptsFilePath}");
            }
            else
            {
                Logger.LogWarning("No prompts generated for DALL·E. Ensure the script has content for prompts.");
            }
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

        // Remove content with parentheses
        script = Regex.Replace(script, @"\([^)]*\)", string.Empty);

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
        script = script.Replace("Host", "");
        script = script.Replace(" : ", "");
        script = script.Replace("Narrator (voiceover):", "Narrator:");
        script = script.Replace("Narrator: Narrator (voiceover):", "Narrator:");
        script = script.Replace("Narrator", "");
        script = script.Replace("Narrator: ", "");
        script = script.Replace("Narrator:", "");
        script = script.Replace("Narrator :", "");
        script = script.Replace(":", "");

        // Correct missing spaces after punctuation
        script = Regex.Replace(script, @"([a-zA-Z])\.([a-zA-Z])", "$1. $2");  // Periods
        script = Regex.Replace(script, @"([a-zA-Z])\,([a-zA-Z])", "$1, $2");  // Commas
        script = Regex.Replace(script, @"([a-zA-Z])\!([a-zA-Z])", "$1! $2");  // Exclamation points
        script = Regex.Replace(script, @"([a-zA-Z])\?([a-zA-Z])", "$1? $2");  // Question marks
        script = Regex.Replace(script, @"([a-zA-Z])\:([a-zA-Z])", "$1: $2");  // Colons
        script = Regex.Replace(script, @"([a-zA-Z])\;([a-zA-Z])", "$1; $2");  // Semicolons

        // Correct multiple spaces to a single space
        script = Regex.Replace(script, @" +", " ");

        // Initial cleaning complete, now remove incomplete sentences and orphaned "Narrator:" lines
        var cleanedScript = new StringBuilder();
        var lines = script.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            if (!string.IsNullOrWhiteSpace(trimmedLine) && !trimmedLine.Equals("Narrator:"))
            {
                cleanedScript.AppendLine(trimmedLine);
            }
        }

        // Return the final cleaned script
        return script.Trim();
    }
}
