using UnityEngine;

public class SubmarineEscapeButton : MonoBehaviour, IInteractable
{
    [Header("Repair Manager")]
    [SerializeField] private SubmarineRepairManager repairManager;

    [Header("Debug")]
    [SerializeField] private bool printDebugLogs = true;

    public void Interact(PlayerInteraction player)
    {
        if (repairManager == null)
        {
            repairManager = FindFirstObjectByType<SubmarineRepairManager>();
        }

        if (repairManager == null)
        {
            Debug.LogWarning("Escape button has no SubmarineRepairManager assigned.");
            return;
        }

        if (printDebugLogs)
        {
            Debug.Log("Final submarine escape button pressed.", this);
        }

        repairManager.TryStartEscape();
    }
}
