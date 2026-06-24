using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class SceneTransitionDoor : MonoBehaviour, IInteractable
{
    [Header("Scene")]
    [SerializeField] private string targetSceneName = "Submarine";
    [SerializeField] private string targetSpawnPointId = "";

    [Header("Debug")]
    [SerializeField] private bool printDebugLogs = true;

    public void Interact(PlayerInteraction player)
    {
        LoadTargetScene();
    }

    public void LoadTargetScene()
    {
        if (string.IsNullOrWhiteSpace(targetSceneName))
        {
            Debug.LogWarning("Scene transition has no target scene assigned.", this);
            return;
        }

        SceneSpawnPoint.SetPendingSpawnPoint(targetSpawnPointId);

        if (printDebugLogs)
        {
            Debug.Log($"Loading scene: {targetSceneName}", this);
        }

        SceneManager.LoadScene(targetSceneName);
    }
}
