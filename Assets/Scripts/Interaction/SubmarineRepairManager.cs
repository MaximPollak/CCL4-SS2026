using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        SubmarineRepairTask.SteeringWheel,
        SubmarineRepairTask.AccessCard,
        SubmarineRepairTask.Battery,
        SubmarineRepairTask.PressureValveTurned,
        SubmarineRepairTask.VentCoverLeft,
    };

    [Header("Escape Result")]
    [SerializeField] private string surviveSceneName = "SurviveScene";
    [SerializeField] private bool useLoadingScreenOnEscape = false;
    [SerializeField] private float escapeLoadingScreenTime = 0.5f;
    [SerializeField] private GameObject objectToEnableOnEscape;
    [SerializeField] private GameObject objectToDisableOnEscape;

    [Header("Debug")]
    [SerializeField] private bool debugTreatAllTasksComplete = false;
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
            if (debugTreatAllTasksComplete)
            {
                return true;
            }

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
            Debug.Log("Cannot start submarine yet. Repairs are still missing: " + GetMissingRequiredTasksText());
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
        LoadSurviveScene();
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

    private void LoadSurviveScene()
    {
        if (string.IsNullOrWhiteSpace(surviveSceneName))
        {
            Debug.LogWarning("Cannot finish submarine escape because no survive scene name is assigned.", this);
            return;
        }

        // Final escape is allowed only after all required tasks, or the debug all-done override, pass.
        if (printDebugLogs)
        {
            Debug.Log("Loading survive scene: " + surviveSceneName, this);
        }

        if (useLoadingScreenOnEscape && !SceneLoadingScreen.IsLoading)
        {
            SceneLoadingScreen.LoadSceneWithScreen(surviveSceneName, escapeLoadingScreenTime);
            return;
        }

        SceneManager.LoadScene(surviveSceneName);
    }

    private string GetMissingRequiredTasksText()
    {
        if (debugTreatAllTasksComplete)
        {
            return "none (debug override is enabled)";
        }

        List<string> missingTasks = new List<string>();

        foreach (SubmarineRepairTask requiredTask in requiredTasks)
        {
            if (!completedTasks.Contains(requiredTask))
            {
                missingTasks.Add(requiredTask.ToString());
            }
        }

        return missingTasks.Count == 0 ? "none" : string.Join(", ", missingTasks);
    }
}
