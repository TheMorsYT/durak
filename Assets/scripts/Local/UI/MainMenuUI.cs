using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [Header("Main Panels")]
    public GameObject panelStats;

    [Header("Profile")]
    public Image avatarDisplay;
    public Button btnAvatarLeft;
    public Button btnAvatarRight;

    [Header("Stats")]
    public TMP_Text textStatsSingle;
    public Button btnStatsOpen;
    public Button btnStatsClose;

    private int selectedAvatarIndex;
    private static readonly Vector2 AvatarSize = new Vector2(100f, 100f);

    private void Start()
    {
        ResolveOptionalReferences();
        BindButtons();
        SetupProfileUi();

        RefreshStatsTexts();
    }

    private void BindButtons()
    {
        BindClick(btnAvatarLeft, () => { PlayClickSound(); StepAvatar(-1); });
        BindClick(btnAvatarRight, () => { PlayClickSound(); StepAvatar(1); });
        BindClick(btnStatsOpen, OpenStatsPanel);
        BindClick(btnStatsClose, CloseStatsAndReturnToMainMenu);
    }

    private void ResolveOptionalReferences()
    {
        avatarDisplay ??= GameObject.Find("AvatarDisplay")?.GetComponent<Image>();

        btnAvatarLeft ??= FindOrCreateButton("Btn_AvatarLeft");
        btnAvatarRight ??= FindOrCreateButton("Btn_AvatarRight");
        btnStatsOpen ??= FindOrCreateButton("Btn_StatsOpen");
        btnStatsClose ??= FindOrCreateButton("Btn_StatsClose");

        panelStats ??= GameObject.Find("Panel_Stats");

        textStatsSingle ??= GameObject.Find("Text_Stats_SP")?.GetComponent<TMP_Text>();
    }

    private void SetupProfileUi()
    {
        selectedAvatarIndex = PlayerProfileStorage.GetAvatarIndex();
        ApplyAvatar(selectedAvatarIndex, false);
    }

    private void RefreshStatsTexts()
    {
        PlayerStatsSnapshot stats = PlayerProfileStorage.GetStatsSnapshot();
        bool isUkrainian = PlayerPrefs.GetInt("GameLanguage", 0) == 0;

        if (textStatsSingle != null)
        {
            textStatsSingle.text = isUkrainian
                ? $"Одиночна гра\nПеремог: {stats.SingleWins}\nПоразок: {stats.SingleLosses}\nНічиїх: {stats.SingleDraws}"
                : $"Singleplayer\nWins: {stats.SingleWins}\nLosses: {stats.SingleLosses}\nDraws: {stats.SingleDraws}";
        }


    }

    public void ForceRefreshStats()
    {
        RefreshStatsTexts();
    }

    public void OpenStatsPanel()
    {
        PlayClickSound();
        if (panelStats == null)
        {
            return;
        }

        panelStats.SetActive(true);
        ForceRefreshStats();
    }

    public void CloseStatsPanel()
    {
        PlayClickSound();
        SetActiveSafe(panelStats, false);
    }

    public void CloseStatsAndReturnToMainMenu()
    {
        PlayClickSound();
        SetActiveSafe(panelStats, false);

        MainMenuManager mainMenuManager = Object.FindFirstObjectByType<MainMenuManager>();
        if (mainMenuManager != null)
        {
            mainMenuManager.ShowMainMenu();
        }
    }

    public void OnBackToMenuFromStatsClicked()
    {
        CloseStatsAndReturnToMainMenu();
    }

    private void StepAvatar(int direction)
    {
        if (AvatarCatalog.Count == 0)
        {
            AvatarCatalog.ForceReload();
        }

        if (AvatarCatalog.Count == 0)
        {
            return;
        }

        ApplyAvatar(selectedAvatarIndex + direction, true);
    }

    private void ApplyAvatar(int avatarIndex, bool save)
    {
        int avatarCount = AvatarCatalog.Count;
        
        if (avatarCount <= 0)
        {
            if (avatarDisplay != null)
            {
                avatarDisplay.sprite = null;
            }

            return;
        }

        selectedAvatarIndex = ((avatarIndex % avatarCount) + avatarCount) % avatarCount;

        if (avatarDisplay != null)
        {
            avatarDisplay.sprite = AvatarCatalog.GetAt(selectedAvatarIndex);
            avatarDisplay.preserveAspect = true;
            RectTransform avatarRect = avatarDisplay.GetComponent<RectTransform>();
            if (avatarRect != null)
            {
                avatarRect.sizeDelta = AvatarSize;
            }
        }

        if (save)
        {
            PlayerProfileStorage.SetAvatarIndex(selectedAvatarIndex);
        }
    }

    private static Button FindOrCreateButton(string objectName)
    {
        GameObject target = GameObject.Find(objectName);
        if (target == null)
        {
            return null;
        }

        Button button = target.GetComponent<Button>();
        if (button != null)
        {
            return button;
        }

        button = target.AddComponent<Button>();
        Image image = target.GetComponent<Image>();
        if (image != null)
        {
            button.targetGraphic = image;
        }

        return button;
    }

    private static void BindClick(Button button, UnityAction action)
    {
        if (button == null || action == null)
        {
            return;
        }

        button.onClick.AddListener(action);
    }

    private void PlayClickSound()
    {
        SoundManager.Instance?.PlayClick();
    }

    private static void SetActiveSafe(GameObject target, bool value)
    {
        if (target == null)
        {
            return;
        }

        target.SetActive(value);
    }
}
