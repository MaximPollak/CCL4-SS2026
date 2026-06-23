using UnityEngine;

public class SubmarineRepeatedToolSocket : MonoBehaviour, IInteractable
{
    [Header("Repair")]
    [SerializeField] private SubmarineRepairManager repairManager;
    [SerializeField] private SubmarineRepairTask repairTask = SubmarineRepairTask.VentScrews;

    [Header("Prerequisite")]
    [SerializeField] private bool requireCompletedTaskFirst = false;
    [SerializeField] private SubmarineRepairTask prerequisiteTask;
    [SerializeField] private bool requireSecondCompletedTask = false;
    [SerializeField] private SubmarineRepairTask secondPrerequisiteTask;
    [SerializeField] private bool hideUntilPrerequisiteComplete = false;
    [SerializeField] private string prerequisiteMissingMessage = "Another repair step must be completed first.";

    [Header("Required Tool")]
    [SerializeField] private string requiredItemId = "Wrench";
    [SerializeField] private int requiredUseCount = 4;

    [Header("Visual Progress")]
    [SerializeField] private GameObject[] objectsToEnablePerUse;
    [SerializeField] private GameObject objectToEnableWhenComplete;

    [Header("Messages")]
    [SerializeField] private string missingItemMessage = "Missing required tool.";
    [SerializeField] private string progressMessage = "Tool used.";
    [SerializeField] private string completedMessage = "Tool task completed.";

    private int currentUseCount;
    private Collider[] ownColliders;
    private Renderer[] ownRenderers;

    private void Awake()
    {
        ownColliders = GetComponents<Collider>();
        ownRenderers = GetComponents<Renderer>();

        UpdatePrerequisiteVisibility();
    }

    private void Update()
    {
        UpdatePrerequisiteVisibility();
    }

    public void Interact(PlayerInteraction player)
    {
        if (repairManager != null && repairManager.IsTaskComplete(repairTask))
        {
            Debug.Log("Already completed: " + repairTask);
            return;
        }

        if (
            repairManager != null
            && (
                (requireCompletedTaskFirst && !repairManager.IsTaskComplete(prerequisiteTask))
                || (requireSecondCompletedTask && !repairManager.IsTaskComplete(secondPrerequisiteTask))
            )
        )
        {
            Debug.Log(prerequisiteMissingMessage);
            return;
        }

        if (player.Inventory == null)
        {
            Debug.LogWarning("Player has no inventory.");
            return;
        }

        if (!player.Inventory.HasItem(requiredItemId))
        {
            Debug.Log(missingItemMessage + " Required tool: " + requiredItemId);
            return;
        }

        currentUseCount++;

        int visualIndex = currentUseCount - 1;

        if (objectsToEnablePerUse != null && visualIndex < objectsToEnablePerUse.Length)
        {
            GameObject progressObject = objectsToEnablePerUse[visualIndex];

            if (progressObject != null)
            {
                progressObject.SetActive(true);
            }
        }

        Debug.Log(progressMessage + " " + currentUseCount + "/" + requiredUseCount);

        if (currentUseCount < requiredUseCount)
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

    private void UpdatePrerequisiteVisibility()
    {
        if (!hideUntilPrerequisiteComplete)
        {
            return;
        }

        bool shouldShow =
            (!requireCompletedTaskFirst && !requireSecondCompletedTask)
            || repairManager == null
            || (
                (!requireCompletedTaskFirst || repairManager.IsTaskComplete(prerequisiteTask))
                && (!requireSecondCompletedTask || repairManager.IsTaskComplete(secondPrerequisiteTask))
            );

        foreach (Collider ownCollider in ownColliders)
        {
            ownCollider.enabled = shouldShow;
        }

        foreach (Renderer ownRenderer in ownRenderers)
        {
            ownRenderer.enabled = shouldShow;
        }
    }
}
