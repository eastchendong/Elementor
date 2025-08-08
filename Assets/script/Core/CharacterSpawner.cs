using UnityEngine;
using System.Collections;

namespace Elementor.Core
{
    public class CharacterSpawner : MonoBehaviour
    {
        [Header("Slot Configuration")]
        [SerializeField] private CharacterSlot targetSlot; // The slot to monitor and spawn into
        
        [Header("Spawn Configuration")]
        [SerializeField] private string characterToSpawn; // Name of the character to spawn
        [SerializeField] private bool autoSpawnEnabled = true; // Whether auto-spawning is enabled
        [SerializeField] private float spawnDelay = 0.5f; // Delay before spawning after slot becomes empty
        [SerializeField] private bool spawnOnStart = false; // Whether to spawn immediately on start
        
        [Header("Spawn Limits")]
        [SerializeField] private bool hasSpawnLimit = false; // Whether there's a limit to spawns
        [SerializeField] private int maxSpawns = 10; // Maximum number of spawns (-1 for unlimited)
        private int currentSpawnCount = 0;
        
        private bool wasSlotOccupied = false;
        private Coroutine spawnCoroutine;
        
        public bool AutoSpawnEnabled 
        { 
            get => autoSpawnEnabled; 
            set => autoSpawnEnabled = value; 
        }
        
        public string CharacterToSpawn 
        { 
            get => characterToSpawn; 
            set => characterToSpawn = value; 
        }
        
        public int CurrentSpawnCount => currentSpawnCount;
        
        private void Start()
        {
            if (targetSlot == null)
            {
                Debug.LogError($"CharacterSpawner on {gameObject.name} has no target slot assigned!");
                return;
            }
            
            if (string.IsNullOrEmpty(characterToSpawn))
            {
                Debug.LogWarning($"CharacterSpawner on {gameObject.name} has no character specified to spawn!");
                return;
            }
            
            // Initialize the occupied state
            wasSlotOccupied = targetSlot.IsOccupied;
            
            // Spawn immediately if requested and slot is empty
            if (spawnOnStart && !targetSlot.IsOccupied && CanSpawn())
            {
                SpawnCharacter();
            }
        }
        
        private void Update()
        {
            if (!autoSpawnEnabled || targetSlot == null) return;
            
            // Check if slot state changed from occupied to empty
            bool isCurrentlyOccupied = targetSlot.IsOccupied;
            
            if (wasSlotOccupied && !isCurrentlyOccupied)
            {
                // Slot just became empty
                OnSlotBecameEmpty();
            }
            
            wasSlotOccupied = isCurrentlyOccupied;
        }
        
        private void OnSlotBecameEmpty()
        {
            if (!CanSpawn()) return;
            
            Debug.Log($"Slot {targetSlot.name} became empty. Scheduling spawn of {characterToSpawn}");
            
            // Cancel any existing spawn coroutine
            if (spawnCoroutine != null)
            {
                StopCoroutine(spawnCoroutine);
            }
            
            // Start spawn with delay
            spawnCoroutine = StartCoroutine(SpawnWithDelay());
        }
        
        private IEnumerator SpawnWithDelay()
        {
            yield return new WaitForSeconds(spawnDelay);
            
            // Double-check that slot is still empty and we can still spawn
            if (!targetSlot.IsOccupied && CanSpawn())
            {
                SpawnCharacter();
            }
            
            spawnCoroutine = null;
        }
        
        public void SpawnCharacter()
        {
            if (targetSlot == null || targetSlot.IsOccupied || !CanSpawn())
            {
                Debug.LogWarning($"Cannot spawn character: slot occupied={targetSlot?.IsOccupied}, can spawn={CanSpawn()}");
                return;
            }
            
            if (CharacterSpawnController.Instance == null)
            {
                Debug.LogError("CharacterSpawnController instance not found!");
                return;
            }
            
            if (string.IsNullOrEmpty(characterToSpawn))
            {
                Debug.LogError("No character specified to spawn!");
                return;
            }
            
            // Spawn the character at the slot's position
            Vector3 spawnPosition = targetSlot.transform.position;
            CharacterSpawnController.Instance.SpawnCharacter(characterToSpawn, spawnPosition);
            
            // Find the spawned character and try to occupy the slot
            StartCoroutine(OccupySlotAfterSpawn());
            
            currentSpawnCount++;
            Debug.Log($"Spawned {characterToSpawn} (spawn count: {currentSpawnCount})");
        }
        
        private IEnumerator OccupySlotAfterSpawn()
        {
            // Wait a frame for the character to be fully spawned
            yield return null;
            
            // Try to find the newly spawned character near the slot
            var spawnedCharacters = CharacterSpawnController.Instance.GetSpawnedCharacters();
            CharacterView closestCharacter = null;
            float closestDistance = float.MaxValue;
            
            foreach (var character in spawnedCharacters)
            {
                if (character != null)
                {
                    float distance = Vector3.Distance(character.transform.position, targetSlot.transform.position);
                    if (distance < closestDistance && distance < 2f) // Within 2 units
                    {
                        closestDistance = distance;
                        closestCharacter = character;
                    }
                }
            }
            
            // Try to occupy the slot with the closest character
            if (closestCharacter != null && !targetSlot.IsOccupied)
            {
                targetSlot.Occupy(closestCharacter);
                Debug.Log($"Automatically occupied slot {targetSlot.name} with spawned character {closestCharacter.name}");
            }
        }
        
        private bool CanSpawn()
        {
            if (!autoSpawnEnabled) return false;
            if (hasSpawnLimit && currentSpawnCount >= maxSpawns) return false;
            return true;
        }
        
        public void ResetSpawnCount()
        {
            currentSpawnCount = 0;
            Debug.Log($"Spawn count reset for {gameObject.name}");
        }
        
        public void ForceSpawn()
        {
            if (targetSlot.IsOccupied)
            {
                Debug.LogWarning("Cannot force spawn: slot is occupied");
                return;
            }
            
            SpawnCharacter();
        }
        
        // Editor helper methods
        public void SetTargetSlot(CharacterSlot slot)
        {
            targetSlot = slot;
        }
        
        public CharacterSlot GetTargetSlot()
        {
            return targetSlot;
        }
        
        private void OnValidate()
        {
            // Ensure spawn delay is not negative
            if (spawnDelay < 0)
                spawnDelay = 0;
                
            // Ensure max spawns is positive if limit is enabled
            if (hasSpawnLimit && maxSpawns < 1)
                maxSpawns = 1;
        }
    }
}
