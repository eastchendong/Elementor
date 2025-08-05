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
                    token = "access_token",
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
            
            var request = new UnityWebRequest(_apiUrl, "POST");
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            request.downloadHandler = new DownloadHandlerBuffer();
            
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer;{_accessToken}");
            
            Debug.Log($"Sending TTS request to Doubao: {text}");
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success) 
            {
                Debug.LogError("Error from Doubao TTS API: " + request.error);
                Debug.LogError("Response: " + request.downloadHandler.text);
                Debug.LogError($"Response Code: {request.responseCode}");
                request.Dispose();
                yield break;
            }
            
            DoubaoTTSResponse response = null;
            bool parseSuccess = false;
            
            try
            {
                response = JsonConvert.DeserializeObject<DoubaoTTSResponse>(request.downloadHandler.text);
                parseSuccess = true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to parse Doubao TTS response: {ex.Message}");
                parseSuccess = false;
            }
            
            request.Dispose();
            
            if (parseSuccess && response != null)
            {
                if (response.code == 3000)
                {
                    byte[] audioBytes = Convert.FromBase64String(response.data);
                    Debug.Log($"Received audio data: {audioBytes.Length} bytes");
                    
                    yield return StartCoroutine(CreateAudioClipFromMP3(audioBytes, "DoubaoTTS_" + reqId));
                }
                else
                {
                    Debug.LogError($"Doubao TTS API returned error code: {response.code}, message: {response.message}");
                }
            }
        }

        private IEnumerator CreateAudioClipFromMP3(byte[] audioData, string clipName)
        {
            string tempPath = System.IO.Path.Combine(Application.temporaryCachePath, clipName + ".mp3");
            bool writeSuccess = false;
            
            try
            {
                System.IO.File.WriteAllBytes(tempPath, audioData);
                Debug.Log($"Saved temporary MP3 file: {tempPath} ({audioData.Length} bytes)");
                writeSuccess = true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to create temporary MP3 file: {ex.Message}");
                writeSuccess = false;
            }
            
            if (writeSuccess)
            {
                yield return StartCoroutine(LoadAudioClipFromFile(tempPath, clipName));
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
                    if (audioClip != null)
                    {
                        audioClip.name = clipName;
                        Debug.Log($"Successfully created AudioClip: {audioClip.name}, length: {audioClip.length}s, frequency: {audioClip.frequency}Hz");
                        AudioReceived.Invoke(audioClip);
                    }
                    else
                    {
                        Debug.LogError("DownloadHandlerAudioClip.GetContent returned null");
                    }
                }
                else
                {
                    Debug.LogError($"Failed to load audio from file: {request.error}");
                    Debug.LogError($"File path: {filePath}");
                    Debug.LogError($"File exists: {System.IO.File.Exists(filePath)}");
                }
            }

            // Clean up temporary file
            try
            {
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                    Debug.Log($"Cleaned up temporary file: {filePath}");
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
