using UnityEngine;
using System.Collections.Generic;
using Elementor.Core;
using System.Linq;
using Meta.XR.MRUtilityKit;

namespace Elementor.Lore
{
    public class LoreSceneGenerator : MonoBehaviour
    {
        [Header("Scene Generation Settings")]
        [SerializeField]
        [Tooltip("The prefab representing the reaction environment. It should contain a ReactionManager and CharacterSlots.")]
        private GameObject environmentPrefab;

        private LoreController loreController;
        private CharacterSpawnController characterSpawnController;
        private SceneAnchorManager sceneAnchorManager;
        private LoreSpawnerHighlighter spawnerHighlighter;
        private GameObject currentEnvironmentInstance;

        void Start()
        {
            loreController = LoreController.Instance;
            characterSpawnController = CharacterSpawnController.Instance;
            sceneAnchorManager = SceneAnchorManager.Instance;
            spawnerHighlighter = FindObjectOfType<LoreSpawnerHighlighter>();

            Debug.Log($"🎬 LoreSceneGenerator Start - Controllers found: LoreController={loreController != null}, CharacterSpawn={characterSpawnController != null}, SceneAnchorManager={sceneAnchorManager != null}, SpawnerHighlighter={spawnerHighlighter != null}");

            if (loreController == null || characterSpawnController == null)
            {
                Debug.LogError("A required controller (Lore, CharacterSpawn");
                return;
            }

            // Subscribe to the lore loading event
            loreController.OnLoreLoaded += HandleLoreLoaded;
            Debug.Log("🔗 LoreSceneGenerator subscribed to OnLoreLoaded event");
            
            // Check if lore is already loaded
            if (loreController.CurrentLore != null)
            {
                Debug.Log("🎯 Lore already loaded on startup, generating scene immediately");
                HandleLoreLoaded();
            }
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
            Debug.Log("🎭 HandleLoreLoaded called in LoreSceneGenerator!");
            
            if (loreController.CurrentLore == null)
            {
                Debug.LogWarning("🚫 HandleLoreLoaded called but CurrentLore is null");
                return;
            }

            Debug.Log($"✅ New lore detected: '{loreController.CurrentLore.story?.title}'. Generating reaction environment...");
            GenerateSceneFromLore();
        }

        private void GenerateSceneFromLore()
        {
            Debug.Log("🏗️ Starting scene generation from lore...");
            
            Transform environmentSpawnPoint = sceneAnchorManager.GetRandomAnchorTransform();
            if (environmentPrefab == null || environmentSpawnPoint == null)
            {
                Debug.LogError($"❌ Environment Prefab is not set ({environmentPrefab == null}) or no spawn point available from SceneAnchorManager ({environmentSpawnPoint == null}).");
                return;
            }

            Debug.Log($"🎯 Spawning environment at position: {environmentSpawnPoint.position}");

            // Instantiate the environment with identity rotation
            currentEnvironmentInstance = Instantiate(environmentPrefab, environmentSpawnPoint.position, Quaternion.identity);

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
                reactionName = loreReaction.equation,
                reactionCondition = string.Join(", ", loreReaction.conditions),
                requirements = new List<SlotRequirement>(),
                outcomes = new List<ReactionOutcome>()
            };

            // Configure reaction requirements using existing input slots
            int inputSlotIndex = 1;
            foreach (var reactant in loreReaction.reactants)
            {
                CharacterSlot slot = FindSlotInPrefab(currentEnvironmentInstance.transform, $"InputSlot{inputSlotIndex}");
                if (slot != null)
                {
                    stage.requirements.Add(new SlotRequirement
                    {
                        slot = slot,
                        requiredName = reactant.name,
                        requiredCount = reactant.count
                    });

                    Debug.Log($"Added requirement: {reactant.name} x{reactant.count} to {slot.name}");
                }
                else
                {
                    Debug.LogWarning($"Could not find InputSlot{inputSlotIndex} in environment prefab");
                }
                inputSlotIndex++;
            }

