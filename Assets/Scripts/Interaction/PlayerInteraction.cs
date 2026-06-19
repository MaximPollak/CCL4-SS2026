using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerHideState))]
public class PlayerInteraction : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;

    [Header("Interaction Settings")]
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private float blockedInteractableSearchRadius = 1.2f;

    public InventorySlot Inventory { get; private set; }
    public PlayerHideState HideState { get; private set; }
    public Camera PlayerCamera => playerCamera;

    private void Awake()
    {
        Inventory = GetComponent<InventorySlot>();
        HideState = GetComponent<PlayerHideState>();

        if (HideState == null)
        {
            HideState = gameObject.AddComponent<PlayerHideState>();
        }

        if (Inventory == null)
        {
            Debug.LogWarning("Player has no InventorySlot component.");
        }

        if (playerCamera == null)
        {
            Debug.LogWarning("PlayerInteraction has no Player Camera assigned.");
        }
    }

    private void Update()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            TryInteract();
        }

        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            TryDropHeldItem();
        }
    }

    private void TryInteract()
    {
        if (HideState != null && HideState.IsHidden)
        {
            if (HideState.CurrentHidingSpot != null)
            {
                HideState.CurrentHidingSpot.Interact(this);
            }

            return;
        }

        if (playerCamera == null)
        {
            Debug.LogWarning("Cannot interact because Player Camera is missing.");
            return;
        }

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance))
        {
            IInteractable interactable = GetInteractableFromHit(hit.collider);

            if (interactable != null)
            {
                interactable.Interact(this);
            }
            else
            {
                interactable = GetNearbyInteractable(hit.point);

                if (interactable != null)
                {
                    interactable.Interact(this);
                }
                else
                {
                    Debug.Log("Looked at object, but it is not interactable: " + hit.collider.name);
                }
            }
        }
        else
        {
            Debug.Log("Nothing in interaction range.");
        }
    }

    private IInteractable GetInteractableFromHit(Collider hitCollider)
    {
        if (hitCollider == null)
        {
            return null;
        }

        if (hitCollider.TryGetComponent(out IInteractable interactable))
        {
            return interactable;
        }

        return hitCollider.GetComponentInParent<IInteractable>();
    }

    private IInteractable GetNearbyInteractable(Vector3 searchPosition)
    {
        if (blockedInteractableSearchRadius <= 0f)
        {
            return null;
        }

        Collider[] nearbyColliders = Physics.OverlapSphere(
            searchPosition,
            blockedInteractableSearchRadius,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Collide
        );

        IInteractable closestInteractable = null;
        float closestDistance = Mathf.Infinity;

        foreach (Collider nearbyCollider in nearbyColliders)
        {
            IInteractable interactable = GetInteractableFromHit(nearbyCollider);

            if (interactable == null || !(interactable is Component interactableComponent))
            {
                continue;
            }

            float distance = Vector3.SqrMagnitude(
                interactableComponent.transform.position - searchPosition
            );

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestInteractable = interactable;
            }
        }

        return closestInteractable;
    }

    private void TryDropHeldItem()
    {
        if (Inventory == null)
        {
            return;
        }

        if (playerCamera == null)
        {
            Debug.LogWarning("Cannot drop item because Player Camera is missing.");
            return;
        }

        Inventory.DropHeldItem(playerCamera.transform);
    }
}
