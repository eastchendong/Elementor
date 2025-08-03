using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace Elementor.Core
{
    public class CharacterSpawnController : MonoBehaviour
    {
        public static CharacterSpawnController Instance { get; private set; }

        [SerializeField] private GameObject fallbackPrefab; // 备用prefab，当找不到指定模型时使用
        [SerializeField] private GameObject characterControllerPrefab; // 包含所有组件和脚本的控制器prefab
        [SerializeField] private GameObject characterGroupPrefab; // 用于组合角色的Prefab
        
        private List<CharacterView> spawnedCharacters = new List<CharacterView>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
            }
        }

        public void SpawnCharacter(Character character, Vector3? location = null, Transform parent = null)
        {
            Vector3 spawnPosition = location ?? Vector3.zero;
            Transform parentTransform = parent ?? transform;

            // 先生成控制器prefab作为主物体
            GameObject controllerObj = Instantiate(characterControllerPrefab, spawnPosition, Quaternion.identity, parentTransform);

            if (controllerObj != null)
            {

                GameObject modelObj = CreateCharacterModel(character, controllerObj.transform);

                CharacterView characterView = controllerObj.GetComponent<CharacterView>();
                CharacterModel characterModel = controllerObj.GetComponent<CharacterModel>();

                if (characterView != null && characterModel != null)
                {
                    characterView.SetCharacterModel(characterModel);

                    characterView.Initialize();
                    characterModel.Initialize(character);
                    
                    controllerObj.name = character.name + "_Controller";
                    
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
        
        public CharacterGroup CreateCharacterGroup(string groupName, Vector3? location = null, Transform parent = null)
        {
            if (characterGroupPrefab == null)
            {
                Debug.LogError("CharacterGroupPrefab is not set in CharacterSpawnController.");
                return null;
            }

            Vector3 spawnPosition = location ?? Vector3.zero;
            Transform parentTransform = parent ?? transform;

            GameObject groupObj = Instantiate(characterGroupPrefab, spawnPosition, Quaternion.identity, parentTransform);
            groupObj.name = groupName;
            CharacterGroup group = groupObj.GetComponent<CharacterGroup>();
            return group;
        }

        public void AddCharacterToGroup(CharacterGroup group, CharacterView character)
        {
            if (group == null || character == null)
            {
                Debug.LogError("Cannot add character to a null group or add a null character.");
                return;
            }
            group.AddCharacter(character);
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
            
            GameObject modelObj = Instantiate(modelPrefab, parent);
            modelObj.transform.localPosition = Vector3.zero;
            modelObj.transform.localRotation = Quaternion.identity;
            
            modelObj.name = $"{character.name}_Model";
            Debug.Log($"Model object name set to: '{modelObj.name}'");

            return modelObj;
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
