using UnityEngine;
using System.IO;
using Elementor.Lore;
using UnityEngine.Networking;
using System.Collections;

namespace Elementor
{
    public class LoreJsonReader : MonoBehaviour
    {
        public static LoreJsonReader Instance { get; private set; }

        [Tooltip("The lore file to load from StreamingAssets folder.")]
        public string loreFilePath = "Generated_JSONs/hydrogen_combustion_tutorial.json"; // Updated to new format

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        [ContextMenu("Load Lore from JSON")]
        public void LoadLoreFromJson()
        {
            StartCoroutine(LoadLoreFromJsonCoroutine());
        }

        private IEnumerator LoadLoreFromJsonCoroutine()
        {
            if (LoreController.Instance == null)
            {
                Debug.LogError("LoreController instance not found.");
                yield break;
            }

            // First try to load from persistent data path (for runtime generated files)
            string persistentPath = Path.Combine(Application.persistentDataPath, loreFilePath);
            bool foundInPersistent = false;
            string jsonContent = "";

            if (File.Exists(persistentPath))
            {
                try
                {
                    jsonContent = File.ReadAllText(persistentPath);
                    foundInPersistent = true;
                    Debug.Log($"Loaded from persistent path: {persistentPath}");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Failed to read from persistent path: {ex.Message}");
                }
            }

            // If not found in persistent, try StreamingAssets
            if (!foundInPersistent)
            {
                string streamingPath = Path.Combine(Application.streamingAssetsPath, loreFilePath);
                Debug.Log($"Attempting to load lore from StreamingAssets: {streamingPath}");

                using (UnityWebRequest request = UnityWebRequest.Get(streamingPath))
                {
                    yield return request.SendWebRequest();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        jsonContent = request.downloadHandler.text;
                        Debug.Log($"Loaded from StreamingAssets: {streamingPath}");
                    }
                    else
                    {
                        Debug.LogError($"Failed to load from both persistent and StreamingAssets paths");
                        LogAvailableFiles();
                        yield break;
                    }
                }
            }

            // Process the loaded JSON content
            yield return StartCoroutine(ProcessLoreJsonContent(jsonContent));
        }

