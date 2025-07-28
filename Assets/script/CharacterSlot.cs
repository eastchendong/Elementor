using UnityEngine;

namespace Elementor
{
    [RequireComponent(typeof(Collider))]
    public class CharacterSlot : MonoBehaviour
    {
        [SerializeField] private Transform slotAnchor; // The point where the character/group will snap to.

        private object occupant; // Can be CharacterView or CharacterGroup

        public bool IsOccupied => occupant != null;

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
            if (slotAnchor == null)
            {
                slotAnchor = transform;
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
    }
}
