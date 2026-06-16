using UnityEngine;

public class InventorySlot : MonoBehaviour
{
    [Header("Current Item")]
    [SerializeField] private PickupItem currentItem;

    [Header("Drop Settings")]
    [SerializeField] private float dropDistance = 1.5f;
    [SerializeField] private float dropHeightOffset = -0.3f;
    [SerializeField] private float plopForce = 2f;

    public bool IsEmpty()
    {
        return currentItem == null;
    }

    public void PickUpItem(PickupItem newItem, Transform playerCameraTransform)
    {
        if (!IsEmpty())
        {
            DropCurrentItem(playerCameraTransform);
        }

        currentItem = newItem;

        Debug.Log("Picked up item: " + currentItem.ItemId);

        currentItem.gameObject.SetActive(false);
    }

    private void DropCurrentItem(Transform playerCameraTransform)
    {
        Vector3 dropPosition =
            playerCameraTransform.position +
            playerCameraTransform.forward * dropDistance +
            Vector3.up * dropHeightOffset;

        currentItem.DropTo(dropPosition, playerCameraTransform.forward, plopForce);

        Debug.Log("Dropped item: " + currentItem.ItemId);

        currentItem = null;
    }

    public bool HasItem(string itemId)
    {
        return currentItem != null && currentItem.ItemId == itemId;
    }

    public string GetCurrentItemId()
    {
        if (currentItem == null)
        {
            return "";
        }

        return currentItem.ItemId;
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
    }
}