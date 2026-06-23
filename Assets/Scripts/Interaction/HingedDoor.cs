using UnityEngine;

[DisallowMultipleComponent]
public class HingedDoor : MonoBehaviour, IInteractable
{
    [Header("Door Rotation")]
    [Tooltip("Rotation added to the door's placed local rotation when it opens. Use a negative Y value to open in the opposite direction.")]
    [SerializeField] private Vector3 openRotation = new Vector3(0f, 90f, 0f);

    [Min(1f)]
    [SerializeField] private float rotationSpeed = 180f;

    [SerializeField] private bool startsOpen = false;

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
    if (Input.GetKeyDown(KeyCode.O))
    {
        Toggle();
        Debug.Log("Door toggled!");
    }

    transform.localRotation = Quaternion.RotateTowards(
        transform.localRotation,
        GetTargetRotation(),
        rotationSpeed * Time.deltaTime
    );
}

    public void Interact(PlayerInteraction player)
    {
        isOpen = !isOpen;
    }

    public void Open()
    {
        isOpen = true;
    }

    public void Close()
    {
        isOpen = false;
    }

    public void Toggle()
    {
        isOpen = !isOpen;
    }

    private Quaternion GetTargetRotation()
    {
        return isOpen ? openLocalRotation : closedLocalRotation;
    }

    
}
