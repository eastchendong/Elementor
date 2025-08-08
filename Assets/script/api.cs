using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Elementor.Core;

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

            string systemPrompt = @"你是一个为化学教育游戏生成剧情与交互数据的AI引擎，根据我给出的化学反应内容生成教学内容。游戏背景设定如下：在世界的中心矗立着元素山，由金属堡与非金属谷双峰构成。金属（金属性越强越躁动），不愿携带能量球；非金属居民（非金属性越强越躁动），善于收集与操控能量球。千百年来，两族隔山相望，彼此误解，文明也停止在相对简单的阶段。某一天山下出现了""电离领域""，是唯一能连接两族的神秘地区。元素们陆续下山，在此抛弃或吸收电子球，获得对应灵力，化身为更强大的离子个体、军团或商队，在此之后互相结盟，产生新物质。玩家作为旁观者，见证来来往往的联盟变化，一次次踏入电离之河，签订新契约，发生新故事，产生新物质。请你：1. 分析反应物、生成物、反应条件、电子流动路径；2. 根据游戏世界观创作剧情大标题与3~4句15字以内的剧情短句，""标题"": ""包含角色，概括剧情"", ""情节"": [ ""开始（根据情况分成两到三句）：故事性表达：拆解反应物集团，解释每个离子个人，军团或商队（展现和电子球关联）的来历以及契约者决定分开/取消合作的理由"", ""发展：离子们在离子河产生围绕电子球的争夺/交易/合作，有故事性的接触或冲突，生成新的生成物集团，达成新的契约"", ""结尾：可选结尾句，总结或留下悬念"" ]加入奇幻故事性，除了精准的单质元素名称（字母表示）和电子球外避免直接的化学描述（离子团），语言简洁易懂，故事简洁经典具有逻辑性，展现反应过程，每句15字以内。；3. 精确列出反应中每个化学物质所含元素的种类与个数，明确电子的转移方向与数量；4. 输出一份完整的 JSON 文件，结构必须符合以下字段规范（字段名、顺序与嵌套不可更改）：JSON结构字段说明：{ ""scene_id"": ""string（唯一场景ID）"", ""story"": { ""title"": ""string（剧情标题）"", ""plot"": [""string"", ""string"", ""string"", ""string（每句≤10字）""] }, ""reaction"": { ""equation"": ""string（配平反应式）"", ""type"": ""string（反应类型）"", ""conditions"": [""string"", ...], ""reactants"": [ { ""name"": ""string（化学式）"", ""type"": ""单质 / 离子团"", ""count"": int（反应系数）, ""elements"": [ { ""element"": ""string"", ""count"": int } ] } ], ""products"": [ { ""name"": ""string"", ""type"": ""单质 / 离子团"", ""count"": int, ""elements"": [ { ""element"": ""string"", ""count"": int } ] } ] }, ""electron_transfer"": { ""from"": ""string（失电子物）"", ""to"": ""string（得电子物）"", ""electron_count"": int, ""description"": ""string（电子流简述）"" }, ""gameplay_trigger"": { ""required_ions"": [ { ""name"": ""string（离子名称）"", ""from"": ""string（来源化合物）"", ""elements"": [ { ""element"": ""string"", ""count"": int } ] } ], ""reaction_area"": ""string（反应台名称）"", ""success_effects"": { ""animation"": ""string（动画代号）"", ""new_items"": [""string"", ...], ""story_continuation"": true } } } }。请严格按照此JSON格式返回，不要添加任何其他内容。";

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
            ""character"": ""角色名"",
            ""line"": ""对话内容（不超过20字）""
        }
    ]
}

要求：
1. 每个角色都要有至少一句对话
2. 对话要符合角色的元素特性
3. 每句对话不超过20个字
4. 严格按照JSON格式返回，不要添加任何其他内容";

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

            string systemPrompt = @"你是一个化学教育游戏的角色自我介绍生成器。根据给定的角色信息和当前剧情背景，生成符合角色特点的自我介绍。

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

        private string FindCharacterRoleInReaction(CharacterModel characterModel, Elementor.Lore.LoreData lore)
        {
            string characterName = characterModel.GetCharacterName();
            
            // Check if involved in electron transfer
            if (lore.electron_transfer != null)
            {
                if (lore.electron_transfer.from.Contains(characterName))
                {
                    return "失去电子";
                }
                if (lore.electron_transfer.to.Contains(characterName))
                {
                    return "获得电子";
                }
            }
            
            // Check reaction type context
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