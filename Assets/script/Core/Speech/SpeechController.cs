using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Text;

namespace Elementor.Core.Speech
{
    public enum SpeechTriggerType
    {
        ReactionSuccess,
        ReactionFailure,
        SynthesisSuccess,
        SynthesisFailure,
        GameStart,
        CharacterMeet
    }

    [System.Serializable]
    public class DialogueLine
    {
        public string characterName;
        public string text;
        public float duration;
        public AudioClip audioClip;
    }

    [System.Serializable]
    public class DialogueSequence
    {
        public SpeechTriggerType triggerType;
        public List<DialogueLine> lines;
    }

    public class SpeechController : MonoBehaviour
    {
        public static SpeechController Instance { get; private set; }

        [SerializeField] private List<DialogueSequence> predefinedSequences; // Now used as fallback
        [SerializeField] private bool isPlayingDialogue = false;
        
        private Queue<DialogueLine> currentDialogueQueue = new Queue<DialogueLine>();

        private void Awake()
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

        private void Start()
        {

        }

        [ContextMenu("Test Speech System")]
        public void TestSpeechSystem()
        {
            var testCharacters = FindObjectsOfType<CharacterView>().Take(2).ToList();
            if (testCharacters.Count > 0)
            {
                TriggerSpeech(SpeechTriggerType.GameStart, testCharacters);
            }
            else
            {
                Debug.LogWarning("No characters found for speech test.");
            }
        }

        public void TriggerSpeech(SpeechTriggerType triggerType, List<CharacterView> participants)
        {
            if (isPlayingDialogue)
            {
                Debug.Log("Dialogue already playing, ignoring new trigger.");
                return;
            }

            Debug.Log($"Triggering speech for: {triggerType} with {participants.Count} participants");

            // Priority 1: Try AI generation first
            if (API.Instance != null)
            {
                StartCoroutine(GenerateAIDialogue(triggerType, participants));
                return;
            }

            // Priority 2: Fall back to predefined sequences
            var predefinedSequence = predefinedSequences.FirstOrDefault(s => s.triggerType == triggerType);
            if (predefinedSequence != null && predefinedSequence.lines.Count > 0)
            {
                Debug.Log("Using predefined dialogue sequence as fallback");
                PlayDialogueSequence(predefinedSequence.lines);
                return;
            }
            else
            {
                Debug.LogWarning($"No predefined dialogue sequence found for trigger: {triggerType}");
            }

        }

