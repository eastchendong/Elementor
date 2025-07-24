using UnityEngine;
using System;

namespace Elementor
{
    public class CharacterView : MonoBehaviour
    {
        [SerializeField] private CharacterModel characterModel;
        
        // Meta Quest手部交互预留接口
        public event Action<CharacterView> OnCharacterSelected;
        public event Action<CharacterView, Vector3> OnCharacterMoved;
        
        private void Awake()
        {
            if (characterModel == null)
                characterModel = GetComponent<CharacterModel>();
        }
        
        public void SetCharacterModel(CharacterModel model)
        {
            characterModel = model;
        }

        public void Initialize(Character character)
        {
            // 确保characterModel不为null
            if (characterModel == null)
            {
                characterModel = GetComponent<CharacterModel>();
                if (characterModel == null)
                {
                    Debug.LogError("CharacterModel组件未找到，无法初始化角色");
                    return;
                }
            }
            
            characterModel.Initialize(character);
            UpdateVisual();
        }
        
        private void UpdateVisual()
        {
            // 根据角色类型和名称更新可视化表现
            gameObject.name = $"{characterModel.GetCharacterType()}_{characterModel.GetCharacterName()}";
        }
        
        // Meta Quest交互预留接口
        public void OnHandGrab()
        {
            OnCharacterSelected?.Invoke(this);
        }
        
        public void OnHandDrag(Vector3 newPosition)
        {
            transform.position = newPosition;
            OnCharacterMoved?.Invoke(this, newPosition);
        }
        
        public void OnHandRelease()
        {
            // 释放时的逻辑
        }
        
        public CharacterModel GetModel()
        {
            return characterModel;
        }
    }
}
