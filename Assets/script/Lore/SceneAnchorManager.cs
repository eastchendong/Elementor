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
        /// Gets the transform of a random volume anchor.
        /// </summary>
        /// <returns>A random anchor's transform, or null if no volume anchors are available.</returns>
        public Transform GetRandomAnchorTransform()
        {
            if (_volumeAnchorTransforms.Count == 0)
            {
                Debug.LogWarning("No volume anchors available to choose from.");
                return null;
            }
            return _volumeAnchorTransforms.Values.ElementAt(Random.Range(0, _volumeAnchorTransforms.Count));
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
