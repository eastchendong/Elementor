using UnityEngine;
using System;
using Oculus.Interaction;

namespace Elementor
{
    public class CharacterModel : MonoBehaviour
    {
        [SerializeField] private Character characterData;

        private CharacterAnimationState currentState = CharacterAnimationState.Idle;

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
                default:
                    return false;
            }
        }

        public void StartGrab()
        {
            if (CanTransitionTo(CharacterAnimationState.Grabbed))
            {
                SetAnimationState(CharacterAnimationState.Grabbed);
            }
        }

        public void EndGrab()
        {
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
    }
}