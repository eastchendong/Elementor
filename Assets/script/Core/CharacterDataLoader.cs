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
            TextAsset jsonFile = Resources.Load<TextAsset>(jsonPath);
            
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
                }
            }
            else
            {
                Debug.LogWarning($"Character data file not found: {jsonPath}");
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
                    speakingTrait = "speaks in a neutral tone"
                }
            };
        }
        
        public static void ClearCache()
        {
            characterDataCache.Clear();
        }
    }
}
