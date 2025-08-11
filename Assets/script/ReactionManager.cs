using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.Events;
using Elementor.Core.Speech;
using Elementor.Core;
using System;

namespace Elementor
{
    public class ReactionManager : MonoBehaviour
    {
        [SerializeField] private List<ReactionStage> reactionStages;
        [SerializeField] private int currentReactionIndex = 0;
        [SerializeField] private CharacterSpawnController characterSpawnController;

        [Header("Reaction Effects")]
        [SerializeField] private Transform conditionEffectsParent;
        [SerializeField] private Transform phenomenonEffectsParent;

        [Header("Events")]
        public UnityEvent OnAllReactionsCompleted = new UnityEvent();

        // Static event for global subscription
        public static event Action OnGlobalReactionsCompleted;

        // Track active condition effects
        private List<GameObject> activeConditionEffects = new List<GameObject>();

        public void SetupStages(List<ReactionStage> stages)
        {
            reactionStages = stages;
            currentReactionIndex = 0;
            
            // Activate condition effects for the first stage
            ActivateReactionCondition();
            
            Debug.Log($"ReactionManager setup with {stages.Count} stages.");
        }

        private void Start()
        {
            if (characterSpawnController == null)
            {
                characterSpawnController = FindObjectOfType<CharacterSpawnController>();
                if (characterSpawnController == null)
                {
                    Debug.LogError("ReactionManager needs a reference to CharacterSpawnController.");
                }
            }
        }

        [ContextMenu("Check Reaction Completion")]
        public void CheckReactionCompletion()
        {
            if (currentReactionIndex >= reactionStages.Count)
            {
                Debug.Log("All reactions completed!");
                return;
            }

            ReactionStage currentReaction = reactionStages[currentReactionIndex];
            if (IsReactionComplete(currentReaction))
            {
                Debug.Log($"Reaction '{currentReaction.reactionName}' completed!");

                // Deactivate condition effects
                DeactivateConditionEffects();

                // Activate phenomenon effects
                ActivateReactionPhenomenon(currentReaction);

                currentReaction.onReactionPhenomenon?.Invoke();

                // Process reaction first - speech will be triggered after new groups are created
                ProcessReaction(currentReaction);

                currentReactionIndex++;

                // Activate next stage's condition effects if available
                if (currentReactionIndex < reactionStages.Count)
                {
                    ActivateReactionCondition();
                }

                // Check if all reactions are completed
                if (currentReactionIndex >= reactionStages.Count)
                {
                    Debug.Log("All reaction stages completed! Triggering completion event.");
                    OnAllReactionsCompleted?.Invoke();
                    OnGlobalReactionsCompleted?.Invoke();
                }
            }
            else
            {
                Debug.Log($"Reaction '{currentReaction.reactionName}' requirements not met.");

                // Trigger speech for reaction failure with current participants
                var participantCharacters = GetParticipantCharacters(currentReaction);
                SpeechController.Instance?.TriggerSpeech(SpeechTriggerType.ReactionFailure, participantCharacters);
            }
        }

        /// <summary>
        /// Activates reaction condition effects for the current stage
        /// </summary>
        private void ActivateReactionCondition()
        {
            if (currentReactionIndex >= reactionStages.Count || conditionEffectsParent == null)
                return;

            ReactionStage currentStage = reactionStages[currentReactionIndex];
            
            // Find and activate condition effect GameObject
            if (!string.IsNullOrEmpty(currentStage.conditionEffectName))
            {
                GameObject conditionEffect = FindEffectByName(conditionEffectsParent, currentStage.conditionEffectName);
                if (conditionEffect != null)
                {
                    conditionEffect.SetActive(true);
                    activeConditionEffects.Add(conditionEffect);
                    Debug.Log($"Activated condition effect: {currentStage.conditionEffectName}");
                }
                else
                {
                    Debug.LogWarning($"Condition effect '{currentStage.conditionEffectName}' not found!");
                }
            }
        }

