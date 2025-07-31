using System.Collections;
using System.IO;
using UnityEngine;

namespace Elementor
{
    public class ChemicalAnalysisHelper : MonoBehaviour
    {
        [Header("Components")]
        public API chemicalAssistant;
        public LoreJsonReader loreJsonReader;
        
        [Header("Settings")]
        public string outputFilePrefix = "generated_lore_";
        
        private bool isProcessing = false;
        private string currentGeneratedFileName;
        
        /// <summary>
        /// Public method to start the complete workflow
        /// </summary>
        public void StartChemicalAnalysisWorkflow()
        {
            if (isProcessing)
            {
                Debug.LogWarning("Analysis workflow is already in progress.");
                return;
            }
            
            if (chemicalAssistant == null)
            {
                Debug.LogError("API reference is missing!");
                return;
            }
            
            if (loreJsonReader == null)
            {
                Debug.LogError("LoreJsonReader reference is missing!");
                return;
            }
            
            string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            currentGeneratedFileName = $"{outputFilePrefix}{timestamp}.json";
            
            StartCoroutine(ExecuteWorkflow());
        }
        
        private IEnumerator ExecuteWorkflow()
        {
            isProcessing = true;
            Debug.Log($"Starting Chemical Analysis Workflow... Output file: {currentGeneratedFileName}");
            
            chemicalAssistant.OnAnalysisComplete += OnAnalysisComplete;
            
            chemicalAssistant.StartAnalysisFromImage();
            
            float timeout = 120f;
            float elapsed = 0f;
            
            while (chemicalAssistant.IsAnalyzing && elapsed < timeout)
            {
                yield return new WaitForSeconds(0.1f);
                elapsed += 0.1f;
            }
            
            if (elapsed >= timeout)
            {
                Debug.LogError("Analysis workflow timed out!");
                chemicalAssistant.OnAnalysisComplete -= OnAnalysisComplete;
            }
            
            isProcessing = false;
        }
        
        private void OnAnalysisComplete(string jsonResponse)
        {
            Debug.Log("Analysis complete, processing response...");
            
            if (IsValidJson(jsonResponse))
            {
                SaveJsonResponse(jsonResponse);
                LoadGeneratedLore();
            }
            else
            {
                Debug.LogError("Received invalid JSON response, cannot proceed");
            }
            
            chemicalAssistant.OnAnalysisComplete -= OnAnalysisComplete;
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
    }
}

