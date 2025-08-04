using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;
using System.IO;
using Elementor.Core;

namespace Elementor
{
    public class API : MonoBehaviour
    {
        [System.Serializable]
        public class ChemicalResponse
        {
            public string scene_id;
            public StoryData story;
            public ReactionData reaction;
            public ElectronTransferData electron_transfer;
            public GameplayTriggerData gameplay_trigger;
        }

        [System.Serializable]
        public class StoryData
        {
            public string title;
            public string[] plot;
        }

        [System.Serializable]
        public class ReactionData
        {
            public string equation;
            public string type;
            public string[] conditions;
            public ReactantData[] reactants;
            public ProductData[] products;
        }

        [System.Serializable]
        public class ReactantData
        {
            public string name;
            public string type;
            public int count;
            public ElementData[] elements;
        }

        [System.Serializable]
        public class ProductData
        {
            public string name;
            public string type;
            public int count;
            public ElementData[] elements;
        }

        [System.Serializable]
        public class ElementData
        {
            public string element;
            public int count;
        }

        [System.Serializable]
        public class ElectronTransferData
        {
            public string from;
            public string to;
            public int electron_count;
            public string description;
        }

        [System.Serializable]
        public class GameplayTriggerData
        {
            public RequiredIonData[] required_ions;
            public string reaction_area;
            public SuccessEffectsData success_effects;
        }

        [System.Serializable]
        public class RequiredIonData
        {
            public string name;
            public string from;
            public ElementData[] elements;
        }

        [System.Serializable]
        public class SuccessEffectsData
        {
            public string animation;
            public string[] new_items;
            public bool story_continuation;
        }

        // API configuration - now loaded from environment or resources
        private string apiKey => APIConfigManager.Config.openai_api_key;
        private string apiUrl => APIConfigManager.Config.openai_api_url;

        // UI组件
        [Header("UI Components")]
        public TMP_InputField equationInput;
        public Button analyzeButton;
        public TMP_Text resultText;

        [HideInInspector]public string chemicalFormula = "";

        public System.Action<string> OnAnalysisComplete;

        public bool IsAnalyzing { get; private set; } = false;

        void Start()
        {
            // Validate API configuration
            if (!APIConfigManager.ValidateConfiguration())
            {
                Debug.LogError("API Configuration is incomplete. Please check your environment variables or Resources/APIConfig.json file.");
            }

            // 设置按钮点击事件
            if (analyzeButton != null)
            {
                analyzeButton.onClick.AddListener(OnAnalyzeButtonClick);
            }
        }

        public void OnAnalyzeButtonClick()
        {
            if (equationInput != null && !string.IsNullOrEmpty(equationInput.text))
            {
                string chemicalFormula = equationInput.text;
                StartCoroutine(AnalyzeChemicalFormula(chemicalFormula));
            }
        }

        public void StartAnalysisFromImage()
        {
            if (IsAnalyzing)
            {
                Debug.LogWarning("Analysis is already in progress.");
                return;
            }

            StartCoroutine(AnalyzeChemicalFormula(chemicalFormula));
        }

