using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Elementor.Core;
using System.IO;

namespace Elementor
{
    public class API : MonoBehaviour
    {
        public static API Instance { get; private set; }

        public System.Action<string> OnAnalysisComplete;
        public System.Action<SynthesisResponse> OnSynthesisCheckComplete;
        public System.Action<string> OnDialogueGenerated; // Add dedicated dialogue event

        public bool IsAnalyzing => HttpRequestManager.Instance?.IsRequesting ?? false;

        private bool isDialogueRequesting = false; // Add separate flag for dialogue requests
        private bool isSelfIntroRequesting = false; // Add flag for self-introduction requests

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

        public void AnalyzeFromText(string content)
        {
            if (IsAnalyzing)
            {
                Debug.LogWarning("Analysis is already in progress.");
                return;
            }

            string systemPrompt = @"你是一名化学幻想游戏编剧助手，请根据输入的化学反应自动生成游戏剧情JSON。请严格遵循以下步骤和世界观设定：

            世界观核心设定
            核心理念： 原子即居民，物质即团体/联盟加现象，反应即故事，合作创繁荣（盐）
            1. 元素国度：
               - 金属镇
               - 非金属谷

            2. 物质联盟团体：
            -单质：单个居民（原子数为1）或某族兄弟/姐妹/朋友（原子数大于1时体现数量感）
              - 金属氧化卫队：金属和氧的联盟尝试，友好
               - 非金属氧化信使：非金属和氧的联盟尝试
               - 酸军团(酸类物质)：酸性，有攻击性
               - 碱军团(碱类物质)：碱性，性格强烈
               - 结晶盟约：盐，最终组织形态
             

            3.故事线：元素国度曾分裂为金属城与非金属城，隔绝而枯燥。智者氢族两姐妹与氧族两兄弟感受召唤，集结出发，在熊熊烈火里尝试签订了契约，拉起手来的瞬间他们感受到了奇异的变化，并召唤出了水，揭开元素组合的奇迹。他们将这件事传播，并身体力行地努力，希望大家更多合作，居民们纷纷尝试组合，形成特质鲜明的联盟团体，一开始团体规模较小相对友善，在酸碱两大集团出现后却也引发劫掠、净化等激烈冲突。后来，元素们领悟到：最狂暴的碰撞（酸+碱）反而催生最平和的结晶（盐+水），而盐盟约正是万物繁荣的基石。最终，金属与非金属通过盐晶紧密联结，共筑绚丽多彩的化合盛世。



            ### JSON生成规则
            严格按以下结构生成数据，严格保持字段顺序和嵌套，不要添加任何其他内容：
            {
              ""scene_id"": ""生成唯一ID（元素名缩写_反应类型）"",
            ""story"": {（剧情语言生动，有想象力和奇幻童话感）    
            ""title"": ""18字内标题，含角色名和剧情核心"",   
             ""plot"": [        ""≤50字：包含反应物身份（金属/非金属/联盟背景）人数和集结过程"",     
                                 ""≤50字：反应条件地点发生的友好联合/暴力冲突的反应剧情"",     
                                ""≤50字：新联盟体诞生，反应现象产生（避免纯粹化学术语）""
                                ],
              },
              ""reaction"": {
                ""equation"": ""配平的反应式"",
                ""type"": ""反应类型（）"",
                ""conditions"": [“仅在以下几种里选择输出：加热/点燃/无”],
                ""phenomena"": [仅在以下几种里选择输出：冒大气泡/冒小气泡/燃烧/升温/沉淀/产生液体/无],
                ""reactants"": [
                  {
                    ""name"": ""化学式"",
                    ""type"": ""仅在以下几种中选出：金属/非金属/金属氧化物/非金属氧化物/酸/碱/盐"",
                    ""reactant_count"": ""系数"",
                    ""elements"": [{""element"": ""元素符号"", ""count"": 原子数}]
                  }
                ],
                ""products"": [
                  {
                    ""name"": ""化学式"",
                    ""type"": ""仅在以下几种中选出：金属/非金属/金属氧化物/非金属氧化物/酸/碱/盐"",
                    ""product_count"": ""系数"",
                    ""elements"": [{""element"": ""元素符号"", ""count"": 原子数}]
                  }
                ]
              },
              "
        ;

            string userPrompt = $"请分析以下化学反应内容并生成剧情：\n{content}";

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
            
            // Save generated lore data to persistent storage for APK builds
            SaveGeneratedLoreData(cleanedJson);
            
            OnAnalysisComplete?.Invoke(cleanedJson);
        }

        void OnAnalysisError(string error)
        {
            Debug.LogError("Chemical analysis failed: " + error);
            OnAnalysisComplete?.Invoke("{}");
        }

        public void GenerateDialogue(string prompt)
        {
            if (isDialogueRequesting)
            {
                Debug.LogWarning("Dialogue generation is already in progress.");
                OnDialogueGenerated?.Invoke("");
                return;
            }

            if (HttpRequestManager.Instance?.IsRequesting == true)
            {
                Debug.LogWarning("Another API request is in progress, queuing dialogue request...");
                // Wait a bit and retry
                StartCoroutine(RetryDialogueGeneration(prompt, 1f));
                return;
            }

            isDialogueRequesting = true;

            string systemPrompt = @"你是一个化学教育游戏的角色对话生成器。根据给出的角色信息和情境，生成符合角色特点的对话。

请严格按照以下JSON格式返回对话内容：
{
    ""dialogues"": [
        {
            ""character"": ""元素名"",
            ""line"": ""对话内容（不超过20字）""
        }
    ]
}

要求：
1. 为每种不同的元素生成对话（相同元素只需一句代表性对话）
2. 对话要符合元素特性和当前情境
3. 每句对话不超过20个字
4. 对话要构成完整的剧情片段，体现化学反应的过程或结果
5. 严格按照JSON格式返回，不要添加任何其他内容";

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
        ""max_tokens"": 500,
        ""temperature"": 0.7
        }";

            Debug.Log("Generating dialogue with prompt: " + prompt);

            HttpRequestManager.Instance?.SendRequest(
                jsonBody,
                OnDialogueSuccess,
                OnDialogueError
            );
        }