            // Create output slots dynamically based on product counts
            int totalOutputSlots = 0;
            foreach (var product in loreReaction.products)
            {
                totalOutputSlots += product.count;
            }
            
            Debug.Log($"Creating {totalOutputSlots} output slots for products");
            List<CharacterSlot> createdOutputSlots = CreateOutputSlots(currentEnvironmentInstance.transform, totalOutputSlots);

            // Configure reaction outcomes - one outcome per individual product unit
            int outputSlotIndex = 0;
            foreach (var product in loreReaction.products)
            {
                var charactersInGroup = new List<string>();
                foreach (var element in product.elements)
                {
                    for (int i = 0; i < element.count; i++)
                    {
                        charactersInGroup.Add(element.element);
                    }
                }

                // Create one outcome for each product count
                for (int productIndex = 0; productIndex < product.count; productIndex++)
                {
                    if (outputSlotIndex < createdOutputSlots.Count)
                    {
                        stage.outcomes.Add(new ReactionOutcome
                        {
                            outputSlot = createdOutputSlots[outputSlotIndex],
                            newGroupName = product.name,
                            characterNamesInGroup = charactersInGroup,
                            productCount = 1 // Each outcome represents one unit
                        });

                        Debug.Log($"Added outcome: {product.name} to OutputSlot{outputSlotIndex + 1}");
                        outputSlotIndex++;
                    }
                }
            }

            // Hook up the reaction completion event
            stage.onReactionPhenomenon.AddListener(OnReactionCompleted);

            reactionManager.SetupStages(new List<ReactionStage> { stage });
            
            Debug.Log($"Scene generation completed for reaction: {loreReaction.equation}");
            Debug.Log($"Requirements: {stage.requirements.Count}, Outcomes: {stage.outcomes.Count}");
        }

        private List<CharacterSlot> CreateOutputSlots(Transform environmentParent, int slotCount)
        {
            List<CharacterSlot> createdSlots = new List<CharacterSlot>();
            
            // Find existing OutputSlot1 to use as template
            CharacterSlot templateSlot = FindSlotInPrefab(environmentParent, "OutputSlot1");
            if (templateSlot == null)
            {
                Debug.LogError("Cannot find OutputSlot1 template in environment prefab");
                return createdSlots;
            }

            // Find or create output slots container
            Transform outputContainer = environmentParent.Find("OutputSlots");
            if (outputContainer == null)
            {
                outputContainer = templateSlot.transform.parent;
            }

            // Create required number of output slots
            for (int i = 1; i <= slotCount; i++)
            {
                CharacterSlot slot;
                
                if (i == 1)
                {
                    // Use existing OutputSlot1
                    slot = templateSlot;
                }
                else
                {
                    // Create new slots based on template
                    GameObject newSlotObj = Instantiate(templateSlot.gameObject, outputContainer);
                    newSlotObj.name = $"OutputSlot{i}";
                    
                    // Position slots horizontally with spacing
                    Vector3 basePosition = templateSlot.transform.position;
                    newSlotObj.transform.position = basePosition + Vector3.right * (i - 1) * 2.0f;
                    
                    slot = newSlotObj.GetComponent<CharacterSlot>();
                }
                
                createdSlots.Add(slot);
                Debug.Log($"Created/configured OutputSlot{i} at position {slot.transform.position}");
            }

            return createdSlots;
        }

        /// <summary>
        /// Called by the ReactionManager when a reaction is successfully completed.
        /// </summary>
        public void OnReactionCompleted()
        {
            Debug.Log("Reaction completed! Cleaning up lore and preparing for the next stage.");

            // 1. Clear the lore data from the controller. The environment prefab remains as a memento.
            loreController.ClearCurrentLore();
            
            // 2. Clear spawner highlights when lore is unloaded
            if (spawnerHighlighter != null)
            {
                spawnerHighlighter.RefreshHighlighting();
                Debug.Log("🔄 Cleared spawner highlights after reaction completion");
            }
            
            currentEnvironmentInstance = null;

            // 3. Trigger the next lore reading (placeholder for future logic).
            Debug.Log("Triggering next lore read... (Interface for next step)");
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
