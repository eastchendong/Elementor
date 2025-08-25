using UnityEngine;
using System.Collections.Generic;
using System.IO;
using UnityEngine.UI;
using System.Collections;

namespace Elementor
{
    public class BookManager : MonoBehaviour
    {
        public ScriptBoy.ProceduralBook.Book book;
        public int currentPageIndex; // Inspector里显示当前页码

        [Header("Integration")]
        public ChemicalAnalysisHelper analysisHelper;
        
        [Header("Settings")]
        public string pageContentFile = "page_content.json";
        [Tooltip("Timeout in seconds for analysis completion")]
        public float analysisTimeoutSeconds = 10f;
        
        [Header("Manual Lore Testing")]
        [Tooltip("Assign a JSON lore file for manual testing")]
        public string manualLoreFilePath = "Generated_JSONs/example_lore.json";

        [Header("Book Control Buttons")]
        public Button nextPageButton;
        public Button previousPageButton;
        public Button selectBookButton;

        private int lastPageIndex = -1;
        private PageContentData pageContentData;
        private bool bookInteractionEnabled = true;

        [System.Serializable]
        public class PageContent
        {
            public int page_index;
            public string content_type;
            public string title;
            public string description;
            public string chemical_equation;
        }

        [System.Serializable]
        public class PageContentData
        {
            public PageContent[] pages;
        }

        void Start()
        {
            LoadPageContentData();

            // Subscribe to lore loading events
            if (LoreController.Instance != null)
            {
                LoreController.Instance.OnLoreLoaded += OnLoreLoaded;
            }

            // Subscribe directly to reaction completion static event
            ReactionManager.OnGlobalReactionsCompleted += OnReactionCompleted;

            CloseBookAndDisableInteractions();
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (LoreController.Instance != null)
            {
                LoreController.Instance.OnLoreLoaded -= OnLoreLoaded;
            }
            
            // Unsubscribe from reaction completion event
            ReactionManager.OnGlobalReactionsCompleted -= OnReactionCompleted;
        }

        private void OnLoreLoaded()
        {
            Debug.Log("BookManager: Lore loaded, disabling interactions but keeping current page");
            // Only disable interactions, don't close the book or change pages
            SetBookInteractionEnabled(false);
        }

        public void OnReactionCompleted()
        {
            Debug.Log("BookManager: All reactions completed, restoring book interactions");
            SetBookInteractionEnabled(true);
        }

        private void CloseBookAndDisableInteractions()
        {
            if (book != null && book.isBuilt)
            {
                book.SetOpenProgress(0f);
            }
            
            // Disable book interaction buttons
            SetBookInteractionEnabled(false);
        }

        public void SetBookInteractionEnabled(bool enabled)
        {
            bookInteractionEnabled = enabled;
            
            if (nextPageButton != null)
                nextPageButton.interactable = enabled;
                
            if (previousPageButton != null)
                previousPageButton.interactable = enabled;
                
            if (selectBookButton != null)
                selectBookButton.interactable = enabled;
                
            Debug.Log($"BookManager: Book interactions {(enabled ? "enabled" : "disabled")}");
        }

        void LoadPageContentData()
        {
            StartCoroutine(LoadPageContentDataCoroutine());
        }

        private IEnumerator LoadPageContentDataCoroutine()
        {
            // First try to load from persistent data path (for runtime generated files)
            string persistentPath = Path.Combine(Application.persistentDataPath, pageContentFile);
            bool foundInPersistent = false;
            string jsonContent = "";

            if (File.Exists(persistentPath))
            {
                try
                {
                    jsonContent = File.ReadAllText(persistentPath);
                    foundInPersistent = true;
                    Debug.Log($"📖 BookManager loaded from persistent path: {persistentPath}");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"❌ Failed to read from persistent path: {ex.Message}");
                }
            }

