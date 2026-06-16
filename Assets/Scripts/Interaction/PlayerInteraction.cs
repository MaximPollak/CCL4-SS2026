using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;

    [Header("Interaction Settings")]
    [SerializeField] private float interactionDistance = 3f;

    public InventorySlot Inventory { get; private set; }
    public Camera PlayerCamera => playerCamera;

    private void Awake()
    {
        Inventory = GetComponent<InventorySlot>();

        if (Inventory == null)
        {
            Debug.LogWarning("Player has no InventorySlot component.");
        }
    }

    private void Update()
    {
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            TryInteract();
        }
    }

    private void TryInteract()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance))
        {
            if (hit.collider.TryGetComponent(out IInteractable interactable))
            {
                interactable.Interact(this);
            }
            else
            {
                Debug.Log("Looked at object, but it is not interactable: " + hit.collider.name);
            }
        }
        else
        {
            Debug.Log("Nothing in interaction range.");
        }
    }
}