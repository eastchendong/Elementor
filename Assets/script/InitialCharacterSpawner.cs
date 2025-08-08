using UnityEngine;
using System.IO;
using Elementor.Core;

namespace Elementor
{
    public class InitialCharacterSpawner : MonoBehaviour
    {
        [SerializeField] private Transform spawnArea;
        [SerializeField] private string jsonFileName = "character_spawn_config.json";
        [SerializeField] private Vector2 spawnAreaSize = new Vector2(2f, 2f);

        private CharacterSpawnData spawnData;

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
                Debug.LogWarning($"JSON文件未找到: {filePath}");
            }
        }

        private void SpawnCharacters()
        {
            if (spawnData?.characterNames == null) return;
            if (CharacterSpawnController.Instance == null)
            {
                Debug.LogError("CharacterSpawnController instance not found. Cannot spawn characters.");
                return;
            }

            for (int i = 0; i < spawnData.characterNames.Length; i++)
            {
                Vector3 spawnPosition = GetRandomSpawnPosition();
                CharacterSpawnController.Instance.SpawnCharacter(spawnData.characterNames[i], spawnPosition, spawnArea.parent);
            }
        }

        private Vector3 GetRandomSpawnPosition()
        {
            float x = Random.Range(-spawnAreaSize.x / 2, spawnAreaSize.y / 2);
            float z = Random.Range(-spawnAreaSize.y / 2, spawnAreaSize.y / 2);

            // 计算相对于spawn area的世界坐标位置
            Vector3 localOffset = new Vector3(x, 0, z);

            // 将本地偏移转换为世界坐标，考虑spawn area的scale和rotation
            Vector3 worldOffset = spawnArea.TransformVector(localOffset);

            // 返回spawn area的世界位置加上转换后的偏移
            return spawnArea.position + worldOffset;
        }
    }
}