            // If not found in persistent, try StreamingAssets (Android-compatible way)
            if (!foundInPersistent)
            {
                string streamingPath = Path.Combine(Application.streamingAssetsPath, pageContentFile);
                Debug.Log($"🔍 Attempting to load page content from StreamingAssets: {streamingPath}");

                // Convert to proper URI format for all platforms
                string uri = streamingPath;
                if (!uri.StartsWith("file://") && !uri.StartsWith("jar:") && !uri.StartsWith("http"))
                {
                    if (Application.platform == RuntimePlatform.Android)
                    {
                        // Android already provides the correct URI format
                        uri = streamingPath;
                    }
                    else
                    {
                        // For other platforms, ensure proper file:// prefix
                        uri = "file://" + streamingPath.Replace("\\", "/");
                    }
                }

                using (UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequest.Get(uri))
                {
                    yield return request.SendWebRequest();

                    if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                    {
                        jsonContent = request.downloadHandler.text;
                        Debug.Log($"📖 BookManager loaded from StreamingAssets: {streamingPath}");
                    }
                    else
                    {
                        Debug.LogError($"❌ BookManager failed to load page content file: {pageContentFile}");
                        Debug.LogError($"💡 Error: {request.error}");
                        Debug.LogError($"💡 Response code: {request.responseCode}");
                        Debug.LogError($"💡 File path: {streamingPath}");
                        Debug.LogError($"💡 URI: {uri}");
                        
                        // Check if file exists (for non-Android platforms)
                        if (Application.platform != RuntimePlatform.Android)
                        {
                            Debug.LogError($"💡 File exists: {File.Exists(streamingPath)}");
                        }
                        yield break;
                    }
                }
            }

