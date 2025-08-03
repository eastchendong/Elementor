using UnityEngine;
using System;
using Oculus.Interaction;

namespace Elementor
{
    [RequireComponent(typeof(Rigidbody))]
    public class CharacterView : MonoBehaviour
    {
        [SerializeField] private CharacterModel characterModel;
        [SerializeField] private Animator animator;        
        public event Action<CharacterView> OnCharacterSelected;
        public event Action<CharacterView, Vector3> OnCharacterMoved;
        public event Action<CharacterView, CharacterAnimationState, CharacterAnimationState> OnAnimationStateChanged;
        
        private Animator FindAnimatorInChildren(Transform parent)
        {
            Animator foundAnimator = parent.GetComponent<Animator>();
            if (foundAnimator != null)
                return foundAnimator;

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
            if (characterModel != null)
                characterModel.OnAnimationStateChanged -= HandleAnimationStateChanged;
            
            characterModel = model;
            
            if (characterModel != null)
                characterModel.OnAnimationStateChanged += HandleAnimationStateChanged;
        }

        public void Initialize()
        {
            if (characterModel == null)
            {
                characterModel = GetComponent<CharacterModel>();
                if (characterModel == null)
                {
                    Debug.LogError("CharacterModel组件未找到，无法初始化角色");
                    return;
                }
            }

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
                if (animator == null)
                {
                    animator = FindAnimatorInChildren(transform);
                }
            }
            
            // 确保其他组件存在
            GetComponent<Rigidbody>();

            UpdateVisual();
        }
        
        private void UpdateVisual()
        {
            // 根据角色类型和名称更新可视化表现
            gameObject.name = $"{characterModel.GetCharacterType()}_{characterModel.GetCharacterName()}";
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
        
        // 扩展接口 - 技能释放完成
        public void ReleaseFromSkill()
        {
            if (characterModel != null && characterModel.CurrentAnimationState == CharacterAnimationState.CastingSkill)
            {
                transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
                characterModel.SetAnimationState(CharacterAnimationState.Idle);
            }
        }
        
        private System.Collections.IEnumerator ReturnToIdleAfterSkill()
        {
            yield return new WaitForSeconds(2f); // 假设技能持续2秒
            ReleaseFromSkill();
        }

        private void HandleAnimationStateChanged(CharacterAnimationState previousState, CharacterAnimationState newState)
        {
            Debug.Log($"{characterModel.GetCharacterName()} 动画状态从 {previousState} 转换到 {newState}");
            
            UpdateAnimatorState(newState);
            
            // 根据状态变化触发相应事件
            switch (newState)
            {
                case CharacterAnimationState.Grabbed:
                    OnCharacterSelected?.Invoke(this);
                    break;
                case CharacterAnimationState.Idle:
                    if (previousState == CharacterAnimationState.Grabbed)
                    {
                        Debug.Log($"{characterModel.GetCharacterName()} 已被释放");
                    }
                    break;
            }
            
            OnAnimationStateChanged?.Invoke(this, previousState, newState);
        }
        
        private void UpdateAnimatorState(CharacterAnimationState newState)
        {
            if (animator == null)
            {
            Debug.LogError("Animator is not assigned in CharacterView.");
            return;
            }
            
            // 重置所有状态
            animator.SetBool("IsIdle", false);
            animator.SetBool("IsGrabbed", false);
            animator.SetBool("IsRunning", false);
            animator.SetBool("IsCastingSkill", false);
            animator.SetBool("IsSlotted", false);
            
            // 设置当前状态
            switch (newState)
            {
            case CharacterAnimationState.Idle:
            case CharacterAnimationState.Falling: // Falling can use Idle animation
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
            case CharacterAnimationState.Slotted:
                animator.SetBool("IsSlotted", true);
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

