using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;

namespace Elementor
{
    [System.Serializable]
    public class TutorialStep
    {
        public int step;
        public string title;
        public string content;
    }

    [System.Serializable]
    public class TutorialData
    {
        public string tutorial_id;
        public string title;
        public TutorialStep[] steps;
        public string next_level;
    }

    public class LoreInitializer : MonoBehaviour
    {
        [Header("Tutorial UI References")]
        [SerializeField] private GameObject tutorialPanel;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI contentText;
        [SerializeField] private Button nextButton;

        [Header("Tutorial Settings")]
        [SerializeField] private string tutorialJsonPath = "Generated_JSONs/tutorial_instructions.json";
        [SerializeField] private bool autoStartTutorial = true;

        private TutorialData currentTutorial;
        private int currentStepIndex = 0;
        private LoreJsonReader loreJsonReader;

        // Start is called before the first frame update
        void Start()
        {
            // Get reference to LoreJsonReader
            loreJsonReader = LoreJsonReader.Instance;
            if (loreJsonReader == null)
            {
                loreJsonReader = FindObjectOfType<LoreJsonReader>();
            }

            // Setup UI
            SetupTutorialUI();

            // Auto-start tutorial if enabled
            if (autoStartTutorial)
            {
                StartTutorial();
            }
        }

        private void SetupTutorialUI()
        {
            if (nextButton != null)
            {
                nextButton.onClick.AddListener(OnNextButtonClicked);
            }

            // Initially hide tutorial panel
            if (tutorialPanel != null)
            {
                tutorialPanel.SetActive(false);
            }
        }

        public void StartTutorial()
        {
            Debug.Log("🎓 Starting tutorial system...");
            LoadTutorialData();
        }

        private void LoadTutorialData()
        {
            string fullPath = Path.Combine(Application.streamingAssetsPath, tutorialJsonPath);
            Debug.Log($"📖 Loading tutorial from: {fullPath}");

            if (File.Exists(fullPath))
            {
                try
                {
                    string jsonContent = File.ReadAllText(fullPath);
                    currentTutorial = JsonUtility.FromJson<TutorialData>(jsonContent);

                    if (currentTutorial != null && currentTutorial.steps != null && currentTutorial.steps.Length > 0)
                    {
                        Debug.Log($"✅ Tutorial loaded: {currentTutorial.title} with {currentTutorial.steps.Length} steps");
                        currentStepIndex = 0;
                        ShowTutorialPanel();
                        DisplayCurrentStep();
                    }
                    else
                    {
                        Debug.LogError("❌ Tutorial data is invalid or empty");
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"❌ Failed to load tutorial: {ex.Message}");
                }
            }
            else
            {
                Debug.LogError($"❌ Tutorial file not found: {fullPath}");
            }
        }

        private void ShowTutorialPanel()
        {
            if (tutorialPanel != null)
            {
                tutorialPanel.SetActive(true);
                Debug.Log("📋 Tutorial panel shown");
            }
        }

        private void HideTutorialPanel()
        {
            if (tutorialPanel != null)
            {
                tutorialPanel.SetActive(false);
                Debug.Log("📋 Tutorial panel hidden");
            }
        }

        private void DisplayCurrentStep()
        {
            if (currentTutorial == null || currentStepIndex >= currentTutorial.steps.Length)
            {
                Debug.LogError("❌ Cannot display step: invalid tutorial or step index");
                return;
            }

            TutorialStep currentStep = currentTutorial.steps[currentStepIndex];
            
            // Update UI texts
            if (titleText != null)
            {
                titleText.text = currentStep.title;
            }

            if (contentText != null)
            {
                contentText.text = currentStep.content;
            }

            // Update button text for last step
            if (nextButton != null)
            {
                TextMeshProUGUI buttonText = nextButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    if (currentStepIndex >= currentTutorial.steps.Length - 1)
                    {
                        buttonText.text = "开始游戏";
                    }
                    else
                    {
                        buttonText.text = "下一步";
                    }
                }
            }

            Debug.Log($"📝 Displaying step {currentStepIndex + 1}/{currentTutorial.steps.Length}: {currentStep.title}");
        }

        private void OnNextButtonClicked()
        {
            if (currentTutorial == null) return;

            currentStepIndex++;

            if (currentStepIndex >= currentTutorial.steps.Length)
            {
                // Tutorial completed, load the game level
                CompleteTutorial();
            }
            else
            {
                // Show next step
                DisplayCurrentStep();
            }
        }

        private void CompleteTutorial()
        {
            Debug.Log("🎉 Tutorial completed! Loading game level...");
            
            HideTutorialPanel();

            // Load the next level specified in tutorial data
            if (currentTutorial != null && !string.IsNullOrEmpty(currentTutorial.next_level))
            {
                if (loreJsonReader != null)
                {
                    loreJsonReader.LoadSpecificLoreFile(currentTutorial.next_level);
                    Debug.Log($"🎮 Loading game level: {currentTutorial.next_level}");
                }
                else
                {
                    Debug.LogError("❌ LoreJsonReader not found, cannot load game level");
                }
            }
            else
            {
                Debug.LogWarning("⚠️ No next level specified in tutorial data");
            }

            // Reset tutorial state
            currentTutorial = null;
            currentStepIndex = 0;
        }

        // Public method to restart tutorial
        public void RestartTutorial()
        {
            currentStepIndex = 0;
            if (currentTutorial != null)
            {
                ShowTutorialPanel();
                DisplayCurrentStep();
            }
            else
            {
                StartTutorial();
            }
        }
    }
}
