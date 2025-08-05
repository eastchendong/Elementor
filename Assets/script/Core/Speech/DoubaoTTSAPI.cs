using System;
using System.Collections;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace Elementor.Core.Speech
{
    public class DoubaoTTSAPI : MonoBehaviour 
    {
        private string _appId => APIConfigManager.Config.doubao_appid;
        private string _accessToken => APIConfigManager.Config.doubao_access_token;
        private string _apiUrl => APIConfigManager.Config.doubao_api_url;
        
        [SerializeField]
        private string defaultVoiceType = "zh_male_M392_conversation_wvae_bigtts";
        
        [SerializeField]
        private string audioEncoding = "mp3";
        
        [Range(0.8f, 2.0f)]
        public float speedRatio = 1.0f;

        public UnityEvent<AudioClip> AudioReceived;

        void Start()
        {
            // Validate API configuration
            if (!APIConfigManager.ValidateConfiguration())
            {
                Debug.LogError("Doubao TTS API Configuration is incomplete. Please check your environment variables or Resources/APIConfig.json file.");
            }
        }

        public void GetAudio(string text, string voiceType = null) 
        {
            StartCoroutine(DoRequest(text, voiceType));
        }

        [ContextMenu("Test API Connection")]
        public void TestAPIConnection()
        {
            if (string.IsNullOrEmpty(_appId) || string.IsNullOrEmpty(_accessToken))
            {
                Debug.LogError("AppID or Access Token is not set. Please configure your Doubao TTS credentials in environment variables or Resources/APIConfig.json.");
                return;
            }

            Debug.Log("Testing Doubao TTS API connection...");
            GetAudio("你好，这是豆包语音合成API的测试。");
        }

        IEnumerator DoRequest(string text, string voiceType = null) 
        {
            string activeVoiceType = !string.IsNullOrEmpty(voiceType) ? voiceType : defaultVoiceType;
            string reqId = System.Guid.NewGuid().ToString();

            var postData = new DoubaoTTSRequest 
            {
                app = new AppConfig
                {
                    appid = _appId,
                    token = "access_token", // 根据文档，这里应该是固定字符串
                    cluster = "volcano_tts"
                },
                user = new UserConfig
                {
                    uid = "unity_user_" + System.DateTime.Now.Ticks
                },
                audio = new AudioConfig
                {
                    voice_type = activeVoiceType,
                    encoding = audioEncoding,
                    speed_ratio = speedRatio
                },
                request = new RequestConfig
                {
                    reqid = reqId,
                    text = text,
                    operation = "query"
                }
            };
            
            var json = JsonConvert.SerializeObject(postData);
            Debug.Log($"Request JSON: {json}");
            
            var request = new UnityWebRequest(_apiUrl, "POST");
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            request.downloadHandler = new DownloadHandlerBuffer();
            
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer;{_accessToken}");
            
            Debug.Log($"Sending TTS request to Doubao: {text}");
            Debug.Log($"Authorization header: Bearer;{_accessToken}");
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success) 
            {
                Debug.LogError("Error from Doubao TTS API: " + request.error);
                Debug.LogError("Response: " + request.downloadHandler.text);
                Debug.LogError($"Response Code: {request.responseCode}");
                yield break;
            }
            
            try
            {
                var response = JsonConvert.DeserializeObject<DoubaoTTSResponse>(request.downloadHandler.text);
                
                if (response.code == 3000) // Success code
                {
                    byte[] audioBytes = Convert.FromBase64String(response.data);
                    AudioClip audioClip = CreateAudioClipFromMP3(audioBytes, "DoubaoTTS_" + reqId);
                    
                    if (audioClip != null)
                    {
                        Debug.Log($"Successfully received audio clip: {audioClip.name}, length: {audioClip.length}s");
                        AudioReceived.Invoke(audioClip);
                    }
                    else
                    {
                        Debug.LogError("Failed to create AudioClip from received data");
                    }
                }
                else
                {
                    Debug.LogError($"Doubao TTS API returned error code: {response.code}, message: {response.message}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to parse Doubao TTS response: {ex.Message}");
            }
            
            request.Dispose();
        }

        private AudioClip CreateAudioClipFromMP3(byte[] audioData, string clipName)
        {
            // For MP3 data, we need to save it temporarily and load it
            // Unity's AudioClip.Create doesn't directly support MP3 decoding
            string tempPath = System.IO.Path.Combine(Application.temporaryCachePath, clipName + ".mp3");
            
            try
            {
                System.IO.File.WriteAllBytes(tempPath, audioData);
                
                // Use UnityWebRequest to load the MP3 file
                StartCoroutine(LoadAudioClipFromFile(tempPath, clipName));
                
                return null; // Will be handled in coroutine
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to create temporary MP3 file: {ex.Message}");
                return null;
            }
        }

        private IEnumerator LoadAudioClipFromFile(string filePath, string clipName)
        {
            using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip("file://" + filePath, AudioType.MPEG))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    AudioClip audioClip = DownloadHandlerAudioClip.GetContent(request);
                    audioClip.name = clipName;
                    AudioReceived.Invoke(audioClip);
                }
                else
                {
                    Debug.LogError($"Failed to load audio from file: {request.error}");
                }
            }

            // Clean up temporary file
            try
            {
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to delete temporary file: {ex.Message}");
            }
        }

        [Serializable]
        public class DoubaoTTSRequest 
        {
            public AppConfig app;
            public UserConfig user;
            public AudioConfig audio;
            public RequestConfig request;
        }

        [Serializable]
        public class AppConfig
        {
            public string appid;
            public string token;
            public string cluster;
        }

        [Serializable]
        public class UserConfig
        {
            public string uid;
        }

        [Serializable]
        public class AudioConfig
        {
            public string voice_type;
            public string encoding;
            public float speed_ratio;
        }

        [Serializable]
        public class RequestConfig
        {
            public string reqid;
            public string text;
            public string operation;
        }

        [Serializable]
        public class DoubaoTTSResponse
        {
            public string reqid;
            public int code;
            public string operation;
            public string message;
            public int sequence;
            public string data;
            public AdditionInfo addition;
        }

        [Serializable]
        public class AdditionInfo
        {
            public string duration;
        }
    }
}