        IEnumerator AnalyzeChemicalFormula(string chemicalFormula)
        {
            IsAnalyzing = true;
            resultText.text = "Loading...";

            // 构造system prompt
            string systemPrompt = @"你是一个为化学教育游戏生成剧情与交互数据的AI引擎，根据我给出的图片提取化学方程式并教学。游戏背景设定如下：在世界的中心矗立着元素山，由金属堡与非金属谷双峰构成。金属（金属性越强越躁动），不愿携带能量球；非金属居民（非金属性越强越躁动），善于收集与操控能量球。千百年来，两族隔山相望，彼此误解，文明也停止在相对简单的阶段。某一天山下出现了""电离领域""，是唯一能连接两族的神秘地区。元素们陆续下山，在此抛弃或吸收电子球，获得对应灵力，化身为更强大的离子个体、军团或商队，在此之后互相结盟，产生新物质。玩家作为旁观者，见证来来往往的联盟变化，一次次踏入电离之河，签订新契约，发生新故事，产生新物质。请你：1. 分析反应物、生成物、反应条件、电子流动路径；2. 根据游戏世界观创作剧情大标题与3~4句15字以内的剧情短句，""标题"": ""包含角色，概括剧情"", ""情节"": [ ""开始（根据情况分成两到三句）：故事性表达：拆解反应物集团，解释每个离子个人，军团或商队（展现和电子球关联）的来历以及契约者决定分开/取消合作的理由"", ""发展：离子们在离子河产生围绕电子球的争夺/交易/合作，有故事性的接触或冲突，生成新的生成物集团，达成新的契约"", ""结尾：可选结尾句，总结或留下悬念"" ]加入奇幻故事性，除了精准的单质元素名称（字母表示）和电子球外避免直接的化学描述（离子团），语言简洁易懂，故事简洁经典具有逻辑性，展现反应过程，每句15字以内。；3. 精确列出反应中每个化学物质所含元素的种类与个数，明确电子的转移方向与数量；4. 输出一份完整的 JSON 文件，结构必须符合以下字段规范（字段名、顺序与嵌套不可更改）：JSON结构字段说明：{ ""scene_id"": ""string（唯一场景ID）"", ""story"": { ""title"": ""string（剧情标题）"", ""plot"": [""string"", ""string"", ""string"", ""string（每句≤10字）""] }, ""reaction"": { ""equation"": ""string（配平反应式）"", ""type"": ""string（反应类型）"", ""conditions"": [""string"", ...], ""reactants"": [ { ""name"": ""string（化学式）"", ""type"": ""单质 / 离子团"", ""count"": int（反应系数）, ""elements"": [ { ""element"": ""string"", ""count"": int } ] } ], ""products"": [ { ""name"": ""string"", ""type"": ""单质 / 离子团"", ""count"": int, ""elements"": [ { ""element"": ""string"", ""count"": int } ] } ] }, ""electron_transfer"": { ""from"": ""string（失电子物）"", ""to"": ""string（得电子物）"", ""electron_count"": int, ""description"": ""string（电子流简述）"" }, ""gameplay_trigger"": { ""required_ions"": [ { ""name"": ""string（离子名称）"", ""from"": ""string（来源化合物）"", ""elements"": [ { ""element"": ""string"", ""count"": int } ] } ], ""reaction_area"": ""string（反应台名称）"", ""success_effects"": { ""animation"": ""string（动画代号）"", ""new_items"": [""string"", ...], ""story_continuation"": true } } } }。请严格按照此JSON格式返回，不要添加任何其他内容。";

            // 读取图片文件 - 修改为使用StreamingAssets/Images/路径
            string imagePath = Path.Combine(Application.streamingAssetsPath, "Images", "zn.jpg");
            if (!System.IO.File.Exists(imagePath))
            {
                Debug.LogError("Image file not found: " + imagePath);
                resultText.text = "Error: Image file not found";
                IsAnalyzing = false;
                OnAnalysisComplete?.Invoke("{}");
                yield break;
            }

            // 读取图片并转换为base64
            byte[] imageBytes = System.IO.File.ReadAllBytes(imagePath);
            string base64Image = System.Convert.ToBase64String(imageBytes);

            string jsonBody = @"{
        ""model"": ""gpt-4o"",
        ""messages"": [
        {
            ""role"": ""system"",
            ""content"": """ + systemPrompt.Replace("\"", "\\\"") + @"""
        },
        {
            ""role"": ""user"",
            ""content"": [
                {
                    ""type"": ""text"",
                    ""text"": ""请分析这张图片中的化学方程式，并按照指定的JSON格式返回结果。""
                },
                {
                    ""type"": ""image_url"",
                    ""image_url"": {
                        ""url"": ""data:image/jpeg;base64," + base64Image + @"""
                    }
                }
            ]
        }
        ],
        ""max_tokens"": 4000,
        ""temperature"": 0.1
        }   ";

            // 创建请求
            UnityWebRequest request = new UnityWebRequest(apiUrl, "POST");
            request.redirectLimit = 10; // 设置重定向限制
            request.timeout = 60; // 设置60秒超时
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + apiKey);
            request.SetRequestHeader("Accept", "application/json");

            // 调试：打印请求信息
            Debug.Log("Request URL: " + apiUrl);
            Debug.Log("Request Body: " + jsonBody);

            // 发送请求
            yield return request.SendWebRequest();

