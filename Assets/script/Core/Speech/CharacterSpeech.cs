using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.IO;

namespace Elementor.Core.Speech
{
    public class CharacterSpeech : MonoBehaviour
    {
        [SerializeField] private AudioSource audioSource;
        [SerializeField] public Animator characterAnimator;
        [SerializeField] private DoubaoTTSAPI doubaoTTSAPI;

        [Header("Speech UI")]
        [SerializeField] private GameObject speechUIPanel; // Panel for speech UI
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
            // Setup button listener if available
            if (greetingButton != null)
            {
                greetingButton.onClick.RemoveAllListeners();
                greetingButton.onClick.AddListener(SelfIntroduction);
            }
            
            // Clear speech text initially
            if (speechText != null)
            {
                speechText.text = "";
            }
            
            // Initially hide speech UI panel
            if (speechUIPanel != null)
            {
                speechUIPanel.SetActive(false);
            }
        }

        private void OnAnimationStateChanged(CharacterView view, CharacterAnimationState previousState, CharacterAnimationState newState)
        {
            UpdateCharacterUI(newState);
        }

        private void UpdateCharacterUI(CharacterAnimationState state)
        {
            bool shouldShowSpeechUI = state == CharacterAnimationState.Slotted;
            
            // Show/hide speech UI panel based on state
            if (speechUIPanel != null)
            {
                speechUIPanel.SetActive(shouldShowSpeechUI);
            }
            
            // Show greeting button when slotted and not speaking
            if (greetingButton != null)
            {
                greetingButton.gameObject.SetActive(shouldShowSpeechUI && !isSpeaking);
            }
            
            // Clear speech text when first showing UI (unless currently speaking)
            if (speechText != null && !isSpeaking && shouldShowSpeechUI)
            {
                speechText.text = "";
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
            // Show speech UI panel
            if (speechUIPanel != null)
            {
                speechUIPanel.SetActive(true);
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