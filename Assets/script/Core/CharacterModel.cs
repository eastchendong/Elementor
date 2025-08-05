using UnityEngine;
using System;
using Oculus.Interaction;
using System.Collections;
using Elementor.Core.Speech;

namespace Elementor.Core
{
    public class CharacterModel : MonoBehaviour
    {
        [SerializeField] private Character characterData;

        private CharacterAnimationState currentState = CharacterAnimationState.Idle;
        private CharacterSlot currentSlot;
        private CharacterSlot potentialSlot;
        private CharacterGroup characterGroup;
        public CharacterAnimationState CurrentAnimationState => currentState;
        public event Action<CharacterAnimationState, CharacterAnimationState> OnAnimationStateChanged;

        [SerializeField] public GameObject HandGrabbableController;

        public void Initialize(Character character)
        {
            characterData = character;
            SetAnimationState(CharacterAnimationState.Idle);
            CharacterSpeech speech = GetComponent<CharacterSpeech>();
            if (speech != null)
            {
                speech.characterVoiceId = characterData?.personality.voiceId ?? "NULL";
            }
        }

        public void SetAnimationState(CharacterAnimationState newState)
        {
            if (currentState == newState) return;

            CharacterAnimationState previousState = currentState;
            currentState = newState;
            
            OnAnimationStateChanged?.Invoke(previousState, newState);
            
            if (characterGroup != null)
            {
                Debug.Log($"[{gameObject.name}] Part of a group, skipping individual physics management.");
                return;
            }
            
            // Only manage individual physics if not part of a group
            var rb = GetComponent<Rigidbody>();
            if (rb == null) 
            {
                Debug.LogError($"Rigidbody not found on {gameObject.name}");
                return;
            }

            switch (newState)
            {
                case CharacterAnimationState.Idle:
                    rb.isKinematic = true;
                    rb.useGravity = false;
                    transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
                    rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
                    Debug.Log($"[{gameObject.name}] Idle state set: isKinematic={rb.isKinematic}, useGravity={rb.useGravity}");
                    break;
                case CharacterAnimationState.Running:
                case CharacterAnimationState.CastingSkill:
                case CharacterAnimationState.Slotted:
                    rb.isKinematic = true;
                    rb.useGravity = false;
                    rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
                    break;
                case CharacterAnimationState.Grabbed:
                    rb.isKinematic = false;
                    rb.useGravity = false;
                    rb.constraints = RigidbodyConstraints.None;
                    break;
                case CharacterAnimationState.Falling:
                    rb.isKinematic = false;
                    rb.useGravity = true;
                    rb.constraints = RigidbodyConstraints.None;
                    StartCoroutine(CheckIfSettled());
                    break;
            }
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
                case CharacterAnimationState.Falling:
                    return targetState == CharacterAnimationState.Idle || targetState == CharacterAnimationState.Grabbed; // 下落时可以被再次抓住或恢复静止
                default:
                    return false;
            }
        }

        public void StartGrab()
        {
            if (characterGroup != null)
            {
                characterGroup.StartGrab();
                return;
            }
            
            if (currentSlot != null)
            {
                currentSlot.Release();
                currentSlot = null;
            }
            
            if (CanTransitionTo(CharacterAnimationState.Grabbed))
            {
                SetAnimationState(CharacterAnimationState.Grabbed);
            }
        }

        public void EndGrab()
        {
            if (characterGroup != null)
            {
                characterGroup.EndGrab();
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
            
            transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);

            if (CanTransitionTo(CharacterAnimationState.Falling))
            {
                StartCoroutine(DelayedSetFallingState());
            }
        }

        private IEnumerator DelayedSetFallingState()
        {
            yield return null;
            
            SetAnimationState(CharacterAnimationState.Falling);
            
            // 强制确保物理设置正确
            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.constraints = RigidbodyConstraints.None;
                Debug.Log($"[{gameObject.name}] Forced physics settings: isKinematic={rb.isKinematic}, useGravity={rb.useGravity}");
            }
        }

        private IEnumerator CheckIfSettled()
        {
            var rb = GetComponent<Rigidbody>();
            if (rb == null) yield break;

            // 等待一小段时间，避免释放瞬间就判断为稳定
            yield return new WaitForSeconds(1f);

            while (rb.velocity.sqrMagnitude > 0.01f)
            {
                yield return new WaitForFixedUpdate();
            }

            // 速度足够小，认为已经稳定
            if (currentState == CharacterAnimationState.Falling)
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

        public CharacterPersonality GetPersonality()
        {
            return characterData?.personality ?? new CharacterPersonality();
        }

        public Character GetCharacterData()
        {
            return characterData;
        }

        public void SetGroup(CharacterGroup group)
        {
            characterGroup = group;
        }
        
        public void ClearGroup()
        {
            characterGroup = null;
        }   

        private void OnTriggerEnter(Collider other)
        {
            // If part of a group, ignore individual trigger detection
            if (characterGroup != null) return;

            if (other.TryGetComponent<CharacterSlot>(out var slot))
            {
                potentialSlot = slot;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            // If part of a group, ignore individual trigger detection
            if (characterGroup != null) return;
            
            if (other.TryGetComponent<CharacterSlot>(out var slot) && potentialSlot == slot)
            {
                potentialSlot = null;
            }
        }

        public void DisableIndividualPhysics()
        {
            HandGrabbableController.SetActive(false);

            BoxCollider boxCollider = GetComponent<BoxCollider>();
            if (boxCollider != null)
            {
                boxCollider.isTrigger = true;
            }

            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
                rb.constraints = RigidbodyConstraints.FreezeAll;
                Debug.Log($"[{gameObject.name}] Individual physics disabled for group");
            }
        }

        public void EnableIndividualPhysics()
        {
            HandGrabbableController.SetActive(true);

            BoxCollider boxCollider = GetComponent<BoxCollider>();
            if (boxCollider != null)
            {
                boxCollider.isTrigger = false;
            }

            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                Debug.Log($"[{gameObject.name}] Individual physics re-enabled");
                // Restore appropriate physics based on current state
                // Temporarily clear group reference to allow physics control
                var tempGroup = characterGroup;
                characterGroup = null;
                SetAnimationState(currentState);
                characterGroup = tempGroup;
            }
        }
    }
}