        /// <summary>
        /// Activates reaction phenomenon effects for the completed reaction
        /// </summary>
        private void ActivateReactionPhenomenon(ReactionStage completedStage)
        {
            if (phenomenonEffectsParent == null)
                return;

            // Find and activate phenomenon effect GameObject
            if (!string.IsNullOrEmpty(completedStage.phenomenonEffectName))
            {
                GameObject phenomenonEffect = FindEffectByName(phenomenonEffectsParent, completedStage.phenomenonEffectName);
                if (phenomenonEffect != null)
                {
                    phenomenonEffect.SetActive(true);
                    Debug.Log($"Activated phenomenon effect: {completedStage.phenomenonEffectName}");
                    
                    // Optionally deactivate after some time
                    StartCoroutine(DeactivateEffectAfterDelay(phenomenonEffect, 5.0f));
                }
                else
                {
                    Debug.LogWarning($"Phenomenon effect '{completedStage.phenomenonEffectName}' not found!");
                }
            }
        }

        /// <summary>
        /// Deactivates all active condition effects
        /// </summary>
        private void DeactivateConditionEffects()
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
        private System.Collections.IEnumerator DeactivateEffectAfterDelay(GameObject effect, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (effect != null)
            {
                effect.SetActive(false);
                Debug.Log($"Auto-deactivated effect: {effect.name}");
            }
        }

        public void SetupStagesFromJSON(string jsonPath)
        {
            try
            {
                string jsonText = System.IO.File.ReadAllText(jsonPath);
                var loreData = JsonUtility.FromJson<LoreData>(jsonText);

                var stages = ParseReactionFromLore(loreData);
                SetupStages(stages);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to setup stages from JSON: {e.Message}");
            }
        }

        private List<ReactionStage> ParseReactionFromLore(LoreData loreData)
        {
            var stages = new List<ReactionStage>();
            var stage = new ReactionStage();
            stage.reactionName = loreData.reaction.equation;
            
            // Set effect names based on reaction name or type
            stage.conditionEffectName = GetConditionEffectName(loreData.reaction);
            stage.phenomenonEffectName = GetPhenomenonEffectName(loreData.reaction);
            
            stage.requirements = new List<SlotRequirement>();
            stage.outcomes = new List<ReactionOutcome>();

            // Parse reactants
            foreach (var reactant in loreData.reaction.reactants)
            {
                var requirement = new SlotRequirement();
                requirement.requiredCount = reactant.count;
                requirement.requiredName = reactant.name;
                stage.requirements.Add(requirement);
            }

            // Parse products
            foreach (var product in loreData.reaction.products)
            {
                var outcome = new ReactionOutcome();
                outcome.newGroupName = product.name;
                outcome.productCount = product.count;
                outcome.characterNamesInGroup = new List<string>();

                foreach (var element in product.elements)
                {
                    for (int i = 0; i < element.count; i++)
                    {
                        outcome.characterNamesInGroup.Add(element.element);
                    }
                }

                stage.outcomes.Add(outcome);
            }

            stages.Add(stage);
            return stages;
        }

        /// <summary>
        /// Determines condition effect name based on reaction data
        /// Override this method to customize effect naming logic
        /// </summary>
        protected virtual string GetConditionEffectName(ReactionData reactionData)
        {
            // Example: use reaction equation or customize based on your naming convention
            if (reactionData.equation.Contains("燃烧") || reactionData.equation.Contains("burn"))
                return "BurningCondition";
            else if (reactionData.equation.Contains("发光") || reactionData.equation.Contains("glow"))
                return "GlowingCondition";
            
            // Default naming
            return reactionData.equation + "_Condition";
        }

        /// <summary>
        /// Determines phenomenon effect name based on reaction data
        /// Override this method to customize effect naming logic
        /// </summary>
        protected virtual string GetPhenomenonEffectName(ReactionData reactionData)
        {
            // Example: use reaction equation or customize based on your naming convention
            if (reactionData.equation.Contains("燃烧") || reactionData.equation.Contains("burn"))
                return "BurningPhenomenon";
            else if (reactionData.equation.Contains("发光") || reactionData.equation.Contains("glow"))
                return "GlowingPhenomenon";
            
            // Default naming
            return reactionData.equation + "_Phenomenon";
        }

