using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class HingedDoor : MonoBehaviour, IInteractable
{
    [Header("Door Rotation")]
    [Tooltip("Rotation added to the door's placed local rotation when it opens. Use a negative Y value to open in the opposite direction.")]
    [SerializeField] private Vector3 openRotation = new Vector3(0f, 90f, 0f);

    [Min(1f)]
    [SerializeField] private float rotationSpeed = 180f;

    [SerializeField] private bool startsOpen = false;

    [Header("Debug")]
    [SerializeField] private bool enableKeyboardTestToggle = false;
    [SerializeField] private bool printDebugLogs = false;

    private Quaternion closedLocalRotation;
    private Quaternion openLocalRotation;
    private bool isOpen;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        closedLocalRotation = transform.localRotation;
        openLocalRotation = closedLocalRotation * Quaternion.Euler(openRotation);
        isOpen = startsOpen;
        transform.localRotation = GetTargetRotation();
    }

    private void Update()
    {
        if (enableKeyboardTestToggle
            && Keyboard.current != null
            && Keyboard.current.oKey.wasPressedThisFrame)
        {
            Toggle();
        }

        transform.localRotation = Quaternion.RotateTowards(
            transform.localRotation,
            GetTargetRotation(),
            rotationSpeed * Time.deltaTime
        );
    }

    public void Interact(PlayerInteraction player)
    {
        Toggle();
    }

    public void Open()
    {
        isOpen = true;
        LogStateChange();
    }

    public void Close()
    {
        isOpen = false;
        LogStateChange();
    }

    public void Toggle()
    {
        isOpen = !isOpen;
        LogStateChange();
    }

    private Quaternion GetTargetRotation()
    {
        return isOpen ? openLocalRotation : closedLocalRotation;
    }

    private void LogStateChange()
    {
        if (!printDebugLogs)
        {
            return;
        }

        Debug.Log($"{name} door {(isOpen ? "opened" : "closed")}.", this);
    }
}
