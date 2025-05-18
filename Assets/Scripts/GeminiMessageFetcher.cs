using System.Collections;
using System.Collections.Generic; // <-- Add this line
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;
using SimpleJSON; // Add this at the top

public class GeminiMessageFetcher : MonoBehaviour
{
    public string apiKey = "AIzaSyDYhQFC5ulPJLaSRmI2s2w0TSV2xHMYXck";
    public string apiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key=";

    // Add this restriction string (can be made public if you want to edit in Inspector)
    private string promptRestrictions = @"
    Don't make the sentences too long and don't add any line breaks.
    Along with the response, also return three one word options for the player to choose from.
    in this JSON format:
    {
    ""mainMessage"": ""..."",
    ""options"": [option1, option2, option3]
    }
    Only return the JSON object, nothing else.";

    public IEnumerator GetGeminiMessage(string playerChoice, string customPrompt, System.Action<string> callback)
    {
        // The base prompt, always the same
        string basePrompt = @"Converse with the player as if you're John Pork, a humanoid pig. John pork is friendly with the player until they decline his call. When the player starts a conversation i want you to ask if the player will accept or decline your call. John pork becomes extremely aggresive and unforgiving and mean after the player declines.
Don't make the sentences too long and don't add any line breaks.
Along with the response, also return three one word options for the player to choose from.
in this JSON format:
{
""mainMessage"": ""..."",
""options"": [option1, option2, option3]
}";

        // Build the full prompt in your required format
        string fullPrompt = basePrompt + "\nConversation so far:\n" + customPrompt +
            $"\nDon't make the sentences too long and don't add any line breaks.\nAlong with the response, also return three one word options for the player to choose from.\nin this JSON format:\n{{\n\"mainMessage\": \"...\",\n\"options\": [option1, option2, option3]\n}}\nOnly return the JSON object, nothing else. The player chose: \"{playerChoice}\". Respond accordingly.";

        Debug.Log("Full Prompt: " + fullPrompt);

        string escapedPrompt = fullPrompt.Replace("\"", "\\\"");
        string jsonBody = "{\"contents\":[{\"parts\":[{\"text\":\"" + escapedPrompt + "\"}]}]}";

        UnityWebRequest request = new UnityWebRequest(apiUrl + apiKey, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string response = request.downloadHandler.text;
            Debug.Log("Gemini API Response: " + response);
            string message = ExtractMessageFromResponse(response);
            callback(message);
        }
        else
        {
            Debug.LogError("Gemini API Error: " + request.error + "\nResponse: " + request.downloadHandler.text);
            callback("Failed to get Gemini message.");
        }
    }

    private string ExtractMessageFromResponse(string json)
    {
        var root = JSON.Parse(json);
        var text = root?["candidates"]?[0]?["content"]?["parts"]?[0]?["text"];
        if (text == null) return "No message found.";

        // Remove markdown code block if present
        string raw = text.Value.Trim();
        var match = Regex.Match(raw, @"```json\s*(.*?)\s*```", RegexOptions.Singleline);
        if (match.Success)
            return match.Groups[1].Value.Trim();
        else
            return raw;
    }

    public class GeminiResponse
    {
        public string mainMessage;
        public List<string> options;
    }

    public GeminiResponse ParseGeminiResponse(string aiResponse)
    {
        // aiResponse is now the JSON string, not the whole markdown block
        var root = SimpleJSON.JSON.Parse(aiResponse);
        string mainMessage = root?["mainMessage"];
        List<string> options = new List<string>();

        var opts = root?["options"];
        if (opts != null)
        {
            foreach (var opt in opts.Children)
            {
                options.Add(opt.Value);
            }
        }
        return new GeminiResponse { mainMessage = mainMessage, options = options };
    }
}