using UnityEngine;
using System.Collections.Generic;
using Elementor.Core;
using Elementor.Lore;

namespace Elementor.Lore
{
    public class LoreSpawnerHighlighter : MonoBehaviour
    {
        [Header("Spawner Management")]
        [SerializeField] private List<CharacterSpawner> allSpawners = new List<CharacterSpawner>();
        
        private LoreController loreController;
        private List<CharacterSpawner> currentHighlightedSpawners = new List<CharacterSpawner>();
        
        private void Start()
        {
            loreController = LoreController.Instance;
            
            if (loreController == null)
            {
                Debug.LogError("LoreController instance not found!");
                return;
            }

            
            // Subscribe to lore events
            loreController.OnLoreLoaded += OnLoreLoaded;
            
            Debug.Log($"🔆 LoreSpawnerHighlighter initialized with {allSpawners.Count} spawners");
            
            // Check if lore is already loaded
            if (loreController.CurrentLore != null)
            {
                OnLoreLoaded();
            }
        }
        
        private void OnDestroy()
        {
            if (loreController != null)
            {
                loreController.OnLoreLoaded -= OnLoreLoaded;
            }
            
            // Clear any remaining highlights
            ClearAllHighlights();
        }
        
        
        private void OnLoreLoaded()
        {
            Debug.Log("LoreSpawnerHighlighter: Lore loaded, analyzing required elements...");
            
            if (loreController.CurrentLore == null)
            {
                Debug.LogWarning("OnLoreLoaded called but CurrentLore is null");
                return;
            }
            
            // Clear previous highlights
            ClearAllHighlights();
            
            // Get required elements from lore
            HashSet<string> requiredElements = GetRequiredElementsFromLore();
            
            if (requiredElements.Count == 0)
            {
                Debug.LogWarning("📭 No required elements found in current lore");
                return;
            }
            
            Debug.Log($"Required elements: {string.Join(", ", requiredElements)}");
            
            // Highlight matching spawners
            HighlightMatchingSpawners(requiredElements);
        }
        
        private HashSet<string> GetRequiredElementsFromLore()
        {
            HashSet<string> requiredElements = new HashSet<string>();
            
            var reaction = loreController.GetReaction();
            if (reaction == null)
            {
                Debug.LogWarning("No reaction data found in lore");
                return requiredElements;
            }
            
            // Extract elements from reactants
            if (reaction.reactants != null)
            {
                foreach (var reactant in reaction.reactants)
                {
                    if (reactant.elements != null)
                    {
                        foreach (var element in reactant.elements)
                        {
                            requiredElements.Add(element.element);
                            Debug.Log($"🧪 Added required element: {element.element}");
                        }
                    }
                    
                    // Also add the compound name itself as it might match spawner names
                    if (!string.IsNullOrEmpty(reactant.name))
                    {
                        requiredElements.Add(reactant.name);
                        Debug.Log($"🧪 Added required compound: {reactant.name}");
                    }
                }
            }
            
            // Extract elements from gameplay trigger required ions
            var gameplayTrigger = loreController.CurrentLore.gameplay_trigger;
            if (gameplayTrigger?.required_ions != null)
            {
                foreach (var requiredIon in gameplayTrigger.required_ions)
                {
                    if (!string.IsNullOrEmpty(requiredIon.name))
                    {
                        requiredElements.Add(requiredIon.name);
                        Debug.Log($"🎯 Added required ion: {requiredIon.name}");
                    }
                    
                    if (requiredIon.elements != null)
                    {
                        foreach (var element in requiredIon.elements)
                        {
                            requiredElements.Add(element.element);
                            Debug.Log($"🎯 Added ion element: {element.element}");
                        }
                    }
                }
            }
            
            return requiredElements;
        }
        
