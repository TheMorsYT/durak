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
        if (SoundManager.Instance != null) SoundManager.Instance.PlayClick();
        settingsPanel.SetActive(true);
    }

    public void CloseSettingsWindow()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayClick();
        settingsPanel.SetActive(false);
    }

    public void OpenAboutWindow()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayClick();
        aboutPanel.SetActive(true);
    }

    public void CloseAboutWindow()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayClick();
        aboutPanel.SetActive(false);
    }

    public void OnPlayButtonClicked()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayClick();
        mainMenuPanel.SetActive(false);
        modeSelectionPanel.SetActive(true);
    }

    public void OnBackToMainMenuFromMode()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayClick();
        modeSelectionPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    public void SelectGameMode(int mode)
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayClick();
        selectedGameMode = mode;
        modeSelectionPanel.SetActive(false);
        cardSelectionPanel.SetActive(true);
    }

    public void OnBackToModeFromCardSelection()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayClick();
        cardSelectionPanel.SetActive(false);
        modeSelectionPanel.SetActive(true);
    }

    public void SelectDeckSize(int deckSize)
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayClick();
        selectedDeckSize = deckSize;
        cardSelectionPanel.SetActive(false);
        difficultySelectionPanel.SetActive(true);
    }

    public void OnBackToCardSelectionFromDifficulty()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayClick();
        difficultySelectionPanel.SetActive(false);
        cardSelectionPanel.SetActive(true);
    }

    public void SelectDifficulty(int difficultyLevel)
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayClick();

        PlayerPrefs.SetInt("GameMode", selectedGameMode);
        PlayerPrefs.SetInt("DeckSize", selectedDeckSize);
        PlayerPrefs.SetInt("BotDifficulty", difficultyLevel);
        PlayerPrefs.Save();

        StartGame();
    }

    public void ShowConfirmExit()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayClick();
        confirmExitPanel.SetActive(true);
    }

    public void HideConfirmExit()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayClick();
        confirmExitPanel.SetActive(false);
    }

    public void ConfirmQuit()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayClick();
        Application.Quit();
    }

    private void StartGame()
    {
        StartCoroutine(LoadGameRoutine());
    }

    private IEnumerator LoadGameRoutine()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayTransition();
        yield return new WaitForSeconds(0.4f);
        SceneManager.LoadScene("game");
    }
}