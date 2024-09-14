using Oculus.Interaction;
using UnityEngine;
using UnityEngine.Events;

public class InteractiveBook : MonoBehaviour
{
    public InteractableUnityEventWrapper eventWrapper;

    public static UnityEvent onGrabbed;
    public GameObject Dialogue;

    private void Awake()
    {
        // 确保InteractableUnityEventWrapper已经正确注入了InteractableView
        eventWrapper.InjectInteractableView(GetComponent<IInteractableView>());

        // 订阅选择事件
        eventWrapper.WhenSelect.AddListener(HandleGrabbed);
    }

    private void HandleGrabbed()
    {
        // 当物体被抓取时调用
        Debug.Log("Interactable was grabbed.");
        onGrabbed?.Invoke();
        Dialogue.SetActive(true);
    }
}