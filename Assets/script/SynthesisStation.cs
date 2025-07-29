using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
        [SerializeField] private CharacterSpawnController characterSpawnController;
        [SerializeField] private List<SynthesisRecipe> recipes;
        private List<CharacterView> charactersOnStation = new List<CharacterView>();

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
            if (characterSpawnController == null)
            {
                characterSpawnController = FindObjectOfType<CharacterSpawnController>();
                if (characterSpawnController == null)
                {
                    Debug.LogError("SynthesisStation requires a CharacterSpawnController.");
                }
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
            foreach (var recipe in recipes)
            {
                if (CanSynthesize(recipe))
                {
                    Synthesize(recipe);
                    return; // Synthesize one group at a time
                }
            }
            Debug.Log("No valid group combination found on the station.");
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
            if (characterSpawnController == null)
            {
                Debug.LogError("Cannot synthesize, CharacterSpawnController is missing.");
                return;
            }
            Debug.Log($"Recipe for {recipe.resultingGroupName} matched. Synthesizing...");

            List<CharacterView> members = new List<CharacterView>();
            List<string> namesToFind = new List<string>(recipe.requiredCharacterNames);

            // Create a copy to iterate over while removing from the original list
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

            // Create the group using the controller
            CharacterGroup group = characterSpawnController.CreateCharacterGroup(recipe.resultingGroupName, transform.position, transform.parent);
            if (group == null) return;

            // Add members to the group using the controller
            foreach (var member in members)
            {
                characterSpawnController.AddCharacterToGroup(group, member);
            }
        }
    }
}
