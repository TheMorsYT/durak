using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI Панелі")]
    public GameObject mainMenuPanel;
    public GameObject aboutPanel;
    public GameObject modeSelectionPanel;
    public GameObject difficultySelectionPanel;

    [Header("Панель виходу")]
    public GameObject confirmExitPanel;

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
        difficultySelectionPanel.SetActive(false);


        if (confirmExitPanel != null) confirmExitPanel.SetActive(false);
    }

    public void OpenAboutWindow() { aboutPanel.SetActive(true); }
    public void CloseAboutWindow() { aboutPanel.SetActive(false); }

    public void OnPlayButtonClicked()
    {
        mainMenuPanel.SetActive(false);
        modeSelectionPanel.SetActive(true);
    }

    public void OnBackToMainMenuFromMode()
    {
        modeSelectionPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    public void OnBackToModeFromDifficulty()
    {
        difficultySelectionPanel.SetActive(false);
        modeSelectionPanel.SetActive(true);
    }

    public void SelectMode(int deckSize)
    {
        selectedDeckSize = deckSize;
        modeSelectionPanel.SetActive(false);
        difficultySelectionPanel.SetActive(true);
    }

    public void SelectDifficulty(int difficultyLevel)
    {
        PlayerPrefs.SetInt("DeckSize", selectedDeckSize);
        PlayerPrefs.SetInt("BotDifficulty", difficultyLevel);
        PlayerPrefs.Save();
        StartGame();
    }

    public void ShowConfirmExit()
    {
        confirmExitPanel.SetActive(true);
    }


    public void HideConfirmExit()
    {
        confirmExitPanel.SetActive(false);
    }


    public void ConfirmQuit()
    {
        Debug.Log("Вихід на робочий стіл...");
        Application.Quit();
    }

    private void StartGame()
    {
        SceneManager.LoadScene("game");
    }
}