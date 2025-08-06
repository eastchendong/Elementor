using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace Elementor
{
    public class BookPagePrinter : MonoBehaviour
    {
        public ScriptBoy.ProceduralBook.Book book;
        public int currentPageIndex; // Inspector里显示当前页码

        [Header("Integration")]
        public ChemicalAnalysisHelper analysisHelper;
        
        [Header("Settings")]
        public string pageContentFile = "page_content.json";

        private int lastPageIndex = -1;
        private PageContentData pageContentData;

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
        }

        void LoadPageContentData()
        {
            string filePath = Path.Combine(Application.streamingAssetsPath, pageContentFile);
            if (File.Exists(filePath))
            {
                try
                {
                    string jsonContent = File.ReadAllText(filePath);
                    pageContentData = JsonUtility.FromJson<PageContentData>(jsonContent);
                    Debug.Log($"BookPagePrinter loaded page content data with {pageContentData.pages.Length} pages");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Failed to load page content data: {ex.Message}");
                }
            }
            else
            {
                Debug.LogError($"Page content file not found: {filePath}");
            }
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
            // You can add auto-trigger here if needed
            // if (analysisHelper != null)
            // {
            //     TriggerAnalysisForCurrentPage();
            // }
        }

        public void PrintCurrentPage()
        {
            Debug.Log("当前显示页码索引: " + currentPageIndex);
        }

        public void TriggerAnalysisForCurrentPage()
        {
            if (analysisHelper != null)
            {
                string content = GetCurrentPageContentAsString();
                if (!string.IsNullOrEmpty(content))
                {
                    analysisHelper.StartWorkflowWithText(content);
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

        /// <summary>
        /// Get formatted text content for current page
        /// </summary>
        public string GetCurrentPageTextContent()
        {
            return GetCurrentPageContentAsString();
        }
    }
}