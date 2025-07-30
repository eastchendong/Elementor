using UnityEngine;
using Elementor.Lore;
using System;

namespace Elementor
{
    public class LoreController : MonoBehaviour
    {
        public static LoreController Instance { get; private set; }

        public event Action OnLoreLoaded;

        public LoreData CurrentLore { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
        }

        public void LoadLore(LoreData loreData)
        {
            CurrentLore = loreData;
            Debug.Log($"Lore '{CurrentLore.story.title}' loaded successfully.");
            OnLoreLoaded?.Invoke();
        }

        public void ClearCurrentLore()
        {
            CurrentLore = null;
            Debug.Log("Current lore has been cleared.");
        }

        public LoreStory GetStory()
        {
            return CurrentLore?.story;
        }

        public LoreReaction GetReaction()
        {
            return CurrentLore?.reaction;
        }
    }
}
