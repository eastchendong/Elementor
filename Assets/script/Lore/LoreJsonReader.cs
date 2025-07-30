using UnityEngine;
using System.IO;
using Elementor.Lore;

namespace Elementor
{
    public class LoreJsonReader : MonoBehaviour
    {
        [Tooltip("The lore file to load from StreamingAssets folder.")]
        public string loreFilePath = "original_lore.json"; // Relative to StreamingAssets


        [ContextMenu("Load Lore from JSON")]
        public void LoadLoreFromJson()
        {
            if (LoreController.Instance == null)
            {
                Debug.LogError("LoreController instance not found.");
                return;
            }

            string fullPath = Path.Combine(Application.streamingAssetsPath, loreFilePath);

            if (File.Exists(fullPath))
            {
                string jsonContent = File.ReadAllText(fullPath);
                LoreData loreData = JsonUtility.FromJson<LoreData>(jsonContent);

                if (loreData != null)
                {
                    LoreController.Instance.LoadLore(loreData);
                }
                else
                {
                    Debug.LogError("Failed to parse lore JSON.");
                }
            }
            else
            {
                Debug.LogError($"Lore file not found at path: {fullPath}");
            }
        }
    }
}
