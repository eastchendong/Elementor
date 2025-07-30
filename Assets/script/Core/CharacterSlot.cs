using UnityEngine;

namespace Elementor
{
    [RequireComponent(typeof(Collider))]
    public class CharacterSlot : MonoBehaviour
    {
        [SerializeField] private Transform slotAnchor; // The point where the character/group will snap to.
        [SerializeField] private Material shiningMaterial; // Material to use for guidance
        private Material originalMaterial;
        private Renderer _renderer;

        private object occupant; // Can be CharacterView or CharacterGroup

        public bool IsOccupied => occupant != null;

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
            if (slotAnchor == null)
            {
                slotAnchor = transform;
            }
            _renderer = GetComponent<Renderer>();
            if (_renderer != null)
            {
                originalMaterial = _renderer.material;
            }
        }

        public bool Occupy(object newOccupant)
        {
            if (IsOccupied) return false;

            occupant = newOccupant;
            
            Transform occupantTransform = null;
            if (newOccupant is CharacterView characterView)
            {
                occupantTransform = characterView.transform;
                characterView.GetModel().SetAnimationState(CharacterAnimationState.Slotted);
            }
            else if (newOccupant is CharacterGroup characterGroup)
            {
                occupantTransform = characterGroup.transform;
                characterGroup.SetState(CharacterAnimationState.Slotted);
            }

            if (occupantTransform != null)
            {
                occupantTransform.SetParent(slotAnchor, true);
                occupantTransform.position = slotAnchor.position;
                occupantTransform.rotation = slotAnchor.rotation;
                StopShining(); // Stop shining when occupied
            }
            
            Debug.Log($"{name} is now occupied.");
            return true;
        }

        public void Release()
        {
            if (!IsOccupied) return;

            if (occupant is CharacterView characterView)
            {
                characterView.transform.SetParent(null, true);
            }
            else if (occupant is CharacterGroup characterGroup)
            {
                characterGroup.transform.SetParent(null, true);
            }
            
            Debug.Log($"{name} is now free.");
            occupant = null;
        }

        public object GetOccupant()
        {
            return occupant;
        }

        public void StartShining()
        {
            if (_renderer != null && shiningMaterial != null)
            {
                _renderer.material = shiningMaterial;
            }
        }

        public void StopShining()
        {
            if (_renderer != null && originalMaterial != null)
            {
                _renderer.material = originalMaterial;
            }
        }
    }
}
