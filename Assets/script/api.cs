using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Elementor
{

    public class ChemicalAssistant2 : MonoBehaviour
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

        // API配置
        public string apiKey = "sk-SSOirqmgtsdKAmhsmKZbVc6NftMo5MCdEgoSQbxh7kbSIwHL";
        public string apiUrl = "https://yibuapi.com/v1/chat/completions";

        // UI组件
        [Header("UI Components")]
        public TMP_InputField equationInput; // 化学方程式输入框
        public Button analyzeButton; // 分析按钮
        public TMP_Text resultText; // 结果显示文本

        // 化学式
        public string chemicalFormula = "H2O";

        void Start()
        {
            // 设置按钮点击事件
            if (analyzeButton != null)
            {
                analyzeButton.onClick.AddListener(OnAnalyzeButtonClick);
            }
        }

        void Update()
        {
            // 按A键快速测试
            if (Input.GetKeyDown(KeyCode.A))
            {
                StartCoroutine(TestCoroutine());
            }
        }

        private IEnumerator TestCoroutine()
        {
            yield return new WaitForSeconds(0.1f);
            test();
        }

        public async Task test()
        {
            // 按A键时直接调用化学分析，使用默认化学式
            StartCoroutine(AnalyzeChemicalFormula(chemicalFormula));
            await Task.Delay(1000);
        }

        // 按钮点击事件
        public void OnAnalyzeButtonClick()
        {
            if (equationInput != null && !string.IsNullOrEmpty(equationInput.text))
            {
                string chemicalFormula = equationInput.text;
                StartCoroutine(AnalyzeChemicalFormula(chemicalFormula));
            }
        }

        // 分析化学方程式（现在支持图片输入）
        IEnumerator AnalyzeChemicalFormula(string chemicalFormula)
        {
            resultText.text = "Loading...";

            // 构造system prompt
            string systemPrompt = @"你是一个为化学教育游戏生成剧情与交互数据的AI引擎，根据我给出的图片提取化学方程式并教学。游戏背景设定如下：在世界的中心矗立着元素山，由金属堡与非金属谷双峰构成。金属（金属性越强越躁动），不愿携带能量球；非金属居民（非金属性越强越躁动），善于收集与操控能量球。千百年来，两族隔山相望，彼此误解，文明也停止在相对简单的阶段。某一天山下出现了""电离领域""，是唯一能连接两族的神秘地区。元素们陆续下山，在此抛弃或吸收电子球，获得对应灵力，化身为更强大的离子个体、军团或商队，在此之后互相结盟，产生新物质。玩家作为旁观者，见证来来往往的联盟变化，一次次踏入电离之河，签订新契约，发生新故事，产生新物质。请你：1. 分析反应物、生成物、反应条件、电子流动路径；2. 根据游戏世界观创作剧情大标题与3~4句15字以内的剧情短句，""标题"": ""包含角色，概括剧情"", ""情节"": [ ""开始（根据情况分成两到三句）：故事性表达：拆解反应物集团，解释每个离子个人，军团或商队（展现和电子球关联）的来历以及契约者决定分开/取消合作的理由"", ""发展：离子们在离子河产生围绕电子球的争夺/交易/合作，有故事性的接触或冲突，生成新的生成物集团，达成新的契约"", ""结尾：可选结尾句，总结或留下悬念"" ]加入奇幻故事性，除了精准的单质元素名称（字母表示）和电子球外避免直接的化学描述（离子团），语言简洁易懂，故事简洁经典具有逻辑性，展现反应过程，每句15字以内。；3. 精确列出反应中每个化学物质所含元素的种类与个数，明确电子的转移方向与数量；4. 输出一份完整的 JSON 文件，结构必须符合以下字段规范（字段名、顺序与嵌套不可更改）：JSON结构字段说明：{ ""scene_id"": ""string（唯一场景ID）"", ""story"": { ""title"": ""string（剧情标题）"", ""plot"": [""string"", ""string"", ""string"", ""string（每句≤10字）""] }, ""reaction"": { ""equation"": ""string（配平反应式）"", ""type"": ""string（反应类型）"", ""conditions"": [""string"", ...], ""reactants"": [ { ""name"": ""string（化学式）"", ""type"": ""单质 / 离子团"", ""count"": int（反应系数）, ""elements"": [ { ""element"": ""string"", ""count"": int } ] } ], ""products"": [ { ""name"": ""string"", ""type"": ""单质 / 离子团"", ""count"": int, ""elements"": [ { ""element"": ""string"", ""count"": int } ] } ] }, ""electron_transfer"": { ""from"": ""string（失电子物）"", ""to"": ""string（得电子物）"", ""electron_count"": int, ""description"": ""string（电子流简述）"" }, ""gameplay_trigger"": { ""required_ions"": [ { ""name"": ""string（离子名称）"", ""from"": ""string（来源化合物）"", ""elements"": [ { ""element"": ""string"", ""count"": int } ] } ], ""reaction_area"": ""string（反应台名称）"", ""success_effects"": { ""animation"": ""string（动画代号）"", ""new_items"": [""string"", ...], ""story_continuation"": true } } } }。请严格按照此JSON格式返回，不要添加任何其他内容。";

            // 读取图片文件
            string imagePath = "";
            
           
                // 如果没有保存的图片，使用默认图片
                imagePath = Application.dataPath + "/zn.jpg";
                

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
                Debug.Log("🔍 完整的AI返回内容: " + responseText);
                ParseChemicalResponse(responseText);
            }
            else
            {
                Debug.LogError("Error: " + request.error);
                Debug.LogError("Response Code: " + request.responseCode);
                Debug.LogError("Response Headers: " + request.GetResponseHeader("Content-Type"));
                resultText.text = "Error: " + request.error;
            }
        }

        // 解析化学响应
        void ParseChemicalResponse(string jsonResponse)
        {
            // 直接显示完整的AI返回内容
            Debug.Log("📦 完整的AI返回内容: " + jsonResponse);
            resultText.text = "完整的AI返回内容:\n" + jsonResponse;
        }

        // 公共方法，可以从外部调用
        public void AnalyzeFormula(string formula)
        {
            chemicalFormula = formula;
            StartCoroutine(AnalyzeChemicalFormula(formula));
        }
    }
}