        private void HighlightMatchingSpawners(HashSet<string> requiredElements)
        {
            int highlightedCount = 0;
            
            foreach (var spawner in allSpawners)
            {
                if (spawner == null) continue;
                
                string spawnerCharacter = spawner.CharacterToSpawn;
                if (string.IsNullOrEmpty(spawnerCharacter)) continue;
                
                // Check if spawner's character matches any required element
                bool shouldHighlight = false;
                
                // Direct match
                if (requiredElements.Contains(spawnerCharacter))
                {
                    shouldHighlight = true;
                    Debug.Log($"🎯 Direct match: spawner '{spawnerCharacter}' matches required element");
                }
                
                // Check if spawner character is part of a compound
                // For example: spawner "O" should match when "O2" is required
                if (!shouldHighlight)
                {
                    foreach (string requiredElement in requiredElements)
                    {
                        // Check if the required element contains the spawner character
                        // This handles cases like "O2" containing "O"
                        if (requiredElement.Contains(spawnerCharacter) && spawnerCharacter.Length > 0)
                        {
                            shouldHighlight = true;
                            Debug.Log($"🔍 Compound match: spawner '{spawnerCharacter}' found in required element '{requiredElement}'");
                            break;
                        }
                        
                        // Also check the reverse - if spawner is a compound containing required element
                        if (spawnerCharacter.Contains(requiredElement) && requiredElement.Length > 0)
                        {
                            shouldHighlight = true;
                            Debug.Log($"🔍 Reverse match: required element '{requiredElement}' found in spawner '{spawnerCharacter}'");
                            break;
                        }
                    }
                }
                
                if (shouldHighlight)
                {
                    HighlightSpawner(spawner);
                    currentHighlightedSpawners.Add(spawner);
                    highlightedCount++;
                    
                    Debug.Log($"✨ Highlighted spawner for '{spawnerCharacter}' at {spawner.transform.position}");
                }
            }
            
            Debug.Log($"Highlighted {highlightedCount} spawners for current lore");
            
            // Debug: Show what we're looking for vs what we found
            Debug.Log($"📋 Required elements: {string.Join(", ", requiredElements)}");
            Debug.Log($"🎯 Available spawners: {string.Join(", ", allSpawners.ConvertAll(s => s?.CharacterToSpawn ?? "null"))}");
        }
        
        private void HighlightSpawner(CharacterSpawner spawner)
        {
            // Get the target slot from the spawner and use its highlighting method
            CharacterSlot targetSlot = spawner.GetTargetSlot();
            if (targetSlot != null)
            {
                targetSlot.StartShining();
                Debug.Log($"Started shining for spawner slot: {targetSlot.name}");
            }
            else
            {
                Debug.LogWarning($"Spawner {spawner.name} has no target slot to highlight");
            }
        }
        
        private void ClearAllHighlights()
        {
            foreach (var spawner in currentHighlightedSpawners)
            {
                if (spawner != null)
                {
                    ClearSpawnerHighlight(spawner);
                }
            }
            
            currentHighlightedSpawners.Clear();
            Debug.Log("🔄 Cleared all spawner highlights");
        }
        
        private void ClearSpawnerHighlight(CharacterSpawner spawner)
        {
            CharacterSlot targetSlot = spawner.GetTargetSlot();
            if (targetSlot != null)
            {
                targetSlot.StopShining();
                Debug.Log($"Stopped shining for spawner slot: {targetSlot.name}");
            }
        }
        
        /// <summary>
        /// Manually add a spawner to the managed list
        /// </summary>
        public void AddSpawner(CharacterSpawner spawner)
        {
            if (spawner != null && !allSpawners.Contains(spawner))
            {
                allSpawners.Add(spawner);
                Debug.Log($"Added spawner {spawner.name} to highlighter");
                
                // If lore is currently loaded, check if this spawner should be highlighted
                if (loreController?.CurrentLore != null)
                {
                    HashSet<string> requiredElements = GetRequiredElementsFromLore();
                    if (requiredElements.Contains(spawner.CharacterToSpawn))
                    {
                        HighlightSpawner(spawner);
                        currentHighlightedSpawners.Add(spawner);
                    }
                }
            }
        }
        
        /// <summary>
        /// Manually remove a spawner from the managed list
        /// </summary>
        public void RemoveSpawner(CharacterSpawner spawner)
        {
            if (spawner != null && allSpawners.Contains(spawner))
            {
                // Clear highlight if it's currently highlighted
                if (currentHighlightedSpawners.Contains(spawner))
                {
                    ClearSpawnerHighlight(spawner);
                    currentHighlightedSpawners.Remove(spawner);
                }
                
                allSpawners.Remove(spawner);
                Debug.Log($"Removed spawner {spawner.name} from highlighter");
            }
        }
        
        /// <summary>
        /// Force refresh spawner highlighting based on current lore
        /// </summary>
        public void RefreshHighlighting()
        {
            if (loreController?.CurrentLore != null)
            {
                OnLoreLoaded();
            }
            else
            {
                ClearAllHighlights();
            }
        }
    }
}
