using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Elementor.Core;

namespace Elementor
{
    public class CharacterGeneratorTool : MonoBehaviour
    {
        [Header("Character Generation Settings")]
        [SerializeField] private List<string> characterNames = new List<string>();
        [SerializeField] private List<Transform> spawnTransforms = new List<Transform>();
        
        [Header("Group Generation Settings")]
        [SerializeField] private string groupInputText = "";
        [SerializeField] private List<string> parsedGroups = new List<string>();
        
        [Header("Offset Settings")]
        [SerializeField] private Vector3 positionOffset = Vector3.zero;
        [SerializeField] private Vector3 rotationOffset = Vector3.zero;
        [SerializeField] private bool useRandomOffset = false;
        [SerializeField] private Vector3 randomOffsetRange = new Vector3(1f, 0f, 1f);
        
        [Header("Generation Options")]
        [SerializeField] private bool generateAtAllTransforms = false;
        [SerializeField] private int selectedTransformIndex = 0;
        
        private List<CharacterView> generatedCharacters = new List<CharacterView>();
        private List<CharacterGroup> generatedGroups = new List<CharacterGroup>();

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

        public void ParseAndGenerateGroups()
        {
            if (string.IsNullOrEmpty(groupInputText))
            {
                Debug.LogWarning("Group input text is empty!");
                return;
            }

            parsedGroups.Clear();
            string[] formulas = groupInputText.Split(new char[] { ',', ';', ' ', '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
            
            foreach (string formula in formulas)
            {
                string trimmedFormula = formula.Trim();
                if (!string.IsNullOrEmpty(trimmedFormula))
                {
                    parsedGroups.Add(trimmedFormula);
                    GenerateFromFormula(trimmedFormula);
                }
            }
        }

        public void GenerateFromFormula(string formula)
        {
            if (IsIndividualElement(formula))
            {
                // Generate individual character
                GenerateCharacter(formula);
            }
            else
            {
                // Generate character group
                GenerateCharacterGroup(formula);
            }
        }

        private bool IsIndividualElement(string formula)
        {
            // Check if formula contains numbers or multiple capital letters indicating a compound
            if (Regex.IsMatch(formula, @"\d"))
                return false; // Contains numbers, likely a compound
            
            if (Regex.Matches(formula, @"[A-Z]").Count > 1)
                return false; // Multiple capital letters, likely a compound
            
            // Single element pattern: Capital letter optionally followed by lowercase letters
            return Regex.IsMatch(formula, @"^[A-Z][a-z]*$");
        }

        private void GenerateCharacterGroup(string formula)
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

            // Parse the formula to extract constituent elements
            List<string> elementNames = ParseChemicalFormula(formula);
            if (elementNames.Count == 0)
            {
                Debug.LogWarning($"Could not parse formula: {formula}");
                return;
            }

            Transform spawnTransform = GetSelectedSpawnTransform();
            Vector3 spawnPosition = CalculateSpawnPosition(spawnTransform);

            // Create the character group
            CharacterGroup group = CharacterSpawnController.Instance.CreateCharacterGroup(
                formula, 
                spawnPosition, 
                spawnTransform.parent
            );

            if (group == null)
            {
                Debug.LogError($"Failed to create character group for {formula}");
                return;
            }

            // Add characters to the group
            foreach (string elementName in elementNames)
            {
                // Create individual character first
                CharacterSpawnController.Instance.SpawnCharacter(elementName, spawnPosition, spawnTransform.parent);
                
                // Get the last spawned character and add it to the group
                var spawnedCharacters = CharacterSpawnController.Instance.GetSpawnedCharacters();
                if (spawnedCharacters.Count > 0)
                {
                    CharacterView lastSpawned = spawnedCharacters[spawnedCharacters.Count - 1];
                    CharacterSpawnController.Instance.AddCharacterToGroup(group, lastSpawned);
                }
            }

            generatedGroups.Add(group);
            Debug.Log($"Generated character group '{formula}' with elements: {string.Join(", ", elementNames)}");
        }

        private List<string> ParseChemicalFormula(string formula)
        {
            List<string> elements = new List<string>();
            
            // Regex to match element symbols and their counts
            MatchCollection matches = Regex.Matches(formula, @"([A-Z][a-z]?)(\d*)");
            
            foreach (Match match in matches)
            {
                string element = match.Groups[1].Value;
                string countStr = match.Groups[2].Value;
                int count = string.IsNullOrEmpty(countStr) ? 1 : int.Parse(countStr);
                
                // Add the element 'count' number of times
                for (int i = 0; i < count; i++)
                {
                    elements.Add(element);
                }
            }
            
            return elements;
        }

        private Transform GetSelectedSpawnTransform()
        {
            if (generateAtAllTransforms)
            {
                // Return first transform for group generation
                return spawnTransforms[0];
            }
            else
            {
                if (selectedTransformIndex >= 0 && selectedTransformIndex < spawnTransforms.Count)
                {
                    return spawnTransforms[selectedTransformIndex];
                }
                else
                {
                    Debug.LogError("Selected transform index is out of range!");
                    return spawnTransforms[0];
                }
            }
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
            Vector3 spawnPosition = CalculateSpawnPosition(spawnTransform);
            
            // Use the simplified spawn method that automatically loads character data
            CharacterSpawnController.Instance.SpawnCharacter(characterName, spawnPosition, spawnTransform.parent);
            
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
            
            foreach (CharacterGroup group in generatedGroups)
            {
                if (group != null)
                {
                    group.ClearAndDestroy();
                }
            }
            generatedGroups.Clear();
            
            Debug.Log("Cleared all generated characters and groups");
        }

        public void SetGroupInputText(string text)
        {
            groupInputText = text;
        }

        public string GetGroupInputText()
        {
            return groupInputText;
        }

        public List<string> GetParsedGroups()
        {
            return new List<string>(parsedGroups);
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