            // Process the loaded JSON content
            try
            {
                // Clean JSON content (remove BOM and other potential issues)
                jsonContent = CleanJsonContent(jsonContent);
                
                // Debug: Log first 200 characters of JSON content
                Debug.Log($"📄 JSON content preview (first 200 chars): {jsonContent.Substring(0, Mathf.Min(200, jsonContent.Length))}...");
                
                // Validate JSON content is not empty
                if (string.IsNullOrEmpty(jsonContent.Trim()))
                {
                    Debug.LogError("❌ JSON content is empty or contains only whitespace");
                    yield break;
                }

                // Check if JSON starts with expected structure
                if (!jsonContent.Trim().StartsWith("{"))
                {
                    Debug.LogError("❌ JSON does not start with valid object notation");
                    Debug.LogError($"💡 Content starts with: '{jsonContent.Substring(0, Mathf.Min(50, jsonContent.Length))}'");
                    yield break;
                }

                pageContentData = JsonUtility.FromJson<PageContentData>(jsonContent);
                
                if (pageContentData != null && pageContentData.pages != null)
                {
                    Debug.Log($"BookManager loaded page content data with {pageContentData.pages.Length} pages");
                }
                else
                {
                    Debug.LogError("❌ Page content data is invalid or empty");
                    if (pageContentData == null)
                    {
                        Debug.LogError("💡 JsonUtility.FromJson returned null - check JSON format");
                        // Try alternative parsing approach
                        Debug.LogError("💡 Attempting basic JSON validation...");
                        if (jsonContent.Contains("\"pages\"") && jsonContent.Contains("\"page_index\""))
                        {
                            Debug.LogError("💡 JSON contains expected fields but JsonUtility failed to parse");
                            Debug.LogError("💡 This might be due to JsonUtility limitations with complex JSON");
                        }
                    }
                    else if (pageContentData.pages == null)
                        Debug.LogError("💡 Pages array is null");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to parse page content data: {ex.Message}");
                Debug.LogError($"💡 Stack trace: {ex.StackTrace}");
                Debug.LogError($"💡 Raw JSON content length: {jsonContent?.Length ?? 0}");
            }
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

        void Update()
        {
            if (book != null)
            {
                List<int> indices = new List<int>();
                book.GetActivePaperSideIndices(indices);
                if (indices.Count > 0)
                {
                    int newPageIndex = indices[0]; // 或 indices[indices.Count-1]
                    if (newPageIndex != lastPageIndex)
                    {
                        currentPageIndex = newPageIndex;
                        OnPageChanged();
                        lastPageIndex = newPageIndex;
                    }
                }
            }
        }

        void OnPageChanged()
        {
            Debug.Log($"Page changed to: {currentPageIndex}");
        }

        public void PrintCurrentPage()
        {
            Debug.Log("当前显示页码索引: " + currentPageIndex);
        }

        [ContextMenu("Trigger Analysis for Current Page")]
        public void TriggerAnalysisForCurrentPage()
        {
            if (analysisHelper != null)
            {
                string content = GetCurrentPageContentAsString();
                if (!string.IsNullOrEmpty(content))
                {
                    StartCoroutine(TriggerAnalysisWithTimeout(content));
                }
                else
                {
                    Debug.LogWarning("No content found for current page!");
                }
            }
            else
            {
                Debug.LogWarning("ChemicalAnalysisHelper not assigned!");
            }
        }

        private IEnumerator TriggerAnalysisWithTimeout(string content)
        {
            Debug.Log($"Starting analysis with {analysisTimeoutSeconds}s timeout for content: {content.Substring(0, Mathf.Min(50, content.Length))}...");
            
            // Start the analysis
            analysisHelper.StartWorkflowWithText(content);
            
            float elapsed = 0f;
            bool analysisCompleted = false;
            
            // Subscribe to completion event
            System.Action<string> onCompleteHandler = (response) => {
                analysisCompleted = true;
                OnAnalysisCompleted();
            };
            
            if (API.Instance != null)
            {
                API.Instance.OnAnalysisComplete += onCompleteHandler;
            }
            
            // Wait for completion or timeout
            while (!analysisCompleted && elapsed < analysisTimeoutSeconds)
            {
                yield return new WaitForSeconds(0.1f);
                elapsed += 0.1f;
                
                // Check if analysis is no longer running
                if (API.Instance != null && !API.Instance.IsAnalyzing)
                {
                    break;
                }
            }
            
            // Cleanup
            if (API.Instance != null)
            {
                API.Instance.OnAnalysisComplete -= onCompleteHandler;
            }
            
            if (elapsed >= analysisTimeoutSeconds)
            {
                Debug.LogWarning($"Analysis timed out after {analysisTimeoutSeconds} seconds");
                OnAnalysisCompleted(); // Still disable interactions on timeout
            }
        }

        private void OnAnalysisCompleted()
        {
            Debug.Log("BookManager: Analysis completed, disabling book interactions but keeping current page");
            // Disable interactions but don't close the book or change pages
            SetBookInteractionEnabled(false);
        }

        [ContextMenu("Trigger Manual Lore (Inspector Assignment)")]
        public void TriggerManualLore()
        {
            if (string.IsNullOrEmpty(manualLoreFilePath))
            {
                Debug.LogWarning("Manual lore file path is not assigned in the inspector!");
                return;
            }

            // Find LoreJsonReader in the scene
            LoreJsonReader loreReader = FindObjectOfType<LoreJsonReader>();
            if (loreReader == null)
            {
                Debug.LogError("LoreJsonReader not found in the scene! Cannot load manual lore.");
                return;
            }

            Debug.Log($"Triggering manual lore load from inspector assignment: {manualLoreFilePath}");
            
            // Load the manually assigned lore file
            loreReader.LoadSpecificLoreFile(manualLoreFilePath);
            
            // Disable book interactions after loading manual lore
            SetBookInteractionEnabled(false);
        }

        string GetCurrentPageContentAsString()
        {
            if (pageContentData?.pages == null)
            {
                Debug.LogError("Page content data not loaded!");
                return "";
            }

            PageContent pageContent = null;
            foreach (var page in pageContentData.pages)
            {
                if (page.page_index == currentPageIndex)
                {
                    pageContent = page;
                    break;
                }
            }

            if (pageContent == null)
            {
                Debug.LogWarning($"No content found for page index {currentPageIndex}, using first page as fallback");
                pageContent = pageContentData.pages.Length > 0 ? pageContentData.pages[0] : null;
            }

            if (pageContent == null)
            {
                return "";
            }

            // Format as a single string with all the information
            return $"标题：{pageContent.title}\n描述：{pageContent.description}\n化学方程式：{pageContent.chemical_equation}";
        }
    }
}