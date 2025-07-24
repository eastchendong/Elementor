using UnityEngine;

namespace Elementor
{
    public class CharacterModel : MonoBehaviour
    {
        [SerializeField] private Character characterData;
        
        public Character CharacterData => characterData;
        
        public void Initialize(Character character)
        {
            characterData = character;
        }
        
        public string GetCharacterType()
        {
            return characterData?.type ?? "";
        }
        
        public string GetCharacterName()
        {
            return characterData?.name ?? "";
        }
    }
}
