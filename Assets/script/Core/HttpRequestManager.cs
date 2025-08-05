using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;

namespace Elementor.Core
{
    public class HttpRequestManager : MonoBehaviour
    {
        public static HttpRequestManager Instance { get; private set; }

        private string apiKey => APIConfigManager.Config.openai_api_key;
        private string apiUrl => APIConfigManager.Config.openai_api_url;

        public bool IsRequesting { get; private set; } = false;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
        }

        public void SendRequest(string requestBody, System.Action<string> onSuccess, System.Action<string> onError)
        {
            if (IsRequesting)
            {
                Debug.LogWarning("Request is already in progress.");
                onError?.Invoke("Request already in progress");
                return;
            }

            StartCoroutine(SendRequestCoroutine(requestBody, onSuccess, onError));
        }

        IEnumerator SendRequestCoroutine(string requestBody, System.Action<string> onSuccess, System.Action<string> onError)
        {
            IsRequesting = true;

            UnityWebRequest request = new UnityWebRequest(apiUrl, "POST");
            request.redirectLimit = 10;
            request.timeout = 60;
            byte[] bodyRaw = Encoding.UTF8.GetBytes(requestBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + apiKey);
            request.SetRequestHeader("Accept", "application/json");

            Debug.Log("Sending request to: " + apiUrl);

            yield return request.SendWebRequest();

            IsRequesting = false;

            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseText = request.downloadHandler.text;
                Debug.Log("Request successful");
                onSuccess?.Invoke(responseText);
            }
            else
            {
                string errorMessage = $"Error: {request.error}, Code: {request.responseCode}";
                Debug.LogError(errorMessage);
                if (request.downloadHandler != null)
                {
                    Debug.LogError("Error Response Body: " + request.downloadHandler.text);
                }
                onError?.Invoke(errorMessage);
            }
        }

        // Helper method to sanitize strings for JSON
        public static string SanitizeJsonString(string input)
        {
            if (string.IsNullOrEmpty(input))
                return "";

            return input
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r\n", "\\n")
                .Replace("\n", "\\n")
                .Replace("\r", "\\n")
                .Replace("\t", "\\t")
                .Replace("\b", "\\b")
                .Replace("\f", "\\f");
        }

        // Helper method to extract JSON from API response
        public static string ExtractJsonFromResponse(string responseText)
        {
            try
            {
                var apiResponse = JsonUtility.FromJson<ApiResponse>(responseText);
                if (apiResponse?.choices != null && apiResponse.choices.Length > 0)
                {
                    string content = apiResponse.choices[0].message.content;
                    return CleanJsonContent(content);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to parse API response: {ex.Message}");
            }
            
            return CleanJsonContent(responseText);
        }
        
        private static string CleanJsonContent(string content)
        {
            if (string.IsNullOrEmpty(content))
                return "{}";
                
            content = content.Trim();
            
            if (content.StartsWith("```json"))
            {
                content = content.Substring(7);
            }
            else if (content.StartsWith("```"))
            {
                content = content.Substring(3);
            }
            
            if (content.EndsWith("```"))
            {
                content = content.Substring(0, content.Length - 3);
            }
            
            content = content.Trim();
            
            if (!content.StartsWith("{") || !content.EndsWith("}"))
            {
                Debug.LogWarning("Content doesn't appear to be valid JSON format");
                int startIndex = content.IndexOf('{');
                int endIndex = content.LastIndexOf('}');
                
                if (startIndex >= 0 && endIndex > startIndex)
                {
                    content = content.Substring(startIndex, endIndex - startIndex + 1);
                }
                else
                {
                    Debug.LogError("Could not extract valid JSON from response");
                    return "{}";
                }
            }
            
            return content;
        }

        [System.Serializable]
        public class ApiResponse
        {
            public Choice[] choices;
        }

        [System.Serializable]
        public class Choice
        {
            public Message message;
        }

        [System.Serializable]
        public class Message
        {
            public string content;
        }
    }
}
