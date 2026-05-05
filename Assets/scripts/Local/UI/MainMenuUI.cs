using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [Header("Main Panels")]
    public GameObject UnvalaibleMultiplayer;
    public GameObject panelChoice;
    public GameObject panelHostSettings;
    public GameObject lobbyUI;
    public GameObject panelClient;

    [Header("Selection Buttons")]
    public Button btnChooseHost;
    public Button btnChooseClient;

    [Header("Host Settings")]
    public TMP_Dropdown dpdGameMode;
    public TMP_Dropdown dpdDeckSize;
    public TMP_Dropdown dpdMaxPlayers;
    public Button btnCreateRoom;
    public Button btnSettingsBack;
    public TMP_Text textError;

    [Header("Lobby")]
    public TMP_Text textRoomCode;
    public Button btnStartGame;
    public Button btnLobbyBack;

    [Header("Client")]
    public TMP_InputField inputJoinCode;
    public Button btnJoinRoom;
    public Button btnClientBack;

    [Header("Profile")]
    public TMP_InputField nicknameInput;
    public Image avatarDisplay;
    public Button btnAvatarLeft;
    public Button btnAvatarRight;

    [Header("Stats")]
    public GameObject panelStats;
    public TMP_Text textStatsSingle;
    public TMP_Text textStatsMulti;
    public Button btnStatsOpen;
    public Button btnStatsClose;

    [Header("Version")]
    public GameObject panelVersion;
    public Button btnVersionClose;

    private int selectedAvatarIndex;
    private Coroutine hideVersionPanelCoroutine;
    private NetworkManager observedNetworkManager;
    private bool networkCallbacksBound;

    private const float VersionPanelLifetimeSeconds = 2.5f;
    private static readonly Vector2 AvatarSize = new Vector2(100f, 100f);

    private void Start()
    {
        ResolveOptionalReferences();
        LocalizeDropdowns();
        BindButtons();
        SetupProfileUi();
        TryBindNetworkCallbacks();

        RefreshStatsTexts();
        ToggleVersionPanel(false);
    }

    private void OnEnable()
    {
        TryBindNetworkCallbacks();
    }

    private void OnDisable()
    {
        UnbindNetworkCallbacks();
    }

    private void OnDestroy()
    {
        UnbindNetworkCallbacks();
        if (nicknameInput != null)
        {
            nicknameInput.onEndEdit.RemoveListener(OnNicknameSubmitted);
        }
    }

    private void BindButtons()
    {
        BindClick(btnChooseHost, () => { PlayClickSound(); ShowPanel(panelHostSettings); });
        BindClick(btnChooseClient, () => { PlayClickSound(); ShowPanel(panelClient); });

        BindClick(btnSettingsBack, () => { PlayClickSound(); ShowPanel(panelChoice); });
        BindClick(btnClientBack, () => { PlayClickSound(); ShowPanel(panelChoice); });

        BindClick(btnLobbyBack, LeaveLobbySafe);

        BindClick(btnCreateRoom, () => { PlayClickSound(); OnCreateRoomClicked(); });
        BindClick(btnJoinRoom, () => { PlayClickSound(); OnJoinRoomClicked(); });
        BindClick(btnStartGame, () => { PlayClickSound(); StartGame(); });

        BindClick(btnAvatarLeft, () => { PlayClickSound(); StepAvatar(-1); });
        BindClick(btnAvatarRight, () => { PlayClickSound(); StepAvatar(1); });
        BindClick(btnStatsOpen, OpenStatsPanel);
        BindClick(btnStatsClose, CloseStatsAndReturnToMainMenu);
        BindClick(btnVersionClose, HideVersionPanel);

        Button codeButton = textRoomCode != null ? textRoomCode.GetComponent<Button>() : null;
        if (codeButton != null)
        {
            codeButton.onClick.AddListener(() => { PlayClickSound(); CopyCode(); });
        }
    }

    private void ResolveOptionalReferences()
    {
        nicknameInput ??= GameObject.Find("NicknameInput")?.GetComponent<TMP_InputField>();
        avatarDisplay ??= GameObject.Find("AvatarDisplay")?.GetComponent<Image>();

        btnAvatarLeft ??= FindOrCreateButton("Btn_AvatarLeft");
        btnAvatarRight ??= FindOrCreateButton("Btn_AvatarRight");
        btnStatsOpen ??= FindOrCreateButton("Btn_StatsOpen");
        btnStatsClose ??= FindOrCreateButton("Btn_StatsClose");
        btnVersionClose ??= FindOrCreateButton("Btn_VersionClose");

        panelStats ??= GameObject.Find("Panel_Stats");
        panelVersion ??= GameObject.Find("PanelVersion");

        textStatsSingle ??= GameObject.Find("Text_Stats_SP")?.GetComponent<TMP_Text>();
        textStatsMulti ??= GameObject.Find("Text_Stats_MP")?.GetComponent<TMP_Text>();
    }

    private void SetupProfileUi()
    {
        selectedAvatarIndex = PlayerProfileStorage.GetAvatarIndex();
        ApplyAvatar(selectedAvatarIndex, false);

        if (nicknameInput == null)
        {
            return;
        }

        nicknameInput.characterLimit = PlayerProfileStorage.MaxNicknameLength;
        nicknameInput.SetTextWithoutNotify(PlayerProfileStorage.GetNickname());
        nicknameInput.onEndEdit.AddListener(OnNicknameSubmitted);
    }

    private void OnNicknameSubmitted(string rawValue)
    {
        string nickname = PlayerProfileStorage.SanitizeNickname(rawValue);
        PlayerProfileStorage.SetNickname(nickname);

        if (nicknameInput != null && nicknameInput.text != nickname)
        {
            nicknameInput.SetTextWithoutNotify(nickname);
        }
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

        if (textStatsMulti != null)
        {
            textStatsMulti.text = isUkrainian
                ? $"Мультиплеєр\nПеремог: {stats.MultiWins}\nПоразок: {stats.MultiLosses}\nНічиїх: {stats.MultiDraws}"
                : $"Multiplayer\nWins: {stats.MultiWins}\nLosses: {stats.MultiLosses}\nDraws: {stats.MultiDraws}";
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

        ShowPanel(null);

        MainMenuManager mainMenuManager = Object.FindFirstObjectByType<MainMenuManager>();
        if (mainMenuManager != null)
        {
            mainMenuManager.ShowMainMenu();
            return;
        }

        ShowPanel(panelChoice);
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

    private void TryBindNetworkCallbacks()
    {
        NetworkManager current = NetworkManager.Singleton;
        if (current == null)
        {
            return;
        }

        if (networkCallbacksBound && observedNetworkManager == current)
        {
            return;
        }

        UnbindNetworkCallbacks();
        observedNetworkManager = current;
        observedNetworkManager.OnClientDisconnectCallback += OnClientDisconnected;
        networkCallbacksBound = true;
    }

    private void UnbindNetworkCallbacks()
    {
        if (!networkCallbacksBound || observedNetworkManager == null)
        {
            observedNetworkManager = null;
            networkCallbacksBound = false;
            return;
        }

        observedNetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
        observedNetworkManager = null;
        networkCallbacksBound = false;
    }

    private void OnClientDisconnected(ulong clientId)
    {
        NetworkManager manager = NetworkManager.Singleton;
        if (manager == null || manager.IsServer || clientId != manager.LocalClientId)
        {
            return;
        }

        if (lobbyUI == null || !lobbyUI.activeSelf)
        {
            return;
        }

        manager.Shutdown();
        ShowPanel(panelChoice);

        if (textError != null)
        {
            textError.text = Localized("Хост закрив лобі!", "Host closed the lobby!");
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

    public void OpenMultiplayerMenu()
    {
        PlayClickSound();
        //ShowPanel(panelChoice);
        ShowPanel(UnvalaibleMultiplayer);
    }

    public void CloseUnvalaibleMultiplayerToMainMenu()
    {
        PlayClickSound();

        ShowPanel(null);

        MainMenuManager mainMenuManager = Object.FindFirstObjectByType<MainMenuManager>();
        if (mainMenuManager != null)
        {
            mainMenuManager.ShowMainMenu();
        }
    }

    public void ReturnToPanelClient()
    {
        PlayClickSound();
        ShowPanel(panelClient);
    }

    private void LocalizeDropdowns()
    {
        int lang = PlayerPrefs.GetInt("GameLanguage", 0);

        dpdGameMode.ClearOptions();
        List<string> modeOptions = new List<string>
        {
            lang == 0 ? "Підкидний" : "Throw-in",
            lang == 0 ? "Переводний" : "Passing"
        };

        dpdGameMode.AddOptions(modeOptions);
    }

    private void ShowPanel(GameObject panelToShow)
    {
        SetActiveSafe(UnvalaibleMultiplayer, false);
        SetActiveSafe(panelChoice, false);
        SetActiveSafe(panelHostSettings, false);
        SetActiveSafe(lobbyUI, false);
        SetActiveSafe(panelClient, false);
        SetActiveSafe(panelStats, false);

        SetTextSafe(textError, string.Empty);
        SetActiveSafe(panelToShow, true);

        ToggleVersionPanel(false);
    }

    private async void OnCreateRoomClicked()
    {
        SetInteractableSafe(btnCreateRoom, false);

        int[] deckSizes = { 24, 36, 52, 54 };
        int deckIndex = Mathf.Clamp(dpdDeckSize != null ? dpdDeckSize.value : 1, 0, deckSizes.Length - 1);
        int actualDeckSize = deckSizes[deckIndex];

        int maxPlayers = (dpdMaxPlayers != null ? dpdMaxPlayers.value : 0) + 2;
        int lang = PlayerPrefs.GetInt("GameLanguage", 0);

        if ((actualDeckSize == 24 && maxPlayers > 3) || (actualDeckSize == 36 && maxPlayers > 4))
        {
            if (textError != null)
            {
                textError.text = actualDeckSize == 24
                    ? Localized("Для 24 карт максимум 3 гравці!", "Max 3 players for 24 cards!")
                    : Localized("Для 36 карт максимум 4 гравці!", "Max 4 players for 36 cards!");
            }

            SetInteractableSafe(btnCreateRoom, true);
            return;
        }

        SetTextSafe(textError, Localized("Створення...", "Creating..."));

        if (dpdGameMode != null)
        {
            PlayerPrefs.SetInt("GameMode", dpdGameMode.value == 0 ? 0 : 1);
        }

        PlayerPrefs.SetInt("DeckSize", actualDeckSize);
        PlayerPrefs.SetInt("HostMaxPlayers", maxPlayers);
        PlayerPrefs.SetInt("HostRequiredPlayersToStart", maxPlayers);

        string code = await RelayManager.Instance.CreateRelay(maxPlayers);
        TryBindNetworkCallbacks();

        if (!string.IsNullOrEmpty(code))
        {
            SetTextSafe(textRoomCode, RelayManager.GetDisplayLobbyCode(code));
            ShowPanel(lobbyUI);
            SetActiveSafe(btnStartGame != null ? btnStartGame.gameObject : null, true);
        }
        else if (textError != null)
        {
            textError.text = lang == 0 ? "Помилка підключення!" : "Connection Error!";
        }

        SetInteractableSafe(btnCreateRoom, true);
    }

    private async void OnJoinRoomClicked()
    {
        if (string.IsNullOrWhiteSpace(inputJoinCode != null ? inputJoinCode.text : string.Empty))
        {
            if (textError != null)
            {
                textError.text = Localized("Введіть код лобі!", "Enter lobby code!");
            }

            return;
        }

        SetInteractableSafe(btnJoinRoom, false);

        LobbyCodeValidationResult validation = RelayManager.TryParseVersionedLobbyCode(inputJoinCode.text, out string relayJoinCode);
        if (validation != LobbyCodeValidationResult.Valid)
        {
            ShowJoinValidationError(validation);
            SetInteractableSafe(btnJoinRoom, true);
            return;
        }

        PlayerPrefs.SetString("JoinLobbyCode", relayJoinCode);

        SetTextSafe(textError, Localized("Підключення...", "Connecting..."));

        RelayManager.Instance.JoinRelay(relayJoinCode);
        TryBindNetworkCallbacks();

        int waitCount = 0;
        while (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsConnectedClient && waitCount < 80)
        {
            await Task.Delay(100);
            waitCount++;
        }

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
        {
            SetTextSafe(textRoomCode, relayJoinCode);
            ShowPanel(lobbyUI);
            SetActiveSafe(btnStartGame != null ? btnStartGame.gameObject : null, false);
            SetTextSafe(textError, string.Empty);
        }
        else
        {
            if (textError != null)
            {
                textError.text = Localized(
                    "Помилка: код не знайдено або лобі повне!",
                    "Error: code not found or lobby is full!");
            }

            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.Shutdown();
            }

            UnbindNetworkCallbacks();
        }

        SetInteractableSafe(btnJoinRoom, true);
    }

    private void ShowJoinValidationError(LobbyCodeValidationResult validation)
    {
        if (validation == LobbyCodeValidationResult.VersionMismatch)
        {
            if (textError != null)
            {
                textError.text = Localized(
                    "Версія гри не співпадає з лобі.",
                    "Game version does not match the lobby.");
            }

            ToggleVersionPanel(true);
            return;
        }

        if (textError != null)
        {
            textError.text = Localized(
                "Невірний код лобі.",
                "Invalid lobby code.");
        }
    }

    private void ToggleVersionPanel(bool visible)
    {
        if (panelVersion == null)
        {
            return;
        }

        panelVersion.SetActive(visible);

        if (!visible)
        {
            if (hideVersionPanelCoroutine != null)
            {
                StopCoroutine(hideVersionPanelCoroutine);
                hideVersionPanelCoroutine = null;
            }

            return;
        }

        if (hideVersionPanelCoroutine != null)
        {
            StopCoroutine(hideVersionPanelCoroutine);
        }

        hideVersionPanelCoroutine = StartCoroutine(HideVersionPanelLater());
    }

    public void HideVersionPanel()
    {
        PlayClickSound();
        ToggleVersionPanel(false);
    }

    public void HandleLobbyVersionMismatch()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }

        UnbindNetworkCallbacks();
        ShowPanel(panelClient);

        if (textError != null)
        {
            textError.text = Localized(
                "Версія гри не співпадає з лобі.",
                "Game version does not match the lobby.");
        }

        ToggleVersionPanel(true);
    }

    private IEnumerator HideVersionPanelLater()
    {
        yield return new WaitForSecondsRealtime(VersionPanelLifetimeSeconds);
        SetActiveSafe(panelVersion, false);
        hideVersionPanelCoroutine = null;
    }

    private void StartGame()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            NetworkManager.Singleton.SceneManager.LoadScene("MultiplayerGame", LoadSceneMode.Single);
        }
    }

    private async void LeaveLobbySafe()
    {
        PlayClickSound();

        if (NetworkManager.Singleton != null)
        {
            if (NetworkManager.Singleton.IsServer)
            {
                List<ulong> clients = NetworkManager.Singleton.ConnectedClientsIds.ToList();
                foreach (ulong id in clients)
                {
                    if (id != NetworkManager.Singleton.LocalClientId)
                    {
                        NetworkManager.Singleton.DisconnectClient(id);
                    }
                }

                await Task.Delay(400);
            }

            NetworkManager.Singleton.Shutdown();
        }

        UnbindNetworkCallbacks();
        ShowPanel(panelChoice);
    }

    public void CopyCode()
    {
        if (textRoomCode == null)
        {
            return;
        }

        GUIUtility.systemCopyBuffer = RelayManager.GetDisplayLobbyCode(textRoomCode.text);
    }

    private static string Localized(string ukrainian, string english)
    {
        return PlayerPrefs.GetInt("GameLanguage", 0) == 0 ? ukrainian : english;
    }

    private static void SetActiveSafe(GameObject target, bool value)
    {
        if (target == null)
        {
            return;
        }

        target.SetActive(value);
    }

    private static void SetInteractableSafe(Selectable selectable, bool value)
    {
        if (selectable == null)
        {
            return;
        }

        selectable.interactable = value;
    }

    private static void SetTextSafe(TMP_Text text, string value)
    {
        if (text == null)
        {
            return;
        }

        text.text = value;
    }
}
