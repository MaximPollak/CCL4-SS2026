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

    [Header("Debug")]
    [SerializeField] private bool printInteractionDebugLogs = false;

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
        if (HideState != null && HideState.IsTransitioning)
        {
            return;
        }

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

        if (Physics.Raycast(
            ray,
            out RaycastHit hit,
            interactionDistance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Collide
        ))
        {
            IInteractable interactable = GetInteractableFromHit(hit.collider);

            if (interactable != null)
            {
                interactable.Interact(this);
            }
            else
            {
                interactable = GetNearbyInteractable(hit.point, ray);

                if (interactable != null)
                {
                    interactable.Interact(this);
                }
                else
                {
                    LogInteractionDebug(
                        "Looked at object, but it is not interactable: " + hit.collider.name
                    );
                }
            }
        }
        else
        {
            LogInteractionDebug("Nothing in interaction range.");
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

    private IInteractable GetNearbyInteractable(Vector3 searchPosition, Ray lookRay)
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

        IInteractable bestInteractable = null;
        float bestLookDistance = Mathf.Infinity;
        float bestHitDistance = Mathf.Infinity;

        foreach (Collider nearbyCollider in nearbyColliders)
        {
            IInteractable interactable = GetInteractableFromHit(nearbyCollider);

            if (interactable == null)
            {
                continue;
            }

            Vector3 candidatePosition = nearbyCollider.bounds.center;
            Vector3 toCandidate = candidatePosition - lookRay.origin;
            float distanceAlongLook = Vector3.Dot(toCandidate, lookRay.direction);

            if (distanceAlongLook < 0f || distanceAlongLook > interactionDistance)
            {
                continue;
            }

            Vector3 closestPointOnLook = lookRay.origin + lookRay.direction * distanceAlongLook;
            float lookDistance = Vector3.SqrMagnitude(candidatePosition - closestPointOnLook);
            float hitDistance = Vector3.SqrMagnitude(candidatePosition - searchPosition);

            if (lookDistance > blockedInteractableSearchRadius * blockedInteractableSearchRadius)
            {
                continue;
            }

            if (
                lookDistance < bestLookDistance
                || (Mathf.Approximately(lookDistance, bestLookDistance) && hitDistance < bestHitDistance)
            )
            {
                bestLookDistance = lookDistance;
                bestHitDistance = hitDistance;
                bestInteractable = interactable;
            }
        }

        return bestInteractable;
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

    private void LogInteractionDebug(string message)
    {
        if (!printInteractionDebugLogs)
        {
            return;
        }

        Debug.Log(message);
    }
}
