using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuActions : MonoBehaviour
{
    public void StartGame()
    {
        // Starting from the menu begins a completely new playthrough.
        GameState.ResetRun();
        SceneManager.LoadScene("Map");
    }


    public void BackToMenu(){
        SceneManager.LoadScene("StartScreen");
    }

    public void Guide(){
        SceneManager.LoadScene("GuideScene");
    }

    public void StartToStoryScene()
    {
        SceneManager.LoadScene("StoryScene");
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
