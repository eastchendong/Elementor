using UnityEngine;
using System.Collections.Generic;

namespace Elementor.Core
{
    public static class CharacterDataLoader
    {
        private static Dictionary<string, CharacterData> characterDataCache = new Dictionary<string, CharacterData>();
        
        public static CharacterData LoadCharacterData(string characterName)
        {
            if (characterDataCache.ContainsKey(characterName))
            {
                return characterDataCache[characterName];
            }
            
            string jsonPath = $"CharacterData/{characterName}";
            TextAsset jsonFile = null;
            
            try 
            {
                jsonFile = Resources.Load<TextAsset>(jsonPath);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to load character data from Resources: {ex.Message}");
                Debug.LogWarning($"💡 For Android APK builds, ensure character data file exists at Resources/{jsonPath}.json");
            }
            
            if (jsonFile != null)
            {
                try
                {
                    CharacterData data = JsonUtility.FromJson<CharacterData>(jsonFile.text);
                    characterDataCache[characterName] = data;
                    Debug.Log($"Loaded character data for: {characterName}");
                    return data;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to parse JSON for character {characterName}: {e.Message}");
                    Debug.LogWarning("💡 For Android APK builds, ensure JSON format is valid and matches CharacterData structure");
                }
            }
            else
            {
                Debug.LogWarning($"Character data file not found: {jsonPath}");
                Debug.LogWarning("💡 For Android APK builds, ensure character JSON files are placed in Resources/CharacterData/ folder");
            }
            
            // Return default character data if loading fails
            return CreateDefaultCharacterData(characterName);
        }
        
        private static CharacterData CreateDefaultCharacterData(string characterName)
        {
            return new CharacterData
            {
                type = "NPC",
                name = characterName,
                prefabPath = $"Characters/{characterName}",
                groupId = "",
                personality = new CharacterPersonality
                {
                    speakingTrait = "speaks in a neutral tone",
                    voiceId = "21m00Tcm4TlvDq8ikWAM" // Default voice ID
                }
            };
        }
        
        public static void ClearCache()
        {
            characterDataCache.Clear();
        }
    }
}
