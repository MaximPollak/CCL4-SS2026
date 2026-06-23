using UnityEngine;

public class SubmarineEscapeButton : MonoBehaviour, IInteractable
{
    [Header("Repair Manager")]
    [SerializeField] private SubmarineRepairManager repairManager;

    public void Interact(PlayerInteraction player)
    {
        if (repairManager == null)
        {
            Debug.LogWarning("Escape button has no SubmarineRepairManager assigned.");
            return;
        }

        repairManager.TryStartEscape();
    }
}
