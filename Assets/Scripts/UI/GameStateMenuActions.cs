using UnityEngine;
using UnityEngine.SceneManagement;

public class GameStateMenuActions : MonoBehaviour
{
    [SerializeField] private string startRunSceneName = "Map";
    [SerializeField] private string mainMenuSceneName = "StartScreen";

    public void StartNewRun()
    {
        GameState.ResetRun();
        LoadSceneIfAssigned(startRunSceneName);
    }

    public void ReturnToMainMenu()
    {
        GameState.EndRun();
        LoadSceneIfAssigned(mainMenuSceneName);
    }

    public void ResetCurrentRun()
    {
        GameState.ResetRun();
    }

    private void LoadSceneIfAssigned(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            return;
        }

        SceneManager.LoadScene(sceneName);
    }
}
