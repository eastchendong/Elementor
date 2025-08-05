using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO;
using Elementor.Core;

namespace Elementor
{
    public class API : MonoBehaviour
    {
        public static API Instance { get; private set; }

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

        [System.Serializable]
        public class SynthesisResponse
        {
            public bool can_synthesize;
            public string compound_formula;
            public string compound_name;
            public string explanation;
        }

        [HideInInspector] public string chemicalFormula = "";

        public System.Action<string> OnAnalysisComplete;
        public System.Action<SynthesisResponse> OnSynthesisCheckComplete;

        public bool IsAnalyzing => HttpRequestManager.Instance?.IsRequesting ?? false;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
        }

        public void StartAnalysisFromImage()
        {
            if (IsAnalyzing)
            {
                Debug.LogWarning("Analysis is already in progress.");
                return;
            }

            AnalyzeChemicalFormula();
        }

        void AnalyzeChemicalFormula()
        {
            string systemPrompt = @"你是一个为化学教育游戏生成剧情与交互数据的AI引擎，根据我给出的图片提取化学方程式并教学。游戏背景设定如下：在世界的中心矗立着元素山，由金属堡与非金属谷双峰构成。金属（金属性越强越躁动），不愿携带能量球；非金属居民（非金属性越强越躁动），善于收集与操控能量球。千百年来，两族隔山相望，彼此误解，文明也停止在相对简单的阶段。某一天山下出现了""电离领域""，是唯一能连接两族的神秘地区。元素们陆续下山，在此抛弃或吸收电子球，获得对应灵力，化身为更强大的离子个体、军团或商队，在此之后互相结盟，产生新物质。玩家作为旁观者，见证来来往往的联盟变化，一次次踏入电离之河，签订新契约，发生新故事，产生新物质。请你：1. 分析反应物、生成物、反应条件、电子流动路径；2. 根据游戏世界观创作剧情大标题与3~4句15字以内的剧情短句，""标题"": ""包含角色，概括剧情"", ""情节"": [ ""开始（根据情况分成两到三句）：故事性表达：拆解反应物集团，解释每个离子个人，军团或商队（展现和电子球关联）的来历以及契约者决定分开/取消合作的理由"", ""发展：离子们在离子河产生围绕电子球的争夺/交易/合作，有故事性的接触或冲突，生成新的生成物集团，达成新的契约"", ""结尾：可选结尾句，总结或留下悬念"" ]加入奇幻故事性，除了精准的单质元素名称（字母表示）和电子球外避免直接的化学描述（离子团），语言简洁易懂，故事简洁经典具有逻辑性，展现反应过程，每句15字以内。；3. 精确列出反应中每个化学物质所含元素的种类与个数，明确电子的转移方向与数量；4. 输出一份完整的 JSON 文件，结构必须符合以下字段规范（字段名、顺序与嵌套不可更改）：JSON结构字段说明：{ ""scene_id"": ""string（唯一场景ID）"", ""story"": { ""title"": ""string（剧情标题）"", ""plot"": [""string"", ""string"", ""string"", ""string（每句≤10字）""] }, ""reaction"": { ""equation"": ""string（配平反应式）"", ""type"": ""string（反应类型）"", ""conditions"": [""string"", ...], ""reactants"": [ { ""name"": ""string（化学式）"", ""type"": ""单质 / 离子团"", ""count"": int（反应系数）, ""elements"": [ { ""element"": ""string"", ""count"": int } ] } ], ""products"": [ { ""name"": ""string"", ""type"": ""单质 / 离子团"", ""count"": int, ""elements"": [ { ""element"": ""string"", ""count"": int } ] } ] }, ""electron_transfer"": { ""from"": ""string（失电子物）"", ""to"": ""string（得电子物）"", ""electron_count"": int, ""description"": ""string（电子流简述）"" }, ""gameplay_trigger"": { ""required_ions"": [ { ""name"": ""string（离子名称）"", ""from"": ""string（来源化合物）"", ""elements"": [ { ""element"": ""string"", ""count"": int } ] } ], ""reaction_area"": ""string（反应台名称）"", ""success_effects"": { ""animation"": ""string（动画代号）"", ""new_items"": [""string"", ...], ""story_continuation"": true } } } }。请严格按照此JSON格式返回，不要添加任何其他内容。";

            string imagePath = Path.Combine(Application.streamingAssetsPath, "Images", "zn.jpg");
            if (!System.IO.File.Exists(imagePath))
            {
                Debug.LogError("Image file not found: " + imagePath);
                OnAnalysisComplete?.Invoke("{}");
                return;
            }

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
        }";