        private IEnumerator ProcessLoreJsonContent(string jsonContent)
        {
            try
            {
                Debug.Log($"Processing JSON content (first 200 chars): {jsonContent.Substring(0, Mathf.Min(200, jsonContent.Length))}...");
                
                // Clean JSON content (remove BOM and other potential issues)
                jsonContent = CleanJsonContent(jsonContent);
                
                // Validate JSON before parsing
                if (string.IsNullOrEmpty(jsonContent.Trim()))
                {
                    Debug.LogError("JSON content is empty or contains only whitespace");
                    yield break;
                }

                // Check if JSON starts with expected structure
                if (!jsonContent.Trim().StartsWith("{"))
                {
                    Debug.LogError("JSON does not start with valid object notation");
                    yield break;
                }

                LoreData loreData = JsonUtility.FromJson<LoreData>(jsonContent);

                if (loreData != null)
                {
                    // Validate essential fields
                    if (ValidateLoreData(loreData))
                    {
                        Debug.Log($"Successfully parsed lore data. Scene ID: {loreData.scene_id}");
                        Debug.Log($"Story Title: {loreData.story?.title}");
                        Debug.Log($"Reaction: {loreData.reaction?.equation}");
                        LoreController.Instance.LoadLore(loreData);
                    }
                    else
                    {
                        Debug.LogError("Lore data validation failed - missing essential fields");
                    }
                }
                else
                {
                    Debug.LogError("Failed to parse lore JSON - JsonUtility.FromJson returned null");
                    Debug.LogError($"JSON content preview: {jsonContent.Substring(0, Mathf.Min(500, jsonContent.Length))}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Exception while processing lore JSON: {ex.Message}");
                Debug.LogError($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Validates that essential lore data fields are present
        /// </summary>
        private bool ValidateLoreData(LoreData loreData)
        {
            if (string.IsNullOrEmpty(loreData.scene_id))
            {
                Debug.LogError("Missing scene_id in lore data");
                return false;
            }

            if (loreData.story == null)
            {
                Debug.LogError("Missing story data");
                return false;
            }

            if (string.IsNullOrEmpty(loreData.story.title))
            {
                Debug.LogError("Missing story title");
                return false;
            }

            if (loreData.reaction == null)
            {
                Debug.LogError("Missing reaction data");
                return false;
            }

            if (string.IsNullOrEmpty(loreData.reaction.equation))
            {
                Debug.LogError("Missing reaction equation");
                return false;
            }

            if (loreData.reaction.reactants == null || loreData.reaction.reactants.Count == 0)
            {
                Debug.LogError("Missing or empty reactants");
                return false;
            }

            if (loreData.reaction.products == null || loreData.reaction.products.Count == 0)
            {
                Debug.LogError("Missing or empty products");
                return false;
            }

            Debug.Log("Lore data validation passed");
            return true;
        }

        /// <summary>
        /// Logs available JSON files for debugging
        /// </summary>
        private void LogAvailableFiles()
        {
            string streamingAssetsPath = Application.streamingAssetsPath;
            if (Directory.Exists(streamingAssetsPath))
            {
                Debug.Log("Searching for available JSON files...");
                
                string[] jsonFiles = Directory.GetFiles(streamingAssetsPath, "*.json", SearchOption.AllDirectories);
                Debug.Log($"Available JSON files in StreamingAssets (recursive): {string.Join(", ", jsonFiles)}");
                
                // Check Generated_JSONs specifically
                string generatedJsonsPath = Path.Combine(streamingAssetsPath, "Generated_JSONs");
                if (Directory.Exists(generatedJsonsPath))
                {
                    string[] generatedFiles = Directory.GetFiles(generatedJsonsPath, "*.json");
                    Debug.Log($"JSON files in Generated_JSONs: {string.Join(", ", generatedFiles)}");
                    
                    // Show relative paths for easier usage
                    for (int i = 0; i < generatedFiles.Length; i++)
                    {
                        string relativePath = Path.GetRelativePath(streamingAssetsPath, generatedFiles[i]);
                        Debug.Log($"   - {relativePath}");
                    }
                }
                else
                {
                    Debug.LogWarning("Generated_JSONs folder not found in StreamingAssets");
                }
            }
            else
            {
                Debug.LogError("StreamingAssets folder not found");
            }
        }
        
        /// <summary>
        /// Load a specific lore file by filename
        /// </summary>
        public void LoadSpecificLoreFile(string fileName)
        {
            // Ensure the path includes Generated_JSONs folder if not already specified
            loreFilePath = fileName.Contains("Generated_JSONs") ? fileName : Path.Combine("Generated_JSONs", fileName);
            Debug.Log($"Loading specific lore file: {loreFilePath}");
            StartCoroutine(LoadLoreFromJsonCoroutine());
        }

        /// <summary>
        /// Load lore with direct JSON content
        /// </summary>
        public void LoadLoreFromJsonContent(string jsonContent)
        {
            if (LoreController.Instance == null)
            {
                Debug.LogError("LoreController instance not found.");
                return;
            }

            try
            {
                Debug.Log("Loading lore from direct JSON content");
                
                // Clean JSON content (remove BOM and other potential issues)
                jsonContent = CleanJsonContent(jsonContent);
                
                LoreData loreData = JsonUtility.FromJson<LoreData>(jsonContent);

                if (loreData != null && ValidateLoreData(loreData))
                {
                    Debug.Log($"Successfully loaded lore from content. Scene ID: {loreData.scene_id}");
                    LoreController.Instance.LoadLore(loreData);
                }
                else
                {
                    Debug.LogError("Failed to parse or validate lore from JSON content");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Exception while loading lore from content: {ex.Message}");
            }
        }

        /// <summary>
        /// Helper method to clean JSON content from BOM and other encoding issues
        /// </summary>
        private string CleanJsonContent(string content)
        {
            if (string.IsNullOrEmpty(content))
                return content;

            // Remove UTF-8 BOM if present
            if (content.StartsWith("\uFEFF"))
            {
                content = content.Substring(1);
                Debug.Log("Removed UTF-8 BOM from JSON content");
            }

            // Remove any leading/trailing whitespace
            content = content.Trim();

            // Log first few bytes for debugging
            if (content.Length > 0)
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(content.Substring(0, Mathf.Min(10, content.Length)));
                Debug.Log($"First bytes after cleaning: {string.Join(", ", System.Array.ConvertAll(bytes, b => b.ToString()))}");
            }

            return content;
        }

        /// <summary>
        /// Save lore data to persistent data path for runtime access
        /// </summary>
        public void SaveLoreDataToPersistent(string fileName, string jsonContent)
        {
            try
            {
                string persistentDir = Path.Combine(Application.persistentDataPath, "Generated_JSONs");
                if (!Directory.Exists(persistentDir))
                {
                    Directory.CreateDirectory(persistentDir);
                }

                string filePath = Path.Combine(persistentDir, fileName);
                File.WriteAllText(filePath, jsonContent);
                Debug.Log($"Saved lore data to persistent storage: {filePath}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to save lore data: {ex.Message}");
            }
        }
    }
}
