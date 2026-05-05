using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject mainMenuPanel;
    public GameObject aboutPanel;
    public GameObject modeSelectionPanel;
    public GameObject cardSelectionPanel;
    public GameObject difficultySelectionPanel;
    public GameObject settingsPanel;

    [Header("Exit Panel")]
    public GameObject confirmExitPanel;

    private int selectedGameMode;
    private int selectedDeckSize;

    void Start()
    {
        ShowMainMenu();
    }

    public void ShowMainMenu()
    {
        mainMenuPanel.SetActive(true);
        aboutPanel.SetActive(false);
        modeSelectionPanel.SetActive(false);

        if (cardSelectionPanel != null) cardSelectionPanel.SetActive(false);

        difficultySelectionPanel.SetActive(false);

        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (confirmExitPanel != null) confirmExitPanel.SetActive(false);
    }

    public void OpenSettingsWindow()
    {
        PlayClick();
        settingsPanel.SetActive(true);
    }

    public void CloseSettingsWindow()
    {
        PlayClick();
        settingsPanel.SetActive(false);
    }

    public void OpenAboutWindow()
    {
        PlayClick();
        aboutPanel.SetActive(true);
    }

    public void CloseAboutWindow()
    {
        PlayClick();
        aboutPanel.SetActive(false);
    }

    public void OnPlayButtonClicked()
    {
        PlayClick();
        mainMenuPanel.SetActive(false);
        modeSelectionPanel.SetActive(true);
    }

    public void OnBackToMainMenuFromMode()
    {
        PlayClick();
        modeSelectionPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    public void SelectGameMode(int mode)
    {
        PlayClick();
        selectedGameMode = mode;
        modeSelectionPanel.SetActive(false);
        cardSelectionPanel.SetActive(true);
    }

    public void OnBackToModeFromCardSelection()
    {
        PlayClick();
        cardSelectionPanel.SetActive(false);
        modeSelectionPanel.SetActive(true);
    }

    public void SelectDeckSize(int deckSize)
    {
        PlayClick();
        selectedDeckSize = deckSize;
        cardSelectionPanel.SetActive(false);
        difficultySelectionPanel.SetActive(true);
    }

    public void OnBackToCardSelectionFromDifficulty()
    {
        PlayClick();
        difficultySelectionPanel.SetActive(false);
        cardSelectionPanel.SetActive(true);
    }

    public void SelectDifficulty(int difficultyLevel)
    {
        PlayClick();

        PlayerPrefs.SetInt("GameMode", selectedGameMode);
        PlayerPrefs.SetInt("DeckSize", selectedDeckSize);
        PlayerPrefs.SetInt("BotDifficulty", difficultyLevel);
        PlayerPrefs.Save();

        StartGame();
    }

    public void ShowConfirmExit()
    {
        PlayClick();
        confirmExitPanel.SetActive(true);
    }

    public void HideConfirmExit()
    {
        PlayClick();
        confirmExitPanel.SetActive(false);
    }

    public void ConfirmQuit()
    {
        PlayClick();
        Application.Quit();
    }

    private void StartGame()
    {
        StartCoroutine(LoadGameRoutine());
    }

    private IEnumerator LoadGameRoutine()
    {
        SoundManager.Instance?.PlayTransition();
        yield return new WaitForSeconds(0.4f);
        SceneManager.LoadScene("game");
    }

    private static void PlayClick()
    {
        SoundManager.Instance?.PlayClick();
    }
}
