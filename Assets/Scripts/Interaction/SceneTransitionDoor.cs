using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class SceneTransitionDoor : MonoBehaviour, IInteractable
{
    [Header("Scene")]
    [SerializeField] private string targetSceneName = "Submarine";
    [SerializeField] private string targetSpawnPointId = "";

    [Header("Loading Screen")]
    [SerializeField] private bool useMapSubmarineLoadingScreen = true;
    [SerializeField] private float mapSubmarineLoadingScreenTime = 2f;

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

        // Only Map <-> Submarine switches get the polish loading screen.
        if (ShouldUseMapSubmarineLoadingScreen())
        {
            SceneLoadingScreen.LoadSceneWithScreen(targetSceneName, mapSubmarineLoadingScreenTime);
            return;
        }

        SceneManager.LoadScene(targetSceneName);
    }

    private bool ShouldUseMapSubmarineLoadingScreen()
    {
        if (!useMapSubmarineLoadingScreen || SceneLoadingScreen.IsLoading)
        {
            return false;
        }

        string currentSceneName = SceneManager.GetActiveScene().name;

        return IsMapSubmarinePair(currentSceneName, targetSceneName);
    }

    private bool IsMapSubmarinePair(string currentSceneName, string nextSceneName)
    {
        return currentSceneName == "Map" && nextSceneName == "Submarine"
            || currentSceneName == "Submarine" && nextSceneName == "Map";
    }
}
