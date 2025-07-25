using UnityEngine;
using System;

namespace Elementor
{
    public class CharacterView : MonoBehaviour
    {
        [SerializeField] private CharacterModel characterModel;
        [SerializeField] private Animator animator;
        
        // Meta Quest手部交互预留接口
        public event Action<CharacterView> OnCharacterSelected;
        public event Action<CharacterView, Vector3> OnCharacterMoved;
        public event Action<CharacterView, CharacterAnimationState, CharacterAnimationState> OnAnimationStateChanged;
        
        private void Awake()
        {
            if (characterModel == null)
                characterModel = GetComponent<CharacterModel>();
            
            if (animator == null)
            {
                // 首先尝试在直接子物体中查找
                animator = GetComponentInChildren<Animator>();
                
                // 如果仍然没有找到，递归搜索所有子物体
                if (animator == null)
                {
                    animator = FindAnimatorInChildren(transform);
                }
            }
        }
        
        private Animator FindAnimatorInChildren(Transform parent)
        {
            // 检查当前物体
            Animator foundAnimator = parent.GetComponent<Animator>();
            if (foundAnimator != null)
                return foundAnimator;
            
            // 递归检查所有子物体
            for (int i = 0; i < parent.childCount; i++)
            {
                foundAnimator = FindAnimatorInChildren(parent.GetChild(i));
                if (foundAnimator != null)
                    return foundAnimator;
            }
            
            return null;
        }
        
        private void OnEnable()
        {
            if (characterModel != null)
                characterModel.OnAnimationStateChanged += HandleAnimationStateChanged;
        }
        
        private void OnDisable()
        {
            if (characterModel != null)
                characterModel.OnAnimationStateChanged -= HandleAnimationStateChanged;
        }
        
        public void SetCharacterModel(CharacterModel model)
        {
            // 如果之前有model，先取消订阅
            if (characterModel != null)
                characterModel.OnAnimationStateChanged -= HandleAnimationStateChanged;
            
            characterModel = model;
            
            // 订阅新model的事件
            if (characterModel != null)
                characterModel.OnAnimationStateChanged += HandleAnimationStateChanged;
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
        
        // Meta Quest交互接口 - 抓取开始
        public void OnHandGrab()
        {
            if (characterModel != null && characterModel.CanTransitionTo(CharacterAnimationState.Grabbed))
            {
                characterModel.SetAnimationState(CharacterAnimationState.Grabbed);
                OnCharacterSelected?.Invoke(this);
            }
        }
        
        // Meta Quest交互接口 - 拖拽中
        public void OnHandDrag(Vector3 newPosition)
        {
            transform.position = newPosition;
            OnCharacterMoved?.Invoke(this, newPosition);
            
            // 确保在拖拽时保持Grabbed状态
            if (characterModel != null && characterModel.CurrentAnimationState != CharacterAnimationState.Grabbed)
            {
                if (characterModel.CanTransitionTo(CharacterAnimationState.Grabbed))
                    characterModel.SetAnimationState(CharacterAnimationState.Grabbed);
            }
        }
        
        // Meta Quest交互接口 - 释放
        public void OnHandRelease()
        {
            if (characterModel != null && characterModel.CanTransitionTo(CharacterAnimationState.Idle))
            {
                characterModel.SetAnimationState(CharacterAnimationState.Idle);
            }
        }
        
        // 扩展接口 - 开始跑步
        public void StartRunning()
        {
            if (characterModel != null && characterModel.CanTransitionTo(CharacterAnimationState.Running))
            {
                characterModel.SetAnimationState(CharacterAnimationState.Running);
            }
        }
        
        // 扩展接口 - 停止跑步
        public void StopRunning()
        {
            if (characterModel != null && characterModel.CanTransitionTo(CharacterAnimationState.Idle))
            {
                characterModel.SetAnimationState(CharacterAnimationState.Idle);
            }
        }
        
        // 扩展接口 - 释放技能
        public void CastSkill()
        {
            if (characterModel != null && characterModel.CanTransitionTo(CharacterAnimationState.CastingSkill))
            {
                characterModel.SetAnimationState(CharacterAnimationState.CastingSkill);
                
                // 技能释放完成后自动回到Idle状态（可以通过协程或定时器实现）
                StartCoroutine(ReturnToIdleAfterSkill());
            }
        }
        
        private System.Collections.IEnumerator ReturnToIdleAfterSkill()
        {
            yield return new WaitForSeconds(2f); // 假设技能持续2秒
            
            if (characterModel != null && characterModel.CurrentAnimationState == CharacterAnimationState.CastingSkill)
            {
                characterModel.SetAnimationState(CharacterAnimationState.Idle);
            }
        }
        
        private void HandleAnimationStateChanged(CharacterAnimationState previousState, CharacterAnimationState newState)
        {
            Debug.Log($"{characterModel.GetCharacterName()} 动画状态从 {previousState} 转换到 {newState}");
            
            // View负责更新Animator
            UpdateAnimatorState(newState);
            
            OnAnimationStateChanged?.Invoke(this, previousState, newState);
        }
        
        private void UpdateAnimatorState(CharacterAnimationState newState)
        {
            if (animator == null) return;
            
            // 重置所有状态
            animator.SetBool("IsIdle", false);
            animator.SetBool("IsGrabbed", false);
            animator.SetBool("IsRunning", false);
            animator.SetBool("IsCastingSkill", false);
            
            // 设置当前状态
            switch (newState)
            {
                case CharacterAnimationState.Idle:
                    animator.SetBool("IsIdle", true);
                    break;
                case CharacterAnimationState.Grabbed:
                    animator.SetBool("IsGrabbed", true);
                    break;
                case CharacterAnimationState.Running:
                    animator.SetBool("IsRunning", true);
                    break;
                case CharacterAnimationState.CastingSkill:
                    animator.SetBool("IsCastingSkill", true);
                    break;
            }
        }
        
        public CharacterModel GetModel()
        {
            return characterModel;
        }
        
        public CharacterAnimationState GetCurrentAnimationState()
        {
            return characterModel?.CurrentAnimationState ?? CharacterAnimationState.Idle;
        }
    }
}
