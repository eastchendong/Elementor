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
        public string requiredCharacterName; // For single character
        public string requiredGroupName;     // For a character group
    }

    [System.Serializable]
    public class ReactionOutcome
    {
        public CharacterSlot outputSlot;
        public string newGroupName;
        public List<string> characterNamesInGroup;
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

                // Trigger speech for reaction success
                var participantCharacters = GetParticipantCharacters(currentReaction);
                SpeechController.Instance?.TriggerSpeech(SpeechTriggerType.ReactionSuccess, participantCharacters);

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
                
                // Trigger speech for reaction failure
                var participantCharacters = GetParticipantCharacters(currentReaction);
                SpeechController.Instance?.TriggerSpeech(SpeechTriggerType.ReactionFailure, participantCharacters);
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

                object occupant = requirement.slot.GetOccupant();
                
                // Debug log current occupant
                string occupantInfo = "empty";
                if (occupant is CharacterView characterView)
                {
                    occupantInfo = $"Character: {characterView.GetModel().GetCharacterName()}";
                }
                else if (occupant is CharacterGroup characterGroup)
                {
                    occupantInfo = $"Group: {characterGroup.name}";
                }
                
                // Debug log requirement
                string requirementInfo = "";
                if (!string.IsNullOrEmpty(requirement.requiredCharacterName))
                {
                    requirementInfo = $"Required Character: {requirement.requiredCharacterName}";
                }
                else if (!string.IsNullOrEmpty(requirement.requiredGroupName))
                {
                    requirementInfo = $"Required Group: {requirement.requiredGroupName}";
                }
                
                Debug.Log($"Slot occupant: {occupantInfo} | {requirementInfo}");
                
                if (occupant == null) return false; // Slot is empty, requirement not met.

                bool requirementMet = false;
                if (occupant is CharacterView charView && !string.IsNullOrEmpty(requirement.requiredCharacterName))
                {
                    if (charView.GetModel().GetCharacterName() == requirement.requiredCharacterName)
                    {
                        requirementMet = true;
                    }
                }
                else if (occupant is CharacterGroup charGroup && !string.IsNullOrEmpty(requirement.requiredGroupName))
                {
                    if (charGroup.name == requirement.requiredGroupName)
                    {
                        requirementMet = true;
                    }
                }

                Debug.Log($"Requirement met: {requirementMet}");

                if (!requirementMet)
                {
                    return false; // One requirement is not met, so the stage is not complete.
                }
            }

            return true; // All requirements are met.
        }

        private void ProcessReaction(ReactionStage stage)
        {
            // 1. Gather all characters from input slots
            List<CharacterView> allReactantCharacters = new List<CharacterView>();
            List<CharacterGroup> groupsToDestroy = new List<CharacterGroup>();

            foreach (var requirement in stage.requirements)
            {
                object occupant = requirement.slot.GetOccupant();
                if (occupant is CharacterView character)
                {
                    allReactantCharacters.Add(character);
                }
                else if (occupant is CharacterGroup group)
                {
                    allReactantCharacters.AddRange(group.Characters);
                    groupsToDestroy.Add(group);
                }
                requirement.slot.Release();
            }

            // 2. Destroy old groups
            foreach (var group in groupsToDestroy)
            {
                group.ClearAndDestroy();
            }

            // 3. Create new groups and distribute characters
            foreach (var outcome in stage.outcomes)
            {
                if (outcome.outputSlot == null || string.IsNullOrEmpty(outcome.newGroupName)) continue;

                CharacterGroup newGroup = characterSpawnController.CreateCharacterGroup(outcome.newGroupName, outcome.outputSlot.transform.position);
                if (newGroup == null) continue;
                
                List<CharacterView> charactersForNewGroup = new List<CharacterView>();
                foreach (var charName in outcome.characterNamesInGroup)
                {
                    CharacterView charView = allReactantCharacters.FirstOrDefault(c => c.GetModel().GetCharacterName() == charName);
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

                outcome.outputSlot.Occupy(newGroup);
                
                SpeechController.Instance?.TriggerSpeech(SpeechTriggerType.ReactionSuccess, charactersForNewGroup);
            }

            // Handle any remaining single characters if needed
            // ...
        }
    }
}
