using System;
using UnityEngine;

public class InventorySlot : MonoBehaviour
{
    [Header("Current Item")]
    [SerializeField] private PickupItem currentItem;

    [Header("Carry Settings")]
    [SerializeField] private Transform holdPoint;

    [Header("Held Item Visual Settings")]
    [SerializeField] private Vector3 heldLocalPosition = new Vector3(0.45f, -0.35f, 0.75f);
    [SerializeField] private Vector3 heldLocalRotation = new Vector3(15f, -25f, 10f);
    [SerializeField] private float heldScaleMultiplier = 0.35f;

    [Header("Drop Settings")]
    [SerializeField] private float plopForce = 1.7f;
    [SerializeField] private float dropDistance = 1.1f;
    [SerializeField] private float dropHeightOffset = -0.25f;
    [SerializeField] private float dropGroundClearance = 0.2f;
    [SerializeField] private float ignorePlayerCollisionDuration = 0f;
    [SerializeField] private float wallDropClearance = 0.35f;

    [Header("Debug")]
    [SerializeField] private bool printDropDebugLogs = false;

    private Vector3 originalItemScale;

    public event Action<string> OnInventoryChanged;

    public string CurrentItemId
    {
        get
        {
            if (currentItem == null)
            {
                return "";
            }

            return currentItem.ItemId;
        }
    }

    public bool IsEmpty()
    {
        return currentItem == null;
    }

    public bool HasItem(string itemId)
    {
        return currentItem != null && currentItem.ItemId == itemId;
    }

    public void PickUpItem(PickupItem newItem, Transform playerCameraTransform)
    {
        if (newItem == null)
        {
            return;
        }

        if (holdPoint == null)
        {
            Debug.LogWarning("InventorySlot has no HoldPoint assigned.");
            return;
        }

        if (playerCameraTransform == null)
        {
            Debug.LogWarning("InventorySlot needs a player camera transform to pick up items.");
            return;
        }

        if (!IsEmpty())
        {
            DropCurrentItem(playerCameraTransform, false);
        }

        currentItem = newItem;
        originalItemScale = currentItem.transform.localScale;

        CarryCurrentItem();

        Debug.Log("Picked up item: " + currentItem.ItemId);

        OnInventoryChanged?.Invoke(CurrentItemId);
    }

    public void DropHeldItem(Transform playerCameraTransform)
    {
        DropCurrentItem(playerCameraTransform, true);
    }

    public void ClearItem()
    {
        if (currentItem == null)
        {
            return;
        }

        Debug.Log("Removed item from inventory: " + currentItem.ItemId);

        Destroy(currentItem.gameObject);
        currentItem = null;

        OnInventoryChanged?.Invoke(CurrentItemId);
    }

    private void CarryCurrentItem()
    {
        if (currentItem == null)
        {
            return;
        }

        Rigidbody itemRigidbody = currentItem.GetComponent<Rigidbody>();

        if (itemRigidbody != null)
        {
            itemRigidbody.isKinematic = true;
            itemRigidbody.linearVelocity = Vector3.zero;
            itemRigidbody.angularVelocity = Vector3.zero;
        }

        Collider[] itemColliders = currentItem.GetComponentsInChildren<Collider>();

        foreach (Collider itemCollider in itemColliders)
        {
            itemCollider.enabled = false;
        }

        currentItem.transform.SetParent(holdPoint);

        Vector3 finalHeldLocalPosition = currentItem.OverrideHeldPosition
            ? currentItem.HeldLocalPosition
            : heldLocalPosition;

        Vector3 finalHeldLocalRotation = currentItem.OverrideHeldRotation
            ? currentItem.HeldLocalRotation
            : heldLocalRotation;

        currentItem.transform.localPosition = finalHeldLocalPosition;
        currentItem.transform.localRotation = Quaternion.Euler(finalHeldLocalRotation);
        currentItem.transform.localScale =
            originalItemScale
            * heldScaleMultiplier
            * currentItem.HeldScaleMultiplier;
    }

    private void DropCurrentItem(Transform playerCameraTransform, bool notifyInventoryChanged = true)
    {
        if (currentItem == null)
        {
            return;
        }

        if (playerCameraTransform == null)
        {
            Debug.LogWarning("Cannot drop item because player camera transform is missing.");
            return;
        }

        PickupItem itemToDrop = currentItem;

        Vector3 dropPosition = GetDropPosition(playerCameraTransform);
        Collider[] playerColliders = GetComponentsInChildren<Collider>();

        itemToDrop.DropFromHand(
            dropPosition,
            playerCameraTransform.forward,
            originalItemScale,
            plopForce,
            playerColliders,
            ignorePlayerCollisionDuration,
            printDropDebugLogs
        );

        Debug.Log("Dropped item: " + itemToDrop.ItemId);

        currentItem = null;

        if (notifyInventoryChanged)
        {
            OnInventoryChanged?.Invoke(CurrentItemId);
        }
    }

    private Vector3 GetDropPosition(Transform playerCameraTransform)
    {
        float finalDropDistance = dropDistance;
        Ray forwardRay = new Ray(playerCameraTransform.position, playerCameraTransform.forward);

        if (Physics.Raycast(
            forwardRay,
            out RaycastHit forwardHit,
            dropDistance + wallDropClearance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore
        ))
        {
            finalDropDistance = Mathf.Max(0.45f, forwardHit.distance - wallDropClearance);

            if (printDropDebugLogs)
            {
                Debug.Log(
                    "Drop debug | forward ray hit: " + forwardHit.collider.name
                    + " | hit distance: " + forwardHit.distance.ToString("0.00")
                    + " | using drop distance: " + finalDropDistance.ToString("0.00")
                );
            }
        }

        Vector3 dropPosition =
            playerCameraTransform.position
            + playerCameraTransform.forward * finalDropDistance
            + Vector3.up * dropHeightOffset;

        Vector3 floorCheckStart = dropPosition + Vector3.up * 0.7f;

        if (Physics.Raycast(
            floorCheckStart,
            Vector3.down,
            out RaycastHit floorHit,
            2f,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore
        ))
        {
            float minimumHeight = floorHit.point.y + dropGroundClearance;
            dropPosition.y = Mathf.Max(dropPosition.y, minimumHeight);

            if (printDropDebugLogs)
            {
                Debug.Log(
                    "Drop debug | floor ray hit: " + floorHit.collider.name
                    + " | floor y: " + floorHit.point.y.ToString("0.00")
                    + " | final drop position: " + dropPosition
                );
            }
        }
        else if (printDropDebugLogs)
        {
            Debug.Log("Drop debug | no floor found below calculated drop position: " + dropPosition);
        }

        return dropPosition;
    }
}
