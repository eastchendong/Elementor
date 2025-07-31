using UnityEngine;
using System.IO;
using Elementor.Lore;

namespace Elementor
{
    public class LoreJsonReader : MonoBehaviour
    {
        public static LoreJsonReader Instance { get; private set; }

        [Tooltip("The lore file to load from StreamingAssets folder.")]
        public string loreFilePath = "original_lore.json"; // Relative to StreamingAssets

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
                    
                    LoreData loreData = JsonUtility.FromJson<LoreData>(jsonContent);

                    if (loreData != null)
                    {
                        Debug.Log($"✅ Successfully parsed lore data. Scene ID: {loreData.scene_id}");
                        LoreController.Instance.LoadLore(loreData);
                    }
                    else
                    {
                        Debug.LogError("❌ Failed to parse lore JSON - parsed object is null.");
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
                
                // List available files in StreamingAssets for debugging
                string streamingAssetsPath = Application.streamingAssetsPath;
                if (Directory.Exists(streamingAssetsPath))
                {
                    string[] files = Directory.GetFiles(streamingAssetsPath, "*.json");
                    Debug.Log($"📁 Available JSON files in StreamingAssets: {string.Join(", ", files)}");
                }
            }
        }
        
        /// <summary>
        /// Load a specific lore file by filename
        /// </summary>
        public void LoadSpecificLoreFile(string fileName)
        {
            string oldPath = loreFilePath;
            loreFilePath = fileName;
            LoadLoreFromJson();

        }
    }
}
