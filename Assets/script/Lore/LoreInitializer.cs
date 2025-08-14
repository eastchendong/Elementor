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

        [Header("Element Spawning")]
        [SerializeField] private GameObject elementSpawnPoint; // 元素生成点GameObject
        [SerializeField] private ParticleSystem spawnEffect; // 召唤特效
        [SerializeField] private string elementSpawnPointTag = "ElementSpawnPoint"; // 元素生成点的Tag
        [SerializeField] private int elementSpawnStepIndex = 2; // 在第几步召唤元素生成点（从0开始）
        [SerializeField] private float effectDuration = 2f; // 特效持续时间

        [Header("Book Control")]
        [SerializeField] private BookManager bookManager; // 书本管理器的引用
        [SerializeField] private ScriptBoy.ProceduralBook.AutoTurningDemo autoTurningDemo; // 自动翻页控制器
        [SerializeField] private int bookOpenStepIndex = 3; // 在第几步打开书本（从0开始）
        [SerializeField] private int targetPageIndex = 0; // 目标页面索引（翻到第几页）

        private TutorialData currentTutorial;
        private int currentStepIndex = 0;
        private LoreJsonReader loreJsonReader;
        private bool hasFoundElementSpawnPoint = false; // 标记是否已找到元素生成点

        // Start is called before the first frame update
        void Start()
        {
            // Get reference to LoreJsonReader
            loreJsonReader = LoreJsonReader.Instance;
            if (loreJsonReader == null)
            {
                loreJsonReader = FindObjectOfType<LoreJsonReader>();
            }

            // Initialize element spawn point (disable it initially)
            InitializeElementSpawnPoint();

            // Setup UI
            SetupTutorialUI();

            // Auto-start tutorial if enabled
            if (autoStartTutorial)
            {
                StartTutorial();
            }
        }

        // Update is called once per frame
        void Update()
        {
            // 持续搜索元素生成点，直到找到为止
            if (!hasFoundElementSpawnPoint)
            {
                SearchForElementSpawnPoint();
            }
        }

        /// <summary>
        /// 搜索带有指定Tag的元素生成点
        /// </summary>
        private void SearchForElementSpawnPoint()
        {
            if (string.IsNullOrEmpty(elementSpawnPointTag))
            {
                Debug.LogWarning("⚠️ Element spawn point tag is not set!");
                return;
            }

            // 通过Tag查找元素生成点
            GameObject foundSpawnPoint = GameObject.FindGameObjectWithTag(elementSpawnPointTag);
            
            if (foundSpawnPoint != null)
            {
                elementSpawnPoint = foundSpawnPoint;
                Debug.Log($"🔮 Found element spawn point with tag: {elementSpawnPointTag}");

                // 在子物体中查找ParticleSystem
                ParticleSystem foundEffect = elementSpawnPoint.GetComponentInChildren<ParticleSystem>();
                if (foundEffect != null)
                {
                    spawnEffect = foundEffect;
                    Debug.Log("✨ Found spawn effect in child objects");

                    // 禁用特效
                    spawnEffect.gameObject.SetActive(false);
                    Debug.Log("✨ Spawn effect disabled initially");
                }
                else
                {
                    Debug.LogWarning("⚠️ No ParticleSystem found in element spawn point children!");
                }

                // 禁用元素生成点
                elementSpawnPoint.SetActive(false);
                Debug.Log("🔮 Element spawn point disabled initially");

                // 标记已找到
                hasFoundElementSpawnPoint = true;
            }
        }

        private void InitializeElementSpawnPoint()
        {
            // Disable the element spawn point at the beginning
            if (elementSpawnPoint != null)
            {
                elementSpawnPoint.SetActive(false);
                Debug.Log("🔮 Element spawn point disabled initially");
            }
            else
            {
                Debug.LogWarning("⚠️ Element spawn point GameObject not assigned!");
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

            // Check if we need to spawn element spawn point
            if (currentStepIndex == elementSpawnStepIndex)
            {
                StartCoroutine(SpawnElementPointWithEffect());
            }

            // Check if we need to open the book
            if (currentStepIndex == bookOpenStepIndex)
            {
                StartCoroutine(OpenBookWithoutInteraction());
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

        /// <summary>
        /// 协程：召唤元素生成点并播放特效
        /// </summary>
        private IEnumerator SpawnElementPointWithEffect()
        {
            Debug.Log("🌟 Starting element spawn point summoning...");

            // 启用元素生成点
            if (elementSpawnPoint != null)
            {
                elementSpawnPoint.SetActive(true);
                Debug.Log("🔮 Element spawn point activated!");

                // 播放召唤特效
                if (spawnEffect != null)
                {
                    // 确保特效GameObject也被激活
                    spawnEffect.gameObject.SetActive(true);
                    spawnEffect.Play();
                    Debug.Log("✨ Spawn effect started!");
                    
                    // 等待特效播放完成
                    yield return new WaitForSeconds(effectDuration);
                    
                    // 停止特效（如果需要的话）
                    if (spawnEffect.isPlaying)
                    {
                        spawnEffect.Stop();
                        Debug.Log("✨ Spawn effect stopped");
                    }
                }
                else
                {
                    Debug.LogWarning("⚠️ Spawn effect not assigned, skipping effect");
                    // 即使没有特效也等待一小段时间让玩家看到生成点出现
                    yield return new WaitForSeconds(1f);
                }
            }
            else
            {
                Debug.LogError("❌ Element spawn point GameObject not assigned!");
            }

            Debug.Log("🌟 Element spawn point summoning completed!");
        }

        /// <summary>
        /// 协程：打开书本但不允许交互，并翻到指定页面
        /// </summary>
        private IEnumerator OpenBookWithoutInteraction()
        {
            Debug.Log("📖 Opening book without interaction...");

            if (bookManager != null)
            {
                // 确保书本交互被禁用
                bookManager.SetBookInteractionEnabled(false);
                
                // 等待一小段时间确保设置生效
                yield return new WaitForSeconds(0.1f);

                // 打开书本
                if (bookManager.book != null )
                {
                    if (autoTurningDemo != null && targetPageIndex > 0)
                    {
                        Debug.Log($"📖 Turning to page {targetPageIndex}...");
                        
                        // 先翻到第一页
                        autoTurningDemo.AutoTurnFirst();
                        
                        // 等待翻到第一页完成
                        yield return new WaitForSeconds(1f);
                        
                        // 然后翻到目标页面
                        if (targetPageIndex > 0)
                        {
                            // 使用MultiAutoTurn翻到目标页面
                            autoTurningDemo.MultiAutoTurn(ScriptBoy.ProceduralBook.AutoTurnDirection.Next, targetPageIndex);
                            
                            // 等待翻页完成 (每页大约1秒，加上一些缓冲时间)
                            yield return new WaitForSeconds(targetPageIndex * 1.2f + 0.5f);
                        }
                        
                        Debug.Log($"📖 Successfully turned to page {targetPageIndex}");
                    }
                    else if (autoTurningDemo == null)
                    {
                        Debug.LogWarning("⚠️ AutoTurningDemo not assigned, cannot turn to specific page");
                    }
                    else
                    {
                        Debug.Log("📖 Target page is 0, staying at current page");
                    }
                }
                else
                {
                    Debug.LogError("❌ Book is not built or not assigned!");
                }
            }
            else
            {
                Debug.LogError("❌ BookManager not assigned!");
            }

            Debug.Log("📖 Book opening sequence completed!");
        }

        /// <summary>
        /// 公共方法：手动翻到指定页面（用于测试或外部调用）
        /// </summary>
        /// <param name="pageIndex">目标页面索引</param>
        public void TurnToPage(int pageIndex)
        {
            if (autoTurningDemo != null)
            {
                StartCoroutine(TurnToPageCoroutine(pageIndex));
            }
            else
            {
                Debug.LogError("❌ AutoTurningDemo not assigned!");
            }
        }

        /// <summary>
        /// 协程：翻到指定页面
        /// </summary>
        private IEnumerator TurnToPageCoroutine(int pageIndex)
        {
            if (pageIndex < 0)
            {
                Debug.LogWarning("⚠️ Page index cannot be negative");
                yield break;
            }

            Debug.Log($"📖 Turning to page {pageIndex}...");
            
            if (pageIndex == 0)
            {
                // 翻到第一页
                autoTurningDemo.AutoTurnFirst();
            }
            else
            {
                // 先翻到第一页，然后翻到目标页面
                autoTurningDemo.AutoTurnFirst();
                yield return new WaitForSeconds(1f);
                
                // 翻到目标页面
                autoTurningDemo.MultiAutoTurn(ScriptBoy.ProceduralBook.AutoTurnDirection.Next, pageIndex);
            }
            
            // 等待翻页完成
            yield return new WaitForSeconds(pageIndex * 1.2f + 1f);
            Debug.Log($"📖 Finished turning to page {pageIndex}");
        }

        /// <summary>
        /// 设置目标页面索引
        /// </summary>
        /// <param name="pageIndex">页面索引</param>
        public void SetTargetPage(int pageIndex)
        {
            targetPageIndex = pageIndex;
            Debug.Log($"📖 Target page set to: {targetPageIndex}");
        }

        /// <summary>
        /// 重置元素生成点搜索状态，允许重新搜索
        /// </summary>
        public void ResetElementSpawnPointSearch()
        {
            hasFoundElementSpawnPoint = false;
            elementSpawnPoint = null;
            spawnEffect = null;
            Debug.Log("🔄 Element spawn point search reset");
        }

        /// <summary>
        /// 手动设置元素生成点Tag
        /// </summary>
        /// <param name="tag">新的Tag</param>
        public void SetElementSpawnPointTag(string tag)
        {
            elementSpawnPointTag = tag;
            Debug.Log($"🏷️ Element spawn point tag set to: {tag}");
            
            // 重置搜索状态以使用新Tag
            ResetElementSpawnPointSearch();
        }
    }
}
