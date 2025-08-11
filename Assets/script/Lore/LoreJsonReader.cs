using UnityEngine;
using System.IO;
using Elementor.Lore;

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
            if (LoreController.Instance == null)
            {
                Debug.LogError("LoreController instance not found.");
                return;
            }

            string fullPath = Path.Combine(Application.streamingAssetsPath, loreFilePath);
            Debug.Log($"🔍 Attempting to load lore from: {fullPath}");

            if (File.Exists(fullPath))
            {
                try
                {
                    string jsonContent = File.ReadAllText(fullPath);
                    Debug.Log($"📖 Read JSON content (first 200 chars): {jsonContent.Substring(0, Mathf.Min(200, jsonContent.Length))}...");
                    
                    // Validate JSON before parsing
                    if (string.IsNullOrEmpty(jsonContent.Trim()))
                    {
                        Debug.LogError("❌ JSON file is empty or contains only whitespace");
                        return;
                    }

                    // Check if JSON starts with expected structure
                    if (!jsonContent.Trim().StartsWith("{"))
                    {
                        Debug.LogError("❌ JSON does not start with valid object notation");
                        return;
                    }

                    LoreData loreData = JsonUtility.FromJson<LoreData>(jsonContent);

                    if (loreData != null)
                    {
                        // Validate essential fields
                        if (ValidateLoreData(loreData))
                        {
                            Debug.Log($"✅ Successfully parsed lore data. Scene ID: {loreData.scene_id}");
                            Debug.Log($"📚 Story Title: {loreData.story?.title}");
                            Debug.Log($"⚗️ Reaction: {loreData.reaction?.equation}");
                            LoreController.Instance.LoadLore(loreData);
                        }
                        else
                        {
                            Debug.LogError("❌ Lore data validation failed - missing essential fields");
                        }
                    }
                    else
                    {
                        Debug.LogError("❌ Failed to parse lore JSON - JsonUtility.FromJson returned null");
                        Debug.LogError($"JSON content preview: {jsonContent.Substring(0, Mathf.Min(500, jsonContent.Length))}");
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"❌ Exception while loading lore JSON: {ex.Message}");
                    Debug.LogError($"Stack trace: {ex.StackTrace}");
                }
            }
            else
            {
                Debug.LogError($"❌ Lore file not found at path: {fullPath}");
                LogAvailableFiles();
            }
        }

        /// <summary>
        /// Validates that essential lore data fields are present
        /// </summary>
        private bool ValidateLoreData(LoreData loreData)
        {
            if (string.IsNullOrEmpty(loreData.scene_id))
            {
                Debug.LogError("🚫 Missing scene_id in lore data");
                return false;
            }

            if (loreData.story == null)
            {
                Debug.LogError("🚫 Missing story data");
                return false;
            }

            if (string.IsNullOrEmpty(loreData.story.title))
            {
                Debug.LogError("🚫 Missing story title");
                return false;
            }

            if (loreData.reaction == null)
            {
                Debug.LogError("🚫 Missing reaction data");
                return false;
            }

            if (string.IsNullOrEmpty(loreData.reaction.equation))
            {
                Debug.LogError("🚫 Missing reaction equation");
                return false;
            }

            if (loreData.reaction.reactants == null || loreData.reaction.reactants.Count == 0)
            {
                Debug.LogError("🚫 Missing or empty reactants");
                return false;
            }

            if (loreData.reaction.products == null || loreData.reaction.products.Count == 0)
            {
                Debug.LogError("🚫 Missing or empty products");
                return false;
            }

            Debug.Log("✅ Lore data validation passed");
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
                Debug.Log("📁 Searching for available JSON files...");
                
                string[] jsonFiles = Directory.GetFiles(streamingAssetsPath, "*.json", SearchOption.AllDirectories);
                Debug.Log($"📄 Available JSON files in StreamingAssets (recursive): {string.Join(", ", jsonFiles)}");
                
                // Check Generated_JSONs specifically
                string generatedJsonsPath = Path.Combine(streamingAssetsPath, "Generated_JSONs");
                if (Directory.Exists(generatedJsonsPath))
                {
                    string[] generatedFiles = Directory.GetFiles(generatedJsonsPath, "*.json");
                    Debug.Log($"📄 JSON files in Generated_JSONs: {string.Join(", ", generatedFiles)}");
                    
                    // Show relative paths for easier usage
                    for (int i = 0; i < generatedFiles.Length; i++)
                    {
                        string relativePath = Path.GetRelativePath(streamingAssetsPath, generatedFiles[i]);
                        Debug.Log($"   - {relativePath}");
                    }
                }
                else
                {
                    Debug.LogWarning("📁 Generated_JSONs folder not found in StreamingAssets");
                }
            }
            else
            {
                Debug.LogError("📁 StreamingAssets folder not found");
            }
        }
        
        /// <summary>
        /// Load a specific lore file by filename
        /// </summary>
        public void LoadSpecificLoreFile(string fileName)
        {
            // Ensure the path includes Generated_JSONs folder if not already specified
            loreFilePath = fileName.Contains("Generated_JSONs") ? fileName : Path.Combine("Generated_JSONs", fileName);
            Debug.Log($"🎯 Loading specific lore file: {loreFilePath}");
            LoadLoreFromJson();
        }

        /// <summary>
        /// Load lore with direct JSON content
        /// </summary>
        public void LoadLoreFromJsonContent(string jsonContent)
        {
            if (LoreController.Instance == null)
            {
                Debug.LogError("❌ LoreController instance not found.");
                return;
            }

            try
            {
                Debug.Log("📝 Loading lore from direct JSON content");
                LoreData loreData = JsonUtility.FromJson<LoreData>(jsonContent);

                if (loreData != null && ValidateLoreData(loreData))
                {
                    Debug.Log($"✅ Successfully loaded lore from content. Scene ID: {loreData.scene_id}");
                    LoreController.Instance.LoadLore(loreData);
                }
                else
                {
                    Debug.LogError("❌ Failed to parse or validate lore from JSON content");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ Exception while loading lore from content: {ex.Message}");
            }
        }
    }
}
