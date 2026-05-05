using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class PauseMenu : MonoBehaviour
{
    public GameObject pauseMenuUI;
    public GameObject confirmMenuUI;

    public GameObject settingsMenuUI;

    private bool isPaused = false;

    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;

        if (confirmMenuUI != null && confirmMenuUI.activeSelf)
        {
            HideConfirmMenu();
            return;
        }

        if (settingsMenuUI != null && settingsMenuUI.activeSelf)
        {
            CloseSettingsMenu();
            return;
        }

        if (isPaused) Resume();
        else Pause();
    }

    public void Pause()
    {
        PlayClick();
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;
    }

    public void Resume()
    {
        PlayClick();
        pauseMenuUI.SetActive(false);
        if (confirmMenuUI != null) confirmMenuUI.SetActive(false);
        if (settingsMenuUI != null) settingsMenuUI.SetActive(false); 
        Time.timeScale = 1f;
        isPaused = false;
    }

    public void OpenSettingsMenu()
    {
        PlayClick();
        pauseMenuUI.SetActive(false);
        settingsMenuUI.SetActive(true);
    }

    public void CloseSettingsMenu()
    {
        PlayClick();
        settingsMenuUI.SetActive(false);
        pauseMenuUI.SetActive(true);
    }

    public void ShowConfirmMenu()
    {
        PlayClick();
        pauseMenuUI.SetActive(false);
        confirmMenuUI.SetActive(true);
    }

    public void HideConfirmMenu()
    {
        PlayClick();
        confirmMenuUI.SetActive(false);
        pauseMenuUI.SetActive(true);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        StartCoroutine(LoadMainMenuRoutine());
    }

    private IEnumerator LoadMainMenuRoutine()
    {
        SoundManager.Instance?.PlayTransition();
        yield return new WaitForSeconds(0.4f);
        SceneManager.LoadScene("MainMenu");
    }

    private static void PlayClick()
    {
        SoundManager.Instance?.PlayClick();
    }
}
