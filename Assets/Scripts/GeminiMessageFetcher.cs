using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using SimpleJSON; // Add this at the top

public class GeminiMessageFetcher : MonoBehaviour
{
    public string apiKey = "AIzaSyDYhQFC5ulPJLaSRmI2s2w0TSV2xHMYXck";
    public string apiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key=";

    public IEnumerator GetGeminiMessage(string playerChoice, System.Action<string> callback)
    {
        string basePrompt = "Converse with the player as if youre john pork. John pork is friendly" +
            " until the player declines his call" +
            " and then he becomes hostile. John pork will ask the player whether they will answer or decline his call." +
            " Don't make the sentences too long and don't add any line breaks.";

        // Add the player's choice as context
        string fullPrompt = basePrompt + " The player chose: \"" + playerChoice + "\". Respond as John Pork accordingly.";

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
        // Gemini's response structure: { "candidates": [ { "content": { "parts": [ { "text": "..." } ] } } ] }
        var text = root?["candidates"]?[0]?["content"]?["parts"]?[0]?["text"];
        if (text == null) return "No message found.";
        return text.Value;
    }
}