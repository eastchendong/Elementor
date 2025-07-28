using System;
using UnityEngine;

namespace Elementor
{
    public enum CharacterAnimationState
    {
        Idle,
        Grabbed,
        Running,
        CastingSkill,
        Slotted,
    }

    [Serializable]
    public class Character
    {
        public string type;
        public string name;
        public string prefabPath;
        public string groupId;
        
        public Character(string type, string name, string prefabPath = "", string groupId = "")
        {
            this.type = type;
            this.name = name;
            this.prefabPath = prefabPath;
            this.groupId = groupId;
        }
    }
    
    [Serializable]
    public class CharacterSpawnData
    {
        public Character[] characters;
    }
}
