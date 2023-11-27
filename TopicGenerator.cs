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


// The TopicGenerator class is responsible for generating topics and related content using OpenAI's API.
public class TopicGenerator
{
    private const string OpenAIApiUrl = "https://api.openai.com/v1/chat/completions";
    private readonly HttpClient _client;
    private readonly string _generationDirectory;
    private readonly SubtitleEngine _subtitleEngine;

    // Custom factory for creating HttpClient instances.
    public class CustomHttpClientFactory
    {
        // Creates and configures an HttpClient instance.
        public HttpClient CreateClient()
        {
            var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(600);
            return httpClient;
        }
    }


    public TopicGenerator(string generationDirectory)
    {
        string apiKey = ConfigManager.GetAPIKey("Gpt4ApiKey");

        var httpClientFactory = new CustomHttpClientFactory();
        _client = httpClientFactory.CreateClient();
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        _generationDirectory = generationDirectory ?? throw new ArgumentNullException(nameof(generationDirectory));

        if (!Directory.Exists(_generationDirectory))
        {
            Directory.CreateDirectory(_generationDirectory);
        }
    }

    // Tests the availability of the OpenAI API.
    public async Task<bool> TestOpenAIAPIAvailability()
    {
        try
        {
            // Simple test call to OpenAI API
            var testResult = await MakeAPICall("system", "Ping", 5);
            return !string.IsNullOrEmpty(testResult);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"API Test Failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Makes a call to the OpenAI API.
    /// </summary>
    /// <param name="messageRole">Role of the message (system or user).</param>
    /// <param name="messageContent">Content of the message.</param>
    /// <param name="maxTokens">Maximum number of tokens to generate.</param>
    /// <param name="maxRetries">Maximum number of retry attempts.</param>
    /// <returns>A task that represents the asynchronous operation, returning the API response as a string.</returns>
    private async Task<string> MakeAPICall(string messageRole, string messageContent, int maxTokens, int maxRetries = 3)
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

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                var requestContent = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
                var response = await _client.PostAsync(OpenAIApiUrl, requestContent);

                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var responseObject = JObject.Parse(responseBody);
                    return responseObject["choices"][0]["message"]["content"].ToString().Trim();
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                {
                    // Log the retry attempt
                    Logger.LogWarning($"Attempt {attempt + 1}: Server error, retrying...");

                    // Wait before retrying
                    await Task.Delay(TimeSpan.FromSeconds(2));
                    continue;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"OpenAI API call failed. Status: {response.StatusCode}. Error: {errorContent}.");
                }
            }
            catch (HttpRequestException ex)
            {
                if (attempt == maxRetries - 1)
                    throw; // Rethrow the exception if the last attempt fails
                else
                    Logger.LogWarning($"Attempt {attempt + 1}: HttpRequestException, retrying. Error: {ex.Message}");
            }

