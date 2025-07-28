using System.Collections.Generic;
using UnityEngine;
using Oculus.Interaction;

namespace Elementor
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Grabbable))]
    public class CharacterGroup : MonoBehaviour
    {
        [SerializeField] private float slotCheckRadius = 1f;
        private List<CharacterView> characters = new List<CharacterView>();
        public List<CharacterView> Characters => characters;
        private CharacterSlot currentSlot;
        private Grabbable _grabbable;

        private void Awake()
        {
            _grabbable = GetComponent<Grabbable>();
        }

        private void OnEnable()
        {
            _grabbable.WhenPointerEventRaised += HandlePointerEvent;
        }

        private void OnDisable()
        {
            _grabbable.WhenPointerEventRaised -= HandlePointerEvent;
        }

        private void HandlePointerEvent(PointerEvent evt)
        {
            if (evt.Type == PointerEventType.Select)
            {
                StartGrab();
            }
            else if (evt.Type == PointerEventType.Unselect)
            {
                EndGrab();
            }
        }

        public void AddCharacter(CharacterView character)
        {
            if (!characters.Contains(character))
            {
                characters.Add(character);
                // Position characters relative to the group center
                character.transform.SetParent(transform, true);
                character.SetGroup(this);
                ArrangeCharacters();
            }
        }

        private void ArrangeCharacters()
        {
            // Arrange characters in a line formation
            for (int i = 0; i < characters.Count; i++)
            {
                // This is a simple horizontal line arrangement. You can customize the formation.
                float xOffset = (i - (characters.Count - 1) / 2.0f) * 0.5f;
                characters[i].transform.localPosition = new Vector3(xOffset, 0, 0);
                characters[i].transform.localRotation = Quaternion.identity;
            }
        }

        public void SetState(CharacterAnimationState state)
        {
            foreach (var character in characters)
            {
                character.GetModel().SetAnimationState(state);
            }
        }

        private void StartGrab()
        {
            if (currentSlot != null)
            {
                currentSlot.Release();
                currentSlot = null;
            }
            SetState(CharacterAnimationState.Grabbed);
        }

        private void EndGrab()
        {
            // Check for a nearby slot
            Collider[] colliders = Physics.OverlapSphere(transform.position, slotCheckRadius, LayerMask.GetMask("CharacterSlot"));
            foreach (var col in colliders)
            {
                CharacterSlot slot = col.GetComponent<CharacterSlot>();
                if (slot != null && !slot.IsOccupied)
                {
                    if (slot.Occupy(this))
                    {
                        currentSlot = slot;
                        // The Occupy method handles setting the state to Slotted
                        return;
                    }
                }
            }

            // If no slot found, return to idle
            SetState(CharacterAnimationState.Idle);
        }
    }
}
