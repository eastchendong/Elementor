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
        [SerializeField] public Animator characterAnimator;
        [SerializeField] private DoubaoTTSAPI doubaoTTSAPI;

        public string characterVoiceType = "zh_male_M392_conversation_wvae_bigtts"; // Character-specific voice type

        private bool isSpeaking = false;
        private Coroutine currentSpeechCoroutine;
        private string pendingText;
        private float pendingDuration;
        private TextMeshProUGUI speechText;

        private void Awake()
        {
            if (audioSource == null) audioSource = GetComponent<AudioSource>();

            if (speechTextObject != null)
            {
                speechText = speechTextObject.GetComponent<TextMeshProUGUI>();
                if (speechText == null) Debug.LogWarning($"No TextMeshProUGUI found on speechTextObject for {gameObject.name}");
            }

            if (speechUIPanel != null) speechUIPanel.SetActive(false);
            
            SetupTTSAPI();
        }

        private void SetupTTSAPI()
        {
            if (doubaoTTSAPI == null) doubaoTTSAPI = gameObject.AddComponent<DoubaoTTSAPI>();
            doubaoTTSAPI.AudioReceived.AddListener(OnDoubaoAudioReceived);
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

        [ContextMenu("Test Doubao TTS Speech")]
        public void TestDoubaoSpeech()
        {
            Speak($"你好我是{GetComponent<CharacterView>()?.GetModel()?.GetCharacterName() ?? "a character"}. 正在测试豆包，使用的声音: {characterVoiceType}");
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
