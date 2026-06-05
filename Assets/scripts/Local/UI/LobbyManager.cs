using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class LobbyManager : NetworkBehaviour
{
    private const string MultiplayerVersion = "1.0.0";
    public Transform playerListContainer;
    public GameObject playerPrefab;

    public NetworkVariable<int> playersCount = new NetworkVariable<int>(0);

    private readonly NetworkList<FixedString64Bytes> playerNames = new NetworkList<FixedString64Bytes>();
    private readonly NetworkVariable<FixedString32Bytes> lobbyVersion = new NetworkVariable<FixedString32Bytes>();

    private readonly Dictionary<ulong, string> submittedNicknames = new Dictionary<ulong, string>();
    private bool versionMismatchHandled;

    public override void OnNetworkSpawn()
    {
        playerNames.OnListChanged += OnPlayerNamesChanged;
        lobbyVersion.OnValueChanged += OnLobbyVersionChanged;

        if (IsServerActive())
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

            lobbyVersion.Value = new FixedString32Bytes(MultiplayerVersion);
            submittedNicknames[NetworkManager.Singleton.LocalClientId] = PlayerProfileStorage.GetNickname();

            UpdateRoster();
        }

        SubmitLocalNickname();
        ValidateLobbyVersion();
        RedrawLobby();
    }

    public override void OnNetworkDespawn()
    {
        playerNames.OnListChanged -= OnPlayerNamesChanged;
        lobbyVersion.OnValueChanged -= OnLobbyVersionChanged;

        if (IsServerActive())
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }

        submittedNicknames.Clear();
    }

    private void SubmitLocalNickname()
    {
        string nickname = PlayerProfileStorage.GetNickname();

        if (IsServerActive())
        {
            submittedNicknames[NetworkManager.Singleton.LocalClientId] = nickname;
            UpdateRoster();
            return;
        }

        SubmitNicknameServerRpc(nickname);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SubmitNicknameServerRpc(string nickname, RpcParams rpcParams = default)
    {
        string sanitizedNickname = PlayerProfileStorage.SanitizeNickname(nickname);
        ulong senderClientId = rpcParams.Receive.SenderClientId;

        submittedNicknames[senderClientId] = sanitizedNickname;
        UpdateRoster();
    }

    private void OnClientConnected(ulong clientId)
    {
        UpdateRoster();
    }

    private void OnClientDisconnected(ulong clientId)
    {
        submittedNicknames.Remove(clientId);
        UpdateRoster();
    }

    private void UpdateRoster()
    {
        if (!IsServerActive())
        {
            return;
        }

        playerNames.Clear();

        int playerIndex = 0;
        foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
        {
            ulong clientId = client.ClientId;

            string nickname = ResolveNickname(clientId, playerIndex);
            playerNames.Add(new FixedString64Bytes(nickname));
            playerIndex++;
        }

        playersCount.Value = playerNames.Count;
    }

    private void OnPlayerNamesChanged(NetworkListEvent<FixedString64Bytes> changeEvent)
    {
        if (IsServer)
        {
            playersCount.Value = playerNames.Count;
        }

        RedrawLobby();
    }

    private void OnLobbyVersionChanged(FixedString32Bytes oldValue, FixedString32Bytes newValue)
    {
        ValidateLobbyVersion();
    }

    private void ValidateLobbyVersion()
    {
        if (IsServer || versionMismatchHandled)
        {
            return;
        }

        string serverVersion = lobbyVersion.Value.ToString();
        if (string.IsNullOrWhiteSpace(serverVersion))
        {
            return;
        }

        if (serverVersion.Equals(MultiplayerVersion))
        {
            return;
        }

        versionMismatchHandled = true;


        Debug.LogWarning($"[LobbyManager] Version mismatch! Server: {serverVersion}, Client: {MultiplayerVersion}");
        NetworkManager.Singleton?.Shutdown();
    }

    private void RedrawLobby()
    {
        if (playerListContainer == null || playerPrefab == null)
        {
            return;
        }

        foreach (Transform child in playerListContainer)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < playerNames.Count; i++)
        {
            GameObject entry = Instantiate(playerPrefab, playerListContainer);
            TMP_Text textComponent = entry.GetComponent<TMP_Text>();
            if (textComponent == null) continue;

            string nickname = playerNames[i].ToString();
            textComponent.text = i == 0 ? $"{nickname} (Host)" : nickname;
        }
    }

    private bool IsServerActive()
    {
        return IsServer && NetworkManager.Singleton != null;
    }

    private string ResolveNickname(ulong clientId, int playerIndex)
    {
        if (!submittedNicknames.TryGetValue(clientId, out string nickname))
        {
            return $"Player {playerIndex}";
        }

        return string.IsNullOrWhiteSpace(nickname) ? $"Player {playerIndex}" : nickname;
    }
}
