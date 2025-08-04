using System.Collections;
using UnityEngine;
using TMPro;

namespace Elementor.Core.Speech
{
    public class CharacterSpeech : MonoBehaviour
    {
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private GameObject speechUIPanel; // Reference to the UI panel in the character prefab
        [SerializeField] private GameObject speechTextObject; // Reference to the text GameObject in the UI panel
        [SerializeField] private Animator characterAnimator;
        [SerializeField] private ElevenlabsAPI elevenlabsAPI;

        [Header("ElevenLabs Configuration")]
        [SerializeField] private string apiKey;
        [SerializeField] private string voiceId;
        [SerializeField] private bool useElevenLabs = false;

        private bool isSpeaking = false;
        private Coroutine currentSpeechCoroutine;
        private string pendingText;
        private float pendingDuration;
        private TextMeshProUGUI speechText;

        private void Awake()
        {
            // Try to find components if not assigned
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();

            if (characterAnimator == null)
                characterAnimator = GetComponentInChildren<Animator>();

            // Get the TextMeshProUGUI component from the speechTextObject
            if (speechTextObject != null)
            {
                speechText = speechTextObject.GetComponent<TextMeshProUGUI>();
                if (speechText == null)
                {
                    Debug.LogWarning($"No TextMeshProUGUI found on speechTextObject for {gameObject.name}");
                }
            }

            // Initially hide the UI panel
            if (speechUIPanel != null)
            {
                speechUIPanel.SetActive(false);
            }

            // Setup ElevenLabs API if enabled
            if (useElevenLabs)
                SetupElevenLabsAPI();
        }

        private void SetupElevenLabsAPI()
        {
            if (elevenlabsAPI == null)
            {
                elevenlabsAPI = gameObject.AddComponent<ElevenlabsAPI>();
            }

            if (!string.IsNullOrEmpty(apiKey))
                elevenlabsAPI.SetApiKey(apiKey);

            if (!string.IsNullOrEmpty(voiceId))
                elevenlabsAPI.SetVoiceId(voiceId);

            // Subscribe to audio received event
            elevenlabsAPI.AudioReceived.AddListener(OnElevenLabsAudioReceived);
        }

        public void Speak(string text, AudioClip audioClip = null, float duration = 2f)
        {
            if (isSpeaking && currentSpeechCoroutine != null)
            {
                StopCoroutine(currentSpeechCoroutine);
            }

            if (useElevenLabs && elevenlabsAPI != null && audioClip == null)
            {
                // Store text and duration for when audio is received
                pendingText = text;
                pendingDuration = duration;

                // Request audio from ElevenLabs
                elevenlabsAPI.GetAudio(text);

                // Show text immediately while waiting for audio
                ShowSpeechUI(text);
            }
            else
            {
                currentSpeechCoroutine = StartCoroutine(SpeakCoroutine(text, audioClip, duration));
            }
        }

        private void OnElevenLabsAudioReceived(AudioClip audioClip)
        {
            Debug.Log($"Received ElevenLabs audio for character: {gameObject.name}");

            // Use the actual audio duration if available, otherwise use pending duration
            float duration = audioClip != null ? audioClip.length : pendingDuration;

            currentSpeechCoroutine = StartCoroutine(SpeakCoroutine(pendingText, audioClip, duration));
        }

        private void ShowSpeechUI(string text)
        {
            if (speechUIPanel != null && speechText != null)
            {
                speechText.text = text;
                speechUIPanel.SetActive(true);
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

            // Hide speech UI
            if (speechUIPanel != null)
            {
                speechUIPanel.SetActive(false);
            }

            // Stop speaking animation
            if (characterAnimator != null)
            {
                characterAnimator.SetBool("IsSpeaking", false);
            }

            isSpeaking = false;
            currentSpeechCoroutine = null;
        }

        [ContextMenu("Test ElevenLabs Speech")]
        public void TestElevenLabsSpeech()
        {
            if (!useElevenLabs)
            {
                Debug.LogWarning("ElevenLabs is not enabled for this character.");
                return;
            }

            string testText = $"Hello! I am {GetComponent<CharacterView>()?.GetModel()?.GetCharacterName() ?? "a character"}. Testing ElevenLabs integration!";
            Speak(testText);
        }

        public void SetSpeechUIReferences(GameObject uiPanel, GameObject textObject)
        {
            speechUIPanel = uiPanel;
            speechTextObject = textObject;

            if (speechTextObject != null)
            {
                speechText = speechTextObject.GetComponent<TextMeshProUGUI>();
            }

            // Initially hide the UI panel
            if (speechUIPanel != null)
            {
                speechUIPanel.SetActive(false);
            }
        }

        public void SetElevenLabsCredentials(string newApiKey, string newVoiceId)
        {
            apiKey = newApiKey;
            voiceId = newVoiceId;

            if (elevenlabsAPI != null)
            {
                elevenlabsAPI.SetApiKey(apiKey);
                elevenlabsAPI.SetVoiceId(voiceId);
            }
        }

        public void EnableElevenLabs(bool enable)
        {
            useElevenLabs = enable;
            if (enable && elevenlabsAPI == null)
            {
                SetupElevenLabsAPI();
            }
        }

        public void StopSpeaking()
        {
            if (currentSpeechCoroutine != null)
            {
                StopCoroutine(currentSpeechCoroutine);
                currentSpeechCoroutine = null;
            }

            if (speechUIPanel != null)
                speechUIPanel.SetActive(false);

            if (characterAnimator != null)
                characterAnimator.SetBool("IsSpeaking", false);

            if (audioSource != null && audioSource.isPlaying)
                audioSource.Stop();

            isSpeaking = false;
        }

        public bool IsSpeaking()
        {
            return isSpeaking;
        }
    }
}
