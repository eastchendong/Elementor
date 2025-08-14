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

            // Check network connectivity (Android-friendly)
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                Debug.LogError("No internet connection available");
                onError?.Invoke("No internet connection");
                return;
            }

            StartCoroutine(SendRequestCoroutine(requestBody, onSuccess, onError));
        }

        IEnumerator SendRequestCoroutine(string requestBody, System.Action<string> onSuccess, System.Action<string> onError)
        {
            IsRequesting = true;

            UnityWebRequest request = new UnityWebRequest(apiUrl, "POST");
            request.redirectLimit = 10;
            request.timeout = 90; // Increased timeout for mobile networks
            
            byte[] bodyRaw = null;
            try
            {
                bodyRaw = Encoding.UTF8.GetBytes(requestBody);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to encode request body: {ex.Message}");
                IsRequesting = false;
                onError?.Invoke("Failed to encode request");
                yield break;
            }

            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + apiKey);
            request.SetRequestHeader("Accept", "application/json");
            
            // Android-friendly: Add User-Agent header
            request.SetRequestHeader("User-Agent", $"Unity/{Application.unityVersion} (Android)");

            Debug.Log("Sending request to: " + apiUrl);
            Debug.Log($"Network reachability: {Application.internetReachability}");

            yield return request.SendWebRequest();

            IsRequesting = false;

            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseText = request.downloadHandler.text;
                Debug.Log("Request successful");
                
                // Validate response is not empty
                if (string.IsNullOrEmpty(responseText))
                {
                    Debug.LogWarning("Received empty response from server");
                    onError?.Invoke("Empty response from server");
                }
                else
                {
                    onSuccess?.Invoke(responseText);
                }
            }
            else
            {
                string errorMessage = $"Error: {request.error}, Code: {request.responseCode}";
                Debug.LogError(errorMessage);
                
                // More detailed error information for Android debugging
                switch (request.result)
                {
                    case UnityWebRequest.Result.ConnectionError:
                        errorMessage += " (Connection Error - Check network connectivity)";
                        break;
                    case UnityWebRequest.Result.ProtocolError:
                        errorMessage += " (Protocol Error - Server responded with error)";
                        break;
                    case UnityWebRequest.Result.DataProcessingError:
                        errorMessage += " (Data Processing Error - Invalid response data)";
                        break;
                }
                
                if (request.downloadHandler != null && !string.IsNullOrEmpty(request.downloadHandler.text))
                {
                    Debug.LogError("Error Response Body: " + request.downloadHandler.text);
                }
                
                onError?.Invoke(errorMessage);
            }

            // Ensure proper cleanup
            request.Dispose();
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
                // If parsing as API response fails, the responseText might already be clean JSON
                // Try to validate it directly
                string cleanedDirect = CleanJsonContent(responseText);
                if (!string.IsNullOrEmpty(cleanedDirect) && cleanedDirect != "{}")
                {
                    return cleanedDirect;
                }
            }
            
            return CleanJsonContent(responseText);
        }
        
        private static string CleanJsonContent(string content)
        {
            if (string.IsNullOrEmpty(content))
                return "{}";
            
            // Remove UTF-8 BOM if present
            if (content.StartsWith("\uFEFF"))
            {
                content = content.Substring(1);
                Debug.Log("Removed UTF-8 BOM from API response content");
            }
                
            content = content.Trim();
            
            // Remove markdown code blocks
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
            
            // If content already looks like valid JSON, try to validate it first
            if ((content.StartsWith("{") && content.EndsWith("}")) || 
                (content.StartsWith("[") && content.EndsWith("]")))
            {
                try
                {
                    // Try to parse it as a generic JSON structure to validate
                    // Since Unity's JsonUtility doesn't support object type, 
                    // we'll just check if it has proper bracket matching
                    if (IsValidJsonStructure(content))
                    {
                        Debug.Log("Content appears to be valid JSON, returning as-is");
                        return content;
                    }
                }
                catch
                {
                    Debug.Log("Content looks like JSON but failed validation, proceeding with cleanup");
                }
            }
            
            // Handle cases where response might contain extra text before/after JSON
            if (!content.StartsWith("{") && !content.StartsWith("["))
            {
                Debug.LogWarning("Content doesn't appear to start with valid JSON format");
                int startIndex = content.IndexOf('{');
                int arrayStartIndex = content.IndexOf('[');
                
                // Choose the earlier valid start
                int jsonStart = -1;
                if (startIndex >= 0 && arrayStartIndex >= 0)
                {
                    jsonStart = Mathf.Min(startIndex, arrayStartIndex);
                }
                else if (startIndex >= 0)
                {
                    jsonStart = startIndex;
                }
                else if (arrayStartIndex >= 0)
                {
                    jsonStart = arrayStartIndex;
                }
                
                if (jsonStart >= 0)
                {
                    content = content.Substring(jsonStart);
                }
            }
            
            // Find the end of JSON
            if (!content.EndsWith("}") && !content.EndsWith("]"))
            {
                Debug.LogWarning("Content doesn't appear to end with valid JSON format");
                int endIndex = content.LastIndexOf('}');
                int arrayEndIndex = content.LastIndexOf(']');
                
                // Choose the later valid end
                int jsonEnd = -1;
                if (endIndex >= 0 && arrayEndIndex >= 0)
                {
                    jsonEnd = Mathf.Max(endIndex, arrayEndIndex);
                }
                else if (endIndex >= 0)
                {
                    jsonEnd = endIndex;
                }
                else if (arrayEndIndex >= 0)
                {
                    jsonEnd = arrayEndIndex;
                }
                
                if (jsonEnd >= 0)
                {
                    content = content.Substring(0, jsonEnd + 1);
                }
            }
            
            // Check if we have valid JSON structure
            if (content.StartsWith("{") && content.EndsWith("}"))
            {
                return content; // Valid JSON object
            }
            else if (content.StartsWith("[") && content.EndsWith("]"))
            {
                // Valid JSON array, but we might need to wrap it for dialogue response
                Debug.Log("Detected JSON array, wrapping as dialogues object");
                return $"{{\"dialogues\": {content}}}";
            }
            else
            {
                // If no valid JSON found, try to wrap plain text as dialogue content
                if (!string.IsNullOrEmpty(content) && content.Contains("[") && content.Contains("]:"))
                {
                    // Format dialogue-like content into JSON
                    Debug.Log("Attempting to format dialogue text as JSON");
                    return $"{{\"raw_dialogue\": \"{SanitizeJsonString(content)}\"}}";
                }
                
                Debug.LogWarning($"Could not extract valid JSON from response: {content.Substring(0, Mathf.Min(100, content.Length))}...");
                return "{}";
            }
        }
        
        private static bool IsValidJsonStructure(string content)
        {
            if (string.IsNullOrEmpty(content))
                return false;
            
            content = content.Trim();
            
            // Basic bracket matching validation
            int braceCount = 0;
            int bracketCount = 0;
            bool inString = false;
            bool escaped = false;
            
            for (int i = 0; i < content.Length; i++)
            {
                char c = content[i];
                
                if (escaped)
                {
                    escaped = false;
                    continue;
                }
                
                if (c == '\\')
                {
                    escaped = true;
                    continue;
                }
                
                if (c == '"')
                {
                    inString = !inString;
                    continue;
                }
                
                if (!inString)
                {
                    if (c == '{') braceCount++;
                    else if (c == '}') braceCount--;
                    else if (c == '[') bracketCount++;
                    else if (c == ']') bracketCount--;
                    
                    // If counts go negative, invalid structure
                    if (braceCount < 0 || bracketCount < 0)
                        return false;
                }
            }
            
            // All brackets should be closed
            return braceCount == 0 && bracketCount == 0 && !inString;
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
