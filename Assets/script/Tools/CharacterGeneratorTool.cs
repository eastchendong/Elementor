using UnityEngine;
using System.Collections.Generic;
using Elementor.Core;

namespace Elementor
{
    public class CharacterGeneratorTool : MonoBehaviour
    {
        [Header("Character Generation Settings")]
        [SerializeField] private List<string> characterNames = new List<string>();
        [SerializeField] private List<Transform> spawnTransforms = new List<Transform>();
        
        [Header("Character Properties")]
        [SerializeField] private string defaultCharacterType = "NPC";
        [SerializeField] private string defaultGroupId = "";
        
        [Header("Offset Settings")]
        [SerializeField] private Vector3 positionOffset = Vector3.zero;
        [SerializeField] private Vector3 rotationOffset = Vector3.zero;
        [SerializeField] private bool useRandomOffset = false;
        [SerializeField] private Vector3 randomOffsetRange = new Vector3(1f, 0f, 1f);
        
        [Header("Generation Options")]
        [SerializeField] private bool generateAtAllTransforms = false;
        [SerializeField] private int selectedTransformIndex = 0;
        
        private List<CharacterView> generatedCharacters = new List<CharacterView>();

        public void GenerateCharacter(string characterName)
        {
            if (CharacterSpawnController.Instance == null)
            {
                Debug.LogError("CharacterSpawnController instance not found!");
                return;
            }

            if (spawnTransforms.Count == 0)
            {
                Debug.LogError("No spawn transforms assigned!");
                return;
            }

            if (generateAtAllTransforms)
            {
                GenerateAtAllTransforms(characterName);
            }
            else
            {
                GenerateAtSelectedTransform(characterName);
            }
        }

        public void GenerateAllCharacters()
        {
            foreach (string characterName in characterNames)
            {
                if (!string.IsNullOrEmpty(characterName))
                {
                    GenerateCharacter(characterName);
                }
            }
        }

        public void ClearGeneratedCharacters()
        {
            foreach (CharacterView character in generatedCharacters)
            {
                if (character != null)
                {
                    DestroyImmediate(character.gameObject);
                }
            }
            generatedCharacters.Clear();
            Debug.Log("Cleared all generated characters");
        }

        private void GenerateAtAllTransforms(string characterName)
        {
            foreach (Transform spawnTransform in spawnTransforms)
            {
                CreateCharacterAtTransform(characterName, spawnTransform);
            }
        }

        private void GenerateAtSelectedTransform(string characterName)
        {
            if (selectedTransformIndex >= 0 && selectedTransformIndex < spawnTransforms.Count)
            {
                CreateCharacterAtTransform(characterName, spawnTransforms[selectedTransformIndex]);
            }
            else
            {
                Debug.LogError("Selected transform index is out of range!");
            }
        }

        private void CreateCharacterAtTransform(string characterName, Transform spawnTransform)
        {
            // Create prefab path using character name
            string characterPrefabPath = $"Characters/{characterName}";
            
            // Create character data using the proper constructor
            Character character = new Character(
                defaultCharacterType,  // type (required first parameter)
                characterName,         // name (required second parameter)
                characterPrefabPath,   // prefabPath (using character name)
                defaultGroupId         // groupId (optional fourth parameter)
            );

            // Calculate spawn position with offset
            Vector3 spawnPosition = CalculateSpawnPosition(spawnTransform);

            // Spawn the character
            CharacterSpawnController.Instance.SpawnCharacter(character, spawnPosition, spawnTransform.parent);

            Debug.Log($"Generated character '{characterName}' at {spawnPosition}");
        }

        private Vector3 CalculateSpawnPosition(Transform spawnTransform)
        {
            Vector3 finalPosition = spawnTransform.position + positionOffset;

            if (useRandomOffset)
            {
                Vector3 randomOffset = new Vector3(
                    Random.Range(-randomOffsetRange.x, randomOffsetRange.x),
                    Random.Range(-randomOffsetRange.y, randomOffsetRange.y),
                    Random.Range(-randomOffsetRange.z, randomOffsetRange.z)
                );
                finalPosition += randomOffset;
            }

            return finalPosition;
        }

        public void AddCharacterName(string name)
        {
            if (!string.IsNullOrEmpty(name) && !characterNames.Contains(name))
            {
                characterNames.Add(name);
            }
        }

        public void RemoveCharacterName(int index)
        {
            if (index >= 0 && index < characterNames.Count)
            {
                characterNames.RemoveAt(index);
            }
        }

        public void AddSpawnTransform(Transform transform)
        {
            if (transform != null && !spawnTransforms.Contains(transform))
            {
                spawnTransforms.Add(transform);
            }
        }

        public void RemoveSpawnTransform(int index)
        {
            if (index >= 0 && index < spawnTransforms.Count)
            {
                spawnTransforms.RemoveAt(index);
            }
        }

        private void OnDrawGizmos()
        {
            if (spawnTransforms == null) return;

            Gizmos.color = Color.yellow;
            foreach (Transform spawnTransform in spawnTransforms)
            {
                if (spawnTransform != null)
                {
                    Vector3 spawnPosition = CalculateSpawnPosition(spawnTransform);
                    Gizmos.DrawWireSphere(spawnPosition, 0.5f);
                    Gizmos.DrawRay(spawnPosition, Vector3.up);
                }
            }
        }
    }
}
