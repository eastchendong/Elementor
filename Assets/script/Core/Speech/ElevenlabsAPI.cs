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
        [SerializeField] private string _voiceId;
        [SerializeField] private string _apiKey;
        [SerializeField] private string _apiUrl = "https://api.elevenlabs.io";
        
        private AudioClip _audioClip;

        public bool Streaming;

        [Range(0, 4)]
        public int LatencyOptimization;

        public UnityEvent<AudioClip> AudioReceived;

        public ElevenlabsAPI(string apiKey, string voiceId) 
        {
            _apiKey = apiKey;
            _voiceId = voiceId;
        }

        public void SetApiKey(string apiKey)
        {
            _apiKey = apiKey;
        }

        public void SetVoiceId(string voiceId)
        {
            _voiceId = voiceId;
        }

        public void GetAudio(string text) 
        {
            StartCoroutine(DoRequest(text));
        }

        [ContextMenu("Test API Connection")]
        public void TestAPIConnection()
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                Debug.LogError("API Key is not set. Please configure your ElevenLabs API key.");
                return;
            }

            if (string.IsNullOrEmpty(_voiceId))
            {
                Debug.LogError("Voice ID is not set. Please configure a voice ID.");
                return;
            }

            Debug.Log("Testing ElevenLabs API connection...");
            GetAudio("Hello, this is a test of the ElevenLabs text to speech API integration.");
        }

        IEnumerator DoRequest(string message) 
        {
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
            var url = $"{_apiUrl}/v1/text-to-speech/{_voiceId}{stream}?optimize_streaming_latency={LatencyOptimization}";
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
