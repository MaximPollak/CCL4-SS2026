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

        itemToDrop.DropFromHand(playerCameraTransform.forward, originalItemScale, plopForce);

        Debug.Log("Dropped item: " + itemToDrop.ItemId);

        currentItem = null;

        if (notifyInventoryChanged)
        {
            OnInventoryChanged?.Invoke(CurrentItemId);
        }
    }
}
