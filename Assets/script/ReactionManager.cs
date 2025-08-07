using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.Events;
using Elementor.Core.Speech;
using Elementor.Core;
using System;


namespace Elementor
{
    [System.Serializable]
    public class SlotRequirement
    {
        public CharacterSlot slot;
        public string requiredName;  
        public int requiredCount = 1;
    }

    [System.Serializable]
    public class ReactionOutcome
    {
        public CharacterSlot outputSlot;
        public string newGroupName;
        public List<string> characterNamesInGroup;
        public int productCount = 1;         // Number of products to create
    }

    [System.Serializable]
    public class ReactionStage
    {
        public string reactionName;
        public List<SlotRequirement> requirements; // 反应物 (Reactants)
        public List<ReactionOutcome> outcomes;     // 生成物 (Products)
        public string reactionCondition;           // 反应条件 (Condition)
        public UnityEvent onReactionPhenomenon = new UnityEvent();    // 反应现象 (Phenomenon)
    }

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
                
                if (reactant.type == "单质" && reactant.elements.Length > 1)
                {
                    // This is a molecular compound like Cl2
                    requirement.requiredName = reactant.name;
                }
                else
                {
                    // This is individual atoms like Na
                    requirement.requiredName = reactant.elements[0].element;
                }

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
            // 1. Gather all characters from input slots (considering coefficients)
            List<CharacterView> allReactantCharacters = new List<CharacterView>();
            List<CharacterGroup> groupsToDestroy = new List<CharacterGroup>();

            foreach (var requirement in stage.requirements)
            {
                object occupant = requirement.slot.GetOccupant();
                int coefficient = requirement.slot.Coefficient;

                if (occupant is CharacterView characterView)
                {
                    // For individual characters, add multiple copies based on coefficient
                    for (int i = 0; i < coefficient; i++)
                    {
                        allReactantCharacters.Add(characterView);
                    }
                }
                else if (occupant is CharacterGroup characterGroup)
                {
                    // For groups, add all characters from the group, multiplied by coefficient
                    for (int i = 0; i < coefficient; i++)
                    {
                        allReactantCharacters.AddRange(characterGroup.Characters);
                    }
                    groupsToDestroy.Add(characterGroup);
                }
                
                requirement.slot.Release();
            }

            // 2. Destroy old groups
            foreach (var group in groupsToDestroy)
            {
                group.ClearAndDestroy();
            }

            // 3. Create new groups based on products and their counts
            List<CharacterGroup> newlyCreatedGroups = new List<CharacterGroup>();
            List<CharacterView> allNewCharacters = new List<CharacterView>();

            foreach (var outcome in stage.outcomes)
            {
                if (outcome.outputSlot == null || string.IsNullOrEmpty(outcome.newGroupName)) continue;

                // Create multiple products if required
                for (int productIndex = 0; productIndex < outcome.productCount; productIndex++)
                {
                    CharacterSlot targetSlot = outcome.outputSlot;
                    Vector3 position = targetSlot.transform.position;

                    if (outcome.productCount > 1)
                    {
                        // Offset position for multiple products
                        position += Vector3.right * productIndex * 1.0f;
                    }

                    CharacterGroup newGroup = characterSpawnController.CreateCharacterGroup(
                        outcome.newGroupName, position);
                    if (newGroup == null) continue;

                    List<CharacterView> charactersForNewGroup = new List<CharacterView>();
                    foreach (var charName in outcome.characterNamesInGroup)
                    {
                        CharacterView charView = allReactantCharacters.FirstOrDefault(c =>
                            c.GetModel().GetCharacterName() == charName);
                        if (charView != null)
                        {
                            charactersForNewGroup.Add(charView);
                            allReactantCharacters.Remove(charView);
                        }
                    }

                    foreach (var member in charactersForNewGroup)
                    {
                        characterSpawnController.AddCharacterToGroup(newGroup, member);
                    }

                    // Only occupy the slot with the first product, others are positioned nearby
                    if (productIndex == 0)
                    {
                        targetSlot.Occupy(newGroup);
                        // Set the coefficient for the product slot
                        targetSlot.SetCoefficient(outcome.productCount);
                    }

                    SpeechController.Instance?.TriggerSpeech(SpeechTriggerType.ReactionSuccess, charactersForNewGroup);
                }
            }
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