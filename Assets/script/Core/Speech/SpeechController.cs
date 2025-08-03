using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Elementor.Lore;

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
        
        private LoreController loreController;
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
            loreController = LoreController.Instance;
            if (loreController == null)
            {
                Debug.LogWarning("LoreController not found. SpeechController may not work properly.");
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

            // Generate dynamic dialogue based on lore and personalities
            var generatedDialogue = GenerateDialogue(triggerType, participants);
            if (generatedDialogue.Count > 0)
            {
                PlayDialogueSequence(generatedDialogue);
            }
        }

        private List<DialogueLine> GenerateDialogue(SpeechTriggerType triggerType, List<CharacterView> participants)
        {
            var dialogueLines = new List<DialogueLine>();

            if (participants.Count == 0) return dialogueLines;

            // Get personality data from lore if available
            string contextInfo = GetContextFromLore(triggerType);

            foreach (var character in participants)
            {
                string characterName = character.GetModel().GetCharacterName();
                string characterType = character.GetModel().GetCharacterType();
                
                string dialogueText = GenerateDialogueText(triggerType, characterName, characterType, contextInfo);
                
                var dialogueLine = new DialogueLine
                {
                    characterName = characterName,
                    text = dialogueText,
                    duration = 2f + dialogueText.Length * 0.05f, // Estimate duration based on text length
                    audioClip = null // Could be assigned from resources
                };
                
                dialogueLines.Add(dialogueLine);
            }

            return dialogueLines;
        }

        private string GetContextFromLore(SpeechTriggerType triggerType)
        {
            if (loreController?.CurrentLore == null) return "";

            var story = loreController.GetStory();
            if (story == null) return "";

            switch (triggerType)
            {
                case SpeechTriggerType.ReactionSuccess:
                    return $"Reaction succeeded in the context of {story.title}";
                case SpeechTriggerType.ReactionFailure:
                    return $"Reaction failed in the context of {story.title}";
                case SpeechTriggerType.SynthesisSuccess:
                    return $"Synthesis completed in the context of {story.title}";
                default:
                    return story.title;
            }
        }

        private string GenerateDialogueText(SpeechTriggerType triggerType, string characterName, string characterType, string context)
        {
            // Simple dialogue generation based on trigger type and character
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
                    return $"Hello, I'm {characterName}!";
            }
        }

        private string GetSuccessDialogue(string name, string type)
        {
            var successLines = new[]
            {
                $"Excellent! The reaction worked perfectly!",
                $"Great success! I knew we could do it!",
                $"The elements combined beautifully!",
                $"Perfect harmony achieved!"
            };
            return successLines[Random.Range(0, successLines.Length)];
        }

        private string GetFailureDialogue(string name, string type)
        {
            var failureLines = new[]
            {
                $"Hmm, that didn't work as expected...",
                $"Let's try a different approach next time.",
                $"The elements aren't ready to combine yet.",
                $"We need to adjust our method."
            };
            return failureLines[Random.Range(0, failureLines.Length)];
        }

        private string GetSynthesisDialogue(string name, string type)
        {
            var synthesisLines = new[]
            {
                $"A new creation emerges!",
                $"The synthesis is complete!",
                $"Something wonderful has been born!",
                $"The elements have found their true form!"
            };
            return synthesisLines[Random.Range(0, synthesisLines.Length)];
        }

        private string GetIntroDialogue(string name, string type)
        {
            return $"Hello! I'm {name}, a {type} element. Ready for some alchemy?";
        }

        private string GetMeetingDialogue(string name, string type)
        {
            return $"Nice to meet you! I'm {name}. Let's work together!";
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

        public bool IsPlayingDialogue()
        {
            return isPlayingDialogue;
        }
    }
}
