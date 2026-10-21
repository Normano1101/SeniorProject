using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    private static bool pauseAfterReload;

    public GameObject pauseMenuUI;

    private bool isPaused = false;
    private bool gameIsOver = false;

    void Awake()
    {
        if (!pauseAfterReload)
            return;

        pauseAfterReload = false;
        isPaused = true;
        Time.timeScale = 0f;
        pauseMenuUI.SetActive(true);
    }

    void Update()
    {
        if (gameIsOver)
            return;

        // Press ESC to pause or resume
        if (Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (isPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    public void Pause()
    {
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;
    }

    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;
    }

    public void Restart()
    {
        pauseAfterReload = true;
        Time.timeScale = 0f;
        SceneManager.LoadScene(
            SceneManager.GetActiveScene().name
        );
    }

    public void MainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    public void OnGameOver()
    {
        gameIsOver = true;
        isPaused = false;
        pauseMenuUI.SetActive(false);
    }
}