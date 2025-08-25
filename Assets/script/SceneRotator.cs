using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Elementor
{
    public class SceneRotator : MonoBehaviour
    {
        [Header("Rotation Settings")]
        public Transform targetObject; // Object to rotate towards camera
        
        private Camera mainCamera;

        void Awake()
        {
            // Get reference to main camera
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = FindObjectOfType<Camera>();
            }

            if (mainCamera == null)
            {
                Debug.LogError("SceneRotator: No camera found in scene!");
                return;
            }

            if (targetObject == null)
            {
                Debug.LogWarning("SceneRotator: No target object assigned!");
                return;
            }

            RotateToFaceCamera();
        }

        private void RotateToFaceCamera()
        {
            // Calculate direction from target object to camera
            Vector3 directionToCamera = (mainCamera.transform.position - targetObject.position).normalized;
            
            // Calculate the angle in degrees
            float angle = Mathf.Atan2(directionToCamera.x, directionToCamera.z) * Mathf.Rad2Deg;
            
            // Snap to nearest 90-degree increment
            float snappedAngle = Mathf.Round(angle / 90f) * 90f;
            
            // Apply rotation around Y-axis to face camera
            targetObject.rotation = Quaternion.Euler(0, snappedAngle, 0);
            
            Debug.Log($"SceneRotator: Rotated {targetObject.name} to {snappedAngle} degrees to face camera");
        }

        // Start is called before the first frame update
        void Start()
        {
        
        }

        // Update is called once per frame
        void Update()
        {
        
        }
    }
}
