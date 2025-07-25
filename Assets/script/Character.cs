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
        // 可以继续添加更多状态
    }

    [Serializable]
    public class Character
    {
        public string type;
        public string name;
        public string prefabPath; // 添加prefab路径字段
        
        public Character(string type, string name, string prefabPath = "")
        {
            this.type = type;
            this.name = name;
            this.prefabPath = prefabPath;
        }
    }
    
    [Serializable]
    public class CharacterSpawnData
    {
        public Character[] characters;
    }
}
