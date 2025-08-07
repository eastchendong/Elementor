using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Elementor.Core
{
    [RequireComponent(typeof(Collider))]
    public class CharacterSlot : MonoBehaviour
    {
        [SerializeField] private Transform slotAnchor; // The point where the character/group will snap to.
        [SerializeField] private Material shiningMaterial; // Material to use for guidance
        private Material originalMaterial;
        private Renderer _renderer;

        // Coefficient system for stoichiometry
        [SerializeField] private int coefficient = 1;
        [SerializeField] private bool showCoefficientUI = false; // Controls whether to show coefficient controls
        [SerializeField] private GameObject coefficientUI;
        [SerializeField] private TextMeshProUGUI coefficientText;
        [SerializeField] private Button increaseButton;
        [SerializeField] private Button decreaseButton;
        [SerializeField] private int maxCoefficient = 10;

        private object occupant; // Can be CharacterView or CharacterGroup

        public bool IsOccupied => occupant != null;
        public int Coefficient => coefficient;

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

            SetupCoefficientUI();
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

            if (occupant is CharacterView characterView)
            {
                characterView.transform.SetParent(null, true);
                occupantRigidbody = characterView.GetComponent<Rigidbody>();
            }
            else if (occupant is CharacterGroup characterGroup)
            {
                characterGroup.transform.SetParent(null, true);
                occupantRigidbody = characterGroup.GetComponent<Rigidbody>();
            }

            if (occupantRigidbody != null)
            {
                occupantRigidbody.constraints = RigidbodyConstraints.FreezeRotation;
                occupantRigidbody.isKinematic = false;
            }
            
            HideCoefficientUI();
            ResetCoefficient();
            Debug.Log($"{name} is now free.");
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