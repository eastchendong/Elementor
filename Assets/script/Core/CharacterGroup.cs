using System.Collections.Generic;
using UnityEngine;
using Oculus.Interaction;

namespace Elementor
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Grabbable))]
    public class CharacterGroup : MonoBehaviour
    {
        private List<CharacterView> characters = new List<CharacterView>();
        public List<CharacterView> Characters => characters;
        private CharacterSlot currentSlot;
        private Grabbable _grabbable;
        private CharacterSlot potentialSlot; // The slot trigger we are currently inside

        private void Awake()
        {
            _grabbable = GetComponent<Grabbable>();
            // Ensure the group has a collider for trigger detection
            Collider collider = GetComponent<Collider>();
            if (collider == null)
            {
                Debug.LogError("CharacterGroup requires a Collider component for trigger detection.");
            }
            else if (!collider.isTrigger)
            {
                collider.isTrigger = true; // Ensure the collider is set as a trigger
            }
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

        public void ClearAndDestroy()
        {
            foreach (var character in characters)
            {
                character.transform.SetParent(null, true);
                character.SetGroup(null);
            }
            characters.Clear();
            Destroy(gameObject);
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
            GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
            SetState(CharacterAnimationState.Grabbed);
        }

        private void EndGrab()
        {
            // Check if the group is inside a valid slot
            if (potentialSlot != null && !potentialSlot.IsOccupied)
            {
                if (potentialSlot.Occupy(this))
                {
                    currentSlot = potentialSlot;
                    // The Occupy method in CharacterSlot now handles freezing the rigidbody.
                    return;
                }
            }

            // If no valid slot, return to idle
            GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeRotation;
            SetState(CharacterAnimationState.Idle);
        }

        private void OnTriggerEnter(Collider other)
        {
            // Check if the trigger is a CharacterSlot
            if (other.TryGetComponent<CharacterSlot>(out var slot))
            {
                potentialSlot = slot;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            // Check if the exiting trigger is the current potential slot
            if (other.TryGetComponent<CharacterSlot>(out var slot) && potentialSlot == slot)
            {
                potentialSlot = null;
            }
        }
    }
}
