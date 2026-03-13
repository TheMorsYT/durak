using UnityEngine;
using Unity.Netcode;
using TMPro;

public class LobbyManager : NetworkBehaviour
{
    public Transform playerListContainer;
    public GameObject playerPrefab;

    public NetworkVariable<int> playersCount = new NetworkVariable<int>(0);

    public override void OnNetworkSpawn()
    {
        playersCount.OnValueChanged += (oldValue, newValue) => {
            RedrawLobby(newValue);
        };

        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += UpdateCount;
            NetworkManager.Singleton.OnClientDisconnectCallback += UpdateCount;
            UpdateCount(0);
        }

        RedrawLobby(playersCount.Value);
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= UpdateCount;
            NetworkManager.Singleton.OnClientDisconnectCallback -= UpdateCount;
        }
    }

    private void UpdateCount(ulong clientId)
    {
        if (IsServer)
        {
            playersCount.Value = NetworkManager.Singleton.ConnectedClientsList.Count;
        }
    }

    private void RedrawLobby(int count)
    {
        foreach (Transform child in playerListContainer)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < count; i++)
        {
            GameObject entry = Instantiate(playerPrefab, playerListContainer);
            TMP_Text textComponent = entry.GetComponent<TMP_Text>();

            if (textComponent != null)
            {
                if (i == 0) textComponent.text = "Player 0 (Host)";
                else textComponent.text = "Player " + i;
            }
        }
    }
}