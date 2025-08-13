using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using UnityEngine.Networking;

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
        [SerializeField] private string tutorialJsonPath = "tutorial_instructions.json"; // Fixed path - file is directly in StreamingAssets
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
            Debug.Log("Starting tutorial system...");
            StartCoroutine(LoadTutorialDataCoroutine());
        }

        private IEnumerator LoadTutorialDataCoroutine()
        {
            string fullPath = Path.Combine(Application.streamingAssetsPath, tutorialJsonPath);
            Debug.Log($"Loading tutorial from: {fullPath}");

            // Convert to proper URI format for all platforms
            string uri = fullPath;
            if (!uri.StartsWith("file://") && !uri.StartsWith("jar:") && !uri.StartsWith("http"))
            {
                if (Application.platform == RuntimePlatform.Android)
                {
                    // Android already provides the correct URI format
                    uri = fullPath;
                }
                else
                {
                    // For other platforms, ensure proper file:// prefix
                    uri = "file://" + fullPath.Replace("\\", "/");
                }
            }

            using (UnityWebRequest request = UnityWebRequest.Get(uri))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        string jsonContent = request.downloadHandler.text;
                        
                        // Clean JSON content (remove BOM and other potential issues)
                        jsonContent = CleanJsonContent(jsonContent);
                        
                        // Debug: Log first 200 characters of JSON content
                        Debug.Log($"JSON content preview (first 200 chars): {jsonContent.Substring(0, Mathf.Min(200, jsonContent.Length))}...");
                        
                        // Validate JSON content is not empty
                        if (string.IsNullOrEmpty(jsonContent.Trim()))
                        {
                            Debug.LogError("JSON content is empty or contains only whitespace");
                            yield break;
                        }

                        // Check if JSON starts with expected structure
                        if (!jsonContent.Trim().StartsWith("{"))
                        {
                            Debug.LogError("JSON does not start with valid object notation");
                            Debug.LogError($"Content starts with: '{jsonContent.Substring(0, Mathf.Min(50, jsonContent.Length))}'");
                            yield break;
                        }

                        currentTutorial = JsonUtility.FromJson<TutorialData>(jsonContent);

                        if (currentTutorial != null && currentTutorial.steps != null && currentTutorial.steps.Length > 0)
                        {
                            Debug.Log($"Tutorial loaded: {currentTutorial.title} with {currentTutorial.steps.Length} steps");
                            currentStepIndex = 0;
                            ShowTutorialPanel();
                            DisplayCurrentStep();
                        }
                        else
                        {
                            Debug.LogError("Tutorial data is invalid or empty");
                            if (currentTutorial == null)
                            {
                                Debug.LogError("JsonUtility.FromJson returned null - check JSON format");
                                // Try alternative parsing approach
                                Debug.LogError("Attempting basic JSON validation...");
                                if (jsonContent.Contains("\"tutorial_id\"") && jsonContent.Contains("\"steps\""))
                                {
                                    Debug.LogError("JSON contains expected fields but JsonUtility failed to parse");
                                    Debug.LogError("This might be due to JsonUtility limitations with complex JSON");
                                }
                            }
                            else if (currentTutorial.steps == null)
                                Debug.LogError("Tutorial steps array is null");
                            else
                                Debug.LogError("Tutorial steps array is empty");
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"Failed to parse tutorial JSON: {ex.Message}");
                        Debug.LogError($"Stack trace: {ex.StackTrace}");
                        Debug.LogError($"Raw JSON content length: {request.downloadHandler.text?.Length ?? 0}");
                    }
                }
                else
                {
                    Debug.LogError($"Failed to load tutorial file: {request.error}");
                    Debug.LogError($"Response code: {request.responseCode}");
                    Debug.LogError($"File path: {fullPath}");
                    Debug.LogError($"URI: {uri}");
                    
                    // Check if file exists (for non-Android platforms)
                    if (Application.platform != RuntimePlatform.Android)
                    {
                        Debug.LogError($"File exists: {File.Exists(fullPath)}");
                    }
                }
            }
        }

        private void ShowTutorialPanel()
        {
            if (tutorialPanel != null)
            {
                tutorialPanel.SetActive(true);
                Debug.Log("Tutorial panel shown");
            }
        }

        private void HideTutorialPanel()
        {
            if (tutorialPanel != null)
            {
                tutorialPanel.SetActive(false);
                Debug.Log("Tutorial panel hidden");
            }
        }

        private void DisplayCurrentStep()
        {
            if (currentTutorial == null || currentStepIndex >= currentTutorial.steps.Length)
            {
                Debug.LogError("Cannot display step: invalid tutorial or step index");
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

            Debug.Log($"Displaying step {currentStepIndex + 1}/{currentTutorial.steps.Length}: {currentStep.title}");
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

        // Helper method to clean JSON content from BOM and other encoding issues
        private string CleanJsonContent(string content)
        {
            if (string.IsNullOrEmpty(content))
                return content;

            // Remove UTF-8 BOM if present
            if (content.StartsWith("\uFEFF"))
            {
                content = content.Substring(1);
                Debug.Log("💡 Removed UTF-8 BOM from JSON content");
            }

            // Remove any leading/trailing whitespace
            content = content.Trim();

            // Log first few bytes for debugging
            if (content.Length > 0)
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(content.Substring(0, Mathf.Min(10, content.Length)));
                Debug.Log($"💡 First bytes: {string.Join(", ", System.Array.ConvertAll(bytes, b => b.ToString()))}");
            }

            return content;
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
