using System.Collections;
using UnityEngine;

public class BookSpawner : MonoBehaviour
{
    [Header("Target GameObject")]
    public GameObject targetGameObject;    // 要启用和生成的目标GameObject

    [Header("Spawn Point")]
    public Transform spawnPoint;           // 生成的参考点
    public Vector3 spawnOffset;            // 偏移量（基于本地前、上、侧向量）

    [Header("Spawn Settings")]
    public float initialScale = 0.01f;     // 初始缩放比例
    public float targetScale = 1.0f;       // 目标缩放比例
    public float dropDistance = 1.0f;      // 下落的距离
    public float spawnDuration = 2.0f;     // 生成动画的持续时间

    private PortalController portalController;
    private Vector3 originalScale;

    void Start()
    {
        if (targetGameObject == null)
        {
            Debug.LogError("未设置目标GameObject！");
            return;
        }

        if (spawnPoint == null)
        {
            Debug.LogError("未设置生成点！");
            return;
        }

        // 记录目标GameObject的原始缩放
        originalScale = targetGameObject.transform.localScale;
        
        // 在启动时禁用目标GameObject
        targetGameObject.SetActive(false);
        
        StartCoroutine(SpawnSequence());
    }

    IEnumerator SpawnSequence()
    {
        // 等待传送门出现
        yield return StartCoroutine(WaitForPortal());

        if (portalController == null)
        {
            Debug.LogError("未能找到 PortalController！");
            yield break;
        }

        // 等待 5 秒
        yield return new WaitForSeconds(5.0f);

        // 启用目标GameObject并开始生成动画
        targetGameObject.SetActive(true);
        StartCoroutine(SpawnAnimation());

        // 再等待 5 秒
        yield return new WaitForSeconds(5.0f);

        // 调用传送门的隐藏方法
        portalController.HidePortal();
    }

    IEnumerator WaitForPortal()
    {
        // 不断查找 PortalController 实例
        while (portalController == null)
        {
            portalController = FindObjectOfType<PortalController>();
            if (portalController == null)
            {
                yield return null;
            }
        }
    }

    IEnumerator SpawnAnimation()
    {
        // 获取生成点的位置并应用偏移量
        Vector3 startPosition = spawnPoint.position +
                                spawnPoint.forward * spawnOffset.z +
                                spawnPoint.up * spawnOffset.y +
                                spawnPoint.right * spawnOffset.x;

        // 计算目标位置（从生成点位置向下偏移 dropDistance）
        Vector3 targetPosition = startPosition - new Vector3(0, dropDistance, 0);

        // 计算朝向生成点的旋转
        Vector3 directionToSpawnPoint = (spawnPoint.position - targetPosition).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(directionToSpawnPoint);

        // 设置初始状态
        targetGameObject.transform.position = startPosition;
        targetGameObject.transform.localScale = Vector3.one * initialScale;
        targetGameObject.transform.rotation = targetRotation;

        float elapsed = 0f;
        while (elapsed < spawnDuration)
        {
            float t = elapsed / spawnDuration;

            // 位置从起始位置移动到目标位置
            targetGameObject.transform.position = Vector3.Lerp(startPosition, targetPosition, t);

            // 缩放从初始缩放比例到目标缩放比例
            float scale = Mathf.Lerp(initialScale, targetScale, t);
            targetGameObject.transform.localScale = Vector3.one * scale;

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 确保最终位置和缩放准确
        targetGameObject.transform.position = targetPosition;
        targetGameObject.transform.localScale = originalScale * targetScale;
        targetGameObject.transform.rotation = targetRotation;
    }
}