            // 处理响应
            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseText = request.downloadHandler.text;
                Debug.Log("Complete AI response: " + responseText);
                ParseChemicalResponse(responseText);
            }
            else
            {
                Debug.LogError("Error: " + request.error);
                Debug.LogError("Response Code: " + request.responseCode);
                Debug.LogError("Response Headers: " + request.GetResponseHeader("Content-Type"));
                resultText.text = "Error: " + request.error;

                // Notify completion even on error
                IsAnalyzing = false;
                OnAnalysisComplete?.Invoke("{}");
            }
        }

        // 解析化学响应
        void ParseChemicalResponse(string jsonResponse)
        {
            Debug.Log("Complete AI response content: " + jsonResponse);
            resultText.text = "Complete AI response content:\n" + jsonResponse;

            // Extract the actual JSON from the response
            string cleanedJson = ExtractJsonFromResponse(jsonResponse);

            // Mark analysis as complete and notify
            IsAnalyzing = false;
            OnAnalysisComplete?.Invoke(cleanedJson);
        }

        // Helper method to extract JSON from API response
        private string ExtractJsonFromResponse(string responseText)
        {
            try
            {
                // Parse the API response to get the actual content
                var apiResponse = JsonUtility.FromJson<ApiResponse>(responseText);
                if (apiResponse?.choices != null && apiResponse.choices.Length > 0)
                {
                    string content = apiResponse.choices[0].message.content;
                    return CleanJsonContent(content);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to parse API response: {ex.Message}");
            }
            
            // Fallback: try to clean the original response
            return CleanJsonContent(responseText);
        }
        
        // Helper method to clean JSON content from markdown formatting
        private string CleanJsonContent(string content)
        {
            if (string.IsNullOrEmpty(content))
                return "{}";
                
            // Remove markdown code block formatting
            content = content.Trim();
            
            // Remove ```json at the beginning
            if (content.StartsWith("```json"))
            {
                content = content.Substring(7);
            }
            else if (content.StartsWith("```"))
            {
                content = content.Substring(3);
            }
            
            // Remove ``` at the end
            if (content.EndsWith("```"))
            {
                content = content.Substring(0, content.Length - 3);
            }
            
            // Trim whitespace again
            content = content.Trim();
            
            // Validate that it starts and ends with braces
            if (!content.StartsWith("{") || !content.EndsWith("}"))
            {
                Debug.LogWarning("Content doesn't appear to be valid JSON format");
                // Try to find JSON within the content
                int startIndex = content.IndexOf('{');
                int endIndex = content.LastIndexOf('}');
                
                if (startIndex >= 0 && endIndex > startIndex)
                {
                    content = content.Substring(startIndex, endIndex - startIndex + 1);
                }
                else
                {
                    Debug.LogError("Could not extract valid JSON from response");
                    return "{}";
                }
            }
            
            return content;
        }

        [System.Serializable]
        private class ApiResponse
        {
            public Choice[] choices;
        }

        [System.Serializable]
        private class Choice
        {
            public Message message;
        }

        [System.Serializable]
        private class Message
        {
            public string content;
        }

        public void AnalyzeFormula(string formula)
        {
            chemicalFormula = formula;
            StartCoroutine(AnalyzeChemicalFormula(formula));
        }

        public void GenerateDialogue(string prompt)
        {
            StartCoroutine(GenerateDialogueCoroutine(prompt));
        }

        IEnumerator GenerateDialogueCoroutine(string prompt)
        {
            IsAnalyzing = true;

            // 构造简化的system prompt for dialogue
            string systemPrompt = "你是一个化学教育游戏的角色对话生成器。根据给出的角色信息和情境，生成一句简短、符合角色特点的对话。对话应该简洁有趣，不超过15个字。只返回对话内容，不要其他格式。";

            string jsonBody = @"{
        ""model"": ""gpt-4o"",
        ""messages"": [
        {
            ""role"": ""system"",
            ""content"": """ + systemPrompt.Replace("\"", "\\\"") + @"""
        },
        {
            ""role"": ""user"",
            ""content"": """ + prompt.Replace("\"", "\\\"") + @"""
        }
        ],
        ""max_tokens"": 100,
        ""temperature"": 0.7
        }";

            // 创建请求
            UnityWebRequest request = new UnityWebRequest(apiUrl, "POST");
            request.redirectLimit = 10;
            request.timeout = 30; // Shorter timeout for dialogue
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + apiKey);
            request.SetRequestHeader("Accept", "application/json");

            Debug.Log("Generating dialogue with prompt: " + prompt);

            // 发送请求
            yield return request.SendWebRequest();

            // 处理响应
            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseText = request.downloadHandler.text;
                Debug.Log("Dialogue generation response: " + responseText);
                
                // Extract dialogue content
                string dialogueContent = ExtractDialogueContent(responseText);
                
                // Notify completion
                IsAnalyzing = false;
                OnAnalysisComplete?.Invoke(dialogueContent);
            }
            else
            {
                Debug.LogError("Dialogue generation error: " + request.error);
                IsAnalyzing = false;
                OnAnalysisComplete?.Invoke("");
            }
        }

        private string ExtractDialogueContent(string responseText)
        {
            try
            {
                var apiResponse = JsonUtility.FromJson<ApiResponse>(responseText);
                if (apiResponse?.choices != null && apiResponse.choices.Length > 0)
                {
                    string content = apiResponse.choices[0].message.content;
                    return content.Trim().Trim('"'); // Remove quotes if present
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to extract dialogue content: {ex.Message}");
            }
            
            return "";
        }
    }
}