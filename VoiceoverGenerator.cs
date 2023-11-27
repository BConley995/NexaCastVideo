using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace NexaCastVideo
{
    // Class VoiceoverGenerator: Responsible for generating voiceovers from text
    public class VoiceoverGenerator
    {
        private const string _apiEndpoint = "https://api.elevenlabs.io/v1/text-to-speech/TxGEqnHWrfWFTfGW9XjX";
        private readonly string _outputDirectory;
        private readonly string _apiKey;

        // Constructor - Dependency Injection for output directory and API key
        public VoiceoverGenerator(string outputDirectory, string apiKey)
        {
            _outputDirectory = outputDirectory ?? throw new ArgumentNullException(nameof(outputDirectory));
            _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
            Directory.CreateDirectory(_outputDirectory);
        }

        // Asynchronously generate voiceovers for a list of sentences
        public async Task<List<string>> GenerateVoiceovers(List<string> sentences)
        {
            List<string> audioPaths = new List<string>();

            for (int i = 0; i < sentences.Count; i++)
            {
                try
                {
                    // Check if the sentence is a direction (enclosed in square brackets).
                    if (IsDirection(sentences[i]))
                    {
                        Console.WriteLine($"Skipping voiceover generation for direction: '{sentences[i]}'");
                        Logger.LogError($"Skipping voiceover generation for direction: '{sentences[i]}'");
                        continue;
                    }

                    var cleanSentence = CleanSentence(sentences[i]);
                    var audioPath = Path.Combine(_outputDirectory, $"voiceover_{i}.mp3");

                    // Generate the voiceover for the cleaned sentence.
                    bool success = await GenerateVoiceover(cleanSentence, audioPath);

                    if (success)
                    {
                        audioPaths.Add(audioPath);
                    }
                    else
                    {
                        Console.WriteLine($"Failed to generate voiceover for sentence {i}: '{cleanSentence}'");
                        Logger.LogError($"Failed to generate voiceover for sentence {i}: '{cleanSentence}'");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception while generating voiceover for sentence {i}: '{sentences[i]}'. Exception: {ex.Message}");
                    Logger.LogError($"Exception while generating voiceover for sentence {i}: '{sentences[i]}'. Exception: {ex.Message}");
                }
            }

            return audioPaths;
        }

        public async Task<List<string>> GenerateVoiceoversFromFile(string scriptFilePath)
        {
            // Read the content of the script file
            string scriptContent = File.ReadAllText(scriptFilePath);

            // Split the script into paragraphs based on empty lines
            List<string> paragraphs = SplitIntoParagraphs(scriptContent);

            // Generate voiceovers for each paragraph
            return await GenerateVoiceovers(paragraphs);
        }

        // Helper method to split a script into paragraphs
        private List<string> SplitIntoParagraphs(string scriptContent)
        {
            return scriptContent.Split(new string[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries)
                                .ToList();
        }

        // Check if a sentence is a direction (enclosed in square brackets)
        private bool IsDirection(string sentence)
        {
            return sentence.Trim().StartsWith("[") && sentence.Trim().EndsWith("]");
        }

        // Clean the sentence by removing specific labels
        private string CleanSentence(string sentence)
        {
            return sentence.Replace("Host:", "").Trim();
        }

        // Asynchronously generate a voiceover from a sentence
        private async Task<bool> GenerateVoiceover(string sentence, string outputPath)
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    client.DefaultRequestHeaders.Add("xi-api-key", _apiKey);
                    client.DefaultRequestHeaders.Add("accept", "audio/mpeg");

                    var requestBody = new
                    {
                        text = sentence,
                        model_id = "eleven_multilingual_v2",
                        voice_settings = new
                        {
                            stability = 0,
                            similarity_boost = 0,
                            style = 0,
                            use_speaker_boost = true
                        }
                    };

                    var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

                    var response = await client.PostAsync(
                        _apiEndpoint + "?optimize_streaming_latency=0&output_format=mp3_44100_128",
                        content
                    );

                    if (response.IsSuccessStatusCode)
                    {
                        var audioData = await response.Content.ReadAsByteArrayAsync();
                        await File.WriteAllBytesAsync(outputPath, audioData);
                        return true;
                    }
                    else
                    {
                        Console.WriteLine($"API call to generate voiceover failed. Status: {response.StatusCode}. Reason: {response.ReasonPhrase}");
                        Logger.LogError($"API call to generate voiceover failed. Status: {response.StatusCode}. Reason: {response.ReasonPhrase}");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception during API call to generate voiceover. Exception: {ex.Message}");
                    Logger.LogError($"Exception during API call to generate voiceover. Exception: {ex.Message}");
                    return false;
                }
            }
        }
    }
}