        private IEnumerator RetryDialogueGeneration(string prompt, float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (!HttpRequestManager.Instance?.IsRequesting == true)
            {
                GenerateDialogue(prompt);
            }
            else
            {
                Debug.LogWarning("API still busy, failing dialogue generation");
                OnDialogueGenerated?.Invoke("");
            }
        }

        void OnDialogueSuccess(string responseText)
        {
            isDialogueRequesting = false;
            
            try
            {
                string cleanedJson = HttpRequestManager.ExtractJsonFromResponse(responseText);
                Debug.Log("Cleaned dialogue JSON: " + cleanedJson);
                
                // Check if it's the raw_dialogue fallback format
                var rawDialogueCheck = JsonUtility.FromJson<RawDialogueResponse>(cleanedJson);
                if (rawDialogueCheck?.raw_dialogue != null)
                {
                    Debug.Log("Using raw dialogue format");
                    OnDialogueGenerated?.Invoke(rawDialogueCheck.raw_dialogue);
                    return;
                }
                
                // Try to parse as structured dialogue response
                var dialogueResponse = JsonUtility.FromJson<DialogueResponse>(cleanedJson);
                if (dialogueResponse?.dialogues != null && dialogueResponse.dialogues.Length > 0)
                {
                    // Format dialogue lines for the speech system
                    var formattedLines = new System.Collections.Generic.List<string>();
                    foreach (var dialogue in dialogueResponse.dialogues)
                    {
                        formattedLines.Add($"[{dialogue.character}]: {dialogue.line}");
                    }
                    string formattedDialogue = string.Join("\n", formattedLines);
                    Debug.Log("Formatted dialogue: " + formattedDialogue);
                    OnDialogueGenerated?.Invoke(formattedDialogue);
                    return;
                }
                
                // Fallback to direct content extraction
                var apiResponse = JsonUtility.FromJson<HttpRequestManager.ApiResponse>(cleanedJson);
                if (apiResponse?.choices != null && apiResponse.choices.Length > 0)
                {
                    string content = apiResponse.choices[0].message.content;
                    OnDialogueGenerated?.Invoke(content.Trim().Trim('"'));
                    return;
                }
                
                OnDialogueGenerated?.Invoke("");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to extract dialogue content: {ex.Message}");
                OnDialogueGenerated?.Invoke("");
            }
        }

