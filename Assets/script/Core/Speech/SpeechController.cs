using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Elementor.Core.Speech
{
    public enum SpeechTriggerType
    {
        ReactionSuccess,
        ReactionFailure,
        SynthesisSuccess,
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

        [SerializeField] private List<DialogueSequence> predefinedSequences;
        [SerializeField] private bool isPlayingDialogue = false;
        [SerializeField] private string defaultVoiceId;
        
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

            // Try to find predefined sequence first
            var predefinedSequence = predefinedSequences.FirstOrDefault(s => s.triggerType == triggerType);
            if (predefinedSequence != null && predefinedSequence.lines.Count > 0)
            {
                PlayDialogueSequence(predefinedSequence.lines);
                return;
            }

            if (API.Instance != null)
            {
                StartCoroutine(GenerateAIDialogue(triggerType, participants));
            }
            else
            {
                var generatedDialogue = GenerateSimpleDialogue(triggerType, participants);
                if (generatedDialogue.Count > 0)
                {
                    PlayDialogueSequence(generatedDialogue);
                }
            }
        }

        private IEnumerator GenerateAIDialogue(SpeechTriggerType triggerType, List<CharacterView> participants)
        {
            if (participants.Count == 0) yield break;

            var dialogueLines = new List<DialogueLine>();
            
            // Get lore context
            string loreContext = GetLoreContext(triggerType);
            
            foreach (var character in participants)
            {
                string characterName = character.GetModel().GetCharacterName();
                string characterType = character.GetModel().GetCharacterType();
                string speakingTrait = GetCharacterSpeakingTrait(character);
                
                // Create prompt for AI
                string prompt = CreateDialoguePrompt(triggerType, characterName, characterType, speakingTrait, loreContext);
                
                // Request AI response
                bool responseReceived = false;
                string aiResponse = "";
                
                // Subscribe to API completion
                System.Action<string> onComplete = (response) => {
                    aiResponse = response;
                    responseReceived = true;
                };
                
                API.Instance.OnAnalysisComplete += onComplete;
                
                // Use the API to generate dialogue (reusing the existing analyze method)
                yield return StartCoroutine(GenerateDialogueWithAPI(prompt));
                
                // Wait for response
                float timeout = 10f;
                float elapsed = 0f;
                while (!responseReceived && elapsed < timeout)
                {
                    elapsed += Time.deltaTime;
                    yield return null;
                }
                
                API.Instance.OnAnalysisComplete -= onComplete;
                
                // Parse AI response and extract dialogue
                string dialogueText = ExtractDialogueFromAIResponse(aiResponse, characterName);
                if (string.IsNullOrEmpty(dialogueText))
                {
                    dialogueText = GenerateSimpleDialogueText(triggerType, characterName, characterType);
                }
                
                var dialogueLine = new DialogueLine
                {
                    characterName = characterName,
                    text = dialogueText,
                    duration = 2f + dialogueText.Length * 0.05f,
                    audioClip = null
                };
                
                dialogueLines.Add(dialogueLine);
            }
            
            if (dialogueLines.Count > 0)
            {
                PlayDialogueSequence(dialogueLines);
            }
        }

        private IEnumerator GenerateDialogueWithAPI(string prompt)
        {
            // Modify the API call to use our dialogue prompt
            API.Instance.chemicalFormula = prompt;
            yield return StartCoroutine(CallAPIForDialogue(prompt));
        }

        private IEnumerator CallAPIForDialogue(string prompt)
        {
            // Create a custom API call for dialogue generation
            string dialoguePrompt = $"请为化学角色生成一句简短的对话回应。背景信息：{prompt}。请只返回对话内容，不超过20个字。";
            
            // This would need to be implemented similar to the existing API call in api.cs
            // For now, we'll use a placeholder
            yield return new WaitForSeconds(1f); // Simulate API call delay
        }

        private string CreateDialoguePrompt(SpeechTriggerType triggerType, string characterName, string characterType, string speakingTrait, string loreContext)
        {
            string triggerDescription = GetTriggerDescription(triggerType);
            
            return $"角色：{characterName}（{characterType}元素）\n" +
                   $"说话特点：{speakingTrait}\n" +
                   $"情境：{triggerDescription}\n" +
                   $"背景故事：{loreContext}\n" +
                   $"请生成一句符合角色特点和情境的简短对话（不超过15字）";
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
                    return "成功合成新物质";
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
            var characterData = character.GetComponent<CharacterModel>();
            if (characterData != null)
            {
                // This would need to be implemented to get the character data
                // For now, return a default trait based on character type
                string characterType = character.GetModel().GetCharacterType();
                return GetDefaultSpeakingTrait(characterType);
            }
            return "说话平和友善";
        }

        private string GetDefaultSpeakingTrait(string characterType)
        {
            // Provide default speaking traits based on element type
            if (characterType.Contains("金属") || characterType.Contains("Metal"))
                return "说话坚定有力，充满自信";
            else if (characterType.Contains("非金属") || characterType.Contains("NonMetal"))
                return "说话机智敏锐，富有逻辑";
            else
                return "说话温和友善，乐于合作";
        }

        private string GetLoreContext(SpeechTriggerType triggerType)
        {
            var loreController = FindObjectOfType<LoreController>();
            if (loreController?.CurrentLore == null) 
                return "在元素山的电离领域中，各种元素正在进行化学反应";

            var story = loreController.GetStory();
            if (story == null) 
                return "在元素山的电离领域中，各种元素正在进行化学反应";

            string context = story.title;
            if (story.plot != null)
            {
                context += "。" + string.Join("，", story.plot);
            }

            return context;
        }

        private string ExtractDialogueFromAIResponse(string aiResponse, string characterName)
        {
            if (string.IsNullOrEmpty(aiResponse))
                return "";

            // Simple extraction - in a real implementation, you'd parse the JSON response
            // and extract the actual dialogue content
            try
            {
                // For now, return a portion of the response or generate based on character
                if (aiResponse.Length > 50)
                {
                    return aiResponse.Substring(0, 15) + "..."; // Truncate to reasonable length
                }
                return aiResponse;
            }
            catch
            {
                return "";
            }
        }

        private List<DialogueLine> GenerateSimpleDialogue(SpeechTriggerType triggerType, List<CharacterView> participants)
        {
            var dialogueLines = new List<DialogueLine>();

            if (participants.Count == 0) return dialogueLines;

            foreach (var character in participants)
            {
                string characterName = character.GetModel().GetCharacterName();
                string characterType = character.GetModel().GetCharacterType();
                
                string dialogueText = GenerateSimpleDialogueText(triggerType, characterName, characterType);
                
                var dialogueLine = new DialogueLine
                {
                    characterName = characterName,
                    text = dialogueText,
                    duration = 2f + dialogueText.Length * 0.05f,
                    audioClip = null
                };
                
                dialogueLines.Add(dialogueLine);
            }

            return dialogueLines;
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
                
                // Find the character and trigger their speech
                var character = FindCharacterByName(currentLine.characterName);
                if (character != null)
                {
                    var speechComponent = character.GetComponent<CharacterSpeech>();
                    if (speechComponent != null)
                    {
                        speechComponent.Speak(currentLine.text, currentLine.audioClip, currentLine.duration);
                    }
                }

                Debug.Log($"[{currentLine.characterName}]: {currentLine.text}");
                yield return new WaitForSeconds(currentLine.duration);
            }

            isPlayingDialogue = false;
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

