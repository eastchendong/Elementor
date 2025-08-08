using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Elementor
{
    public class ButtonEventAssigner : MonoBehaviour
    {
        [SerializeField] private Button btn;

        [Header("Dynamic Assignment")]
        [Tooltip("The target singleton script to call the method from.")]
        [SerializeField] private MonoBehaviour targetSingleton;

        [Tooltip("The name of the method to invoke on the singleton.")]
        [SerializeField] private string methodName;

        void Start()
        {
            if (targetSingleton == null)
            {
                Debug.LogError("Target singleton is not assigned.");
                return;
            }

            if (string.IsNullOrEmpty(methodName))
            {
                Debug.LogError("Method name is not specified.");
                return;
            }

            // Use reflection to find and invoke the method dynamically
            var methodInfo = targetSingleton.GetType().GetMethod(methodName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            if (methodInfo == null)
            {
                Debug.LogError($"Method '{methodName}' not found in {targetSingleton.GetType().Name}.");
                return;
            }

            btn.onClick.AddListener(() => {
                methodInfo.Invoke(targetSingleton, null);
            });
        }
    }
}
