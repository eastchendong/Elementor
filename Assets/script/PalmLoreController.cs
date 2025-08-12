using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Elementor.Lore;

namespace Elementor
{
    public class PalmLoreController : MonoBehaviour
    {
        public static PalmLoreController Instance { get; private set; }

        [Header("Palm Display Settings")]
        [SerializeField]
        [Tooltip("Text component to display lore information on the palm")]
        private TextMeshProUGUI palmLoreText;

        [SerializeField]
        [Tooltip("Text component to display current objective")]
        private TextMeshProUGUI palmObjectiveText;

        [SerializeField]
        [Tooltip("Canvas or UI container for the palm display")]
        private GameObject palmUI;

        [Header("Display Configuration")]
        [SerializeField]
        [Tooltip("Maximum characters to display in lore text")]
        private int maxLoreTextLength = 150;

        [SerializeField]
        [Tooltip("Default text when no lore is loaded")]
        private string defaultLoreText = "No active lore";

        [SerializeField]
        [Tooltip("Default objective text")]
        private string defaultObjectiveText = "Explore and interact";

        private LoreController loreController;

        private void Awake()
        {
            // Singleton pattern implementation
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            Debug.Log("🤚 PalmLoreController initialized as singleton");
        }

        void Start()
        {
            loreController = LoreController.Instance;
            
            if (loreController == null)
            {
                Debug.LogError("❌ LoreController instance not found in PalmLoreController");
                return;
            }

            // Subscribe to lore events
            loreController.OnLoreLoaded += HandleLoreLoaded;
            
            Debug.Log("🔗 PalmLoreController subscribed to LoreController events");

            // Initialize display
            InitializePalmDisplay();

            // Check if lore is already loaded
            if (loreController.CurrentLore != null)
            {
                HandleLoreLoaded();
            }
        }

        private void OnDestroy()
        {
            if (loreController != null)
            {
                loreController.OnLoreLoaded -= HandleLoreLoaded;
            }
        }

        /// <summary>
        /// Initialize the palm display with default values
        /// </summary>
        private void InitializePalmDisplay()
        {
            if (palmLoreText != null)
            {
                palmLoreText.text = defaultLoreText;
            }
            
            if (palmObjectiveText != null)
            {
                palmObjectiveText.text = defaultObjectiveText;
            }

            if (palmUI != null)
            {
                palmUI.SetActive(true);
            }

            Debug.Log("✋ Palm display initialized with default values");
        }

        /// <summary>
        /// Handle when new lore is loaded
        /// </summary>
        private void HandleLoreLoaded()
        {
            Debug.Log("📖 PalmLoreController handling lore loaded event");
            
            if (loreController.CurrentLore == null)
            {
                ResetPalmDisplay();
                return;
            }

            UpdateLoreDisplay();
            UpdateObjectiveDisplay();
        }

        /// <summary>
        /// Update the lore text display on the palm
        /// </summary>
        private void UpdateLoreDisplay()
        {
            if (palmLoreText == null || loreController.CurrentLore == null)
                return;

            var story = loreController.GetStory();
            if (story != null)
            {
                string displayText = story.title;
                
                // Use plot instead of description since LoreStory has plot field, not description
                if (story.plot != null && story.plot.Count > 0)
                {
                    displayText += "\n" + string.Join(" ", story.plot);
                }

                // Truncate if too long
                if (displayText.Length > maxLoreTextLength)
                {
                    displayText = displayText.Substring(0, maxLoreTextLength - 3) + "...";
                }

                palmLoreText.text = displayText;
                Debug.Log($"📝 Updated palm lore text: {story.title}");
            }
        }

        /// <summary>
        /// Update the objective text display on the palm
        /// </summary>
        private void UpdateObjectiveDisplay()
        {
            if (palmObjectiveText == null || loreController.CurrentLore == null)
                return;

            var reaction = loreController.GetReaction();
            if (reaction != null)
            {
                string objectiveText = $"Objective: Perform {reaction.equation}";
                
                // Add reactant requirements
                if (reaction.reactants != null && reaction.reactants.Count > 0)
                {
                    objectiveText += "\nRequired: ";
                    for (int i = 0; i < reaction.reactants.Count; i++)
                    {
                        var reactant = reaction.reactants[i];
                        objectiveText += $"{reactant.name} x{reactant.count}";
                        if (i < reaction.reactants.Count - 1)
                            objectiveText += ", ";
                    }
                }

                palmObjectiveText.text = objectiveText;
                Debug.Log($"🎯 Updated palm objective text: {reaction.equation}");
            }
        }

        /// <summary>
        /// Reset palm display to default values
        /// </summary>
        public void ResetPalmDisplay()
        {
            if (palmLoreText != null)
            {
                palmLoreText.text = defaultLoreText;
            }
            
            if (palmObjectiveText != null)
            {
                palmObjectiveText.text = defaultObjectiveText;
            }

            Debug.Log("🔄 Palm display reset to default values");
        }

        /// <summary>
        /// Show or hide the palm UI
        /// </summary>
        public void SetPalmUIActive(bool active)
        {
            if (palmUI != null)
            {
                palmUI.SetActive(active);
                Debug.Log($"👋 Palm UI set to {(active ? "active" : "inactive")}");
            }
        }

        /// <summary>
        /// Manually update the palm display (useful for external calls)
        /// </summary>
        public void RefreshPalmDisplay()
        {
            if (loreController != null && loreController.CurrentLore != null)
            {
                UpdateLoreDisplay();
                UpdateObjectiveDisplay();
            }
            else
            {
                ResetPalmDisplay();
            }
        }

        /// <summary>
        /// Set custom objective text (overrides automatic objective)
        /// </summary>
        public void SetCustomObjective(string customObjective)
        {
            if (palmObjectiveText != null)
            {
                palmObjectiveText.text = customObjective;
                Debug.Log($"🎯 Set custom objective: {customObjective}");
            }
        }

        /// <summary>
        /// Get current lore text being displayed
        /// </summary>
        public string GetCurrentLoreText()
        {
            return palmLoreText != null ? palmLoreText.text : defaultLoreText;
        }

        /// <summary>
        /// Get current objective text being displayed
        /// </summary>
        public string GetCurrentObjectiveText()
        {
            return palmObjectiveText != null ? palmObjectiveText.text : defaultObjectiveText;
        }
    }
}
