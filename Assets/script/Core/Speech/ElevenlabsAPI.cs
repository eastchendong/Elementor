using System;
using System.Collections;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace Elementor.Core.Speech
{
    public class ElevenlabsAPI : MonoBehaviour 
    {
        private string _apiKey => APIConfigManager.Config.doubao_access_token;
        private string _apiUrl => APIConfigManager.Config.doubao_api_url;
        
        [SerializeField]
        private string defaultVoiceId = "21m00Tcm4TlvDq8ikWAM"; // Default ElevenLabs voice
        public bool Streaming;

        [Range(0, 4)]
        public int LatencyOptimization;

        public UnityEvent<AudioClip> AudioReceived;

        void Start()
        {
            // Validate API configuration
            if (!APIConfigManager.ValidateConfiguration())
            {
                Debug.LogError("ElevenLabs API Configuration is incomplete. Please check your environment variables or Resources/APIConfig.json file.");
            }
        }

        public void GetAudio(string text, string voiceId = null) 
        {
            StartCoroutine(DoRequest(text, voiceId));
        }

        [ContextMenu("Test API Connection")]
        public void TestAPIConnection()
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                Debug.LogError("API Key is not set. Please configure your ElevenLabs API key in environment variables or Resources/APIConfig.json.");
                return;
            }

            Debug.Log("Testing ElevenLabs API connection...");
            GetAudio("Hello, this is a test of the ElevenLabs text to speech API integration.");
        }

        IEnumerator DoRequest(string message, string voiceId = null) 
        {
            string activeVoiceId = !string.IsNullOrEmpty(voiceId) ? voiceId : defaultVoiceId;

            var postData = new TextToSpeechRequest 
            {
                text = message,
                model_id = "eleven_multilingual_v2"
            };

            var voiceSetting = new VoiceSettings 
            {
                stability = 0,
                similarity_boost = 0,
                style = 0.5f,
                use_speaker_boost = true
            };
            postData.voice_settings = voiceSetting;
            
            var json = JsonConvert.SerializeObject(postData);
            var uH = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            var stream = (Streaming) ? "/stream" : "";
            var url = $"{_apiUrl}/v1/text-to-speech/{activeVoiceId}{stream}?optimize_streaming_latency={LatencyOptimization}";
            var request = UnityWebRequest.PostWwwForm(url, json);
            var downloadHandler = new DownloadHandlerAudioClip(url, AudioType.MPEG);
            
            if (Streaming) 
            {
                downloadHandler.streamAudio = true;
            }
            
            request.uploadHandler = uH;
            request.downloadHandler = downloadHandler;
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("xi-api-key", _apiKey);
            request.SetRequestHeader("Accept", "audio/mpeg");
            
            Debug.Log($"Sending TTS request to ElevenLabs: {message}");
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success) 
            {
                Debug.LogError("Error downloading audio: " + request.error);
                Debug.LogError("Response: " + request.downloadHandler.text);
                yield break;
            }
            
            AudioClip audioClip = downloadHandler.audioClip;
            Debug.Log($"Successfully received audio clip: {audioClip.name}, length: {audioClip.length}s");
            AudioReceived.Invoke(audioClip);
            request.Dispose();
        }

        [Serializable]
        public class TextToSpeechRequest 
        {
            public string text;
            public string model_id;
            public VoiceSettings voice_settings;
        }

        [Serializable]
        public class VoiceSettings 
        {
            public int stability;
            public int similarity_boost;
            public float style;
            public bool use_speaker_boost;
        }
    }
}
