using System;
using UnityEngine;

namespace Elementor.Core
{
    [Serializable]
    public class APIConfiguration
    {
        public string openai_api_key;
        public string elevenlabs_api_key;
        public string openai_api_url;
        public string elevenlabs_api_url;
    }

    public static class APIConfigManager
    {
        private static APIConfiguration _config;
        private static bool _configLoaded = false;

        public static APIConfiguration Config
        {
            get
            {
                if (!_configLoaded)
                {
                    LoadConfiguration();
                }
                return _config;
            }
        }

        private static void LoadConfiguration()
        {
            try
            {
                // First try to load from environment variables
                string openaiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
                string elevenlabsKey = Environment.GetEnvironmentVariable("ELEVENLABS_API_KEY");
                string elevenlabsVoiceId = Environment.GetEnvironmentVariable("ELEVENLABS_VOICE_ID");
                
                if (!string.IsNullOrEmpty(openaiKey) || !string.IsNullOrEmpty(elevenlabsKey))
                {
                    // Use environment variables
                    _config = new APIConfiguration
                    {
                        openai_api_key = openaiKey ?? "",
                        elevenlabs_api_key = elevenlabsKey ?? "",
                        openai_api_url = Environment.GetEnvironmentVariable("OPENAI_API_URL") ?? "https://yibuapi.com/v1/chat/completions",
                        elevenlabs_api_url = Environment.GetEnvironmentVariable("ELEVENLABS_API_URL") ?? "https://api.elevenlabs.io"
                    };
                    Debug.Log("API Configuration loaded from environment variables");
                }
                else
                {
                    // Fallback to Resources file
                    TextAsset configFile = Resources.Load<TextAsset>("APIConfig");
                    if (configFile != null)
                    {
                        _config = JsonUtility.FromJson<APIConfiguration>(configFile.text);
                        Debug.Log("API Configuration loaded from Resources/APIConfig.json");
                    }
                    else
                    {
                        Debug.LogError("APIConfig.json not found in Resources folder and no environment variables set");
                        _config = new APIConfiguration(); // Create empty config
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load API configuration: {ex.Message}");
                _config = new APIConfiguration(); // Create empty config as fallback
            }
            
            _configLoaded = true;
        }

        public static bool ValidateConfiguration()
        {
            var config = Config;
            bool isValid = true;

            if (string.IsNullOrEmpty(config.openai_api_key))
            {
                Debug.LogWarning("OpenAI API key is not configured. Set OPENAI_API_KEY environment variable or update Resources/APIConfig.json");
                isValid = false;
            }

            if (string.IsNullOrEmpty(config.elevenlabs_api_key))
            {
                Debug.LogWarning("ElevenLabs API key is not configured. Set ELEVENLABS_API_KEY environment variable or update Resources/APIConfig.json");
                isValid = false;
            }


            return isValid;
        }

        // Method to reload configuration (useful for development)
        public static void ReloadConfiguration()
        {
            _configLoaded = false;
            LoadConfiguration();
        }
    }
}