        private bool IsReactionComplete(ReactionStage stage)
        {
            Debug.Log($"Checking reaction completion for '{stage.reactionName}':");

            foreach (var requirement in stage.requirements)
            {
                if (requirement.slot == null)
                {
                    Debug.LogWarning($"Requirement in reaction '{stage.reactionName}' has a null slot.");
                    continue;
                }

                bool requirementMet = false;
                string occupantName = requirement.slot.GetOccupantName();
                int slotCoefficient = requirement.slot.Coefficient;

                // Simplified check - only match name and coefficient, regardless of character/group type
                if (occupantName == requirement.requiredName && slotCoefficient == requirement.requiredCount)
                {
                    requirementMet = true;
                }

                Debug.Log($"Slot '{requirement.slot.name}': Occupant='{occupantName}', Coefficient={slotCoefficient}, " +
                         $"Required='{requirement.requiredName}', RequiredCount={requirement.requiredCount}, Met={requirementMet}");

                if (!requirementMet)
                {
                    return false;
                }
            }

            return true;
        }

        private void ProcessReaction(ReactionStage stage)
        {
            // 1. Handle coefficient-based spawning for each slot
            List<CharacterView> allAvailableCharacters = new List<CharacterView>();
            List<CharacterGroup> groupsToDestroy = new List<CharacterGroup>();

            foreach (var requirement in stage.requirements)
            {
                object occupant = requirement.slot.GetOccupant();
                int coefficient = requirement.slot.Coefficient;

                if (occupant is CharacterView characterView)
                {
                    // For individual characters, spawn additional copies based on coefficient
                    allAvailableCharacters.Add(characterView);
                    
                    // Spawn additional characters if coefficient > 1
                    for (int i = 1; i < coefficient; i++)
                    {
                        Vector3 spawnPosition = characterView.transform.position + Vector3.up * 3.0f + UnityEngine.Random.insideUnitSphere * 1.0f;
                        string characterName = characterView.GetModel().GetCharacterName();
                        characterSpawnController.SpawnCharacter(characterName, spawnPosition, requirement.slot.transform.parent);
                        
                        // Get the newly spawned character
                        var newCharacters = characterSpawnController.GetSpawnedCharacters();
                        if (newCharacters.Count > 0)
                        {
                            var newCharacter = newCharacters[newCharacters.Count - 1];
                            allAvailableCharacters.Add(newCharacter);
                            newCharacter.GetModel().SetAnimationState(CharacterAnimationState.Falling);
                        }
                    }
                }
                else if (occupant is CharacterGroup characterGroup)
                {
                    // For groups, extract all characters and mark group for destruction
                    var groupCharacters = new List<CharacterView>(characterGroup.Characters);
                    allAvailableCharacters.AddRange(groupCharacters);
                    groupsToDestroy.Add(characterGroup);
                    
                    // Spawn additional copies based on coefficient
                    for (int i = 1; i < coefficient; i++)
                    {
                        foreach (var character in groupCharacters)
                        {
                            Vector3 spawnPosition = character.transform.position + Vector3.up * 3.0f + UnityEngine.Random.insideUnitSphere * 1.0f;
                            string characterName = character.GetModel().GetCharacterName();
                            characterSpawnController.SpawnCharacter(characterName, spawnPosition, requirement.slot.transform.parent);
                            
                            // Get the newly spawned character
                            var newCharacters = characterSpawnController.GetSpawnedCharacters();
                            if (newCharacters.Count > 0)
                            {
                                var newCharacter = newCharacters[newCharacters.Count - 1];
                                allAvailableCharacters.Add(newCharacter);
                                newCharacter.GetModel().SetAnimationState(CharacterAnimationState.Falling);
                            }
                        }
                    }
                }
                
                requirement.slot.Release();
            }

            // 2. Disassemble groups - remove group relationships but keep characters
            foreach (var group in groupsToDestroy)
            {
                var groupCharacters = new List<CharacterView>(group.Characters);
                foreach (var character in groupCharacters)
                {
                    group.RemoveCharacter(character);
                    character.GetModel().EnableIndividualPhysics();
                }
                // Destroy the empty group
                group.ClearAndDestroy();
            }

            // 3. Process each outcome sequentially with shared character list
            StartCoroutine(ProcessAllOutcomes(stage.outcomes, allAvailableCharacters));
        }

