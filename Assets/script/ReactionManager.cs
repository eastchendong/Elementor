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

        [Header("Events")]
        public UnityEvent OnAllReactionsCompleted = new UnityEvent();

        // Static event for global subscription
        public static event Action OnGlobalReactionsCompleted;

        public void SetupStages(List<ReactionStage> stages)
        {
            reactionStages = stages;
            currentReactionIndex = 0;
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

                currentReaction.onReactionPhenomenon?.Invoke();

                // Process reaction first - speech will be triggered after new groups are created
                ProcessReaction(currentReaction);

                currentReactionIndex++;

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
            // 1. Gather all available characters from input slots
            List<CharacterView> availableCharacters = new List<CharacterView>();
            List<CharacterGroup> availableGroups = new List<CharacterGroup>();
            List<CharacterGroup> groupsToDestroy = new List<CharacterGroup>();

            foreach (var requirement in stage.requirements)
            {
                object occupant = requirement.slot.GetOccupant();
                int coefficient = requirement.slot.Coefficient;

                if (occupant is CharacterView characterView)
                {
                    // For individual characters, add them based on coefficient
                    for (int i = 0; i < coefficient; i++)
                    {
                        availableCharacters.Add(characterView);
                    }
                }
                else if (occupant is CharacterGroup characterGroup)
                {
                    // For groups, add the group itself and track for running animation
                    for (int i = 0; i < coefficient; i++)
                    {
                        availableGroups.Add(characterGroup);
                        availableCharacters.AddRange(characterGroup.Characters);
                    }
                    groupsToDestroy.Add(characterGroup);
                }
                
                requirement.slot.Release();
            }

            // 2. Process each outcome with running animation
            foreach (var outcome in stage.outcomes)
            {
                if (outcome.outputSlot == null || string.IsNullOrEmpty(outcome.newGroupName)) continue;

                StartCoroutine(ProcessReactionWithAnimation(outcome, availableCharacters, availableGroups, groupsToDestroy));
            }
        }

        private System.Collections.IEnumerator ProcessReactionWithAnimation(
            ReactionOutcome outcome, 
            List<CharacterView> availableCharacters, 
            List<CharacterGroup> availableGroups,
            List<CharacterGroup> groupsToDestroy)
        {
            // Calculate required elements for ONE unit of the product
            Dictionary<string, int> singleProductElementCounts = new Dictionary<string, int>();
            foreach (var elementName in outcome.characterNamesInGroup)
            {
                if (singleProductElementCounts.ContainsKey(elementName))
                    singleProductElementCounts[elementName]++;
                else
                    singleProductElementCounts[elementName] = 1;
            }

            Debug.Log($"Creating {outcome.productCount} independent units of {outcome.newGroupName}");
            foreach (var req in singleProductElementCounts)
            {
                Debug.Log($"Each unit requires {req.Key}: {req.Value}");
            }

            // Count all available elements
            Dictionary<string, int> totalAvailableCounts = new Dictionary<string, int>();
            foreach (var character in availableCharacters)
            {
                string elementName = character.GetModel().GetCharacterName();
                if (totalAvailableCounts.ContainsKey(elementName))
                    totalAvailableCounts[elementName]++;
                else
                    totalAvailableCounts[elementName] = 1;
            }

            Debug.Log("Available elements:");
            foreach (var avail in totalAvailableCounts)
            {
                Debug.Log($"Available {avail.Key}: {avail.Value}");
            }

            // Calculate total needed elements for all products
            Dictionary<string, int> totalRequiredCounts = new Dictionary<string, int>();
            foreach (var element in singleProductElementCounts)
            {
                totalRequiredCounts[element.Key] = element.Value * outcome.productCount;
            }

            // Separate participating and excess characters/groups
            List<CharacterView> participatingCharacters = new List<CharacterView>();
            List<CharacterGroup> participatingGroups = new List<CharacterGroup>();
            List<CharacterView> excessCharacters = new List<CharacterView>();
            List<CharacterGroup> excessGroups = new List<CharacterGroup>();
            Dictionary<string, int> allocatedCounts = new Dictionary<string, int>();

            // First, allocate from available groups
            foreach (var group in availableGroups)
            {
                bool groupParticipates = false;
                foreach (var character in group.Characters)
                {
                    string elementName = character.GetModel().GetCharacterName();
                    if (totalRequiredCounts.ContainsKey(elementName))
                    {
                        if (!allocatedCounts.ContainsKey(elementName))
                            allocatedCounts[elementName] = 0;

                        if (allocatedCounts[elementName] < totalRequiredCounts[elementName])
                        {
                            if (!groupParticipates)
                            {
                                participatingGroups.Add(group);
                                groupParticipates = true;
                            }
                            allocatedCounts[elementName]++;
                        }
                    }
                }
                
                if (!groupParticipates)
                {
                    excessGroups.Add(group);
                }
            }

            // Then allocate from individual characters
            foreach (var character in availableCharacters)
            {
                if (participatingGroups.Any(g => g.Characters.Contains(character)))
                    continue;
                
                if (excessGroups.Any(g => g.Characters.Contains(character)))
                    continue;

                string elementName = character.GetModel().GetCharacterName();
                if (totalRequiredCounts.ContainsKey(elementName))
                {
                    if (!allocatedCounts.ContainsKey(elementName))
                        allocatedCounts[elementName] = 0;

                    if (allocatedCounts[elementName] < totalRequiredCounts[elementName])
                    {
                        participatingCharacters.Add(character);
                        allocatedCounts[elementName]++;
                    }
                    else
                    {
                        excessCharacters.Add(character);
                    }
                }
                else
                {
                    excessCharacters.Add(character);
                }
            }

            Debug.Log($"Participating characters: {participatingCharacters.Count}");
            Debug.Log($"Participating groups: {participatingGroups.Count}");
            Debug.Log($"Excess characters: {excessCharacters.Count}");
            Debug.Log($"Excess groups: {excessGroups.Count}");

            // 3. Start animations
            Vector3 targetPosition = outcome.outputSlot.transform.position;
            Vector3 exitPosition = targetPosition + Vector3.left * 4.0f;

            // Make participating groups run to target
            foreach (var group in participatingGroups)
            {
                group.SetState(CharacterAnimationState.Running);
                StartCoroutine(MoveToTarget(group.transform, targetPosition, 2.0f));
            }

            // Make participating individual characters run to target
            foreach (var character in participatingCharacters)
            {
                character.GetModel().SetAnimationState(CharacterAnimationState.Running);
                StartCoroutine(MoveToTarget(character.transform, targetPosition + UnityEngine.Random.insideUnitSphere * 0.5f, 2.0f));
            }

            // Make excess groups run away from reaction
            foreach (var group in excessGroups)
            {
                group.SetState(CharacterAnimationState.Running);
                Vector3 randomExitPos = exitPosition + UnityEngine.Random.insideUnitSphere * 2.0f;
                randomExitPos.y = targetPosition.y;
                StartCoroutine(MoveToTarget(group.transform, randomExitPos, 2.0f));
            }

            // Make excess individual characters run away from reaction
            foreach (var character in excessCharacters)
            {
                character.GetModel().SetAnimationState(CharacterAnimationState.Running);
                Vector3 randomExitPos = exitPosition + UnityEngine.Random.insideUnitSphere * 2.0f;
                randomExitPos.y = targetPosition.y;
                StartCoroutine(MoveToTarget(character.transform, randomExitPos, 2.0f));
            }

            // Wait for running animation to complete
            yield return new WaitForSeconds(2.5f);

            // 4. Handle excess characters/groups - set them to idle after moving away
            foreach (var group in excessGroups)
            {
                group.SetState(CharacterAnimationState.Idle);
            }
            foreach (var character in excessCharacters)
            {
                character.GetModel().SetAnimationState(CharacterAnimationState.Idle);
            }

            // 5. Destroy old participating groups and extract their characters
            List<CharacterView> extractedCharacters = new List<CharacterView>(participatingCharacters);
            foreach (var group in groupsToDestroy)
            {
                if (participatingGroups.Contains(group))
                {
                    var groupCharacters = new List<CharacterView>(group.Characters);
                    foreach (var character in groupCharacters)
                    {
                        group.RemoveCharacter(character);
                        if (!extractedCharacters.Contains(character))
                        {
                            extractedCharacters.Add(character);
                        }
                    }
                    group.ClearAndDestroy();
                }
            }

            // 6. Spawn missing characters if needed (系数驱动的额外生成)
            List<CharacterView> allAvailableCharacters = new List<CharacterView>(extractedCharacters);
            
            foreach (var required in totalRequiredCounts)
            {
                int currentCount = allocatedCounts.ContainsKey(required.Key) ? allocatedCounts[required.Key] : 0;
                int neededCount = required.Value - currentCount;

                if (neededCount > 0)
                {
                    Debug.Log($"Coefficient-driven spawning: {neededCount} additional {required.Key} characters");
                    
                    for (int i = 0; i < neededCount; i++)
                    {
                        Vector3 spawnPosition = targetPosition + Vector3.up * 3.0f + UnityEngine.Random.insideUnitSphere * 1.0f;
                        characterSpawnController.SpawnCharacter(required.Key, spawnPosition, outcome.outputSlot.transform.parent);
                        
                        yield return new WaitForEndOfFrame();
                        
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

            // Wait for falling characters to settle
            yield return new WaitForSeconds(1.0f);

            // 7. Create multiple independent product groups
            List<CharacterGroup> createdGroups = new List<CharacterGroup>();
            List<CharacterView> allCreatedCharacters = new List<CharacterView>();

            for (int productIndex = 0; productIndex < outcome.productCount; productIndex++)
            {
                // Collect characters needed for this specific product unit
                List<CharacterView> charactersForThisProduct = new List<CharacterView>();
                Dictionary<string, int> neededForThisProduct = new Dictionary<string, int>(singleProductElementCounts);

                // Allocate characters for this product
                List<CharacterView> remainingCharacters = new List<CharacterView>(allAvailableCharacters);
                foreach (var character in remainingCharacters)
                {
                    string elementName = character.GetModel().GetCharacterName();
                    if (neededForThisProduct.ContainsKey(elementName) && neededForThisProduct[elementName] > 0)
                    {
                        charactersForThisProduct.Add(character);
                        allAvailableCharacters.Remove(character);
                        neededForThisProduct[elementName]--;
                    }
                }

                // Create individual product group
                if (charactersForThisProduct.Count > 0)
                {
                    Vector3 productPosition = targetPosition + Vector3.right * productIndex * 1.5f; // Spread products horizontally
                    
                    CharacterGroup newGroup = characterSpawnController.CreateCharacterGroup(
                        outcome.newGroupName, 
                        productPosition,
                        outcome.outputSlot.transform.parent
                    );

                    if (newGroup != null)
                    {
                        foreach (var character in charactersForThisProduct)
                        {
                            characterSpawnController.AddCharacterToGroup(newGroup, character);
                        }

                        newGroup.SetState(CharacterAnimationState.Falling);
                        createdGroups.Add(newGroup);
                        allCreatedCharacters.AddRange(charactersForThisProduct);
                    }
                }
            }

            // 8. Occupy slot with the first group (representing all products)
            if (createdGroups.Count > 0)
            {
                outcome.outputSlot.Occupy(createdGroups[0]);
                outcome.outputSlot.SetCoefficient(1); // No coefficient on final products
                
                yield return new WaitForSeconds(0.5f);
                SpeechController.Instance?.TriggerSpeech(SpeechTriggerType.ReactionSuccess, allCreatedCharacters);
            }
        }

        private System.Collections.IEnumerator MoveToTarget(Transform mover, Vector3 target, float duration)
        {
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
}