using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using Elementor.Core;
using Elementor.Core.Speech;

namespace Elementor
{
    public class ReactionProcessor : MonoBehaviour
    {
        private CharacterSpawnController characterSpawnController;
        [SerializeField] private List<CharacterSlot> predefinedOutputSlots = new List<CharacterSlot>();

        public void Initialize(CharacterSpawnController spawnController, List<CharacterSlot> outputSlots)
        {
            characterSpawnController = spawnController;
            predefinedOutputSlots = outputSlots;
        }

        public void ProcessReaction(ReactionStage stage)
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
                    allAvailableCharacters.Add(characterView);
                    SpawnAdditionalCharacters(characterView, coefficient, requirement.slot.transform.parent, allAvailableCharacters);
                }
                else if (occupant is CharacterGroup characterGroup)
                {
                    var groupCharacters = new List<CharacterView>(characterGroup.Characters);
                    allAvailableCharacters.AddRange(groupCharacters);
                    groupsToDestroy.Add(characterGroup);

                    SpawnAdditionalGroupCharacters(groupCharacters, coefficient, requirement.slot.transform.parent, allAvailableCharacters);
                }

                requirement.slot.Release();
            }

            // 2. Disassemble groups
            DisassembleGroups(groupsToDestroy);

            // 3. Process outcomes
            StartCoroutine(ProcessAllOutcomes(stage.outcomes, allAvailableCharacters));
        }

        private void SpawnAdditionalCharacters(CharacterView originalCharacter, int coefficient, Transform parent, List<CharacterView> characterList)
        {
            for (int i = 1; i < coefficient; i++)
            {
                Vector3 spawnPosition = originalCharacter.transform.position + Vector3.up * 3.0f + Random.insideUnitSphere * 1.0f;
                string characterName = originalCharacter.GetModel().GetCharacterName();
                characterSpawnController.SpawnCharacter(characterName, spawnPosition, parent);

                var newCharacters = characterSpawnController.GetSpawnedCharacters();
                if (newCharacters.Count > 0)
                {
                    var newCharacter = newCharacters[newCharacters.Count - 1];
                    characterList.Add(newCharacter);
                    newCharacter.GetModel().SetAnimationState(CharacterAnimationState.Falling);
                }
            }
        }

        private void SpawnAdditionalGroupCharacters(List<CharacterView> groupCharacters, int coefficient, Transform parent, List<CharacterView> characterList)
        {
            for (int i = 1; i < coefficient; i++)
            {
                foreach (var character in groupCharacters)
                {
                    Vector3 spawnPosition = character.transform.position + Vector3.up * 3.0f + Random.insideUnitSphere * 1.0f;
                    string characterName = character.GetModel().GetCharacterName();
                    characterSpawnController.SpawnCharacter(characterName, spawnPosition, parent);

                    var newCharacters = characterSpawnController.GetSpawnedCharacters();
                    if (newCharacters.Count > 0)
                    {
                        var newCharacter = newCharacters[newCharacters.Count - 1];
                        characterList.Add(newCharacter);
                        newCharacter.GetModel().SetAnimationState(CharacterAnimationState.Falling);
                    }
                }
            }
        }

        private void DisassembleGroups(List<CharacterGroup> groupsToDestroy)
        {
            foreach (var group in groupsToDestroy)
            {
                var groupCharacters = new List<CharacterView>(group.Characters);
                foreach (var character in groupCharacters)
                {
                    group.RemoveCharacter(character);
                    character.GetModel().EnableIndividualPhysics();
                }
                group.ClearAndDestroy();
            }
        }

        private IEnumerator ProcessAllOutcomes(List<ReactionOutcome> outcomes, List<CharacterView> allAvailableCharacters)
        {
            List<CharacterView> allProductCharacters = new List<CharacterView>();

            for (int i = 0; i < outcomes.Count; i++)
            {
                var outcome = outcomes[i];
                if (string.IsNullOrEmpty(outcome.newGroupName)) continue;

                // Only create new slot if we don't have enough predefined slots
                if (outcome.outputSlot == null)
                {
                    if (i < predefinedOutputSlots.Count)
                    {
                        outcome.outputSlot = predefinedOutputSlots[i];
                        Debug.Log($"Using predefined slot {outcome.outputSlot.name} for outcome {outcome.newGroupName}");
                    }
                    else
                    {
                        // Last resort: create new slot only if we have more outcomes than predefined slots
                        Debug.LogWarning($"Not enough predefined slots! Creating additional slot for outcome {outcome.newGroupName}");
                        outcome.outputSlot = CreateOutputSlot(GetNextSlotPosition(i), GetSlotParent(), $"GeneratedOutputSlot_{i}");
                    }
                }

                var productCharacters = ProcessSingleOutcome(outcome, allAvailableCharacters);
                if (productCharacters != null && productCharacters.Count > 0)
                {
                    allProductCharacters.AddRange(productCharacters);

                    Vector3 targetPosition = outcome.outputSlot.transform.position;
                    foreach (var character in productCharacters)
                    {
                        StartCoroutine(MoveToTarget(character.transform, targetPosition + Random.insideUnitSphere * 0.5f, 2.0f));
                    }

                    yield return new WaitForSeconds(2.5f);
                    yield return StartCoroutine(CreateProductGroup(outcome, productCharacters, targetPosition));
                }
            }

            // Handle excess characters
            HandleExcessCharacters(allAvailableCharacters);

            yield return new WaitForSeconds(2.5f);

            // Trigger speech
            if (allProductCharacters.Count > 0)
            {
                var speechController = FindObjectOfType<Elementor.Core.Speech.SpeechController>();
                speechController?.TriggerSpeech(Elementor.Core.Speech.SpeechTriggerType.ReactionSuccess, allProductCharacters);
            }
        }

        private CharacterSlot GetOrCreateOutputSlot(int outcomeIndex)
        {
            // Always try to use predefined slots first
            if (outcomeIndex < predefinedOutputSlots.Count)
            {
                var slot = predefinedOutputSlots[outcomeIndex];
                Debug.Log($"Using predefined slot {slot.name} for outcome index {outcomeIndex}");
                return slot;
            }

            // Only create new slot if we absolutely need more slots than predefined
            Debug.LogWarning($"Creating additional output slot for outcome index {outcomeIndex} - not enough predefined slots!");
            return CreateOutputSlot(GetNextSlotPosition(outcomeIndex), GetSlotParent(), $"GeneratedOutputSlot_{outcomeIndex}");
        }

        /// <summary>
        /// Gets the position for the next slot when we need to create one
        /// </summary>
        private Vector3 GetNextSlotPosition(int outcomeIndex)
        {
            if (predefinedOutputSlots.Count > 0)
            {
                // Position relative to last predefined slot
                var lastSlot = predefinedOutputSlots[predefinedOutputSlots.Count - 1];
                int extraSlotsNeeded = outcomeIndex - predefinedOutputSlots.Count + 1;
                return lastSlot.transform.position + Vector3.right * 3.0f * extraSlotsNeeded;
            }
            else
            {
                // Fallback position relative to reaction processor
                return transform.position + Vector3.right * 3.0f * (outcomeIndex + 1);
            }
        }

        /// <summary>
        /// Gets the appropriate parent for new slots
        /// </summary>
        private Transform GetSlotParent()
        {
            if (predefinedOutputSlots.Count > 0)
            {
                return predefinedOutputSlots[0].transform.parent;
            }
            else
            {
                return transform.parent;
            }
        }

        private CharacterSlot CreateOutputSlot(Vector3 position, Transform parent, string slotName)
        {
            GameObject slotGO = new GameObject(slotName);
            slotGO.transform.position = position;
            slotGO.transform.SetParent(parent, true);

            CharacterSlot slot = slotGO.AddComponent<CharacterSlot>();
            BoxCollider collider = slotGO.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = new Vector3(2f, 2f, 2f);

            Debug.Log($"Created new output slot: {slotName} at position {position}");
            return slot;
        }

        private void HandleExcessCharacters(List<CharacterView> allAvailableCharacters)
        {
            if (allAvailableCharacters.Count > 0)
            {
                Debug.Log($"Handling {allAvailableCharacters.Count} excess characters");
                Vector3 exitPosition = transform.position + Vector3.left * 4.0f;

                foreach (var character in allAvailableCharacters)
                {
                    Vector3 randomExitPos = exitPosition + Random.insideUnitSphere * 2.0f;
                    randomExitPos.y = transform.position.y;
                    StartCoroutine(MoveToTarget(character.transform, randomExitPos, 2.0f));
                }
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

        private IEnumerator CreateProductGroup(ReactionOutcome outcome, List<CharacterView> productCharacters, Vector3 targetPosition)
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
                        Vector3 spawnPosition = targetPosition + Vector3.up * 3.0f + Random.insideUnitSphere * 1.0f;
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

        private IEnumerator MoveToTarget(Transform mover, Vector3 target, float duration)
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
    }
}
