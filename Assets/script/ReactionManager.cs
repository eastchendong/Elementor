using System.Collections.Generic;
using UnityEngine;
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
        private CharacterSpawnController characterSpawnController;

        [Header("Predefined Output Slots")]
        [SerializeField] private List<CharacterSlot> predefinedOutputSlots = new List<CharacterSlot>();

        [Header("Component References")]
        [SerializeField] private ReactionEffectsManager effectsManager;
        [SerializeField] private ReactionProcessor reactionProcessor;

        [Header("Events")]
        public UnityEvent OnAllReactionsCompleted = new UnityEvent();

        // Static event for global subscription
        public static event Action OnGlobalReactionsCompleted;

        private void Start()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            if (characterSpawnController == null)
            {
                characterSpawnController = CharacterSpawnController.Instance;
                if (characterSpawnController == null)
                {
                    Debug.LogError("ReactionManager needs a reference to CharacterSpawnController.");
                }
            }

            if (effectsManager == null)
            {
                effectsManager = GetComponent<ReactionEffectsManager>();
                if (effectsManager == null)
                {
                    effectsManager = gameObject.AddComponent<ReactionEffectsManager>();
                }
            }

            if (reactionProcessor == null)
            {
                reactionProcessor = GetComponent<ReactionProcessor>();
                if (reactionProcessor == null)
                {
                    reactionProcessor = gameObject.AddComponent<ReactionProcessor>();
                }
            }

            reactionProcessor.Initialize(characterSpawnController, predefinedOutputSlots);
        }

        public void SetupStages(List<ReactionStage> stages)
        {
            reactionStages = stages;
            currentReactionIndex = 0;
            
            AssignPredefinedOutputSlots();
            effectsManager.ActivateReactionCondition(GetCurrentStage());
            
            Debug.Log($"ReactionManager setup with {stages.Count} stages.");
        }

        public void SetupStagesFromJSON(string jsonPath)
        {
            var stages = ReactionDataParser.ParseReactionFromJSON(jsonPath);
            SetupStages(stages);
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
                HandleReactionSuccess(currentReaction);
            }
            else
            {
                HandleReactionFailure(currentReaction);
            }
        }

        private void HandleReactionSuccess(ReactionStage currentReaction)
        {
            Debug.Log($"Reaction '{currentReaction.reactionName}' completed!");

            effectsManager.DeactivateConditionEffects();
            effectsManager.ActivateReactionPhenomenon(currentReaction);
            currentReaction.onReactionPhenomenon?.Invoke();

            reactionProcessor.ProcessReaction(currentReaction);

            currentReactionIndex++;

            if (currentReactionIndex < reactionStages.Count)
            {
                effectsManager.ActivateReactionCondition(GetCurrentStage());
            }

            if (currentReactionIndex >= reactionStages.Count)
            {
                Debug.Log("All reaction stages completed! Triggering completion event.");
                OnAllReactionsCompleted?.Invoke();
                OnGlobalReactionsCompleted?.Invoke();
            }
        }

        private void HandleReactionFailure(ReactionStage currentReaction)
        {
            Debug.Log($"Reaction '{currentReaction.reactionName}' requirements not met.");
            
            var participantCharacters = GetParticipantCharacters(currentReaction);
            SpeechController.Instance?.TriggerSpeech(SpeechTriggerType.ReactionFailure, participantCharacters);
        }

        private void AssignPredefinedOutputSlots()
        {
            int slotIndex = 0;
            int totalOutcomes = 0;
            
            // First count total outcomes needed
            foreach (var stage in reactionStages)
            {
                totalOutcomes += stage.outcomes.Count;
            }
            
            Debug.Log($"Total outcomes needed: {totalOutcomes}, Predefined slots available: {predefinedOutputSlots.Count}");
            
            if (totalOutcomes > predefinedOutputSlots.Count)
            {
                Debug.LogWarning($"Not enough predefined output slots! Need {totalOutcomes} but only have {predefinedOutputSlots.Count}. Additional slots will be generated as needed.");
            }
            else if (predefinedOutputSlots.Count > totalOutcomes)
            {
                Debug.Log($"More predefined slots ({predefinedOutputSlots.Count}) than needed ({totalOutcomes}). Only the first {totalOutcomes} slots will be used.");
            }
            
            // Assign predefined slots to outcomes
            foreach (var stage in reactionStages)
            {
                foreach (var outcome in stage.outcomes)
                {
                    if (slotIndex < predefinedOutputSlots.Count)
                    {
                        outcome.outputSlot = predefinedOutputSlots[slotIndex];
                        slotIndex++;
                        Debug.Log($"Assigned predefined slot {outcome.outputSlot.name} to outcome {outcome.newGroupName}");
                    }
                    else
                    {
                        // Will be handled during reaction processing - generate new slot only if needed
                        Debug.Log($"No predefined slot available for outcome {outcome.newGroupName}, will be assigned during reaction processing");
                    }
                }
            }
        }

        private ReactionStage GetCurrentStage()
        {
            return currentReactionIndex < reactionStages.Count ? reactionStages[currentReactionIndex] : null;
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