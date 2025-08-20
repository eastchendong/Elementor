using UnityEngine;
using Meta.XR.MRUtilityKit;
using System.Collections.Generic;
using System.Linq;

namespace Elementor
{
    /// <summary>
    /// Manages MRUK volume anchors in the scene.
    /// Collects all volume anchors when a room is created and provides methods to access their transforms.
    /// </summary>
    public class SceneAnchorManager : MonoBehaviour
    {
        public static SceneAnchorManager Instance { get; private set; }

        private readonly Dictionary<string, Transform> _volumeAnchorTransforms = new Dictionary<string, Transform>();
        private readonly List<string> _usedAnchorNames = new List<string>();
        private string _lastUsedAnchorName;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
            }
        }

        public void CollectVolumeAnchors()
        {
            _volumeAnchorTransforms.Clear();
            Debug.Log("Collecting LAMP volume anchors...");

            var room = MRUK.Instance.GetCurrentRoom();
            if (room == null || room.Anchors.Count == 0)
            {
                Debug.LogWarning("No anchors found in the current room.");
                return;
            }

            int lampCount = 0;
            foreach (var anchor in room.Anchors)
            {
                if (anchor.VolumeBounds.HasValue)
                {
                    string anchorName = anchor.gameObject.name;
                    // Only collect anchors that contain "LAMP" in their name
                    if (anchorName.ToUpper().Contains("LAMP"))
                    {
                        // Create unique key by appending position-based suffix for duplicate names
                        string uniqueKey = anchorName;
                        if (_volumeAnchorTransforms.ContainsKey(uniqueKey))
                        {
                            lampCount++;
                            uniqueKey = $"{anchorName}_{lampCount}";
                        }
                        
                        _volumeAnchorTransforms[uniqueKey] = anchor.transform;
                        Debug.Log($"Collected LAMP anchor: {uniqueKey} at position {anchor.transform.position}");
                    }
                }
            }
            
            Debug.Log($"Total LAMP anchors collected: {_volumeAnchorTransforms.Count}");
        }

        /// <summary>
        /// Gets the transform of a specific volume anchor by name.
        /// </summary>
        /// <param name="name">The name of the anchor's GameObject.</param>
        /// <returns>The transform of the anchor, or null if not found.</returns>
        public Transform GetAnchorTransform(string name)
        {
            if (_volumeAnchorTransforms.TryGetValue(name, out Transform anchorTransform))
            {
                return anchorTransform;
            }
            Debug.LogWarning($"Volume anchor with name '{name}' not found.");
            return null;
        }

        /// <summary>
        /// Gets the transform of the next unused volume anchor in a sequential order.
        /// If all anchors have been used, it resets and starts from the beginning.
        /// </summary>
        /// <returns>The next unused anchor's transform, or null if no anchors are available.</returns>
        public Transform GetNextUnusedAnchorTransform()
        {
            if (_volumeAnchorTransforms.Count == 0)
            {
                Debug.LogWarning("No LAMP anchors available to choose from.");
                return null;
            }

            // Get all anchor keys and sort them to ensure a consistent order
            var sortedAnchorKeys = _volumeAnchorTransforms.Keys.OrderBy(k => k).ToList();

            // Find the first anchor in the sorted list that has not been used yet
            string selectedKey = sortedAnchorKeys.FirstOrDefault(key => !_usedAnchorNames.Contains(key));

            // If all anchors have been used, reset usage and select the first one
            if (string.IsNullOrEmpty(selectedKey))
            {
                Debug.LogWarning("All LAMP anchors have been used. Resetting usage tracking and starting from the first anchor.");
                ResetAnchorUsage();
                selectedKey = sortedAnchorKeys.FirstOrDefault();
            }

            if (string.IsNullOrEmpty(selectedKey))
            {
                Debug.LogError("Could not select any anchor even after reset.");
                return null;
            }
            
            // Mark as used immediately
            MarkAnchorAsUsed(selectedKey);
            
            Debug.Log($"Selected next unused LAMP anchor: {selectedKey} (total used: {_usedAnchorNames.Count}/{_volumeAnchorTransforms.Count})");
            
            return _volumeAnchorTransforms[selectedKey];
        }

        /// <summary>
        /// Gets the transform of a random volume anchor, avoiding all previously used ones.
        /// </summary>
        /// <returns>A random anchor's transform that hasn't been used before, or null if no unused anchors are available.</returns>
        public Transform GetRandomUnusedAnchorTransform()
        {
            if (_volumeAnchorTransforms.Count == 0)
            {
                Debug.LogWarning("No LAMP anchors available to choose from.");
                return null;
            }

            // Get available anchors (excluding all previously used ones)
            var availableAnchors = _volumeAnchorTransforms
                .Where(kvp => !_usedAnchorNames.Contains(kvp.Key))
                .ToList();

            // If no unused anchors available, reset usage and use any
            if (availableAnchors.Count == 0)
            {
                Debug.LogWarning("All LAMP anchors have been used. Resetting usage tracking and selecting randomly.");
                ResetAnchorUsage();
                availableAnchors = _volumeAnchorTransforms.ToList();
            }

            // Select random anchor from available ones
            var selectedAnchor = availableAnchors[Random.Range(0, availableAnchors.Count)];
            
            // Mark as used immediately
            MarkAnchorAsUsed(selectedAnchor.Key);
            
            Debug.Log($"Available unused LAMP anchors: [{string.Join(", ", availableAnchors.Select(kvp => kvp.Key))}]");
            Debug.Log($"Selected unused LAMP anchor: {selectedAnchor.Key} (total used: {_usedAnchorNames.Count}/{_volumeAnchorTransforms.Count})");
            
            return selectedAnchor.Value;
        }

        /// <summary>
        /// Gets the transform of a random volume anchor, avoiding the previously used one.
        /// </summary>
        /// <returns>A random anchor's transform that wasn't used last time, or null if no volume anchors are available.</returns>
        public Transform GetRandomAnchorTransform()
        {
            if (_volumeAnchorTransforms.Count == 0)
            {
                Debug.LogWarning("No LAMP anchors available to choose from.");
                return null;
            }

            // Get available anchors (excluding the last used one)
            var availableAnchors = _volumeAnchorTransforms
                .Where(kvp => kvp.Key != _lastUsedAnchorName)
                .ToList();

            // If no available anchors (only 1 LAMP total), reset and use any
            if (availableAnchors.Count == 0)
            {
                Debug.LogWarning("Only one LAMP anchor available, reusing the same LAMP.");
                availableAnchors = _volumeAnchorTransforms.ToList();
            }

            // Select random anchor from available ones
            var selectedAnchor = availableAnchors[Random.Range(0, availableAnchors.Count)];
            
            Debug.Log($"Available LAMP anchors: [{string.Join(", ", _volumeAnchorTransforms.Keys)}]");
            Debug.Log($"Selected LAMP anchor: {selectedAnchor.Key} (avoiding previous: {_lastUsedAnchorName ?? "none"})");
            
            _lastUsedAnchorName = selectedAnchor.Key;
            
            return selectedAnchor.Value;
        }

        /// <summary>
        /// Marks an anchor as used and tracks it in the usage history.
        /// </summary>
        /// <param name="anchorName">The name of the anchor that was used.</param>
        public void MarkAnchorAsUsed(string anchorName)
        {
            _lastUsedAnchorName = anchorName;
            
            if (!_usedAnchorNames.Contains(anchorName))
            {
                _usedAnchorNames.Add(anchorName);
                Debug.Log($"Marked anchor as used: {anchorName}. Total used anchors: {_usedAnchorNames.Count}/{_volumeAnchorTransforms.Count}");
            }
        }

        /// <summary>
        /// Resets the usage tracking, allowing all anchors to be used again.
        /// </summary>
        public void ResetAnchorUsage()
        {
            _usedAnchorNames.Clear();
            _lastUsedAnchorName = null;
            Debug.Log("Reset anchor usage tracking.");
        }

        /// <summary>
        /// Gets the name of the anchor from its transform.
        /// </summary>
        /// <param name="anchorTransform">The transform to find the name for.</param>
        /// <returns>The name of the anchor, or null if not found.</returns>
        public string GetAnchorName(Transform anchorTransform)
        {
            foreach (var kvp in _volumeAnchorTransforms)
            {
                if (kvp.Value == anchorTransform)
                {
                    return kvp.Key;
                }
            }
            return null;
        }

        private void OnDrawGizmos()
        {
            if (_volumeAnchorTransforms == null || _volumeAnchorTransforms.Count == 0) return;

            foreach (var anchorTransform in _volumeAnchorTransforms.Values)
            {
                if (anchorTransform != null)
                {
                    // Draw a sphere at the anchor's position
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawSphere(anchorTransform.position, 0.1f);

                    // Draw lines to represent rotation
                    const float lineLength = 0.25f;
                    
                    // Forward (Z - blue)
                    Gizmos.color = Color.blue;
                    Gizmos.DrawRay(anchorTransform.position, anchorTransform.forward * lineLength);
                    
                    // Up (Y - green)
                    Gizmos.color = Color.green;
                    Gizmos.DrawRay(anchorTransform.position, anchorTransform.up * lineLength);
                    
                    // Right (X - red)
                    Gizmos.color = Color.red;
                    Gizmos.DrawRay(anchorTransform.position, anchorTransform.right * lineLength);
                }
            }
        }
    }
}
