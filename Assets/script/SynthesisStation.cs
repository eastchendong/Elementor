using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Elementor.Core;
using Elementor.Core.Speech;    

namespace Elementor
{
    [System.Serializable]
    public class SynthesisRecipe
    {
        public string resultingGroupName;
        public List<string> requiredCharacterNames;
    }

    [RequireComponent(typeof(Collider))]
    public class SynthesisStation : MonoBehaviour
    {
        [SerializeField] private List<SynthesisRecipe> recipes; // Keep for fallback
        private List<CharacterView> charactersOnStation = new List<CharacterView>();
        private bool isCheckingSynthesis = false;

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;

            // Subscribe to API synthesis response
            if (API.Instance != null)
            {
                API.Instance.OnSynthesisCheckComplete += OnSynthesisCheckComplete;
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from API events
            if (API.Instance != null)
            {
                API.Instance.OnSynthesisCheckComplete -= OnSynthesisCheckComplete;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            CharacterView characterView = other.GetComponentInParent<CharacterView>();
            if (characterView != null && !charactersOnStation.Contains(characterView))
            {
                charactersOnStation.Add(characterView);
                Debug.Log($"{characterView.GetModel().GetCharacterName()} entered the synthesis station.");
            }
        }

        private void OnTriggerExit(Collider other)
        {
            CharacterView characterView = other.GetComponentInParent<CharacterView>();
            if (characterView != null && charactersOnStation.Contains(characterView))
            {
                charactersOnStation.Remove(characterView);
                Debug.Log($"{characterView.GetModel().GetCharacterName()} left the synthesis station.");
            }
        }


        [ContextMenu("Try Synthesize Group")]
        public void TrySynthesizeGroup()
        {
            if (isCheckingSynthesis)
            {
                Debug.Log("Already checking synthesis possibility...");
                return;
            }

            if (charactersOnStation.Count < 2)
            {
                Debug.Log("Need at least 2 characters for synthesis.");
                SpeechController.Instance?.TriggerSpeech(SpeechTriggerType.SynthesisFailure, charactersOnStation);
                return;
            }

            // Get element names from characters
            List<string> elementNames = charactersOnStation
                .Select(c => c.GetModel().GetCharacterName())
                .ToList();

            Debug.Log($"Checking synthesis for elements: {string.Join(", ", elementNames)}");

            // Use API to check synthesis possibility
            if (API.Instance != null)
            {
                isCheckingSynthesis = true;
                API.Instance.CheckSynthesisPossibility(elementNames);
            }
            else
            {
                Debug.LogError("API Instance not found, falling back to predefined recipes.");
                TrySynthesizeWithRecipes();
            }
        }

        private void OnSynthesisCheckComplete(SynthesisResponse response)
        {
            isCheckingSynthesis = false;

            if (response.can_synthesize)
            {
                Debug.Log($"Synthesis successful: {response.compound_formula} ({response.compound_name})");
                Debug.Log($"Explanation: {response.explanation}");

                // Store the current characters before clearing them
                List<CharacterView> successfulCharacters = new List<CharacterView>(charactersOnStation);
                SynthesizeWithAPIResult(response);
                SpeechController.Instance?.TriggerSpeech(SpeechTriggerType.SynthesisSuccess, successfulCharacters);
            }
            else
            {
                Debug.Log($"Synthesis failed: {response.explanation}");
                // Pass the current characters on station for failure speech
                SpeechController.Instance?.TriggerSpeech(SpeechTriggerType.SynthesisFailure, new List<CharacterView>(charactersOnStation));
            }
        }

        private void SynthesizeWithAPIResult(SynthesisResponse response)
        {
            if (CharacterSpawnController.Instance == null)
            {
                Debug.LogError("Cannot synthesize, CharacterSpawnController instance is missing.");
                return;
            }

            // Use the compound formula as the group name
            string resultingGroupName = response.compound_formula;

            List<CharacterView> members = new List<CharacterView>(charactersOnStation);
            charactersOnStation.Clear();

            CharacterGroup group = CharacterSpawnController.Instance.CreateCharacterGroup(
                resultingGroupName,
                transform.position,
                transform.parent
            );

            if (group == null) return;

            foreach (var member in members)
            {
                CharacterSpawnController.Instance.AddCharacterToGroup(group, member);
            }

            // Update the group name with the synthesis result
            group.UpdateNameFromSynthesis(response);
            group.SetState(CharacterAnimationState.Falling);
        }

        // Fallback method using predefined recipes
        private void TrySynthesizeWithRecipes()
        {
            foreach (var recipe in recipes)
            {
                if (CanSynthesize(recipe))
                {
                    Synthesize(recipe);
                    return;
                }
            }
            Debug.Log("No valid group combination found on the station.");
            SpeechController.Instance?.TriggerSpeech(SpeechTriggerType.SynthesisFailure, charactersOnStation);
        }

        private bool CanSynthesize(SynthesisRecipe recipe)
        {
            var stationCharacterNames = charactersOnStation.Select(c => c.GetModel().GetCharacterName()).ToList();
            var requiredNames = new List<string>(recipe.requiredCharacterNames);

            if (stationCharacterNames.Count < requiredNames.Count) return false;

            foreach (var name in requiredNames)
            {
                if (stationCharacterNames.Contains(name))
                {
                    stationCharacterNames.Remove(name);
                }
                else
                {
                    return false;
                }
            }
            return true;
        }

        private void Synthesize(SynthesisRecipe recipe)
        {
            if (CharacterSpawnController.Instance == null)
            {
                Debug.LogError("Cannot synthesize, CharacterSpawnController instance is missing.");
                return;
            }
            Debug.Log($"Recipe for {recipe.resultingGroupName} matched. Synthesizing...");

            List<CharacterView> members = new List<CharacterView>();
            List<string> namesToFind = new List<string>(recipe.requiredCharacterNames);

            List<CharacterView> stationCharactersCopy = new List<CharacterView>(charactersOnStation);

            foreach (var character in stationCharactersCopy)
            {
                string characterName = character.GetModel().GetCharacterName();
                if (namesToFind.Contains(characterName))
                {
                    members.Add(character);
                    namesToFind.Remove(characterName);
                    charactersOnStation.Remove(character);
                }
            }

            CharacterGroup group = CharacterSpawnController.Instance.CreateCharacterGroup(recipe.resultingGroupName, transform.position, transform.parent);
            if (group == null) return;

            foreach (var member in members)
            {
                CharacterSpawnController.Instance.AddCharacterToGroup(group, member);
            }

            group.SetState(CharacterAnimationState.Falling);
            SpeechController.Instance.TriggerSpeech(SpeechTriggerType.SynthesisSuccess, members);
        }
    }
}
