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
        private PalmLoreController palmLoreController;
        private GameObject currentEnvironmentInstance;

        void Start()
        {
            loreController = LoreController.Instance;
            characterSpawnController = CharacterSpawnController.Instance;
            sceneAnchorManager = SceneAnchorManager.Instance;
            spawnerHighlighter = FindObjectOfType<LoreSpawnerHighlighter>();
            palmLoreController = PalmLoreController.Instance;

            Debug.Log($"🎬 LoreSceneGenerator Start - Controllers found: LoreController={loreController != null}, CharacterSpawn={characterSpawnController != null}, SceneAnchorManager={sceneAnchorManager != null}, SpawnerHighlighter={spawnerHighlighter != null}, PalmLore={palmLoreController != null}");

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
            
            Transform environmentSpawnPoint = sceneAnchorManager.GetRandomUnusedAnchorTransform();
            if (environmentPrefab == null || environmentSpawnPoint == null)
            {
                Debug.LogError($"❌ Environment Prefab is not set ({environmentPrefab == null}) or no spawn point available from SceneAnchorManager ({environmentSpawnPoint == null}).");
                return;
            }

            // Mark the selected anchor as used
            string selectedAnchorName = sceneAnchorManager.GetAnchorName(environmentSpawnPoint);
            if (!string.IsNullOrEmpty(selectedAnchorName))
            {
                sceneAnchorManager.MarkAnchorAsUsed(selectedAnchorName);
                Debug.Log($"🎯 Spawning environment on table: {selectedAnchorName} at position: {environmentSpawnPoint.position}");
            }
            else
            {
                Debug.Log($"🎯 Spawning environment at position: {environmentSpawnPoint.position}");
            }

            // Instantiate the environment with identity rotation
            currentEnvironmentInstance = Instantiate(environmentPrefab, environmentSpawnPoint.position, Quaternion.identity);

            // Get the ReactionManager from the new instance - check both root and slot container
            ReactionManager reactionManager = FindReactionManagerInHierarchy(currentEnvironmentInstance.transform);
            if (reactionManager == null)
            {
                Debug.LogError("❌ The environment prefab is missing a ReactionManager component. Searched entire hierarchy.");
                Destroy(currentEnvironmentInstance);
                return;
            }

            Debug.Log($"✅ Found ReactionManager at: {GetTransformPath(reactionManager.transform)}");

            var loreReaction = loreController.GetReaction();
            if (loreReaction == null)
            {
                Debug.LogError("❌ Lore data contains no reaction.");
                return;
            }

            var stage = new ReactionStage
            {
                reactionName = loreReaction.equation,
                conditionEffectName = GetConditionEffectName(loreReaction),
                phenomenonEffectName = GetPhenomenonEffectName(loreReaction),
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

            // Use existing predefined output slots instead of creating new ones
            List<CharacterSlot> availableOutputSlots = FindAllOutputSlots(currentEnvironmentInstance.transform);
            Debug.Log($"Found {availableOutputSlots.Count} predefined output slots in environment");

            // Configure reaction outcomes using predefined slots
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
                    if (outputSlotIndex < availableOutputSlots.Count)
                    {
                        stage.outcomes.Add(new ReactionOutcome
                        {
                            outputSlot = availableOutputSlots[outputSlotIndex],
                            newGroupName = product.name,
                            characterNamesInGroup = charactersInGroup,
                            productCount = 1 // Each outcome represents one unit
                        });

                        Debug.Log($"Added outcome: {product.name} to {availableOutputSlots[outputSlotIndex].name}");
                        outputSlotIndex++;
                    }
                    else
                    {
                        Debug.LogWarning($"Not enough predefined output slots for all products. Need {product.count} but only have {availableOutputSlots.Count} available.");
                        break;
                    }
                }
            }

            // Hook up the reaction completion event
            if (stage.onReactionPhenomenon == null)
            {
                stage.onReactionPhenomenon = new UnityEngine.Events.UnityEvent();
            }
            stage.onReactionPhenomenon.AddListener(OnReactionCompleted);

            Debug.Log($"🔧 Setting up ReactionManager with {stage.requirements.Count} requirements and {stage.outcomes.Count} outcomes");
            reactionManager.SetupStages(new List<ReactionStage> { stage });
            
            Debug.Log($"✅ Scene generation completed for reaction: {loreReaction.equation}");
            Debug.Log($"Requirements: {stage.requirements.Count}, Outcomes: {stage.outcomes.Count}");
        }

        /// <summary>
        /// Finds ReactionManager in the environment hierarchy, checking both root and slot container
        /// </summary>
        private ReactionManager FindReactionManagerInHierarchy(Transform environmentRoot)
        {
            Debug.Log($"🔍 Searching for ReactionManager in environment hierarchy starting from '{environmentRoot.name}'");
            
            // First check the root
            ReactionManager reactionManager = environmentRoot.GetComponent<ReactionManager>();
            if (reactionManager != null)
            {
                Debug.Log($"✅ Found ReactionManager on root: {environmentRoot.name}");
                return reactionManager;
            }

            // Check immediate children
            reactionManager = environmentRoot.GetComponentInChildren<ReactionManager>();
            if (reactionManager != null)
            {
                Debug.Log($"✅ Found ReactionManager in children: {reactionManager.transform.name}");
                return reactionManager;
            }

            // Specifically check slot container since ReactionManager is likely there
            Transform slotContainer = FindSlotContainer(environmentRoot);
            if (slotContainer != null)
            {
                Debug.Log($"📁 Checking slot container '{slotContainer.name}' for ReactionManager");
                reactionManager = slotContainer.GetComponent<ReactionManager>();
                if (reactionManager != null)
                {
                    Debug.Log($"✅ Found ReactionManager on slot container: {slotContainer.name}");
                    return reactionManager;
                }

                reactionManager = slotContainer.GetComponentInChildren<ReactionManager>();
                if (reactionManager != null)
                {
                    Debug.Log($"✅ Found ReactionManager in slot container children: {reactionManager.transform.name}");
                    return reactionManager;
                }
            }

            // Last resort: comprehensive recursive search
            Debug.Log("🔄 Performing comprehensive recursive search for ReactionManager");
            reactionManager = FindReactionManagerRecursive(environmentRoot);
            
            if (reactionManager != null)
            {
                Debug.Log($"✅ Found ReactionManager via recursive search: {GetTransformPath(reactionManager.transform)}");
            }
            else
            {
                Debug.LogError("❌ ReactionManager not found anywhere in the hierarchy");
                PrintHierarchy(environmentRoot, 0, 3); // Debug print to help identify the issue
            }

            return reactionManager;
        }

        /// <summary>
        /// Recursively searches for ReactionManager component in the hierarchy
        /// </summary>
        private ReactionManager FindReactionManagerRecursive(Transform parent)
        {
            // Check current transform
            ReactionManager manager = parent.GetComponent<ReactionManager>();
            if (manager != null)
            {
                return manager;
            }

            // Search children recursively
            foreach (Transform child in parent)
            {
                manager = FindReactionManagerRecursive(child);
                if (manager != null)
                {
                    return manager;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets condition effect name from lore reaction conditions
        /// </summary>
        private string GetConditionEffectName(LoreReaction reaction)
        {
            if (reaction.conditions != null && reaction.conditions.Count > 0)
            {
                // Use the first condition as the effect name
                return reaction.conditions[0];
            }
            
            // Fallback to reaction type or equation
            return !string.IsNullOrEmpty(reaction.type) ? reaction.type + "_Condition" : reaction.equation + "_Condition";
        }

        /// <summary>
        /// Gets phenomenon effect name from lore reaction type or success effects
        /// </summary>
        private string GetPhenomenonEffectName(LoreReaction reaction)
        {
            // First check if reaction has phenomena field (new format)
            if (reaction.phenomena != null && reaction.phenomena.Count > 0)
            {
                return reaction.phenomena[0]; // Use first phenomenon
            }

            // Use success_effects.animation from gameplay_trigger if available
            var currentLore = loreController.CurrentLore;
            if (currentLore?.gameplay_trigger?.success_effects != null &&
                !string.IsNullOrEmpty(currentLore.gameplay_trigger.success_effects.animation))
            {
                return currentLore.gameplay_trigger.success_effects.animation;
            }

            else
            {
                Debug.LogError(" No phenomena or success effects animation found in lore reaction");
                return !string.IsNullOrEmpty(reaction.type) ? reaction.type + "_Phenomenon" : reaction.equation + "_Phenomenon";
            }

        }

        /// <summary>
        /// Finds all existing output slots in the environment prefab
        /// </summary>
        private List<CharacterSlot> FindAllOutputSlots(Transform environmentRoot)
        {
            List<CharacterSlot> outputSlots = new List<CharacterSlot>();
            
            // Search for slots with "OutputSlot" in their name
            CharacterSlot[] allSlots = environmentRoot.GetComponentsInChildren<CharacterSlot>();
            
            foreach (var slot in allSlots)
            {
                if (slot.name.Contains("OutputSlot"))
                {
                    outputSlots.Add(slot);
                    Debug.Log($"Found existing output slot: {slot.name} at position {slot.transform.position}");
                }
            }
            
            // Sort by name to ensure consistent ordering (OutputSlot1, OutputSlot2, etc.)
            outputSlots.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));
            
            return outputSlots;
        }

        /// <summary>
        /// Called by the ReactionManager when a reaction is successfully completed.
        /// </summary>
        public void OnReactionCompleted()
        {
            Debug.Log("Reaction completed! Cleaning up lore and preparing for the next stage.");

            // 1. Clear the lore data from the controller. The environment prefab remains as a memento.
            loreController.ClearCurrentLore();
            
            // 2. Update palm display to reflect completion
            if (palmLoreController != null)
            {
                palmLoreController.SetCustomObjective("Reaction completed! Find next lore.");
                Debug.Log("🤚 Updated palm display for reaction completion");
            }
            
            // 3. Clear spawner highlights when lore is unloaded
            if (spawnerHighlighter != null)
            {
                spawnerHighlighter.RefreshHighlighting();
                Debug.Log("🔄 Cleared spawner highlights after reaction completion");
            }
            
            currentEnvironmentInstance = null;

            // 4. Trigger the next lore reading (placeholder for future logic).
            Debug.Log("Triggering next lore read... (Interface for next step)");
        }

        private CharacterSlot FindSlotInPrefab(Transform parent, string slotName)
        {
            Debug.Log($"🔍 Searching for slot '{slotName}' in prefab hierarchy starting from '{parent.name}'");
            
            // First, try to find a "slot" or "slots" container
            Transform slotContainer = FindSlotContainer(parent);
            if (slotContainer != null)
            {
                Debug.Log($"📁 Found slot container: '{slotContainer.name}', searching within it");
                CharacterSlot slot = SearchForSlotRecursive(slotContainer, slotName);
                if (slot != null)
                {
                    Debug.Log($"✅ Found slot '{slotName}' at path: {GetTransformPath(slot.transform)}");
                    return slot;
                }
            }
            
            // Fallback: search the entire hierarchy recursively
            Debug.Log($"🔄 Slot container search failed, searching entire hierarchy for '{slotName}'");
            CharacterSlot fallbackSlot = SearchForSlotRecursive(parent, slotName);
            if (fallbackSlot != null)
            {
                Debug.Log($"✅ Found slot '{slotName}' via fallback at path: {GetTransformPath(fallbackSlot.transform)}");
                return fallbackSlot;
            }
            
            // Debug: Print the entire hierarchy to help understand the structure
            Debug.LogWarning($"❌ Could not find slot '{slotName}' in the environment prefab. Hierarchy dump:");
            PrintHierarchy(parent, 0, 3); // Print up to 3 levels deep
            
            return null;
        }

        /// <summary>
        /// Finds a container that likely holds slots (named "slot", "slots", etc.)
        /// </summary>
        private Transform FindSlotContainer(Transform parent)
        {
            // Check common slot container names
            string[] containerNames = { "slot", "slots", "Slot", "Slots", "SlotContainer", "slot container" };
            
            foreach (string containerName in containerNames)
            {
                Transform container = parent.Find(containerName);
                if (container != null)
                {
                    return container;
                }
            }
            
            // Search recursively for containers
            foreach (Transform child in parent)
            {
                foreach (string containerName in containerNames)
                {
                    if (child.name.ToLower().Contains(containerName.ToLower()))
                    {
                        return child;
                    }
                }
                
                // Recursive search
                Transform found = FindSlotContainer(child);
                if (found != null)
                {
                    return found;
                }
            }
            
            return null;
        }

        /// <summary>
        /// Recursively searches for a slot by name
        /// </summary>
        private CharacterSlot SearchForSlotRecursive(Transform parent, string slotName)
        {
            // Check direct children first
            Transform slotTransform = parent.Find(slotName);
            if (slotTransform != null)
            {
                CharacterSlot slot = slotTransform.GetComponent<CharacterSlot>();
                if (slot != null)
                {
                    return slot;
                }
            }
            
            // Search all children recursively
            foreach (Transform child in parent)
            {
                // Check if this child is the slot we're looking for
                if (child.name == slotName)
                {
                    CharacterSlot slot = child.GetComponent<CharacterSlot>();
                    if (slot != null)
                    {
                        return slot;
                    }
                }
                
                // Recursive search
                CharacterSlot found = SearchForSlotRecursive(child, slotName);
                if (found != null)
                {
                    return found;
                }
            }
            
            return null;
        }

        /// <summary>
        /// Gets the full transform path for debugging
        /// </summary>
        private string GetTransformPath(Transform transform)
        {
            string path = transform.name;
            Transform current = transform.parent;
            
            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }
            
            return path;
        }

        /// <summary>
        /// Prints hierarchy for debugging purposes
        /// </summary>
        private void PrintHierarchy(Transform parent, int currentDepth, int maxDepth)
        {
            if (currentDepth > maxDepth) return;
            
            string indent = new string(' ', currentDepth * 2);
            CharacterSlot slot = parent.GetComponent<CharacterSlot>();
            string slotInfo = slot != null ? " [HAS SLOT]" : "";
            
            Debug.Log($"{indent}- {parent.name}{slotInfo}");
            
            foreach (Transform child in parent)
            {
                PrintHierarchy(child, currentDepth + 1, maxDepth);
            }
        }
    }
}
