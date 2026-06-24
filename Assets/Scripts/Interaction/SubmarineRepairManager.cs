using System.Collections.Generic;
using UnityEngine;

public class SubmarineRepairManager : MonoBehaviour
{
    [Header("Required Tasks")]
    [SerializeField]
    private SubmarineRepairTask[] requiredTasks =
    {
        SubmarineRepairTask.OxygenTankLeft,
        SubmarineRepairTask.OxygenTankRight,
        SubmarineRepairTask.PressureValve,
        SubmarineRepairTask.OilTank,
        SubmarineRepairTask.CodeBox,
        SubmarineRepairTask.VentilatorLeft,
        SubmarineRepairTask.VentilatorRight,
        SubmarineRepairTask.SteeringWheel,
        SubmarineRepairTask.AccessCard,
        SubmarineRepairTask.Battery,
        SubmarineRepairTask.PressureValveTurned,
        SubmarineRepairTask.VentCoverLeft,
    };

    [Header("Escape Result")]
    [SerializeField] private GameObject objectToEnableOnEscape;
    [SerializeField] private GameObject objectToDisableOnEscape;

    [Header("Debug")]
    [SerializeField] private bool printDebugLogs = true;

    private readonly HashSet<SubmarineRepairTask> completedTasks = new HashSet<SubmarineRepairTask>();
    private bool escapeStarted;

    private void Awake()
    {
        RestoreCompletedTasksFromGameState();
    }

    public bool IsEscapeReady
    {
        get
        {
            foreach (SubmarineRepairTask requiredTask in requiredTasks)
            {
                if (!completedTasks.Contains(requiredTask))
                {
                    return false;
                }
            }

            return true;
        }
    }

    public bool IsTaskComplete(SubmarineRepairTask task)
    {
        return completedTasks.Contains(task);
    }

    public void CompleteTask(SubmarineRepairTask task)
    {
        GameState.Instance.CompleteSubmarineTask(task);

        if (!completedTasks.Add(task))
        {
            return;
        }

        if (printDebugLogs)
        {
            Debug.Log("Submarine task completed: " + task);
        }
    }

    public bool TryStartEscape()
    {
        if (escapeStarted)
        {
            Debug.Log("Escape sequence already started.");
            return true;
        }

        if (!IsEscapeReady)
        {
            Debug.Log("Cannot start submarine yet. Repairs are still missing.");
            return false;
        }

        escapeStarted = true;

        if (objectToDisableOnEscape != null)
        {
            objectToDisableOnEscape.SetActive(false);
        }

        if (objectToEnableOnEscape != null)
        {
            objectToEnableOnEscape.SetActive(true);
        }

        Debug.Log("Submarine escape sequence started.");
        return true;
    }

    public void ClearTask(SubmarineRepairTask task)
    {
        GameState.Instance.ClearSubmarineTask(task);

        if (!completedTasks.Remove(task))
        {
            return;
        }

        if (printDebugLogs)
        {
            Debug.Log("Submarine task cleared: " + task);
        }
    }

    public int GetTaskProgress(SubmarineRepairTask task)
    {
        return GameState.Instance.GetSubmarineTaskProgress(task);
    }

    public void SetTaskProgress(SubmarineRepairTask task, int progress)
    {
        GameState.Instance.SetSubmarineTaskProgress(task, progress);
    }

    private void RestoreCompletedTasksFromGameState()
    {
        completedTasks.Clear();

        foreach (SubmarineRepairTask requiredTask in requiredTasks)
        {
            if (GameState.Instance.IsSubmarineTaskComplete(requiredTask))
            {
                completedTasks.Add(requiredTask);
            }
        }
    }
}
