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

    [Header("Task Completion Indicator")]
    [SerializeField] private TaskCompletionIndicator taskCompletionIndicator;

    [Header("Messages")]
    [SerializeField] private string missingItemMessage = "Missing required item.";
    [SerializeField] private string progressMessage = "Added item.";
    [SerializeField] private string completedMessage = "Multi-item task completed.";

    private int currentItemCount;

    private void Start()
    {
        RestoreStateFromGameState();
    }

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

        GameState.Instance.MarkItemConsumed(requiredItemId);
        player.Inventory.ClearItem();

        currentItemCount++;
        SaveProgress();
        ApplyProgressVisuals();

        Debug.Log(progressMessage + " " + currentItemCount + "/" + requiredItemCount);

        if (currentItemCount < requiredItemCount)
        {
            return;
        }

        if (repairManager != null)
        {
            repairManager.CompleteTask(repairTask);
        }

        ApplyProgressVisuals();

        Debug.Log(completedMessage);
    }

    private void RestoreStateFromGameState()
    {
        if (repairManager == null)
        {
            ApplyProgressVisuals();
            return;
        }

        currentItemCount = repairManager.GetTaskProgress(repairTask);

        if (repairManager.IsTaskComplete(repairTask))
        {
            currentItemCount = Mathf.Max(currentItemCount, requiredItemCount);
        }

        currentItemCount = Mathf.Clamp(currentItemCount, 0, requiredItemCount);

        ApplyProgressVisuals();
    }

    private void SaveProgress()
    {
        if (repairManager == null)
        {
            return;
        }

        repairManager.SetTaskProgress(repairTask, currentItemCount);
    }

    private void ApplyProgressVisuals()
    {
        if (objectsToEnablePerItem != null)
        {
            for (int i = 0; i < objectsToEnablePerItem.Length; i++)
            {
                GameObject progressObject = objectsToEnablePerItem[i];

                if (progressObject != null)
                {
                    progressObject.SetActive(i < currentItemCount);
                }
            }
        }

        bool isComplete = currentItemCount >= requiredItemCount;

        if (objectToEnableWhenComplete != null)
        {
            objectToEnableWhenComplete.SetActive(isComplete);
        }

        if (taskCompletionIndicator != null)
        {
            taskCompletionIndicator.SetComplete(isComplete);
        }
    }
}