        void OnDialogueError(string error)
        {
            isDialogueRequesting = false;
            Debug.LogError("Dialogue generation failed: " + error);
            OnDialogueGenerated?.Invoke("");
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

        public void GenerateSelfIntroduction(CharacterModel characterModel, System.Action<string> onComplete)
        {
            if (isSelfIntroRequesting)
            {
                Debug.LogWarning("Self-introduction generation is already in progress.");
                onComplete?.Invoke("");
                return;
            }

            if (HttpRequestManager.Instance?.IsRequesting == true)
            {
                Debug.LogWarning("Another API request is in progress, queuing self-introduction request...");
                StartCoroutine(RetrySelfIntroduction(characterModel, onComplete, 1f));
                return;
            }

            isSelfIntroRequesting = true;

            string characterInfo = BuildCharacterInfoPrompt(characterModel);
            string loreContext = BuildLoreContextPrompt(characterModel);

            string systemPrompt = @"你是一个化学教育游戏的角色自我介绍生成器。根据给定的元素信息和当前剧情背景，生成符合元素特点的自我介绍。

要求：
1. 自我介绍要体现角色的元素特性（如金属性、非金属性、化学性质等）
2. 如果角色参与当前反应，要体现角色在反应中的作用和态度
3. 如果角色不参与当前反应，要明确表达不参与的态度
4. 语言要符合游戏世界观（元素山、金属堡、非金属谷、电离领域等设定）
5. 自我介绍控制在30字以内
6. 语调要符合角色性格

请严格按照以下JSON格式返回：
{
    ""introduction"": ""自我介绍内容""
}";

            string userPrompt = $"{characterInfo}\n\n{loreContext}\n\n请为这个角色生成一句自我介绍。";

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
        ""max_tokens"": 200,
        ""temperature"": 0.8
        }";

            Debug.Log("Generating self-introduction for: " + characterModel.GetCharacterName());

            HttpRequestManager.Instance?.SendRequest(
                jsonBody,
                (response) => OnSelfIntroductionSuccess(response, onComplete),
                (error) => OnSelfIntroductionError(error, onComplete)
            );
        }

        private string BuildCharacterInfoPrompt(CharacterModel characterModel)
        {
            var characterData = characterModel.GetCharacterData();
            var personality = characterModel.GetPersonality();

            string info = $"角色信息：\n";
            info += $"- 名称：{characterModel.GetCharacterName()}\n";
            info += $"- 类型：{characterModel.GetCharacterType()}\n";
            
            if (!string.IsNullOrEmpty(personality.speakingTrait))
            {
                info += $"- 性格特征：{personality.speakingTrait}\n";
            }
            
            if (characterData != null)
            {
                info += $"- 元素符号：{characterData.name}\n";
            }

            return info;
        }

        private string BuildLoreContextPrompt(CharacterModel characterModel)
        {
            var loreController = LoreController.Instance;
            if (loreController?.CurrentLore == null)
            {
                return "当前背景：没有特定的化学反应进行中，角色处于日常状态。";
            }

            var currentLore = loreController.CurrentLore;
            string context = $"当前剧情背景：\n";
            context += $"- 反应标题：{currentLore.story.title}\n";
            context += $"- 反应方程式：{currentLore.reaction.equation}\n";
            
            // Check if character participates in current reaction
            bool participatesInReaction = CheckCharacterParticipation(characterModel, currentLore);
            
            if (participatesInReaction)
            {
                context += $"- 角色状态：{characterModel.GetCharacterName()}参与这次反应\n";
                
                // Find character's role in the reaction
                string role = FindCharacterRoleInReaction(characterModel, currentLore);
                if (!string.IsNullOrEmpty(role))
                {
                    context += $"- 角色作用：{role}\n";
                }
            }
            else
            {
                context += $"- 角色状态：{characterModel.GetCharacterName()}不参与这次反应\n";
            }

            return context;
        }

        private bool CheckCharacterParticipation(CharacterModel characterModel, Elementor.Lore.LoreData lore)
        {
            string characterName = characterModel.GetCharacterName();
            
            // Check in reactants
            foreach (var reactant in lore.reaction.reactants)
            {
                foreach (var element in reactant.elements)
                {
                    if (element.element == characterName)
                        return true;
                }
            }
            
            // Check in products
            foreach (var product in lore.reaction.products)
            {
                foreach (var element in product.elements)
                {
                    if (element.element == characterName)
                        return true;
                }
            }
            
            return false;
        }

