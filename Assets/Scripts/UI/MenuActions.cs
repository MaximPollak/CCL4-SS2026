using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuActions : MonoBehaviour
{
    public void StartGame()
    {

        SceneManager.LoadScene("Map");
        
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
