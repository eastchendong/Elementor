using UnityEngine;
using System.IO;
using Elementor.Lore;

namespace Elementor
{
    public class LoreJsonReader : MonoBehaviour
    {
        public static LoreJsonReader Instance { get; private set; }

        [Tooltip("The lore file to load from StreamingAssets folder.")]
        public string loreFilePath = "Generated_JSONs/character_spawn_config.json"; // Updated default path

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
            Debug.Log($"Attempting to load lore from: {fullPath}");

            if (File.Exists(fullPath))
            {
                try
                {
                    string jsonContent = File.ReadAllText(fullPath);
                    Debug.Log($"Read JSON content (first 200 chars): {jsonContent.Substring(0, Mathf.Min(200, jsonContent.Length))}...");
                    
                    LoreData loreData = JsonUtility.FromJson<LoreData>(jsonContent);

                    if (loreData != null)
                    {
                        Debug.Log($"Successfully parsed lore data. Scene ID: {loreData.scene_id}");
                        LoreController.Instance.LoadLore(loreData);
                    }
                    else
                    {
                        Debug.LogError("Failed to parse lore JSON - parsed object is null.");
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Exception while loading lore JSON: {ex.Message}");
                    Debug.LogError($"Stack trace: {ex.StackTrace}");
                }
            }
            else
            {
                Debug.LogError($"Lore file not found at path: {fullPath}");
                
                // List available files in StreamingAssets and Generated_JSONs for debugging
                string streamingAssetsPath = Application.streamingAssetsPath;
                if (Directory.Exists(streamingAssetsPath))
                {
                    string[] jsonFiles = Directory.GetFiles(streamingAssetsPath, "*.json", SearchOption.AllDirectories);
                    Debug.Log($"Available JSON files in StreamingAssets (recursive): {string.Join(", ", jsonFiles)}");
                    
                    // Also check Generated_JSONs specifically
                    string generatedJsonsPath = Path.Combine(streamingAssetsPath, "Generated_JSONs");
                    if (Directory.Exists(generatedJsonsPath))
                    {
                        string[] generatedFiles = Directory.GetFiles(generatedJsonsPath, "*.json");
                        Debug.Log($"JSON files in Generated_JSONs: {string.Join(", ", generatedFiles)}");
                    }
                }
            }
        }
        
        /// <summary>
        /// Load a specific lore file by filename
        /// </summary>
        public void LoadSpecificLoreFile(string fileName)
        {
            string oldPath = loreFilePath;
            // Ensure the path includes Generated_JSONs folder if not already specified
            loreFilePath = fileName.Contains("Generated_JSONs") ? fileName : Path.Combine("Generated_JSONs", fileName);
            LoadLoreFromJson();

        }
    }
}
