using UnityEngine;
using System;
using Oculus.Interaction;

namespace Elementor
{
    public class CharacterModel : MonoBehaviour
    {
        [SerializeField] private Character characterData;

        private CharacterAnimationState currentState = CharacterAnimationState.Idle;
        private CharacterSlot currentSlot;
        private CharacterSlot potentialSlot; // The slot trigger we are currently inside
        private CharacterGroup characterGroup;

        public Character CharacterData => characterData;
        public CharacterAnimationState CurrentAnimationState => currentState;
        public event Action<CharacterAnimationState, CharacterAnimationState> OnAnimationStateChanged;

        public void Initialize(Character character)
        {
            characterData = character;
            SetAnimationState(CharacterAnimationState.Idle);
        }

        public void SetAnimationState(CharacterAnimationState newState)
        {
            if (currentState == newState) return;

            CharacterAnimationState previousState = currentState;
            currentState = newState;

            // 触发状态改变事件，让View处理动画
            OnAnimationStateChanged?.Invoke(previousState, newState);
        }

        public bool CanTransitionTo(CharacterAnimationState targetState)
        {
            // 定义状态转换规则
            switch (currentState)
            {
                case CharacterAnimationState.Idle:
                    return true; // Idle可以转换到任何状态
                case CharacterAnimationState.Grabbed:
                    return targetState != CharacterAnimationState.Running; // 被抓住时不能跑步
                case CharacterAnimationState.Running:
                    return targetState != CharacterAnimationState.Grabbed; // 跑步时不能被抓住
                case CharacterAnimationState.CastingSkill:
                    return targetState == CharacterAnimationState.Idle; // 释放技能后只能回到Idle
                case CharacterAnimationState.Slotted:
                    return targetState == CharacterAnimationState.Grabbed; // 在槽里只能被抓取
                default:
                    return false;
            }
        }

        public void StartGrab()
        {
            if (currentSlot != null)
            {
                currentSlot.Release();
                currentSlot = null;
            }
            
            if (CanTransitionTo(CharacterAnimationState.Grabbed))
            {
                if (characterGroup != null)
                {
                    characterGroup.SetState(CharacterAnimationState.Grabbed);
                }
                else
                {
                    SetAnimationState(CharacterAnimationState.Grabbed);
                }
            }
        }

        public void EndGrab()
        {
            if (characterGroup != null)
            {
                // 团队的 EndGrab 逻辑应该在团队的 Grabbable 组件上处理
                return;
            }

            if (potentialSlot != null && !potentialSlot.IsOccupied)
            {
                if (potentialSlot.Occupy(GetComponent<CharacterView>()))
                {
                    currentSlot = potentialSlot;
                    SetAnimationState(CharacterAnimationState.Slotted); 
                    return;
                }
            }
            
            if (CanTransitionTo(CharacterAnimationState.Idle))
            {
                SetAnimationState(CharacterAnimationState.Idle);
            }
        }

        public bool IsGrabbed()
        {
            return currentState == CharacterAnimationState.Grabbed;
        }

        public string GetCharacterType()
        {
            return characterData?.type ?? "";
        }

        public string GetCharacterName()
        {
            return characterData?.name ?? "";
        }

        public void SetGroup(CharacterGroup group)
        {
            characterGroup = group;
        }

        private void OnTriggerEnter(Collider other)
        {
            // 检查进入的是否是插槽
            if (other.TryGetComponent<CharacterSlot>(out var slot))
            {
                potentialSlot = slot;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            // 检查离开的是否是之前记录的插槽
            if (other.TryGetComponent<CharacterSlot>(out var slot) && potentialSlot == slot)
            {
                potentialSlot = null;
            }
        }
    }
}