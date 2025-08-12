using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Elementor.Core
{
    [RequireComponent(typeof(Collider))]
    public class CharacterSlot : MonoBehaviour
    {
        [SerializeField] private Transform slotAnchor; // The point where the character/group will snap to.
        [SerializeField] private GameObject highlightArrow; // Arrow GameObject to show for guidance

        // Coefficient system for stoichiometry
        [SerializeField] private int coefficient = 1;
        [SerializeField] private bool showCoefficientUI = false; // Controls whether to show coefficient controls
        [SerializeField] private GameObject coefficientUI;
        [SerializeField] private TextMeshProUGUI coefficientText;
        [SerializeField] private Button increaseButton;
        [SerializeField] private Button decreaseButton;
        [SerializeField] private int maxCoefficient = 10;

        // Slot type identification
        [SerializeField] private bool isPredefinedOutputSlot = false;

        private object occupant; // Can be CharacterView or CharacterGroup
        private bool wasOccupiedLastFrame = false; // Track occupancy state

        public bool IsOccupied => occupant != null;
        public int Coefficient => coefficient;
        public bool IsPredefinedOutputSlot => isPredefinedOutputSlot;

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
            if (slotAnchor == null)
            {
                slotAnchor = transform;
            }
            
            // Initialize highlight arrow as disabled
            if (highlightArrow != null)
            {
                highlightArrow.SetActive(false);
            }

            SetupCoefficientUI();
        }

        private void Update()
        {
            // Check if occupant has been moved away or destroyed
            if (IsOccupied && !IsOccupantStillInSlot())
            {
                Debug.Log($"Occupant of {name} has left the slot area or been destroyed");
                ForceRelease();
            }
            
            // Track occupancy state changes
            bool currentlyOccupied = IsOccupied;
            if (wasOccupiedLastFrame != currentlyOccupied)
            {
                if (!currentlyOccupied)
                {
                    OnSlotBecameEmpty();
                }
                wasOccupiedLastFrame = currentlyOccupied;
            }
        }

        private bool IsOccupantStillInSlot()
        {
            if (occupant == null) return false;
            
            Transform occupantTransform = null;
            
            if (occupant is CharacterView characterView)
            {
                if (characterView == null) return false;
                occupantTransform = characterView.transform;
            }
            else if (occupant is CharacterGroup characterGroup)
            {
                if (characterGroup == null) return false;
                occupantTransform = characterGroup.transform;
            }
            
            if (occupantTransform == null) return false;
            
            // Check if occupant is still a child of this slot
            if (occupantTransform.parent != slotAnchor) return false;
            
            // Check distance from slot anchor (additional safety check)
            float distance = Vector3.Distance(occupantTransform.position, slotAnchor.position);
            return distance < 2f; // Tolerance of 2 units
        }

        private void OnSlotBecameEmpty()
        {
            Debug.Log($"Slot {name} became empty");
            // This can be used by other systems (like CharacterSpawner) to react to empty slots
        }

        private void ForceRelease()
        {
            Debug.Log($"Force releasing occupant from {name}");
            occupant = null;
            HideCoefficientUI();
            ResetCoefficient();
        }

        private void SetupCoefficientUI()
        {
            if (coefficientUI != null)
            {
                coefficientUI.SetActive(false);
            }

            if (increaseButton != null)
            {
                increaseButton.onClick.AddListener(IncreaseCoefficient);
            }

            if (decreaseButton != null)
            {
                decreaseButton.onClick.AddListener(DecreaseCoefficient);
            }

            UpdateCoefficientDisplay();
        }

        public bool Occupy(object newOccupant)
        {
            if (IsOccupied) return false;

            occupant = newOccupant;
            
            Transform occupantTransform = null;
            Rigidbody occupantRigidbody = null;

            if (newOccupant is CharacterView characterView)
            {
                occupantTransform = characterView.transform;
                occupantRigidbody = characterView.GetComponent<Rigidbody>();
                characterView.GetModel().SetAnimationState(CharacterAnimationState.Slotted);
            }
            else if (newOccupant is CharacterGroup characterGroup)
            {
                occupantTransform = characterGroup.transform;
                occupantRigidbody = characterGroup.GetComponent<Rigidbody>();
                characterGroup.SetState(CharacterAnimationState.Slotted);
            }

            if (occupantTransform != null)
            {
                occupantTransform.SetParent(slotAnchor, true);
                occupantTransform.position = slotAnchor.position;
                occupantTransform.rotation = slotAnchor.rotation;
                if (occupantRigidbody != null)
                {
                    occupantRigidbody.constraints = RigidbodyConstraints.FreezeAll;
                }
                StopShining();
                ShowCoefficientUI();
            }
            
            Debug.Log($"{name} is now occupied with coefficient {coefficient}.");
            return true;
        }

        public void Release()
        {
            if (!IsOccupied) return;

            Rigidbody occupantRigidbody = null;
            Transform occupantTransform = null;

            if (occupant is CharacterView characterView)
            {
                occupantTransform = characterView.transform;
                occupantRigidbody = characterView.GetComponent<Rigidbody>();
                
                // Remove parent hierarchy - make it independent
                occupantTransform.SetParent(null, true);
            }
            else if (occupant is CharacterGroup characterGroup)
            {
                occupantTransform = characterGroup.transform;
                occupantRigidbody = characterGroup.GetComponent<Rigidbody>();
                
                // Remove parent hierarchy - make it independent
                occupantTransform.SetParent(null, true);
            }

            if (occupantRigidbody != null)
            {
                occupantRigidbody.constraints = RigidbodyConstraints.FreezeRotation;
                occupantRigidbody.isKinematic = false;
            }
            
            HideCoefficientUI();
            ResetCoefficient();
            Debug.Log($"{name} is now free. Occupant parent hierarchy removed.");
            occupant = null;
        }

        public object GetOccupant()
        {
            return occupant;
        }

        public string GetOccupantName()
        {
            if (occupant is CharacterView characterView)
            {
                return characterView.GetModel().GetCharacterName();
            }
            else if (occupant is CharacterGroup characterGroup)
            {
                return characterGroup.name;
            }
            return null;
        }

        // Coefficient management
        public void IncreaseCoefficient()
        {
            if (coefficient < maxCoefficient)
            {
                coefficient++;
                UpdateCoefficientDisplay();
                Debug.Log($"{name} coefficient increased to {coefficient}");
            }
        }

        public void DecreaseCoefficient()
        {
            if (coefficient > 1)
            {
                coefficient--;
                UpdateCoefficientDisplay();
                Debug.Log($"{name} coefficient decreased to {coefficient}");
            }
        }

        public void SetCoefficient(int value)
        {
            coefficient = Mathf.Clamp(value, 1, maxCoefficient);
            UpdateCoefficientDisplay();
        }

        private void ResetCoefficient()
        {
            coefficient = 1;
            UpdateCoefficientDisplay();
        }

        private void UpdateCoefficientDisplay()
        {
            if (coefficientText != null)
            {
                coefficientText.text = coefficient.ToString();
            }
        }

        private void ShowCoefficientUI()
        {
            if (coefficientUI != null && showCoefficientUI)
            {
                coefficientUI.SetActive(true);
            }
        }

        private void HideCoefficientUI()
        {
            if (coefficientUI != null && showCoefficientUI)
            {
                coefficientUI.SetActive(false);
            }
        }

        public void StartShining()
        {
            if (highlightArrow != null)
            {
                highlightArrow.SetActive(true);
                Debug.Log($"Started highlighting arrow for slot {name}");
            }
        }

        public void StopShining()
        {
            if (highlightArrow != null)
            {
                highlightArrow.SetActive(false);
                Debug.Log($"Stopped highlighting arrow for slot {name}");
            }
        }

        // Property to control coefficient UI visibility at runtime
        public bool ifShowCoefficientUI
        {
            get => showCoefficientUI;
            set
            {
                showCoefficientUI = value;
                if (!showCoefficientUI && coefficientUI != null)
                {
                    coefficientUI.SetActive(false);
                }
                else if (showCoefficientUI && IsOccupied && coefficientUI != null)
                {
                    coefficientUI.SetActive(true);
                }
            }
        }
    }
}