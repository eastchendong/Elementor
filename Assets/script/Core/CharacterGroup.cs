using System.Collections.Generic;
using UnityEngine;
using Oculus.Interaction;
using System.Collections;

namespace Elementor.Core
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Grabbable))]
    public class CharacterGroup : MonoBehaviour
    {
        [Header("Group Settings")]
        [SerializeField] [Range(0.1f, 2.0f)] private float colliderSizePercentage = 0.75f;
        [SerializeField] [Range(0.1f, 2.0f)] private float characterSpacing = 0.1f;
        
        private List<CharacterView> characters = new List<CharacterView>();
        public List<CharacterView> Characters => characters;
        private CharacterSlot currentSlot;
        private CharacterSlot potentialSlot;

        private void Awake()
        {
            Collider collider = GetComponent<Collider>();
            if (collider == null)
            {
                Debug.LogError("CharacterGroup requires a Collider component for trigger detection.");
            }
            else if (!collider.isTrigger)
            {
                collider.isTrigger = false;
            }
            
            UpdateColliderSize();
        }



        public void AddCharacter(CharacterView character)
        {
            if (!characters.Contains(character))
            {
                characters.Add(character);
                character.transform.SetParent(transform, true);

                character.GetModel().SetGroup(this);
                character.GetModel().DisableIndividualPhysics();
                
                ArrangeCharacters();
                UpdateColliderSize();
            }
        }

        public void RemoveCharacter(CharacterView character)
        {
            if (characters.Contains(character))
            {
                characters.Remove(character);
                character.transform.SetParent(null, true);

                character.GetModel().ClearGroup();
                character.GetModel().EnableIndividualPhysics();
                
                UpdateColliderSize();
            }
        }

        public void ClearAndDestroy()
        {
            foreach (var character in characters)
            {
                character.transform.SetParent(null, true);
                // Re-enable individual character physics before destroying group
                character.GetModel().EnableIndividualPhysics();
            }
            characters.Clear();
            Destroy(gameObject);
        }

        private void ArrangeCharacters()
        {
            // Arrange characters in a line formation
            for (int i = 0; i < characters.Count; i++)
            {
                // Use configurable character spacing
                float xOffset = (i - (characters.Count - 1) / 2.0f) * characterSpacing;
                characters[i].transform.localPosition = new Vector3(xOffset, 0, 0);
                characters[i].transform.localRotation = Quaternion.identity;
            }
        }

        private void UpdateColliderSize()
        {
            if (characters.Count == 0) return;
            
            Bounds bounds = CalculateGroupBounds();
            Collider collider = GetComponent<Collider>();
            
            if (collider is BoxCollider boxCollider)
            {
                boxCollider.center = bounds.center;
                boxCollider.size = bounds.size * colliderSizePercentage;
            }
            else if (collider is SphereCollider sphereCollider)
            {
                sphereCollider.center = bounds.center;
                sphereCollider.radius = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z) * 0.5f * colliderSizePercentage;
            }
            else if (collider is CapsuleCollider capsuleCollider)
            {
                capsuleCollider.center = bounds.center;
                capsuleCollider.height = bounds.size.y * colliderSizePercentage;
                capsuleCollider.radius = Mathf.Max(bounds.size.x, bounds.size.z) * 0.5f * colliderSizePercentage;
            }
        }

        private Bounds CalculateGroupBounds()
        {
            if (characters.Count == 0) return new Bounds(transform.position, Vector3.one);
            
            Bounds bounds = new Bounds(characters[0].transform.position, Vector3.zero);
            
            foreach (var character in characters)
            {
                Renderer[] renderers = character.GetComponentsInChildren<Renderer>();
                foreach (var renderer in renderers)
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }
            
            // Convert to local space
            bounds.center = transform.InverseTransformPoint(bounds.center);
            bounds.size = transform.InverseTransformVector(bounds.size);
            
            return bounds;
        }

        public void SetState(CharacterAnimationState state)
        {
            // Set animation state for all characters without physics control
            foreach (var character in characters)
            {
                character.GetModel().SetAnimationState(state);
            }
            
            // Group manages all physics
            var rb = GetComponent<Rigidbody>();
            if (rb == null) return;

            switch (state)
            {
                case CharacterAnimationState.Idle:
                case CharacterAnimationState.Slotted:
                    rb.isKinematic = true;
                    rb.useGravity = false;
                    rb.constraints = RigidbodyConstraints.FreezeAll;
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
                case CharacterAnimationState.Running:
                case CharacterAnimationState.CastingSkill:
                    rb.isKinematic = true;
                    rb.useGravity = false;
                    rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
                    break;
            }
        }

        public void StartGrab()
        {
            if (currentSlot != null)
            {
                currentSlot.Release();
                currentSlot = null;
            }
            // Ensure the Rigidbody is not kinematic when grabbing starts,
            // so the Grabbable component can manage it correctly.
            GetComponent<Rigidbody>().isKinematic = false;
            SetState(CharacterAnimationState.Grabbed);
        }

        public void EndGrab()
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

            // If no valid slot, enter falling state to settle physically
            StartCoroutine(DelayedSetFallingState());
        }

        private IEnumerator DelayedSetFallingState()
        {
            yield return null;
            
            SetState(CharacterAnimationState.Falling);
            
            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.constraints = RigidbodyConstraints.None;
            }
        }

        private IEnumerator CheckIfSettled()
        {
            var rb = GetComponent<Rigidbody>();
            if (rb == null) yield break;

            yield return new WaitForSeconds(0.5f);

            while (rb.velocity.sqrMagnitude > 0.01f || rb.angularVelocity.sqrMagnitude > 0.01f)
            {
                yield return new WaitForFixedUpdate();
            }

            SetState(CharacterAnimationState.Idle);
            transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent<CharacterSlot>(out var slot))
            {
                potentialSlot = slot;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.TryGetComponent<CharacterSlot>(out var slot) && potentialSlot == slot)
            {
                potentialSlot = null;
            }
        }

        // Property accessors for runtime adjustment
        public float ColliderSizePercentage
        {
            get => colliderSizePercentage;
            set
            {
                colliderSizePercentage = Mathf.Clamp(value, 0.1f, 2.0f);
                UpdateColliderSize();
            }
        }

        public float CharacterSpacing
        {
            get => characterSpacing;
            set
            {
                characterSpacing = Mathf.Clamp(value, 0.1f, 2.0f);
                ArrangeCharacters();
                UpdateColliderSize();
            }
        }
    }
}
