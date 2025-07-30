using UnityEngine;
using System.Collections.Generic;
using Elementor.Lore;
using System.Linq;
using Meta.XR.MRUtilityKit;

namespace Elementor
{
    public class LoreSceneGenerator : MonoBehaviour
    {
        [Header("Scene Generation Settings")]
        [SerializeField]
        [Tooltip("The prefab representing the reaction environment. It should contain a ReactionManager and CharacterSlots.")]
        private GameObject environmentPrefab;

        [SerializeField]
        [Tooltip("The location where the environment prefab will be instantiated.")]
        private Transform environmentSpawnPoint;

        private LoreController loreController;
        private CharacterSpawnController characterSpawnController;
        private LoreJsonReader loreJsonReader;
        private GameObject currentEnvironmentInstance;

        void Start()
        {
            loreController = LoreController.Instance;
            characterSpawnController = CharacterSpawnController.Instance;
            loreJsonReader = FindObjectOfType<LoreJsonReader>();

            if (loreController == null || characterSpawnController == null || loreJsonReader == null)
            {
                Debug.LogError("A required controller (Lore, CharacterSpawn, or LoreJsonReader) is missing.");
                return;
            }

            // Subscribe to the lore loading event
            loreController.OnLoreLoaded += HandleLoreLoaded;
        }

        private void OnDestroy()
        {
            if (loreController != null)
            {
                loreController.OnLoreLoaded -= HandleLoreLoaded;
            }
        }

        /// <summary>
        /// Handles the event triggered when new lore is loaded.
        /// </summary>
        private void HandleLoreLoaded()
        {
            if (loreController.CurrentLore == null) return;

            Debug.Log("New lore detected. Generating reaction environment...");
            GenerateSceneFromLore();
        }

        private void GenerateSceneFromLore()
        {
            if (environmentPrefab == null || environmentSpawnPoint == null)
            {
                Debug.LogError("Environment Prefab or Spawn Point is not set in LoreSceneGenerator.");
                return;
            }

            // Instantiate the environment
            currentEnvironmentInstance = Instantiate(environmentPrefab, environmentSpawnPoint.position, environmentSpawnPoint.rotation);
            
            // Get the ReactionManager from the new instance
            ReactionManager reactionManager = currentEnvironmentInstance.GetComponentInChildren<ReactionManager>();
            if (reactionManager == null)
            {
                Debug.LogError("The environment prefab is missing a ReactionManager component.");
                Destroy(currentEnvironmentInstance);
                return;
            }

            var loreReaction = loreController.GetReaction();
            if (loreReaction == null)
            {
                Debug.LogWarning("Lore data contains no reaction.");
                return;
            }

            var stage = new ReactionStage
            {
                reactionName = loreReaction.type,
                reactionCondition = string.Join(", ", loreReaction.conditions),
                requirements = new List<SlotRequirement>(),
                outcomes = new List<ReactionOutcome>()
            };

            // Configure reaction requirements by finding slots within the new prefab
            int inputSlotIndex = 1;
            foreach (var reactant in loreReaction.reactants)
            {
                CharacterSlot slot = FindSlotInPrefab(currentEnvironmentInstance.transform, $"InputSlot{inputSlotIndex}");
                if (slot != null)
                {
                    stage.requirements.Add(new SlotRequirement
                    {
                        slot = slot,
                        requiredGroupName = reactant.name,
                        requiredCharacterName = ""
                    });
                }
                inputSlotIndex++;
            }

            // Configure reaction outcomes
            int outputSlotIndex = 1;
            foreach (var product in loreReaction.products)
            {
                CharacterSlot slot = FindSlotInPrefab(currentEnvironmentInstance.transform, $"OutputSlot{outputSlotIndex}");
                if (slot != null)
                {
                    var charactersInGroup = product.elements.SelectMany(e => Enumerable.Repeat(e.element, e.count)).ToList();
                    stage.outcomes.Add(new ReactionOutcome
                    {
                        outputSlot = slot,
                        newGroupName = product.name,
                        characterNamesInGroup = charactersInGroup
                    });
                }
                outputSlotIndex++;
            }

            // Hook up the reaction completion event
            stage.onReactionPhenomenon.AddListener(OnReactionCompleted);

            reactionManager.SetupStages(new List<ReactionStage> { stage });
        }

        /// <summary>
        /// Called by the ReactionManager when a reaction is successfully completed.
        /// </summary>
        public void OnReactionCompleted()
        {
            Debug.Log("Reaction completed! Cleaning up lore and preparing for the next stage.");

            // 1. Clear the lore data from the controller. The environment prefab remains as a memento.
            loreController.ClearCurrentLore();
            currentEnvironmentInstance = null;

            // 2. Trigger the next lore reading (placeholder for future logic).
            // For now, this could reload the same file for testing or be adapted to load "scene_002.json", etc.
            Debug.Log("Triggering next lore read... (Interface for next step)");
            // Example: loreJsonReader.loreFilePath = "next_lore_file.json";
            // loreJsonReader.LoadLoreFromJson();
        }

        private CharacterSlot FindSlotInPrefab(Transform parent, string slotName)
        {
            Transform slotTransform = parent.Find(slotName);
            if (slotTransform == null)
            {
                // Search recursively if not found at the top level
                foreach (Transform child in parent)
                {
                    CharacterSlot found = FindSlotInPrefab(child, slotName);
                    if (found != null) return found;
                }
                Debug.LogWarning($"Could not find slot '{slotName}' in the environment prefab.");
                return null;
            }
            return slotTransform.GetComponent<CharacterSlot>();
        }
    }
}
