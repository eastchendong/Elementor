using System.Collections;
using UnityEngine;

public class PortalController : MonoBehaviour
{
    [Header("Portal Settings")]
    public float appearDuration = 1.0f;        // 传送门显现的持续时间
    public float rotationSpeed = 90f;          // 旋转速度（度/秒）
    public Vector3 targetScale = new Vector3(1.2f, 1.2f, 1.2f); // 目标放大比例

    private Vector3 initialScale;
    private bool isRotating = false;

    [Header("Portal Surface")]
    public string portalSurfaceName = "PortalSurface_low"; // 子对象名称
    private Transform portalSurface; // 子对象的Transform

    void Start()
    {
        StartCoroutine(PortalSequence());
    }

    IEnumerator PortalSequence()
    {
        // 1. 初始化传送门为不可见（缩放为0）
        initialScale = Vector3.zero;
        transform.localScale = initialScale;

        // 2. 查找子对象PortalSurface_low
        portalSurface = transform.Find(portalSurfaceName);
        if (portalSurface == null)
        {
            Debug.LogError($"子对象 '{portalSurfaceName}' 未找到，请确保它存在于传送门Prefab中。");
            yield break;
        }

        // 3. 传送门显现
        yield return StartCoroutine(ScaleOverTime(initialScale, Vector3.one, appearDuration));

        // 4. 开始旋转和放大
        isRotating = true;
        StartCoroutine(RotatePortal());

        // 5. 放大到目标规模
        yield return StartCoroutine(ScaleOverTime(Vector3.one, targetScale, 1.0f));

        // 传送门保持在目标规模，等待外部指令（例如来自 BookSpawner）
    }

    IEnumerator ScaleOverTime(Vector3 fromScale, Vector3 toScale, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            transform.localScale = Vector3.Lerp(fromScale, toScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localScale = toScale;
    }

    IEnumerator RotatePortal()
    {
        while (isRotating && portalSurface != null)
        {
            // 确保使用 Space.Self 以绕自身轴旋转
            portalSurface.Rotate(0, 0, rotationSpeed * Time.deltaTime, Space.Self);
            yield return null;
        }
    }

    // 公共方法，用于隐藏传送门
    public void HidePortal()
    {
        StartCoroutine(HidePortalSequence());
    }

    IEnumerator HidePortalSequence()
    {
        // 停止旋转
        isRotating = false;

        // 缩小传送门
        yield return StartCoroutine(ScaleOverTime(transform.localScale, Vector3.zero, 1.0f));

        // 销毁传送门对象
        Destroy(gameObject);
    }
}