            HttpRequestManager.Instance?.SendRequest(
                jsonBody,
                OnAnalysisSuccess,
                OnAnalysisError
            );
        }

        void OnAnalysisSuccess(string responseText)
        {
            Debug.Log("Chemical analysis response received");
            string cleanedJson = HttpRequestManager.ExtractJsonFromResponse(responseText);
            OnAnalysisComplete?.Invoke(cleanedJson);
        }

        void OnAnalysisError(string error)
        {
            Debug.LogError("Chemical analysis failed: " + error);
            OnAnalysisComplete?.Invoke("{}");
        }

        public void GenerateDialogue(string prompt)
        {
            string systemPrompt = "你是一个化学教育游戏的角色对话生成器。根据给出的角色信息和情境，生成一句简短、符合角色特点的对话。对话应该简洁有趣，不超过15个字。只返回对话内容，不要其他格式。";

            string sanitizedPrompt = HttpRequestManager.SanitizeJsonString(prompt);
            string sanitizedSystemPrompt = HttpRequestManager.SanitizeJsonString(systemPrompt);

            string jsonBody = @"{
        ""model"": ""gpt-4o"",
        ""messages"": [
        {
            ""role"": ""system"",
            ""content"": """ + sanitizedSystemPrompt + @"""
        },
        {
            ""role"": ""user"",
            ""content"": """ + sanitizedPrompt + @"""
        }
        ],
        ""max_tokens"": 100,
        ""temperature"": 0.7
        }";

            Debug.Log("Generating dialogue with prompt: " + prompt);

            HttpRequestManager.Instance?.SendRequest(
                jsonBody,
                OnDialogueSuccess,
                OnDialogueError
            );
        }

        void OnDialogueSuccess(string responseText)
        {
            string dialogueContent = ExtractDialogueContent(responseText);
            OnAnalysisComplete?.Invoke(dialogueContent);
        }

        void OnDialogueError(string error)
        {
            Debug.LogError("Dialogue generation failed: " + error);
            OnAnalysisComplete?.Invoke("");
        }

        private string ExtractDialogueContent(string responseText)
        {
            try
            {
                string cleanedJson = HttpRequestManager.ExtractJsonFromResponse(responseText);
                var apiResponse = JsonUtility.FromJson<HttpRequestManager.ApiResponse>(cleanedJson);
                if (apiResponse?.choices != null && apiResponse.choices.Length > 0)
                {
                    string content = apiResponse.choices[0].message.content;
                    return content.Trim().Trim('"');
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to extract dialogue content: {ex.Message}");
            }
            
            return "";
        }

        public void CheckSynthesisPossibility(List<string> elementNames)
        {
            if (IsAnalyzing)
            {
                Debug.LogWarning("Analysis is already in progress.");
                return;
            }

            Dictionary<string, int> elementCounts = new Dictionary<string, int>();
            foreach (string element in elementNames)
            {
                if (elementCounts.ContainsKey(element))
                    elementCounts[element]++;
                else
                    elementCounts[element] = 1;
            }

            string elementList = string.Join(", ", elementCounts.Select(kvp => $"{kvp.Value}个{kvp.Key}"));

            string systemPrompt = @"你是一个化学合成判断AI。给定一组元素和它们的数量，判断是否能合成一个合理的化合物。

规则：
1. 只考虑元素原子个数的组合，判断能否形成稳定的化学化合物
2. 包括但不限于：单质分子（如H2, O2, N2, Cl2, Br2, I2, F2等）、离子化合物、共价化合物
3. 所有给定的元素都必须被使用，不能有剩余
4. 常见的合成例子：
   - 2个H → H2（氢气）
   - 2个Cl → Cl2（氯气）
   - 1个Na + 1个Cl → NaCl（氯化钠）
   - 1个Fe + 2个Cl → FeCl2（氯化亚铁）
   - 2个H + 1个O → H2O（水）
5. 如果能合成，给出标准的化学分子式和化合物名称
6. 如果不能合成，说明具体原因

请严格按照以下JSON格式返回：
{
    ""can_synthesize"": true/false,
    ""compound_formula"": ""化合物分子式（如果能合成）"",
    ""compound_name"": ""化合物名称（如果能合成）"",
    ""explanation"": ""简短解释""
}";

            string userPrompt = $"请判断以下元素是否能合成化合物：{elementList}";

            string sanitizedSystemPrompt = HttpRequestManager.SanitizeJsonString(systemPrompt);
            string sanitizedUserPrompt = HttpRequestManager.SanitizeJsonString(userPrompt);

            string jsonBody = @"{
        ""model"": ""gpt-4o"",
        ""messages"": [
        {
            ""role"": ""system"",
            ""content"": """ + sanitizedSystemPrompt + @"""
        },
        {
            ""role"": ""user"",
            ""content"": """ + sanitizedUserPrompt + @"""
        }
        ],
        ""max_tokens"": 500,
        ""temperature"": 0.1
        }";

            Debug.Log("Checking synthesis possibility for: " + elementList);

            HttpRequestManager.Instance?.SendRequest(
                jsonBody,
                OnSynthesisSuccess,
                OnSynthesisError
            );
        }

        void OnSynthesisSuccess(string responseText)
        {
            try
            {
                string cleanedJson = HttpRequestManager.ExtractJsonFromResponse(responseText);
                SynthesisResponse response = JsonUtility.FromJson<SynthesisResponse>(cleanedJson);
                
                if (response != null)
                {
                    Debug.Log($"Synthesis result: {response.can_synthesize}, Formula: {response.compound_formula}");
                    OnSynthesisCheckComplete?.Invoke(response);
                }
                else
                {
                    OnSynthesisCheckComplete?.Invoke(new SynthesisResponse 
                    { 
                        can_synthesize = false, 
                        explanation = "解析响应失败" 
                    });
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error parsing synthesis response: {ex.Message}");
                OnSynthesisCheckComplete?.Invoke(new SynthesisResponse 
                { 
                    can_synthesize = false, 
                    explanation = "解析错误" 
                });
            }
        }

        void OnSynthesisError(string error)
        {
            Debug.LogError("Synthesis check failed: " + error);
            OnSynthesisCheckComplete?.Invoke(new SynthesisResponse 
            { 
                can_synthesize = false, 
                explanation = "API调用失败" 
            });
        }

        public void AnalyzeFormula() => StartAnalysisFromImage();
    }
}