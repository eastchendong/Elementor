using System.Collections;
using UnityEngine;
using TMPro;

namespace Elementor.Core.Speech
{
    public class CharacterSpeech : MonoBehaviour
    {
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private GameObject speechBubble;
        [SerializeField] private TextMeshProUGUI speechText;
        [SerializeField] private Animator characterAnimator;
        
        private bool isSpeaking = false;
        private Coroutine currentSpeechCoroutine;

        private void Awake()
        {
            // Try to find components if not assigned
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
            
            if (characterAnimator == null)
                characterAnimator = GetComponentInChildren<Animator>();

            // Create speech bubble if not assigned
            if (speechBubble == null)
                CreateSpeechBubble();
        }

        private void CreateSpeechBubble()
        {
            // Create a simple speech bubble UI element
            speechBubble = new GameObject("SpeechBubble");
            speechBubble.transform.SetParent(transform);
            speechBubble.transform.localPosition = Vector3.up * 2f; // Position above character

            Canvas canvas = speechBubble.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;

            speechText = speechBubble.AddComponent<TextMeshProUGUI>();
            speechText.text = "";
            speechText.fontSize = 24;
            speechText.alignment = TextAlignmentOptions.Center;
            speechText.rectTransform.sizeDelta = new Vector2(200, 100);

            speechBubble.SetActive(false);
        }

        public void Speak(string text, AudioClip audioClip = null, float duration = 2f)
        {
            if (isSpeaking && currentSpeechCoroutine != null)
            {
                StopCoroutine(currentSpeechCoroutine);
            }

            currentSpeechCoroutine = StartCoroutine(SpeakCoroutine(text, audioClip, duration));
        }

        private IEnumerator SpeakCoroutine(string text, AudioClip audioClip, float duration)
        {
            isSpeaking = true;

            // Show speech bubble
            if (speechBubble != null && speechText != null)
            {
                speechText.text = text;
                speechBubble.SetActive(true);
            }

            // Play audio if available
            if (audioSource != null && audioClip != null)
            {
                audioSource.clip = audioClip;
                audioSource.Play();
            }

            // Trigger speaking animation
            if (characterAnimator != null)
            {
                characterAnimator.SetBool("IsSpeaking", true);
            }

            Debug.Log($"[{gameObject.name}] Speaking: {text}");

            // Wait for speech duration
            yield return new WaitForSeconds(duration);

            // Hide speech bubble
            if (speechBubble != null)
            {
                speechBubble.SetActive(false);
            }

            // Stop speaking animation
            if (characterAnimator != null)
            {
                characterAnimator.SetBool("IsSpeaking", false);
            }

            isSpeaking = false;
            currentSpeechCoroutine = null;
        }

        public void StopSpeaking()
        {
            if (currentSpeechCoroutine != null)
            {
                StopCoroutine(currentSpeechCoroutine);
                currentSpeechCoroutine = null;
            }

            if (speechBubble != null)
                speechBubble.SetActive(false);

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