            await Task.Delay(TimeSpan.FromSeconds(2)); // Wait a bit before retrying
        }

        throw new Exception("Max retry attempts reached. Unable to get a successful response.");
    }

    // Ensures that a given segment ends with a complet esentence. 
    private async Task<string> EnsureCompleteSentence(string segment)
    {
        // Check if the segment ends with a punctuation mark
        if (!segment.EndsWith(".") && !segment.EndsWith("?") && !segment.EndsWith("!"))
        {
            // Attempt to complete the sentence
            string completionPrompt = "Complete the following sentence: " + segment.Split('.').Last();
            var completion = await MakeAPICall("You are a script generator for educational YouTube videos.", completionPrompt, 50);

            // Check for repetition in the completion and remove if necessary
            var lastPart = segment.Split('.').Last();
            if (completion.StartsWith(lastPart))
            {
                completion = completion.Substring(lastPart.Length).Trim();
            }

            return segment + completion;
        }
        return segment; // Return the original segment if it ends with a complete sentence.
    }

    /// <summary>
    /// Generates a prompt for DALL·E based on given content.
    /// </summary>
    /// <param name="content">Content to base the DALL·E prompt on.</param>
    /// <param name="topic">The topic of the content.</param>
    /// <returns>A task that represents the asynchronous operation, returning a DALL·E prompt.</returns>
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
            model = "gpt-4-1106-preview",
            messages = messages,
            max_tokens = 60 
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
            Logger.LogInfo($"Starting to generate introduction for topic: {topic}");
            var introduction = await GenerateSegment(context, $"Create an quick introduction saying 'welcome to nexacast' and quickly discussing {topic} and quickly mention this is AI generated.", 60, topic);
            context += introduction;
            script.AppendLine($"Narrator: {introduction}");
            script.AppendLine();

            // Specify areas to cover in main points
            string[] mainPointsTopics = { "early history", "major achievements or advancements" }; 
            for (int i = 0; i < mainPointsTopics.Length; i++)
            {
                Logger.LogInfo($"Generating main point about {mainPointsTopics[i]} for topic: {topic}");
                var mainPoint = await GenerateSegment(context, $"Discuss an aspect of {topic}'s {mainPointsTopics[i]}.", 150, topic);
                context += mainPoint;
                script.AppendLine($"Narrator: {mainPoint}");
                script.AppendLine();
            }

            Logger.LogInfo("Generating conclusion with a unique fact");
            var conclusion = await GenerateSegment(context, "Conclude the discussion with a unique or lesser-known fact about " + topic + ".", 75, topic);
            context += conclusion;
            script.AppendLine($"Narrator: {conclusion}");
            script.AppendLine();

            script.AppendLine("Don’t forget to subscribe, and thank you for enjoying a byte-sized bit with NexaCast, from thought to YouTube.");

            string cleanedScript = ScriptCleaner(script.ToString());

            Logger.LogInfo("Script generation completed successfully.");
            return cleanedScript;
        }
        catch (Exception e)
        {
            Logger.LogError($"Error generating script: {e.Message}");
            throw;
        }
    }



    private async Task<string> GenerateSegment(string context, string prompt, int maxTokens, string topic)
    {
        string generatedSegment;
        int attempts = 0;
        const int maxAttempts = 3;

        do
        {
            string focusedPrompt = $"{context} Discuss {topic} focusing on {prompt}. Avoid Q&A format and make the content flow naturally as in an instructional video.";
            generatedSegment = await MakeAPICall("You are a script generator for educational YouTube videos. Do not use titles or the statement 'did you know' stay on topic to the original {topic}", focusedPrompt, maxTokens);

            // Post-process and check content relevance
            generatedSegment = PostProcessResponse(generatedSegment);
            if (IsContentRelevant(generatedSegment, topic))
            {
                break; // Break the loop if content is on-topic
            }

            attempts++;
        } while (attempts < maxAttempts);

        return await EnsureCompleteSentence(generatedSegment);
    }

    private bool IsContentRelevant(string content, string topic)
    {
        // Simple check to see if the content contains the topic
        return content.Contains(topic, StringComparison.OrdinalIgnoreCase);
    }


    private string PostProcessResponse(string response)
    {
        // Example post-processing to remove direct answer formatting and redundant phrases
        var phrasesToRemove = new List<string> { "Certainly!", "Here's a detailed and relevant fact about", "Here's an interesting fact:", "As we explore", "One of the key events" };
        foreach (var phrase in phrasesToRemove)
        {
            response = response.Replace(phrase, "").Trim();
        }
        return response;
    }


    public async Task<List<string>> GenerateDallePromptsFromScript(string script)
    {
        Logger.LogInfo($"Received script for DALL·E prompts generation:\n{script}");

        var prompts = new List<string>();
        try
        {
            // Updated splitting logic to accommodate different paragraph delimiters
            var paragraphs = script.Split(new string[] { "\r\n\r\n", "\n\n", "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            Logger.LogInfo($"Number of paragraphs extracted: {paragraphs.Length}");

            foreach (var paragraph in paragraphs)
            {
                var content = paragraph.Trim();
                if (!string.IsNullOrEmpty(content))
                {
                    // Generate DALL-E prompt for each paragraph
                    var rawPrompt = await GenerateDetailedPromptForDalle(content, "");
                    var bracketedPrompt = $"[{Regex.Replace(rawPrompt, @"^\w+:\s+", "")}]";
                    prompts.Add(bracketedPrompt);
                    Logger.LogInfo($"Generated prompt for paragraph: {content}");
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

        Logger.LogInfo($"Number of prompts generated: {prompts.Count}");
        return prompts;
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