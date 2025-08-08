using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace Elementor.Core.Speech
{
    public class CharacterSpeech : MonoBehaviour
    {
        [SerializeField] private AudioSource audioSource;
        [SerializeField] public Animator characterAnimator;
        [SerializeField] private DoubaoTTSAPI doubaoTTSAPI;

        [Header("Unified Character UI")]
        [SerializeField] private GameObject characterUIPanel; // Single UI panel for all character interactions
        [SerializeField] private TextMeshProUGUI characterNameText; // Text component that always shows character name
        [SerializeField] private TextMeshProUGUI speechText; // Text component for speech content
        [SerializeField] private Button greetingButton; // Button for greeting

        public string characterVoiceType = "zh_male_M392_conversation_wvae_bigtts"; // Character-specific voice type

        private bool isSpeaking = false;
        private Coroutine currentSpeechCoroutine;
        private string pendingText;
        private float pendingDuration;
        private CharacterView characterView;

        private void Awake()
        {
            if (audioSource == null) audioSource = GetComponent<AudioSource>();

            if (characterUIPanel != null) characterUIPanel.SetActive(false);
            
            characterView = GetComponent<CharacterView>();
            
            SetupTTSAPI();
            SetupCharacterUI();
        }

        private void OnEnable()
        {
            if (characterView != null)
                characterView.OnAnimationStateChanged += OnAnimationStateChanged;
        }

        private void OnDisable()
        {
            if (characterView != null)
                characterView.OnAnimationStateChanged -= OnAnimationStateChanged;
        }

        private void SetupTTSAPI()
        {
            if (doubaoTTSAPI == null) doubaoTTSAPI = gameObject.AddComponent<DoubaoTTSAPI>();
            doubaoTTSAPI.AudioReceived.AddListener(OnDoubaoAudioReceived);
        }

        private void SetupCharacterUI()
        {
            // If UI components are not assigned, try to find them in children
            if (characterUIPanel == null)
            {
                characterUIPanel = transform.Find("CharacterUIPanel")?.gameObject;
            }
            
            if (characterUIPanel != null)
            {
                if (characterNameText == null)
                {
                    // Try to find by name first, then by order
                    Transform nameTransform = characterUIPanel.transform.Find("CharacterNameText");
                    if (nameTransform != null)
                        characterNameText = nameTransform.GetComponent<TextMeshProUGUI>();
                    else
                    {
                        var textComponents = characterUIPanel.GetComponentsInChildren<TextMeshProUGUI>();
                        if (textComponents.Length > 0)
                            characterNameText = textComponents[0]; // First text is for name
                    }
                }
                
                if (speechText == null)
                {
                    // Try to find by name first, then by order
                    Transform speechTransform = characterUIPanel.transform.Find("SpeechText");
                    if (speechTransform != null)
                        speechText = speechTransform.GetComponent<TextMeshProUGUI>();
                    else
                    {
                        var textComponents = characterUIPanel.GetComponentsInChildren<TextMeshProUGUI>();
                        if (textComponents.Length > 1)
                            speechText = textComponents[1]; // Second text is for speech
                    }
                }
                
                if (greetingButton == null)
                {
                    greetingButton = characterUIPanel.GetComponentInChildren<Button>();
                }
                
                // Setup button listener
                if (greetingButton != null)
                {
                    greetingButton.onClick.RemoveAllListeners();
                    greetingButton.onClick.AddListener(SelfIntroduction);
                }
                
                // Initially hide the panel
                characterUIPanel.SetActive(false);
                
                // Clear speech text initially
                if (speechText != null)
                {
                    speechText.text = "";
                }
            }
        }

        private void OnAnimationStateChanged(CharacterView view, CharacterAnimationState previousState, CharacterAnimationState newState)
        {
            UpdateCharacterUI(newState);
        }

        private void UpdateCharacterUI(CharacterAnimationState state)
        {
            if (characterUIPanel == null || characterView?.GetModel() == null) return;
            
            bool shouldShowUI = state == CharacterAnimationState.Slotted;
            
            if (shouldShowUI)
            {
                // Always show character name when UI is activated
                if (characterNameText != null)
                {
                    characterNameText.text = characterView.GetModel().GetCharacterName();
                }
                
                // Clear speech text when first showing UI (unless currently speaking)
                if (speechText != null && !isSpeaking)
                {
                    speechText.text = "";
                }
                
                // Show greeting button when slotted and not speaking
                if (greetingButton != null)
                {
                    greetingButton.gameObject.SetActive(!isSpeaking);
                }
                
                characterUIPanel.SetActive(true);
            }
            else if (!isSpeaking)
            {
                // Hide UI when not slotted and not speaking
                characterUIPanel.SetActive(false);
            }
        }

        public void Speak(string text, AudioClip audioClip = null, float duration = 2f)
        {
            if (isSpeaking && currentSpeechCoroutine != null) StopCoroutine(currentSpeechCoroutine);

            if (doubaoTTSAPI != null && audioClip == null)
            {
                pendingText = text;
                pendingDuration = duration;

                doubaoTTSAPI.GetAudio(text, characterVoiceType);

                ShowSpeechUI(text);
            }
            else
            {
                currentSpeechCoroutine = StartCoroutine(SpeakCoroutine(text, audioClip, duration));
            }
        }

        private void OnDoubaoAudioReceived(AudioClip audioClip)
        {
            Debug.Log($"Received Doubao TTS audio for character: {gameObject.name}");

            // Use the actual audio duration if available, otherwise use pending duration
            float duration = audioClip != null ? audioClip.length : pendingDuration;

            currentSpeechCoroutine = StartCoroutine(SpeakCoroutine(pendingText, audioClip, duration));
        }

        private void ShowSpeechUI(string text)
        {
            if (characterUIPanel != null)
            {
                // Always ensure character name is shown
                if (characterNameText != null && characterView?.GetModel() != null)
                {
                    characterNameText.text = characterView.GetModel().GetCharacterName();
                }
                
                // Show speech content in speech text
                if (speechText != null)
                {
                    speechText.text = text;
                }
                
                // Hide greeting button during speech
                if (greetingButton != null)
                {
                    greetingButton.gameObject.SetActive(false);
                }
                
                characterUIPanel.SetActive(true);
            }
        }

        private IEnumerator SpeakCoroutine(string text, AudioClip audioClip, float duration)
        {
            isSpeaking = true;

            // Show speech UI
            ShowSpeechUI(text);

            // Play audio if available
            if (audioSource != null && audioClip != null)
            {
                audioSource.clip = audioClip;
                audioSource.Play();
                Debug.Log($"Playing generated audio for: {text}");
            }

            // Trigger speaking animation
            if (characterAnimator != null)
            {
                characterAnimator.SetBool("IsSpeaking", true);
            }

            Debug.Log($"[{gameObject.name}] Speaking: {text}");

            // Wait for speech duration
            yield return new WaitForSeconds(duration);

            // Stop speaking animation
            if (characterAnimator != null)
            {
                characterAnimator.SetBool("IsSpeaking", false);
            }

            isSpeaking = false;
            currentSpeechCoroutine = null;

            // Clear speech text after speaking
            if (speechText != null)
            {
                speechText.text = "";
            }

            // After speaking, restore UI based on current state
            var currentState = characterView?.GetCurrentAnimationState() ?? CharacterAnimationState.Idle;
            UpdateCharacterUI(currentState);
        }

        [ContextMenu("Test Doubao TTS Speech")]
        public void TestDoubaoSpeech()
        {
            Speak($"你好我是{GetComponent<CharacterView>()?.GetModel()?.GetCharacterName() ?? "a character"}. 正在测试豆包，使用的声音: {characterVoiceType}");
        }

        [ContextMenu("Test Self Introduction")]
        public void SelfIntroduction()
        {
            var characterModel = characterView?.GetModel();
            
            if (characterModel == null)
            {
                Speak("你好！很高兴见到你！");
                return;
            }

            // Generate AI-powered self-introduction
            API.Instance?.GenerateSelfIntroduction(characterModel, OnSelfIntroductionGenerated);
        }

        private void OnSelfIntroductionGenerated(string introduction)
        {
            if (!string.IsNullOrEmpty(introduction))
            {
                Speak(introduction);
            }
            else
            {
                // Fallback to simple greeting
                var characterName = characterView?.GetModel()?.GetCharacterName() ?? "角色";
                Speak($"你好！我是{characterName}，很高兴见到你！");
            }
        }

        public void StopSpeaking()
        {
            if (currentSpeechCoroutine != null)
            {
                StopCoroutine(currentSpeechCoroutine);
                currentSpeechCoroutine = null;
            }

            if (characterAnimator != null)
                characterAnimator.SetBool("IsSpeaking", false);

            if (audioSource != null && audioSource.isPlaying)
                audioSource.Stop();

            isSpeaking = false;

            // Clear speech text when stopping
            if (speechText != null)
            {
                speechText.text = "";
            }

            // Restore UI based on current state after stopping speech
            var currentState = characterView?.GetCurrentAnimationState() ?? CharacterAnimationState.Idle;
            UpdateCharacterUI(currentState);
        }

        public bool IsSpeaking()
        {
            return isSpeaking;
        }
    }
}

