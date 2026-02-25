using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public GameObject pauseMenuUI;
    public GameObject confirmMenuUI;

    private bool isPaused = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Якщо відкрито вікно підтвердження, Escape повертає до основного меню паузи
            if (confirmMenuUI.activeSelf)
            {
                HideConfirmMenu();
            }
            else if (isPaused)
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
        Time.timeScale = 0f; // Зупиняє час (і бота)
        isPaused = true;
    }

    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        confirmMenuUI.SetActive(false);
        Time.timeScale = 1f; // Відновлює час
        isPaused = false;
    }

    public void ShowConfirmMenu()
    {
        pauseMenuUI.SetActive(false);
        confirmMenuUI.SetActive(true);
    }

    public void HideConfirmMenu()
    {
        confirmMenuUI.SetActive(false);
        pauseMenuUI.SetActive(true);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f; // Обов'язково відновлюємо час перед завантаженням сцени
        SceneManager.LoadScene("MainMenu");
    }
}