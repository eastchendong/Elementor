using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Elementor
{
    public class ButtonJSONAssigner : MonoBehaviour
    {
        [SerializeField] private Button btn;

        void Start()
        {
            if (LoreJsonReader.Instance == null)
            {
                Debug.LogError("LoreJsonReader instance not found.");
                return;
            }

            btn.onClick.AddListener(() => {
                LoreJsonReader.Instance.LoadLoreFromJson();
            });
        }
    }
}