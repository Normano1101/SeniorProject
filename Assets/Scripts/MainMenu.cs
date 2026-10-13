
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void PlayGame()
    {
        // Make sure gameplay is not paused
        Time.timeScale = 1f;

        // Load the tower defense map
        SceneManager.LoadScene("Map1");
    }

    public void QuitGame()
    {
        Debug.Log("Quitting Game...");

        Application.Quit();
    }
}