        private IEnumerator GenerateAIDialogue(SpeechTriggerType triggerType, List<CharacterView> participants)
        {
            if (participants.Count == 0) 
            {
                Debug.LogWarning("No participants for AI dialogue generation");
                yield break;
            }

            Debug.Log($"Generating AI dialogue for {participants.Count} participants");
            var dialogueLines = new List<DialogueLine>();
            
            // Get comprehensive context for AI generation
            string loreContext = GetLoreContext(triggerType);
            string triggerDescription = GetTriggerDescription(triggerType);
            string participantInfo = GetParticipantInfo(participants);
            
            // Create comprehensive prompt for multiple characters
            string prompt = CreateMultiCharacterDialoguePrompt(triggerType, participants, loreContext, triggerDescription, participantInfo);
            
            // Request AI response for all characters
            bool responseReceived = false;
            string aiResponse = "";
            
            System.Action<string> onDialogueComplete = (response) => {
                aiResponse = response;
                responseReceived = true;
                Debug.Log($"Received dialogue response: {response}");
            };
            
            // Subscribe to the dedicated dialogue event BEFORE making the request
            API.Instance.OnDialogueGenerated += onDialogueComplete;
            
            // Make the API request
            API.Instance.GenerateDialogue(prompt);
            
            // Wait for response with timeout
            float timeout = 10f; // Reduced timeout
            float elapsed = 0f;
            while (!responseReceived && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // Always unsubscribe to prevent memory leaks
            API.Instance.OnDialogueGenerated -= onDialogueComplete;
            
            if (responseReceived && !string.IsNullOrEmpty(aiResponse))
            {
                // Parse AI response to extract dialogue for each character
                dialogueLines = ParseMultiCharacterDialogue(aiResponse, participants);
                Debug.Log($"Parsed {dialogueLines.Count} dialogue lines from AI response");
            }
            else
            {
                Debug.LogWarning($"No AI response received within timeout ({timeout}s)");
            }
            
            // If AI generation failed, fall back to predefined or simple dialogue
            if (dialogueLines.Count == 0)
            {
                Debug.LogWarning("AI dialogue generation failed, falling back to predefined sequences");
                var predefinedSequence = predefinedSequences.FirstOrDefault(s => s.triggerType == triggerType);
                if (predefinedSequence != null && predefinedSequence.lines.Count > 0)
                {
                    PlayDialogueSequence(predefinedSequence.lines);
                }
                else
                {
                    // Generate simple fallback dialogue for each participant
                    Debug.Log("Generating simple fallback dialogue");
                    var fallbackLines = new List<DialogueLine>();
                    foreach (var participant in participants)
                    {
                        var simpleLine = new DialogueLine
                        {
                            characterName = participant.GetModel().GetCharacterName(),
                            text = GenerateSimpleDialogueText(triggerType, participant.GetModel().GetCharacterName(), participant.GetModel().GetCharacterType()),
                            duration = 2f,
                            audioClip = null
                        };
                        fallbackLines.Add(simpleLine);
                    }
                    
                    if (fallbackLines.Count > 0)
                    {
                        PlayDialogueSequence(fallbackLines);
                    }
                    else
                    {
                        Debug.LogError("No valid dialogue lines generated or found in predefined sequences");
                    }
                }
                yield break;
            }
            
            Debug.Log($"Successfully generated AI dialogue with {dialogueLines.Count} lines");
            PlayDialogueSequence(dialogueLines);
        }

        private string CreateMultiCharacterDialoguePrompt(SpeechTriggerType triggerType, List<CharacterView> participants, string loreContext, string triggerDescription, string participantInfo)
        {
            // Group participants by element type to avoid redundant dialogue
            var elementGroups = participants
                .GroupBy(p => p.GetModel().GetCharacterName())
                .Select(group => new {
                    ElementName = group.Key,
                    ElementType = group.First().GetModel().GetCharacterType(),
                    Count = group.Count(),
                    Representative = group.First()
                })
                .ToList();

            var uniqueElementInfo = elementGroups.Select(group => {
                string trait = GetDefaultSpeakingTrait(group.ElementType);
                string countInfo = group.Count > 1 ? $"（{group.Count}个）" : "";
                return $"{group.ElementName}{countInfo}（{group.ElementType}，{trait}）";
            });

            var prompt = new StringBuilder();
            prompt.Append("请为以下化学元素角色生成对话序列：");
            prompt.Append($"情境：{triggerDescription}；");
            prompt.Append($"背景故事：{loreContext}；");
            prompt.Append($"参与角色：{string.Join("，", uniqueElementInfo)}；");
            prompt.Append("要求：");
            prompt.Append("1. 为每种不同的元素生成至少一句对话（相同元素不需要重复对话）；");
            prompt.Append("2. 对话要符合角色的元素特性和剧情情境；");
            prompt.Append("3. 每句对话不超过20个字；");
            prompt.Append("4. 对话要构成完整的剧情片段；");
            prompt.Append("5. 请按照以下格式返回：[元素名]: 对话内容；");
            prompt.Append("请生成对话。");
            
            return prompt.ToString();
        }

        private string GetParticipantInfo(List<CharacterView> participants)
        {
            var info = participants.Select(p => {
                string name = p.GetModel().GetCharacterName();
                string type = p.GetModel().GetCharacterType();
                string trait = GetDefaultSpeakingTrait(type);
                return $"{name}（{type}，{trait}）";
            });
            
            return string.Join("，", info);
        }

        private List<DialogueLine> ParseMultiCharacterDialogue(string aiResponse, List<CharacterView> participants)
        {
            var dialogueLines = new List<DialogueLine>();
            
            try
            {
                Debug.Log($"Parsing dialogue response: {aiResponse}");
                
                // Split response into lines and parse each dialogue line
                var lines = aiResponse.Split(new char[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
                
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    
                    // Look for pattern: [CharacterName]: dialogue text or CharacterName: dialogue text
                    var colonIndex = line.IndexOf(':');
                    if (colonIndex > 0)
                    {
                        var characterPart = line.Substring(0, colonIndex).Trim();
                        var dialogueText = line.Substring(colonIndex + 1).Trim();
                        
                        // Extract character name (remove brackets if present)
                        var characterName = characterPart.Replace("[", "").Replace("]", "").Trim();
                        
                        // Try to find matching character (case insensitive, partial match)
                        var character = participants.FirstOrDefault(p => {
                            string participantName = p.GetModel().GetCharacterName();
                            return participantName.Equals(characterName, System.StringComparison.OrdinalIgnoreCase) ||
                                   participantName.Contains(characterName) || 
                                   characterName.Contains(participantName);
                        });
                        
                        if (character != null && !string.IsNullOrEmpty(dialogueText))
                        {
                            var dialogueLine = new DialogueLine
                            {
                                characterName = character.GetModel().GetCharacterName(),
                                text = dialogueText,
                                duration = Mathf.Max(2f, 1f + dialogueText.Length * 0.05f),
                                audioClip = null
                            };
                            
                            dialogueLines.Add(dialogueLine);
                            Debug.Log($"Added dialogue for {character.GetModel().GetCharacterName()}: {dialogueText}");
                        }
                        else
                        {
                            Debug.LogWarning($"No matching character found for: {characterName}");
                        }
                    }
                }
                
                // Ensure all participants have at least one line
                foreach (var participant in participants)
                {
                    string participantName = participant.GetModel().GetCharacterName();
                    if (!dialogueLines.Any(d => d.characterName == participantName))
                    {
                        // Generate a simple line for missing participants
                        var simpleLine = new DialogueLine
                        {
                            characterName = participantName,
                            text = "我是" + participantName + "！",
                            duration = 2f,
                            audioClip = null
                        };
                        dialogueLines.Add(simpleLine);
                        Debug.Log($"Added fallback dialogue for {participantName}");
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to parse AI dialogue response: {e.Message}");
                return new List<DialogueLine>();
            }
            
            return dialogueLines;
        }

        private string GetTriggerDescription(SpeechTriggerType triggerType)
        {
            switch (triggerType)
            {
                case SpeechTriggerType.ReactionSuccess:
                    return "化学反应成功完成";
                case SpeechTriggerType.ReactionFailure:
                    return "化学反应失败";
                case SpeechTriggerType.SynthesisSuccess:
                    return "成功合成新化合物";
                case SpeechTriggerType.SynthesisFailure:
                    return "合成新化合物失败";
                case SpeechTriggerType.GameStart:
                    return "游戏开始，角色介绍自己";
                case SpeechTriggerType.CharacterMeet:
                    return "角色初次相遇";
                default:
                    return "一般情况";
            }
        }

        private string GetCharacterSpeakingTrait(CharacterView character)
        {
            var characterModel = character.GetModel();
            var personality = characterModel?.GetPersonality();
            
            if (personality?.speakingTrait != null && !string.IsNullOrEmpty(personality.speakingTrait))
            {
                return personality.speakingTrait;
            }
            
            // Fall back to default trait based on character type
            string characterType = character.GetModel().GetCharacterType();
            return GetDefaultSpeakingTrait(characterType);
        }

        private string GetDefaultSpeakingTrait(string characterType)
        {
            if (characterType.Contains("金属") || characterType.Contains("Metal"))
                return "说话坚定有力，充满自信";
            else if (characterType.Contains("非金属") || characterType.Contains("NonMetal"))
                return "说话机智敏锐，富有逻辑";
            else if (characterType.Contains("稀有气体") || characterType.Contains("NobleGas"))
                return "说话冷静淡然，不易激动";
            else
                return "说话温和友善，乐于合作";
        }

        private string GetLoreContext(SpeechTriggerType triggerType)
        {
            if (LoreController.Instance?.CurrentLore == null) 
                return "在元素山的电离领域中，各种元素正在进行化学反应";

            var story = LoreController.Instance.GetStory();
            if (story == null) 
                return "在元素山的电离领域中，各种元素正在进行化学反应";

            string context = story.title;
            if (story.plot != null)
            {
                context += "。" + string.Join("，", story.plot);
            }

            return context;
        }


        private string GenerateSimpleDialogueText(SpeechTriggerType triggerType, string characterName, string characterType)
        {
            switch (triggerType)
            {
                case SpeechTriggerType.ReactionSuccess:
                    return GetSuccessDialogue(characterName, characterType);
                case SpeechTriggerType.ReactionFailure:
                    return GetFailureDialogue(characterName, characterType);
                case SpeechTriggerType.SynthesisSuccess:
                    return GetSynthesisDialogue(characterName, characterType);
                case SpeechTriggerType.GameStart:
                    return GetIntroDialogue(characterName, characterType);
                case SpeechTriggerType.CharacterMeet:
                    return GetMeetingDialogue(characterName, characterType);
                default:
                    return $"你好，我是{characterName}！";
            }
        }

        private void PlayDialogueSequence(List<DialogueLine> dialogueLines)
        {
            currentDialogueQueue.Clear();
            foreach (var line in dialogueLines)
            {
                currentDialogueQueue.Enqueue(line);
            }

            StartCoroutine(PlayDialogueCoroutine());
        }

        private IEnumerator PlayDialogueCoroutine()
        {
            isPlayingDialogue = true;

            while (currentDialogueQueue.Count > 0)
            {
                var currentLine = currentDialogueQueue.Dequeue();
                
                var character = FindCharacterByName(currentLine.characterName);
                if (character != null)
                {
                    var speechComponent = character.GetComponent<CharacterSpeech>();
                    if (speechComponent != null)
                    {
                        speechComponent.Speak(currentLine.text, currentLine.audioClip, currentLine.duration);
                        Debug.Log($"[{currentLine.characterName}]: {currentLine.text}");
                    }
                    else
                    {
                        Debug.LogWarning($"No CharacterSpeech component found on {currentLine.characterName}");
                    }
                }
                else
                {
                    Debug.LogWarning($"Character not found: {currentLine.characterName}");
                }

                yield return new WaitForSeconds(currentLine.duration + 0.5f); // Small gap between speakers
            }

            isPlayingDialogue = false;
            Debug.Log("Dialogue sequence completed");
        }

        private CharacterView FindCharacterByName(string characterName)
        {
            var allCharacters = FindObjectsOfType<CharacterView>();
            return allCharacters.FirstOrDefault(c => c.GetModel().GetCharacterName() == characterName);
        }

        private string GetSuccessDialogue(string name, string type)
        {
            var successLines = new[]
            {
                $"太好了！反应成功了！",
                $"完美的化学反应！",
                $"我们做到了！",
                $"元素和谐共鸣！"
            };
            return successLines[Random.Range(0, successLines.Length)];
        }

        private string GetFailureDialogue(string name, string type)
        {
            var failureLines = new[]
            {
                $"嗯，这次没成功...",
                $"让我们再试试别的方法",
                $"元素还没准备好",
                $"需要调整一下"
            };
            return failureLines[Random.Range(0, failureLines.Length)];
        }

        private string GetSynthesisDialogue(string name, string type)
        {
            var synthesisLines = new[]
            {
                $"新的创造诞生了！",
                $"合成完成！",
                $"奇妙的新物质！",
                $"元素找到了真正的形态！"
            };
            return synthesisLines[Random.Range(0, synthesisLines.Length)];
        }

        private string GetIntroDialogue(string name, string type)
        {
            return $"你好！我是{name}，{type}元素。准备开始炼金术吧！";
        }

        private string GetMeetingDialogue(string name, string type)
        {
            return $"很高兴认识你！我是{name}。让我们一起合作吧！";
        }

        public bool IsPlayingDialogue()
        {
            return isPlayingDialogue;
        }

    }
}

