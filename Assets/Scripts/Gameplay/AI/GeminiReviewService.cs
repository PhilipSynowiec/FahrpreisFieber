using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class GeminiReviewService : MonoBehaviour
{
    [SerializeField] private string apiKey;
    [SerializeField] private string geminiModel = "gemini-2.5-flash"; // User confirmed this works in other projects

    private const string ApiUrl = "https://generativelanguage.googleapis.com/v1beta/models/{0}:generateContent?key={1}";

    public void GenerateReview(float timeRatio, bool onTime, Action<string> callback)
    {
        StartCoroutine(PostRequest(timeRatio, onTime, callback));
    }

    private IEnumerator PostRequest(float timeRatio, bool onTime, Action<string> callback)
    {
        // timeRatio: > 0 means early (good), < 0 means late (bad)
        string performanceDesc = onTime 
            ? $"The driver was {timeRatio * 100:F0}% faster than expected." 
            : $"The driver was {Mathf.Abs(timeRatio * 100):F0}% slower than expected (LATE).";

        string prompt = $"You're a passenger writing a funny taxi review. {performanceDesc} " +
                        $"Write 1-2 sentences (max 20 words). Be creative and varied - reference driving style, speed, traffic, the city, " +
                        $"or make unexpected comparisons. Avoid repetitive jokes. If fast: praise creatively. If late: complain humorously. No hashtags.";

        // Construct JSON payload manually to avoid complex nested classes for JsonUtility
        string jsonBody = $@"
        {{
            ""contents"": [
                {{
                    ""parts"": [
                        {{
                            ""text"": ""{EscapeJson(prompt)}""
                        }}
                    ]
                }}
            ]
        }}";

        string url = string.Format(ApiUrl, geminiModel, apiKey);

        using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                if (www.responseCode == 429)
                {
                    Debug.LogWarning("Gemini API Rate Limit Exceeded (429). Using fallback review.");
                }
                else
                {
                    Debug.LogError($"Gemini API Error: {www.error}\nResponse: {www.downloadHandler.text}");
                }
                callback?.Invoke(GetFallbackReview(onTime));
            }
            else
            {
                string responseText = www.downloadHandler.text;
                string review = ParseGeminiResponse(responseText);
                
                if (string.IsNullOrEmpty(review))
                {
                    callback?.Invoke(GetFallbackReview(onTime));
                }
                else
                {
                    callback?.Invoke(review.Trim());
                }
            }
        }
    }

    private string ParseGeminiResponse(string json)
    {
        try
        {
            // Simple parsing to avoid full class structure for just one string
            // We look for "text": "..." inside the response
            int textIndex = json.IndexOf("\"text\": \"");
            if (textIndex == -1) return null;

            textIndex += 9; // length of "text": "
            int endIndex = json.IndexOf("\"", textIndex);
            
            if (endIndex == -1) return null;

            string content = json.Substring(textIndex, endIndex - textIndex);
            return UnescapeJson(content);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error parsing Gemini response: {e.Message}");
            return null;
        }
    }

    private string GetFallbackReview(bool onTime)
    {
        if (onTime)
            return "Fast ride! 5 stars!";
        else
            return "Too slow! My grandma drives faster.";
    }

    private string EscapeJson(string s)
    {
        if (s == null) return "";
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "");
    }

    private string UnescapeJson(string s)
    {
        if (s == null) return "";
        return s.Replace("\\\"", "\"").Replace("\\n", "\n").Replace("\\\\", "\\");
    }
}
