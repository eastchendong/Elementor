using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace Elementor
{
    public class CharacterSpawnController : MonoBehaviour
    {
        [SerializeField] private Transform spawnArea;
        [SerializeField] private string jsonFileName = "character_spawn_config.json";
        [SerializeField] private Vector2 spawnAreaSize = new Vector2(10f, 10f);
        [SerializeField] private GameObject fallbackPrefab; // 备用prefab，当找不到指定模型时使用
        
        private CharacterSpawnData spawnData;
        private List<CharacterView> spawnedCharacters = new List<CharacterView>();
        
        void Start()
        {
            LoadCharacterSpawnData();
            SpawnCharacters();
        }
        
        private void LoadCharacterSpawnData()
        {
            string filePath = Path.Combine(Application.streamingAssetsPath, jsonFileName);
            
            if (File.Exists(filePath))
            {
                string jsonContent = File.ReadAllText(filePath);
                spawnData = JsonUtility.FromJson<CharacterSpawnData>(jsonContent);
            }
            else
            {
                Debug.LogWarning($"JSON文件未找到: {filePath}，使用默认配置");
                CreateDefaultSpawnData();
            }
        }
        
        private void CreateDefaultSpawnData()
        {
            spawnData = new CharacterSpawnData
            {
                characters = new Character[]
                {
                    new Character("Fire", "Flame Warrior", "Characters/FireWarrior"),
                    new Character("Water", "Ice Mage", "Characters/IceMage"),
                    new Character("Earth", "Stone Guardian", "Characters/StoneGuardian")
                }
            };
        }
        
        private void SpawnCharacters()
        {
            if (spawnData?.characters == null) return;
            
            for (int i = 0; i < spawnData.characters.Length; i++)
            {
                Vector3 spawnPosition = GetRandomSpawnPosition();
                GameObject characterObj = CreateCharacterObject(spawnData.characters[i], spawnPosition);
                
                if (characterObj != null)
                {
                    // 先添加CharacterModel组件
                    CharacterModel characterModel = characterObj.GetComponent<CharacterModel>();
                    if (characterModel == null)
                        characterModel = characterObj.AddComponent<CharacterModel>();
                    
                    // 再添加CharacterView组件
                    CharacterView characterView = characterObj.GetComponent<CharacterView>();
                    if (characterView == null)
                        characterView = characterObj.AddComponent<CharacterView>();
                    
                    // 手动设置CharacterView的model引用
                    characterView.SetCharacterModel(characterModel);
                    
                    // 最后进行初始化
                    characterView.Initialize(spawnData.characters[i]);
                    spawnedCharacters.Add(characterView);
                    
                    // 订阅角色交互事件
                    characterView.OnCharacterSelected += OnCharacterSelected;
                    characterView.OnCharacterMoved += OnCharacterMoved;
                }
            }
        }
        
        private GameObject CreateCharacterObject(Character character, Vector3 position)
        {
            GameObject modelPrefab = null;
            
            // 尝试从Resources文件夹加载指定的prefab
            if (!string.IsNullOrEmpty(character.prefabPath))
            {
                modelPrefab = Resources.Load<GameObject>(character.prefabPath);
                
                if (modelPrefab == null)
                {
                    Debug.LogWarning($"找不到prefab: {character.prefabPath}，使用备用prefab");
                }
            }
            
            // 如果找不到指定prefab，使用备用prefab
            if (modelPrefab == null)
            {
                modelPrefab = fallbackPrefab;
            }
            
            if (modelPrefab == null)
            {
                Debug.LogError("没有可用的prefab来生成角色");
                return null;
            }
            
            return Instantiate(modelPrefab, position, Quaternion.identity, spawnArea);
        }
        
        private Vector3 GetRandomSpawnPosition()
        {
            float x = Random.Range(-spawnAreaSize.x / 2, spawnAreaSize.x / 2);
            float z = Random.Range(-spawnAreaSize.y / 2, spawnAreaSize.y / 2);
            return spawnArea.position + new Vector3(x, 0, z);
        }
        
        private void OnCharacterSelected(CharacterView character)
        {
            Debug.Log($"角色被选中: {character.GetModel().GetCharacterName()}");
        }
        
        private void OnCharacterMoved(CharacterView character, Vector3 newPosition)
        {
            Debug.Log($"角色移动: {character.GetModel().GetCharacterName()} 到 {newPosition}");
        }
        
        public List<CharacterView> GetSpawnedCharacters()
        {
            return spawnedCharacters;
        }
    }
}
