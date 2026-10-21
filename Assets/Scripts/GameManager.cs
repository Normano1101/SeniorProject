using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public GameObject gameOverMenu;

    private bool gameIsOver = false;

    public void GameOver()
    {
        if (gameIsOver)
            return;

        gameIsOver = true;

        PauseMenu pauseManager =
            FindAnyObjectByType<PauseMenu>();

        if (pauseManager != null)
        {
            pauseManager.OnGameOver();
        }

        if (gameOverMenu != null)
        {
            gameOverMenu.SetActive(true);
        }

        Time.timeScale = 0f;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.ResetCurrency();
        }

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().buildIndex
        );
    }

    public void MainMenu()
    {
        Time.timeScale = 1f;

        SceneManager.LoadScene("MainMenu");
    }
}
