using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using System.Linq;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    public GameObject panelChoice;
    public GameObject panelHostSettings;
    public GameObject lobbyUI;
    public GameObject panelClient;

    public Button btnChooseHost;
    public Button btnChooseClient;

    public TMP_Dropdown dpdGameMode;
    public TMP_Dropdown dpdDeckSize;
    public TMP_Dropdown dpdMaxPlayers;
    public Button btnCreateRoom;
    public Button btnSettingsBack;
    public TMP_Text textError;

    public TMP_Text textRoomCode;
    public Button btnStartGame;
    public Button btnLobbyBack;

    public TMP_InputField inputJoinCode;
    public Button btnJoinRoom;
    public Button btnClientBack;

    private void Start()
    {
        LocalizeDropdowns();

        btnChooseHost.onClick.AddListener(() => { PlayClickSound(); ShowPanel(panelHostSettings); });
        btnChooseClient.onClick.AddListener(() => { PlayClickSound(); ShowPanel(panelClient); });

        btnSettingsBack.onClick.AddListener(() => { PlayClickSound(); ShowPanel(panelChoice); });
        btnClientBack.onClick.AddListener(() => { PlayClickSound(); ShowPanel(panelChoice); });

        btnLobbyBack.onClick.AddListener(() => { LeaveLobbySafe(); });

        btnCreateRoom.onClick.AddListener(() => { PlayClickSound(); OnCreateRoomClicked(); });
        btnJoinRoom.onClick.AddListener(() => { PlayClickSound(); OnJoinRoomClicked(); });
        btnStartGame.onClick.AddListener(() => { PlayClickSound(); StartGame(); });

        if (textRoomCode.GetComponent<Button>() != null)
        {
            textRoomCode.GetComponent<Button>().onClick.AddListener(() => { PlayClickSound(); CopyCode(); });
        }
    }

    private void Update()
    {
        if (lobbyUI != null && lobbyUI.activeSelf && NetworkManager.Singleton != null)
        {
            if (!NetworkManager.Singleton.IsServer && !NetworkManager.Singleton.IsConnectedClient)
            {
                NetworkManager.Singleton.Shutdown();
                ShowPanel(panelChoice);
                if (textError != null) textError.text = "Хост закрив лоббі!";
            }
        }
    }

    private void PlayClickSound()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayClick();
    }

    public void OpenMultiplayerMenu()
    {
        PlayClickSound();
        ShowPanel(panelChoice);
    }

    private void LocalizeDropdowns()
    {
        int lang = PlayerPrefs.GetInt("GameLanguage", 0);

        dpdGameMode.ClearOptions();
        List<string> modeOptions = new List<string>();

        if (lang == 0)
        {
            modeOptions.Add("Підкидний");
            modeOptions.Add("Переводний");
        }
        else
        {
            modeOptions.Add("Throw-in");
            modeOptions.Add("Passing");
        }
        dpdGameMode.AddOptions(modeOptions);
    }

    private void ShowPanel(GameObject p)
    {
        panelChoice.SetActive(false);
        panelHostSettings.SetActive(false);
        lobbyUI.SetActive(false);
        panelClient.SetActive(false);
        if (textError != null) textError.text = "";
        p.SetActive(true);
    }

    private async void OnCreateRoomClicked()
    {
        btnCreateRoom.interactable = false;

        int deckIndex = dpdDeckSize.value;
        int actualDeckSize = 36;
        if (deckIndex == 0) actualDeckSize = 24;
        else if (deckIndex == 1) actualDeckSize = 36;
        else if (deckIndex == 2) actualDeckSize = 52;
        else if (deckIndex == 3) actualDeckSize = 54;

        int maxPlayers = dpdMaxPlayers.value + 2;
        int lang = PlayerPrefs.GetInt("GameLanguage", 0);

        if (actualDeckSize == 24 && maxPlayers > 3)
        {
            if (textError != null) textError.text = lang == 0 ? "Для 24 карт максимум 3 гравці!" : "Max 3 players for 24 cards!";
            btnCreateRoom.interactable = true;
            return;
        }
        if (actualDeckSize == 36 && maxPlayers > 4)
        {
            if (textError != null) textError.text = lang == 0 ? "Для 36 карт максимум 4 гравці!" : "Max 4 players for 36 cards!";
            btnCreateRoom.interactable = true;
            return;
        }

        if (textError != null) textError.text = "Створення...";

        PlayerPrefs.SetInt("GameMode", dpdGameMode.value == 0 ? 0 : 1);
        PlayerPrefs.SetInt("DeckSize", actualDeckSize);

        string code = await RelayManager.Instance.CreateRelay(maxPlayers);

        if (!string.IsNullOrEmpty(code))
        {
            textRoomCode.text = code;
            ShowPanel(lobbyUI);
            btnStartGame.gameObject.SetActive(true);
        }
        else
        {
            if (textError != null) textError.text = lang == 0 ? "Помилка підключення!" : "Connection Error!";
        }

        btnCreateRoom.interactable = true;
    }

    private async void OnJoinRoomClicked()
    {
        if (!string.IsNullOrEmpty(inputJoinCode.text))
        {
            btnJoinRoom.interactable = false;
            if (textError != null) textError.text = "Підключення...";

            RelayManager.Instance.JoinRelay(inputJoinCode.text);

            int waitCount = 0;
            while (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsConnectedClient && waitCount < 80)
            {
                await Task.Delay(100);
                waitCount++;
            }

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
            {
                if (textRoomCode != null) textRoomCode.text = inputJoinCode.text;
                ShowPanel(lobbyUI);
                btnStartGame.gameObject.SetActive(false);
                if (textError != null) textError.text = "";
            }
            else
            {
                if (textError != null) textError.text = "Помилка: не знайдено код або повна кімната!";
                if (NetworkManager.Singleton != null) NetworkManager.Singleton.Shutdown();
            }

            btnJoinRoom.interactable = true;
        }
    }

    private void StartGame()
    {
        if (NetworkManager.Singleton.IsServer)
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
                var clients = NetworkManager.Singleton.ConnectedClientsIds.ToList();
                foreach (var id in clients)
                {
                    if (id != NetworkManager.Singleton.LocalClientId)
                        NetworkManager.Singleton.DisconnectClient(id);
                }
                await Task.Delay(400);
            }
            NetworkManager.Singleton.Shutdown();
        }
        ShowPanel(panelChoice);
    }

    public void CopyCode()
    {
        GUIUtility.systemCopyBuffer = textRoomCode.text;
    }
}