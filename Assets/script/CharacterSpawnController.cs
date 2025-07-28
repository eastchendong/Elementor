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
        [SerializeField] private GameObject characterControllerPrefab; // 包含所有组件和脚本的控制器prefab
        
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
                
                // 先生成控制器prefab作为主物体
                Transform parentTransform = spawnArea.parent;
                GameObject controllerObj = Instantiate(characterControllerPrefab, spawnPosition, Quaternion.identity, parentTransform);
                
                if (controllerObj != null)
                {
                    // 然后在控制器下面生成3D人偶模型
                    GameObject modelObj = CreateCharacterModel(spawnData.characters[i], controllerObj.transform);
                    
                    // 获取CharacterView和CharacterModel组件
                    CharacterView characterView = controllerObj.GetComponent<CharacterView>();
                    CharacterModel characterModel = controllerObj.GetComponent<CharacterModel>();
                    
                    if (characterView != null && characterModel != null)
                    {
                        // 设置CharacterView的model引用
                        characterView.SetCharacterModel(characterModel);
                        
                        // 进行初始化
                        characterView.Initialize();
                        characterModel.Initialize(spawnData.characters[i]);
                        spawnedCharacters.Add(characterView);
                        
                        // 订阅角色交互事件
                        characterView.OnCharacterSelected += OnCharacterSelected;
                        characterView.OnCharacterMoved += OnCharacterMoved;
                    }
                    else
                    {
                        Debug.LogError("控制器prefab缺少必要的组件 (CharacterView 或 CharacterModel)");
                    }
                }
            }
        }
        
        private GameObject CreateCharacterModel(Character character, Transform parent)
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
                Debug.LogError("没有可用的prefab来生成角色模型");
                return null;
            }
            
            // 在控制器下生成3D模型，使用本地坐标(0,0,0)
            GameObject modelObj = Instantiate(modelPrefab, parent);
            modelObj.transform.localPosition = Vector3.zero;
            modelObj.transform.localRotation = Quaternion.identity;
            
            return modelObj;
        }
        
        private Vector3 GetRandomSpawnPosition()
        {
            float x = Random.Range(-spawnAreaSize.x / 2, spawnAreaSize.x / 2);
            float z = Random.Range(-spawnAreaSize.y / 2, spawnAreaSize.y / 2);
            
            // 计算相对于spawn area的世界坐标位置
            Vector3 localOffset = new Vector3(x, 0, z);
            
            // 将本地偏移转换为世界坐标，考虑spawn area的scale和rotation
            Vector3 worldOffset = spawnArea.TransformVector(localOffset);
            
            // 返回spawn area的世界位置加上转换后的偏移
            return spawnArea.position + worldOffset;
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
