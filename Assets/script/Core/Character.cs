using System;
using UnityEngine;

namespace Elementor.Core
{
    public enum CharacterAnimationState
    {
        Idle,
        Grabbed,
        Running,
        CastingSkill,
        Slotted,
        Falling,
    }

    [Serializable]
    public class CharacterPersonality
    {
        [SerializeField] public string speakingTrait = "说话温和友善"; // Default speaking trait
        [SerializeField] public string voiceId = "NULL"; // Default ElevenLabs voice ID
        
        public CharacterPersonality()
        {
            speakingTrait = "说话温和友善"; // Ensure default value
            voiceId = "NULL"; // Ensure default voice ID
        }
    }

    [Serializable]
    public class CharacterData
    {
        public string type;
        public string name;
        public string prefabPath;
        public string groupId;
        [SerializeField] public CharacterPersonality personality;
        [SerializeField] public bool showNameUI = true;
        [SerializeField] public string displayName; // Custom display name, falls back to 'name' if empty
        
        // Valid types for special effects
        public static readonly string[] ValidEffectTypes = {
            "金属", "非金属", "金属氧化物", "非金属氧化物", "酸", "碱", "盐"
        };
        
        public CharacterData()
        {
            personality = new CharacterPersonality();
            showNameUI = true;
            displayName = "";
        }
        
        public string GetDisplayName()
        {
            return string.IsNullOrEmpty(displayName) ? name : displayName;
        }
        
        public bool HasSpecialEffects()
        {
            return System.Array.Exists(ValidEffectTypes, t => t == type);
        }
        
        public Character ToCharacter()
        {
            Character character = new Character(type, name, prefabPath, groupId);
            character.personality = personality ?? new CharacterPersonality();
            return character;
        }
    }

    [Serializable]
    public class Character
    {
        public string type;
        public string name;
        public string prefabPath;
        public string groupId;
        [SerializeField] public CharacterPersonality personality;
        [SerializeField] public bool showNameUI = true;
        [SerializeField] public string displayName;
        
        public Character(string type, string name, string prefabPath = "", string groupId = "")
        {
            this.type = type;
            this.name = name;
            this.prefabPath = prefabPath;
            this.groupId = groupId;
            this.personality = new CharacterPersonality();
            this.showNameUI = true;
            this.displayName = "";
        }
        
        public string GetDisplayName()
        {
            return string.IsNullOrEmpty(displayName) ? name : displayName;
        }
        
        public bool HasSpecialEffects()
        {
            return System.Array.Exists(CharacterData.ValidEffectTypes, t => t == type);
        }
        
        public static Character CreateFromData(CharacterData data)
        {
            return data.ToCharacter();
        }
    }
    
    [Serializable]
    public class CharacterSpawnData
    {
        public string[] characterNames;
    }
}