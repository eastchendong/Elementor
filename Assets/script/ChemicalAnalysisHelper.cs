using System.Collections;
using System.IO;
using UnityEngine;
using TMPro;

namespace Elementor
{
    public class ChemicalAnalysisHelper : MonoBehaviour
    {
        [Header("Components")]
        public LoreJsonReader loreJsonReader;
        public BookPagePrinter bookPagePrinter;
        public TMP_Text resultText;
        
        [Header("Settings")]
        public string outputFilePrefix = "generated_lore_";
        
        private bool isProcessing = false;
        private string currentGeneratedFileName;
        
        /// <summary>
        /// Public method to start the complete workflow using current book page
        /// </summary>
        public void StartChemicalAnalysisWorkflow()
        {
            if (bookPagePrinter != null)
            {
                string content = bookPagePrinter.GetCurrentPageTextContent();
                if (!string.IsNullOrEmpty(content))
                {
                    StartWorkflowWithText(content);
                }
                else
                {
                    Debug.LogError("No content available from BookPagePrinter!");
                }
            }
            else
            {
                Debug.LogError("BookPagePrinter is not assigned!");
            }
        }

        /// <summary>
        /// Start workflow with specific text content (more generic)
        /// </summary>
        public void StartWorkflowWithText(string content)
        {
            if (isProcessing)
            {
                Debug.LogWarning("Analysis workflow is already in progress.");
                return;
            }
            
            if (API.Instance == null)
            {
                Debug.LogError("API Instance is not available!");
                return;
            }
            
            if (string.IsNullOrEmpty(content))
            {
                Debug.LogError("Content is empty!");
                return;
            }
            
            string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            currentGeneratedFileName = $"{outputFilePrefix}{timestamp}.json";
            
            StartCoroutine(ExecuteWorkflowWithText(content));
        }
        
        private IEnumerator ExecuteWorkflowWithText(string content)
        {
            isProcessing = true;
            Debug.Log($"Starting Chemical Analysis Workflow with content: {content.Substring(0, Mathf.Min(50, content.Length))}...");
            Debug.Log($"Output file: {currentGeneratedFileName}");
            
            // Update UI
            if (resultText != null)
            {
                resultText.text = "Analyzing content...";
            }
            
            API.Instance.OnAnalysisComplete += OnAnalysisComplete;
            
            API.Instance.AnalyzeFromText(content);
            
            float timeout = 120f;
            float elapsed = 0f;
            
            while (API.Instance.IsAnalyzing && elapsed < timeout)
            {
                yield return new WaitForSeconds(0.1f);
                elapsed += 0.1f;
            }
            
            if (elapsed >= timeout)
            {
                Debug.LogError("Analysis workflow timed out!");
                if (resultText != null)
                {
                    resultText.text = "Error: Analysis timed out";
                }
                API.Instance.OnAnalysisComplete -= OnAnalysisComplete;
            }
            
            isProcessing = false;
        }
        
        private void OnAnalysisComplete(string jsonResponse)
        {
            Debug.Log("Analysis complete, processing response...");
            
            // Update UI with response
            if (resultText != null)
            {
                resultText.text = "Analysis complete! Processing response...";
            }
            
            if (IsValidJson(jsonResponse))
            {
                SaveJsonResponse(jsonResponse);
                LoadGeneratedLore();
                
                // Update UI with success
                if (resultText != null)
                {
                    resultText.text = $"Successfully generated and loaded: {currentGeneratedFileName}";
                }
            }
            else
            {
                Debug.LogError("Received invalid JSON response, cannot proceed");
                
                // Update UI with error
                if (resultText != null)
                {
                    resultText.text = "Error: Received invalid JSON response";
                }
            }
            
            API.Instance.OnAnalysisComplete -= OnAnalysisComplete;
        }
        
        private bool IsValidJson(string jsonString)
        {
            if (string.IsNullOrEmpty(jsonString))
                return false;
                
            try
            {
                // Try to parse as generic object to validate JSON structure
                var obj = JsonUtility.FromJson<object>(jsonString);
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"JSON validation failed: {ex.Message}");
                Debug.LogError($"Invalid JSON content: {jsonString}");
                return false;
            }
        }
        
        private void SaveJsonResponse(string jsonResponse)
        {
            try
            {
                string streamingAssetsPath = Application.streamingAssetsPath;
                string generatedJsonsPath = Path.Combine(streamingAssetsPath, "Generated_JSONs");
                
                // Create Generated_JSONs directory if it doesn't exist
                if (!Directory.Exists(generatedJsonsPath))
                {
                    Directory.CreateDirectory(generatedJsonsPath);
                    Debug.Log($"Created directory: {generatedJsonsPath}");
                }
                
                string filePath = Path.Combine(generatedJsonsPath, currentGeneratedFileName);
                File.WriteAllText(filePath, jsonResponse);
                
                Debug.Log($"JSON response saved to: {filePath}");
                Debug.Log($"Saved content preview: {jsonResponse.Substring(0, Mathf.Min(200, jsonResponse.Length))}...");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to save JSON response: {ex.Message}");
            }
        }
        
        private void LoadGeneratedLore()
        {
            string originalPath = loreJsonReader.loreFilePath;
            
            // Set path to include Generated_JSONs folder
            loreJsonReader.loreFilePath = Path.Combine("Generated_JSONs", currentGeneratedFileName);
            
            Debug.Log($"Loading generated lore from: {loreJsonReader.loreFilePath}");
            
            loreJsonReader.LoadLoreFromJson();
            
            Debug.Log("Generated lore loaded");
        }
        
        /// <summary>
        /// Get the filename of the most recently generated lore file
        /// </summary>
        public string GetCurrentGeneratedFileName()
        {
            return currentGeneratedFileName;
        }
        
        /// <summary>
        /// Manually set the LoreJsonReader to use a specific generated file
        /// </summary>
        public void LoadSpecificGeneratedLore(string fileName)
        {
            if (loreJsonReader != null)
            {
                // Ensure the path includes Generated_JSONs folder
                string fullPath = fileName.Contains("Generated_JSONs") ? fileName : Path.Combine("Generated_JSONs", fileName);
                loreJsonReader.loreFilePath = fullPath;
                loreJsonReader.LoadLoreFromJson();
                Debug.Log($"Manually loaded lore from: {fullPath}");
            }
        }
        
        /// <summary>
        /// Start workflow with specific page index (for testing)
        /// </summary>
        public void StartWorkflowWithPageIndex(int pageIndex)
        {
            if (bookPagePrinter != null)
            {
                bookPagePrinter.currentPageIndex = pageIndex;
            }
            StartChemicalAnalysisWorkflow();
        }
    }
}
    