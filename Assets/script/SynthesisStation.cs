using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Elementor
{
    [RequireComponent(typeof(Collider))]
    public class SynthesisStation : MonoBehaviour
    {
        [SerializeField] private GameObject characterGroupPrefab;
        private List<CharacterView> charactersOnStation = new List<CharacterView>();

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
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

        public void TrySynthesizeGroup()
        {
            if (charactersOnStation.Count < 2)
            {
                Debug.Log("Not enough characters to form a group.");
                return;
            }

            // Find a common group ID among the characters on the station
            var potentialGroup = charactersOnStation
                .Where(c => !string.IsNullOrEmpty(c.GetModel().CharacterData.groupId))
                .GroupBy(c => c.GetModel().CharacterData.groupId)
                .FirstOrDefault(g => g.Count() > 1);

            if (potentialGroup != null)
            {
                string groupId = potentialGroup.Key;
                List<CharacterView> members = potentialGroup.ToList();

                Debug.Log($"Found group {groupId} with {members.Count} members. Synthesizing...");

                // Create the group
                GameObject groupObj = Instantiate(characterGroupPrefab, transform.position, transform.rotation);
                CharacterGroup group = groupObj.GetComponent<CharacterGroup>();
                groupObj.name = $"Group_{groupId}";

                // Add members to the group and remove them from the station
                foreach (var member in members)
                {
                    group.AddCharacter(member);
                    charactersOnStation.Remove(member);
                }
            }
            else
            {
                Debug.Log("No valid group combination found on the station.");
            }
        }
    }
}
