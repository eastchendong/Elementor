using System.Collections.Generic;
using UnityEngine;
using System.Collections;

namespace Elementor
{
    public class ReactionEffectsManager : MonoBehaviour
    {
        [Header("Reaction Effects")]
        [SerializeField] private Transform conditionEffectsParent;
        [SerializeField] private Transform phenomenonEffectsParent;

        [Header("Default Effects")]
        [SerializeField] private string defaultConditionEffectName = "default";
        [SerializeField] private string defaultPhenomenonEffectName = "default";

        // Track active condition effects
        private List<GameObject> activeConditionEffects = new List<GameObject>();

        /// <summary>
        /// Activates reaction condition effects for the given stage
        /// </summary>
        public void ActivateReactionCondition(ReactionStage stage)
        {
            if (conditionEffectsParent == null || stage == null)
                return;

            // Use default effect name if stage effect name is empty
            string effectName = string.IsNullOrEmpty(stage.conditionEffectName) 
                ? defaultConditionEffectName 
                : stage.conditionEffectName;

            GameObject conditionEffect = FindEffectByName(conditionEffectsParent, effectName);
            if (conditionEffect != null)
            {
                conditionEffect.SetActive(true);
                activeConditionEffects.Add(conditionEffect);
                Debug.Log($"Activated condition effect: {effectName}");
            }
            else
            {
                Debug.LogWarning($"Condition effect '{effectName}' not found!");
            }
        }

        /// <summary>
        /// Activates reaction phenomenon effects for the completed reaction
        /// </summary>
        public void ActivateReactionPhenomenon(ReactionStage completedStage)
        {
            if (phenomenonEffectsParent == null || completedStage == null)
                return;

            // Use default effect name if stage effect name is empty
            string effectName = string.IsNullOrEmpty(completedStage.phenomenonEffectName) 
                ? defaultPhenomenonEffectName 
                : completedStage.phenomenonEffectName;

            GameObject phenomenonEffect = FindEffectByName(phenomenonEffectsParent, effectName);
            if (phenomenonEffect != null)
            {
                phenomenonEffect.SetActive(true);
                Debug.Log($"Activated phenomenon effect: {effectName}");
                
                // Optionally deactivate after some time
                StartCoroutine(DeactivateEffectAfterDelay(phenomenonEffect, 5.0f));
            }
            else
            {
                Debug.LogWarning($"Phenomenon effect '{effectName}' not found!");
            }
        }

        /// <summary>
        /// Deactivates all active condition effects
        /// </summary>
        public void DeactivateConditionEffects()
        {
            foreach (var effect in activeConditionEffects)
            {
                if (effect != null)
                {
                    effect.SetActive(false);
                    Debug.Log($"Deactivated condition effect: {effect.name}");
                }
            }
            activeConditionEffects.Clear();
        }

        /// <summary>
        /// Finds an effect GameObject by name within a parent transform
        /// </summary>
        private GameObject FindEffectByName(Transform parent, string effectName)
        {
            if (parent == null || string.IsNullOrEmpty(effectName))
                return null;

            // Search direct children first
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name.Equals(effectName, System.StringComparison.OrdinalIgnoreCase))
                {
                    return child.gameObject;
                }
            }

            // Search recursively in children
            for (int i = 0; i < parent.childCount; i++)
            {
                GameObject found = FindEffectByName(parent.GetChild(i), effectName);
                if (found != null)
                    return found;
            }

            return null;
        }

        /// <summary>
        /// Deactivates an effect after a specified delay
        /// </summary>
        private IEnumerator DeactivateEffectAfterDelay(GameObject effect, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (effect != null)
            {
                effect.SetActive(false);
                Debug.Log($"Auto-deactivated effect: {effect.name}");
            }
        }
    }
}
