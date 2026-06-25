using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    private void Start()
    {
        RestoreHeldItemFromGameState();
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

        GameState.Instance.SetHeldItem(currentItem.ItemId);
        OnInventoryChanged?.Invoke(CurrentItemId);
    }

    public void DropHeldItem(Transform playerCameraTransform)
    {
        DropCurrentItem(playerCameraTransform, true);
    }

    public bool DropHeldItemAt(
        Vector3 dropPosition,
        Quaternion dropRotation,
        bool saveToGameState = true
    )
    {
        if (currentItem == null)
        {
            return false;
        }

        PickupItem itemToDrop = currentItem;
        Collider[] playerColliders = GetComponentsInChildren<Collider>();

        // Enemy catches drop the held item exactly where the player was caught.
        itemToDrop.DropFromHand(
            dropPosition,
            dropRotation * Vector3.forward,
            originalItemScale,
            0f,
            playerColliders,
            0f,
            printDropDebugLogs
        );

        itemToDrop.transform.rotation = dropRotation;

        currentItem = null;

        if (saveToGameState)
        {
            GameState.Instance.ClearHeldItem();
            GameState.Instance.AddDroppedWorldItem(
                SceneManager.GetActiveScene().name,
                itemToDrop.ItemId,
                dropPosition,
                dropRotation,
                originalItemScale
            );
        }

        OnInventoryChanged?.Invoke(CurrentItemId);
        return true;
    }

    public void ClearItem()
    {
        ClearItem(true);
    }

    public void ClearItem(bool saveToGameState)
    {
        if (currentItem == null)
        {
            return;
        }

        Debug.Log("Removed item from inventory: " + currentItem.ItemId);

        Destroy(currentItem.gameObject);
        currentItem = null;

        if (saveToGameState)
        {
            GameState.Instance.ClearHeldItem();
        }

        OnInventoryChanged?.Invoke(CurrentItemId);
    }

    public void ClearHeldItemForDeath()
    {
        if (currentItem != null)
        {
            Destroy(currentItem.gameObject);
            currentItem = null;
        }

        string lostItemId = GameState.Instance.ConsumeHeldItemForDeath();

        if (!string.IsNullOrWhiteSpace(lostItemId))
        {
            RandomItemSpawner randomItemSpawner = FindFirstObjectByType<RandomItemSpawner>();

            if (randomItemSpawner != null)
            {
                randomItemSpawner.SpawnSpecificItem(lostItemId);
            }
        }

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

        StartCoroutine(SaveDroppedItemAfterAnimation(itemToDrop));

        Debug.Log("Dropped item: " + itemToDrop.ItemId);

        currentItem = null;
        GameState.Instance.ClearHeldItem();

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

    private IEnumerator SaveDroppedItemAfterAnimation(PickupItem droppedItem)
    {
        if (droppedItem == null)
        {
            yield break;
        }

        yield return new WaitForSeconds(0.35f);

        if (
            droppedItem == null
            || !droppedItem.gameObject.activeInHierarchy
            || droppedItem.transform.parent != null
        )
        {
            yield break;
        }

        GameState.Instance.AddDroppedWorldItem(
            SceneManager.GetActiveScene().name,
            droppedItem.ItemId,
            droppedItem.transform.position,
            droppedItem.transform.rotation,
            droppedItem.transform.localScale
        );

        NotifyMonsterOfPlayerDroppedItem(droppedItem);
    }

    private void NotifyMonsterOfPlayerDroppedItem(PickupItem droppedItem)
    {
        if (droppedItem == null)
        {
            return;
        }

        Debug.Log(
            "Dropped item alert sent | item: " + droppedItem.ItemId
            + " | size: " + droppedItem.Size
            + " | position: " + droppedItem.transform.position,
            droppedItem
        );

        MonsterAI monsterAI = FindFirstObjectByType<MonsterAI>();

        if (monsterAI == null)
        {
            Debug.Log("Dropped item alert skipped because no MonsterAI was found.");

            return;
        }

        // Only intentional player drops call this alert; physics impacts and audio events are ignored.
        monsterAI.TryInvestigateDroppedItemAlert(droppedItem, droppedItem.transform.position);
    }

    private void RestoreHeldItemFromGameState()
    {
        if (currentItem != null)
        {
            return;
        }

        string heldItemId = GameState.Instance.HeldItemId;

        if (string.IsNullOrWhiteSpace(heldItemId))
        {
            return;
        }

        if (holdPoint == null)
        {
            Debug.LogWarning("Cannot restore held item because InventorySlot has no HoldPoint assigned.");
            return;
        }

        if (!GameState.Instance.TryGetItemPrefab(heldItemId, out GameObject itemPrefab))
        {
            Debug.LogWarning("Cannot restore held item because no prefab is registered for: " + heldItemId);
            return;
        }

        GameObject restoredItemObject = Instantiate(
            itemPrefab,
            holdPoint.position,
            holdPoint.rotation
        );

        PickupItem restoredItem = restoredItemObject.GetComponentInChildren<PickupItem>(true);

        if (restoredItem == null)
        {
            Debug.LogWarning("Cannot restore held item because prefab has no PickupItem: " + heldItemId);
            Destroy(restoredItemObject);
            return;
        }

        currentItem = restoredItem;
        originalItemScale = currentItem.transform.localScale;
        CarryCurrentItem();

        OnInventoryChanged?.Invoke(CurrentItemId);
    }
}