        private string FindCharacterRoleInReaction(CharacterModel characterModel, Lore.LoreData lore)
        {
            string characterName = characterModel.GetCharacterName();
            
            
            bool inReactants = false;
            bool inProducts = false;
            
            foreach (var reactant in lore.reaction.reactants)
            {
                foreach (var element in reactant.elements)
                {
                    if (element.element == characterName)
                    {
                        inReactants = true;
                        break;
                    }
                }
            }
            
            foreach (var product in lore.reaction.products)
            {
                foreach (var element in product.elements)
                {
                    if (element.element == characterName)
                    {
                        inProducts = true;
                        break;
                    }
                }
            }
            
            if (inReactants && inProducts)
                return "参与反应转化";
            else if (inReactants)
                return "作为反应物";
            else if (inProducts)
                return "作为生成物";
                
            return "";
        }

        private IEnumerator RetrySelfIntroduction(CharacterModel characterModel, System.Action<string> onComplete, float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (!HttpRequestManager.Instance?.IsRequesting == true)
            {
                GenerateSelfIntroduction(characterModel, onComplete);
            }
            else
            {
                Debug.LogWarning("API still busy, failing self-introduction generation");
                onComplete?.Invoke("");
            }
        }

        void OnSelfIntroductionSuccess(string responseText, System.Action<string> onComplete)
        {
            isSelfIntroRequesting = false;
            
            try
            {
                string cleanedJson = HttpRequestManager.ExtractJsonFromResponse(responseText);
                Debug.Log("Self-introduction response: " + cleanedJson);
                
                var introResponse = JsonUtility.FromJson<SelfIntroductionResponse>(cleanedJson);
                if (introResponse?.introduction != null)
                {
                    onComplete?.Invoke(introResponse.introduction);
                    return;
                }
                
                // Fallback to direct content extraction
                var apiResponse = JsonUtility.FromJson<HttpRequestManager.ApiResponse>(cleanedJson);
                if (apiResponse?.choices != null && apiResponse.choices.Length > 0)
                {
                    string content = apiResponse.choices[0].message.content;
                    onComplete?.Invoke(content.Trim().Trim('"'));
                    return;
                }
                
                onComplete?.Invoke("");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to extract self-introduction content: {ex.Message}");
                onComplete?.Invoke("");
            }
        }

        void OnSelfIntroductionError(string error, System.Action<string> onComplete)
        {
            isSelfIntroRequesting = false;
            Debug.LogError("Self-introduction generation failed: " + error);
            onComplete?.Invoke("");
        }

        /// <summary>
        /// Save generated lore data to persistent data path for APK builds
        /// </summary>
        private void SaveGeneratedLoreData(string jsonContent)
        {
            try
            {
                // Parse to get scene_id for filename
                var tempLore = JsonUtility.FromJson<Elementor.Lore.LoreData>(jsonContent);
                if (tempLore != null && !string.IsNullOrEmpty(tempLore.scene_id))
                {
                    string fileName = $"{tempLore.scene_id}.json";
                    
                    // Use LoreJsonReader to save the data
                    var loreReader = LoreJsonReader.Instance ?? FindObjectOfType<LoreJsonReader>();
                    if (loreReader != null)
                    {
                        loreReader.SaveLoreDataToPersistent(fileName, jsonContent);
                    }
                    else
                    {
                        // Fallback: save directly
                        string persistentDir = Path.Combine(Application.persistentDataPath, "Generated_JSONs");
                        if (!Directory.Exists(persistentDir))
                        {
                            Directory.CreateDirectory(persistentDir);
                        }
                        
                        string filePath = Path.Combine(persistentDir, fileName);
                        System.IO.File.WriteAllText(filePath, jsonContent);
                        Debug.Log($"💾 Saved generated lore data: {filePath}");
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"⚠️ Failed to save generated lore data: {ex.Message}");
            }
        }

    }

    [System.Serializable]
    public class DialogueResponse
    {
        public DialogueLine[] dialogues;
    }

    [System.Serializable]
    public class DialogueLine
    {
        public string character;
        public string line;
    }

    [System.Serializable]
    public class RawDialogueResponse
    {
        public string raw_dialogue;
    }

    [System.Serializable]
    public class SelfIntroductionResponse
    {
        public string introduction;
    }
}