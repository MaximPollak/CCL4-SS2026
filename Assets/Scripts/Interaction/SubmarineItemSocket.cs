using UnityEngine;

public class SubmarineItemSocket : MonoBehaviour, IInteractable
{
    [Header("Repair")]
    [SerializeField] private SubmarineRepairManager repairManager;
    [SerializeField] private SubmarineRepairTask repairTask;

    [Header("Prerequisite")]
    [SerializeField] private bool requireCompletedTaskFirst = false;
    [SerializeField] private SubmarineRepairTask prerequisiteTask;
    [SerializeField] private bool hideUntilPrerequisiteComplete = false;
    [SerializeField] private string prerequisiteMissingMessage = "Another repair step must be completed first.";

    [Header("Required Item")]
    [SerializeField] private string requiredItemId = "OxygenTank";
    [SerializeField] private bool removeItemFromInventory = true;

    [Header("Visual Result")]
    [SerializeField] private GameObject objectToDisableAfterUse;
    [SerializeField] private GameObject objectToEnableAfterUse;

    [Header("Optional Take Back")]
    [SerializeField] private bool canTakeItemBack = false;
    [SerializeField] private PickupItem itemToReturnPrefab;
    [SerializeField] private string takeBackMessage = "Part removed.";

    [Header("Messages")]
    [SerializeField] private string missingItemMessage = "Missing required item.";
    [SerializeField] private string completedMessage = "Part installed.";

    private Collider[] ownColliders;
    private Renderer[] ownRenderers;

    private void Awake()
    {
        ownColliders = GetComponents<Collider>();
        ownRenderers = GetComponents<Renderer>();

        UpdatePrerequisiteVisibility();

        if (objectToEnableAfterUse != null && objectToEnableAfterUse.activeSelf)
        {
            MakePlacedVisualDisplayOnly(objectToEnableAfterUse);
        }
    }

    private void Update()
    {
        UpdatePrerequisiteVisibility();
    }

    public void Interact(PlayerInteraction player)
    {
        if (repairManager != null && repairManager.IsTaskComplete(repairTask))
        {
            TryTakeItemBack(player);
            return;
        }

        if (
            requireCompletedTaskFirst
            && repairManager != null
            && !repairManager.IsTaskComplete(prerequisiteTask)
        )
        {
            Debug.Log(prerequisiteMissingMessage + " Required task: " + prerequisiteTask);
            return;
        }

        if (player.Inventory == null)
        {
            Debug.LogWarning("Player has no inventory.");
            return;
        }

        if (!player.Inventory.HasItem(requiredItemId))
        {
            Debug.Log(missingItemMessage + " Required item: " + requiredItemId);
            return;
        }

        if (removeItemFromInventory)
        {
            player.Inventory.ClearItem();
        }

        if (objectToDisableAfterUse != null)
        {
            objectToDisableAfterUse.SetActive(false);
        }

        if (objectToEnableAfterUse != null)
        {
            objectToEnableAfterUse.SetActive(true);
            MakePlacedVisualDisplayOnly(objectToEnableAfterUse);
        }

        if (repairManager != null)
        {
            repairManager.CompleteTask(repairTask);
        }

        Debug.Log(completedMessage);
    }

    private void TryTakeItemBack(PlayerInteraction player)
    {
        if (!canTakeItemBack)
        {
            Debug.Log("Already completed: " + repairTask);
            return;
        }

        if (player.Inventory == null)
        {
            Debug.LogWarning("Player has no inventory.");
            return;
        }

        if (player.PlayerCamera == null)
        {
            Debug.LogWarning("Cannot take item back because Player Camera is missing.");
            return;
        }

        if (itemToReturnPrefab == null)
        {
            Debug.LogWarning("Cannot take item back because Item To Return Prefab is missing.");
            return;
        }

        if (objectToEnableAfterUse != null)
        {
            objectToEnableAfterUse.SetActive(false);
        }

        if (objectToDisableAfterUse != null)
        {
            objectToDisableAfterUse.SetActive(true);
        }

        PickupItem returnedItem = Instantiate(
            itemToReturnPrefab,
            transform.position,
            transform.rotation
        );

        player.Inventory.PickUpItem(returnedItem, player.PlayerCamera.transform);

        if (repairManager != null)
        {
            repairManager.ClearTask(repairTask);
        }

        if (objectToEnableAfterUse != null)
        {
            objectToEnableAfterUse.SetActive(false);
        }

        Debug.Log(takeBackMessage);
    }

    private void UpdatePrerequisiteVisibility()
    {
        if (!hideUntilPrerequisiteComplete)
        {
            return;
        }

        bool shouldShow =
            !requireCompletedTaskFirst
            || repairManager == null
            || repairManager.IsTaskComplete(prerequisiteTask);

        foreach (Collider ownCollider in ownColliders)
        {
            ownCollider.enabled = shouldShow;
        }

        foreach (Renderer ownRenderer in ownRenderers)
        {
            ownRenderer.enabled = shouldShow;
        }
    }

    private void MakePlacedVisualDisplayOnly(GameObject placedVisual)
    {
        PickupItem[] pickupItems = placedVisual.GetComponentsInChildren<PickupItem>(true);

        foreach (PickupItem pickupItem in pickupItems)
        {
            pickupItem.enabled = false;
        }

        Collider[] colliders = placedVisual.GetComponentsInChildren<Collider>(true);

        foreach (Collider itemCollider in colliders)
        {
            if (itemCollider.GetComponentInParent<SubmarineItemSocket>() != null)
            {
                continue;
            }

            itemCollider.enabled = false;
        }
    }
}