        private System.Collections.IEnumerator ProcessAllOutcomes(List<ReactionOutcome> outcomes, List<CharacterView> allAvailableCharacters)
        {
            List<CharacterView> allProductCharacters = new List<CharacterView>();
            
            // Process each outcome sequentially
            for (int i = 0; i < outcomes.Count; i++)
            {
                var outcome = outcomes[i];
                if (outcome.outputSlot == null || string.IsNullOrEmpty(outcome.newGroupName)) continue;

                var productCharacters = ProcessSingleOutcome(outcome, allAvailableCharacters);
                if (productCharacters != null && productCharacters.Count > 0)
                {
                    allProductCharacters.AddRange(productCharacters);
                    
                    // Start running animation for this outcome's characters
                    Vector3 targetPosition = outcome.outputSlot.transform.position;
                    foreach (var character in productCharacters)
                    {
                        StartCoroutine(MoveToTarget(character.transform, targetPosition + UnityEngine.Random.insideUnitSphere * 0.5f, 2.0f));
                    }
                    
                    // Wait for running animation
                    yield return new WaitForSeconds(2.5f);
                    
                    // Create the group for this outcome
                    yield return StartCoroutine(CreateProductGroup(outcome, productCharacters, targetPosition));
                }
            }

            // 4. Handle excess characters after all outcomes are processed
            if (allAvailableCharacters.Count > 0)
            {
                Debug.Log($"Handling {allAvailableCharacters.Count} excess characters");
                Vector3 exitPosition = transform.position + Vector3.left * 4.0f;
                
                foreach (var character in allAvailableCharacters)
                {
                    Vector3 randomExitPos = exitPosition + UnityEngine.Random.insideUnitSphere * 2.0f;
                    randomExitPos.y = transform.position.y;
                    StartCoroutine(MoveToTarget(character.transform, randomExitPos, 2.0f));
                }
                
                yield return new WaitForSeconds(2.5f);
                
                // Make excess characters disappear or set to idle
                foreach (var character in allAvailableCharacters)
                {
                    character.GetModel().SetAnimationState(CharacterAnimationState.Idle);
                    // Optionally destroy excess characters
                    // Destroy(character.gameObject);
                }
            }

            // 5. Trigger speech after all outcomes are complete
            if (allProductCharacters.Count > 0)
            {
                SpeechController.Instance?.TriggerSpeech(SpeechTriggerType.ReactionSuccess, allProductCharacters);
            }
        }

        private List<CharacterView> ProcessSingleOutcome(ReactionOutcome outcome, List<CharacterView> availableCharacters)
        {
            // Calculate required elements for this single product unit
            Dictionary<string, int> requiredElementCounts = new Dictionary<string, int>();
            foreach (var elementName in outcome.characterNamesInGroup)
            {
                if (requiredElementCounts.ContainsKey(elementName))
                    requiredElementCounts[elementName]++;
                else
                    requiredElementCounts[elementName] = 1;
            }

            Debug.Log($"Processing outcome: {outcome.newGroupName}");
            foreach (var req in requiredElementCounts)
            {
                Debug.Log($"Required {req.Key}: {req.Value}");
            }

            // Allocate required characters from available pool
            List<CharacterView> allocatedCharacters = new List<CharacterView>();
            Dictionary<string, int> allocatedCounts = new Dictionary<string, int>();

            // Iterate backwards to safely remove elements
            for (int i = availableCharacters.Count - 1; i >= 0; i--)
            {
                var character = availableCharacters[i];
                string elementName = character.GetModel().GetCharacterName();
                
                if (requiredElementCounts.ContainsKey(elementName))
                {
                    if (!allocatedCounts.ContainsKey(elementName))
                        allocatedCounts[elementName] = 0;

                    if (allocatedCounts[elementName] < requiredElementCounts[elementName])
                    {
                        allocatedCharacters.Add(character);
                        allocatedCounts[elementName]++;
                        
                        // Remove from available characters pool
                        availableCharacters.RemoveAt(i);
                    }
                }
            }

            return allocatedCharacters;
        }

