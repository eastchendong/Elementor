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
            Debug.Log("Collecting volume anchors...");

            var room = MRUK.Instance.GetCurrentRoom();
            if (room == null || room.Anchors.Count == 0)
            {
                Debug.LogWarning("No anchors found in the current room.");
                return;
            }

            foreach (var anchor in room.Anchors)
            {
                if (anchor.VolumeBounds.HasValue)
                {
                    string anchorName = anchor.gameObject.name;
                    _volumeAnchorTransforms[anchorName] = anchor.transform;
                    Debug.Log($"Collected volume anchor: {anchorName} at position {anchor.transform.position}");
                }
            }
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
        /// Gets the transform of a random volume anchor, avoiding the previously used one.
        /// </summary>
        /// <returns>A random anchor's transform that wasn't used last time, or null if no volume anchors are available.</returns>
        public Transform GetRandomAnchorTransform()
        {
            if (_volumeAnchorTransforms.Count == 0)
            {
                Debug.LogWarning("No volume anchors available to choose from.");
                return null;
            }

            // Get available anchors (excluding the last used one)
            var availableAnchors = _volumeAnchorTransforms
                .Where(kvp => kvp.Key != _lastUsedAnchorName)
                .ToList();

            // If no available anchors (only 1 anchor total), reset and use any
            if (availableAnchors.Count == 0)
            {
                Debug.LogWarning("Only one anchor available, reusing the same anchor.");
                availableAnchors = _volumeAnchorTransforms.ToList();
            }

            // Select random anchor from available ones
            var selectedAnchor = availableAnchors[Random.Range(0, availableAnchors.Count)];
            _lastUsedAnchorName = selectedAnchor.Key;
            
            Debug.Log($"Selected anchor: {selectedAnchor.Key} (avoiding previous: {(_lastUsedAnchorName == selectedAnchor.Key ? "none" : _lastUsedAnchorName)})");
            
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
            }
            
            Debug.Log($"Marked anchor as used: {anchorName}. Total used anchors: {_usedAnchorNames.Count}");
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

            Gizmos.color = Color.yellow;
            foreach (var anchorTransform in _volumeAnchorTransforms.Values)
            {
                if (anchorTransform != null)
                {
                    Gizmos.DrawSphere(anchorTransform.position, 0.1f);
                }
            }
        }
    }
}
