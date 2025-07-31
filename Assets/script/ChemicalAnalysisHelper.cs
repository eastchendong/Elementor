using System.Collections;
using System.IO;
using UnityEngine;

namespace Elementor
{
    public class ChemicalAnalysisHelper : MonoBehaviour
    {
        [Header("Components")]
        public ChemicalAssistant2 chemicalAssistant;
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
                Debug.LogError("ChemicalAssistant2 reference is missing!");
                return;
            }
            
            if (loreJsonReader == null)
            {
                Debug.LogError("LoreJsonReader reference is missing!");
                return;
            }
            
            // Generate timestamped filename
            string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            currentGeneratedFileName = $"{outputFilePrefix}{timestamp}.json";
            
            StartCoroutine(ExecuteWorkflow());
        }
        
        private IEnumerator ExecuteWorkflow()
        {
            isProcessing = true;
            Debug.Log($"🚀 Starting Chemical Analysis Workflow... Output file: {currentGeneratedFileName}");
            
            // Subscribe to the response event
            chemicalAssistant.OnAnalysisComplete += OnAnalysisComplete;
            
            // Start the chemical analysis
            chemicalAssistant.StartAnalysisFromImage();
            
            // Wait for completion with timeout
            float timeout = 120f; // 2 minutes timeout
            float elapsed = 0f;
            
            while (chemicalAssistant.IsAnalyzing && elapsed < timeout)
            {
                yield return new WaitForSeconds(0.1f);
                elapsed += 0.1f;
            }
            
            if (elapsed >= timeout)
            {
                Debug.LogError("⏰ Analysis workflow timed out!");
                chemicalAssistant.OnAnalysisComplete -= OnAnalysisComplete;
            }
            
            isProcessing = false;
        }
        
        private void OnAnalysisComplete(string jsonResponse)
        {
            Debug.Log("📋 Analysis complete, processing response...");
            
            // Validate JSON before saving
            if (IsValidJson(jsonResponse))
            {
                // Save the JSON response
                SaveJsonResponse(jsonResponse);
                
                // Load the saved JSON through LoreJsonReader
                LoadGeneratedLore();
            }
            else
            {
                Debug.LogError("❌ Received invalid JSON response, cannot proceed");
            }
            
            // Unsubscribe from the event
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
                
                // Create StreamingAssets directory if it doesn't exist
                if (!Directory.Exists(streamingAssetsPath))
                {
                    Directory.CreateDirectory(streamingAssetsPath);
                }
                
                string filePath = Path.Combine(streamingAssetsPath, currentGeneratedFileName);
                File.WriteAllText(filePath, jsonResponse);
                
                Debug.Log($"💾 JSON response saved to: {filePath}");
                Debug.Log($"📄 Saved content preview: {jsonResponse.Substring(0, Mathf.Min(200, jsonResponse.Length))}...");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ Failed to save JSON response: {ex.Message}");
            }
        }
        
        private void LoadGeneratedLore()
        {
            string originalPath = loreJsonReader.loreFilePath;
            
            loreJsonReader.loreFilePath = currentGeneratedFileName;
            
            Debug.Log($"Loading generated lore from: {currentGeneratedFileName}");
            
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
                loreJsonReader.loreFilePath = fileName;
                loreJsonReader.LoadLoreFromJson();
                Debug.Log($"🎯 Manually loaded lore from: {fileName}");
            }
        }
    }
}
