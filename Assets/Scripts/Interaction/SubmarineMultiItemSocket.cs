using UnityEngine;

public class SubmarineMultiItemSocket : MonoBehaviour, IInteractable
{
    [Header("Repair")]
    [SerializeField] private SubmarineRepairManager repairManager;
    [SerializeField] private SubmarineRepairTask repairTask = SubmarineRepairTask.OilTank;

    [Header("Required Item")]
    [SerializeField] private string requiredItemId = "OilCanister";
    [SerializeField] private int requiredItemCount = 3;

    [Header("Visual Progress")]
    [SerializeField] private GameObject[] objectsToEnablePerItem;
    [SerializeField] private GameObject objectToEnableWhenComplete;

    [Header("Messages")]
    [SerializeField] private string missingItemMessage = "Missing required item.";
    [SerializeField] private string progressMessage = "Added item.";
    [SerializeField] private string completedMessage = "Multi-item task completed.";

    private int currentItemCount;

    public void Interact(PlayerInteraction player)
    {
        if (repairManager != null && repairManager.IsTaskComplete(repairTask))
        {
            Debug.Log("Already completed: " + repairTask);
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

        player.Inventory.ClearItem();
        currentItemCount++;

        int visualIndex = currentItemCount - 1;

        if (objectsToEnablePerItem != null && visualIndex < objectsToEnablePerItem.Length)
        {
            GameObject progressObject = objectsToEnablePerItem[visualIndex];

            if (progressObject != null)
            {
                progressObject.SetActive(true);
            }
        }

        Debug.Log(progressMessage + " " + currentItemCount + "/" + requiredItemCount);

        if (currentItemCount < requiredItemCount)
        {
            return;
        }

        if (objectToEnableWhenComplete != null)
        {
            objectToEnableWhenComplete.SetActive(true);
        }

        if (repairManager != null)
        {
            repairManager.CompleteTask(repairTask);
        }

        Debug.Log(completedMessage);
    }
}
