using System.Collections;
using UnityEngine;

public class BookSpawner : MonoBehaviour
{
    [Header("Book Settings")]
    public GameObject bookPrefab;          // 书的预制件
    public float bookInitialScale = 0.01f; // 书的初始缩放比例
    public float bookTargetScale = 1.0f;   // 书的目标缩放比例
    public float bookDropDistance = 1.0f;  // 书籍下落的距离
    public float bookDropDuration = 2.0f;  // 书籍下落的持续时间

    private PortalController portalController;

    void Start()
    {
        StartCoroutine(BookSequence());
    }

    IEnumerator BookSequence()
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

        // 生成书籍
        StartCoroutine(SpawnBook());

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

    IEnumerator SpawnBook()
    {
        // 获取传送门的中心位置作为书的起始位置
        Vector3 startPosition = portalController.transform.position;

        // 计算书的目标位置（从传送门位置向下偏移 bookDropDistance）
        Vector3 targetPosition = startPosition - new Vector3(0, bookDropDistance, 0);

        // 实例化书籍
        GameObject bookInstance = Instantiate(bookPrefab, startPosition, Quaternion.identity);
        // 确保书籍不隶属于任何父对象
        bookInstance.transform.SetParent(null);
        // 设置初始缩放为初始值（几乎不可见）
        bookInstance.transform.localScale = Vector3.one * bookInitialScale;

        float elapsed = 0f;
        while (elapsed < bookDropDuration)
        {
            float t = elapsed / bookDropDuration;

            // 位置从传送门中心下落到目标位置
            bookInstance.transform.position = Vector3.Lerp(startPosition, targetPosition, t);

            // 缩放从初始缩放比例到目标缩放比例
            float scale = Mathf.Lerp(bookInitialScale, bookTargetScale, t);
            bookInstance.transform.localScale = Vector3.one * scale;

            // 使书籍始终面向前方（您可以根据需要调整）
            bookInstance.transform.forward = Vector3.forward;

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 确保最终位置和缩放准确
        bookInstance.transform.position = targetPosition;
        bookInstance.transform.localScale = Vector3.one * bookTargetScale;

        // 最后再次确保书籍朝向正确
        bookInstance.transform.forward = Vector3.forward;
    }
}