        private System.Collections.IEnumerator CreateProductGroup(ReactionOutcome outcome, List<CharacterView> productCharacters, Vector3 targetPosition)
        {
            // Spawn missing characters if needed
            Dictionary<string, int> requiredElementCounts = new Dictionary<string, int>();
            foreach (var elementName in outcome.characterNamesInGroup)
            {
                if (requiredElementCounts.ContainsKey(elementName))
                    requiredElementCounts[elementName]++;
                else
                    requiredElementCounts[elementName] = 1;
            }

            Dictionary<string, int> currentCounts = new Dictionary<string, int>();
            foreach (var character in productCharacters)
            {
                string elementName = character.GetModel().GetCharacterName();
                if (currentCounts.ContainsKey(elementName))
                    currentCounts[elementName]++;
                else
                    currentCounts[elementName] = 1;
            }

            // Spawn missing characters
            foreach (var required in requiredElementCounts)
            {
                int currentCount = currentCounts.ContainsKey(required.Key) ? currentCounts[required.Key] : 0;
                int neededCount = required.Value - currentCount;

                if (neededCount > 0)
                {
                    Debug.Log($"Spawning {neededCount} additional {required.Key} characters");
                    
                    for (int i = 0; i < neededCount; i++)
                    {
                        Vector3 spawnPosition = targetPosition + Vector3.up * 3.0f + UnityEngine.Random.insideUnitSphere * 1.0f;
                        characterSpawnController.SpawnCharacter(required.Key, spawnPosition, outcome.outputSlot.transform.parent);
                        
                        yield return new WaitForEndOfFrame();
                        
                        var newCharacters = characterSpawnController.GetSpawnedCharacters();
                        if (newCharacters.Count > 0)
                        {
                            var newCharacter = newCharacters[newCharacters.Count - 1];
                            productCharacters.Add(newCharacter);
                            newCharacter.GetModel().SetAnimationState(CharacterAnimationState.Falling);
                        }
                    }
                }
            }

            // Wait for falling characters to settle
            yield return new WaitForSeconds(1.0f);

            // Create the group
            if (productCharacters.Count > 0)
            {
                CharacterGroup newGroup = characterSpawnController.CreateCharacterGroup(
                    outcome.newGroupName, 
                    targetPosition,
                    outcome.outputSlot.transform.parent
                );

                if (newGroup != null)
                {
                    foreach (var character in productCharacters)
                    {
                        characterSpawnController.AddCharacterToGroup(newGroup, character);
                    }

                    // Occupy the slot with this product
                    outcome.outputSlot.Occupy(newGroup);
                    outcome.outputSlot.SetCoefficient(1);

                    newGroup.SetState(CharacterAnimationState.Falling);
                    yield return new WaitForSeconds(0.5f);
                }
            }
        }

        private System.Collections.IEnumerator MoveToTarget(Transform mover, Vector3 target, float duration)
        {
            mover.GetComponent<CharacterView>().StartRunning();
            Vector3 startPosition = mover.position;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;

                // Use smooth curve for natural movement
                progress = Mathf.SmoothStep(0f, 1f, progress);

                mover.position = Vector3.Lerp(startPosition, target, progress);
                yield return null;
            }

            mover.position = target;
            mover.GetComponent<CharacterView>().StopRunning();
        }

        private List<CharacterView> GetParticipantCharacters(ReactionStage stage)
        {
            var participants = new List<CharacterView>();

            foreach (var requirement in stage.requirements)
            {
                if (requirement.slot == null) continue;

                object occupant = requirement.slot.GetOccupant();
                if (occupant is CharacterView character)
                {
                    participants.Add(character);
                }
                else if (occupant is CharacterGroup group)
                {
                    participants.AddRange(group.Characters);
                }
            }

            return participants;
        }

        // Add data classes for JSON parsing
        [System.Serializable]
        public class LoreData
        {
            public ReactionData reaction;
        }

        [System.Serializable]
        public class ReactionData
        {
            public string equation;
            public ReactantData[] reactants;
            public ProductData[] products;
        }

        [System.Serializable]
        public class ReactantData
        {
            public string name;
            public string type;
            public int count;
            public ElementData[] elements;
        }

        [System.Serializable]
        public class ProductData
        {
            public string name;
            public string type;
            public int count;
            public ElementData[] elements;
        }

        [System.Serializable]
        public class ElementData
        {
            public string element;
            public int count;
        }
    }

    [System.Serializable]
    public class ReactionStage
    {
        public string reactionName;
        public List<SlotRequirement> requirements;
        public List<ReactionOutcome> outcomes;
        public UnityEvent onReactionPhenomenon;
        
        [Header("Effect Names")]
        public string conditionEffectName;  // Name of condition effect GameObject
        public string phenomenonEffectName; // Name of phenomenon effect GameObject
    }
}