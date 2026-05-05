using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public enum LobbyCodeValidationResult
{
    Valid = 0,
    Empty = 1,
    InvalidFormat = 2,
    VersionMismatch = 3
}

public class RelayManager : MonoBehaviour
{
    public static RelayManager Instance { get; private set; }
    public const string MultiplayerVersion = "1.0.0";

    private const char CodeSeparator = '|';

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    private async void Start()
    {
        await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
#if DURAK_VERBOSE_LOGS
            Debug.Log("Signed in anonymously as: " + AuthenticationService.Instance.PlayerId);
#endif
        }
    }

    public async Task<string> CreateRelay(int maxPlayers)
    {
        try
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            if (networkManager == null)
            {
                Debug.LogError("[RelayManager] NetworkManager.Singleton is null.");
                return null;
            }

            UnityTransport transport = EnsureUnityTransport(networkManager);
            if (transport == null)
            {
                return null;
            }

            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers);
            string relayJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            transport.SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );

            bool started = networkManager.StartHost();
            if (!started)
            {
                Debug.LogError("[RelayManager] Failed to start host.");
                return null;
            }
#if DURAK_VERBOSE_LOGS
            Debug.Log("Room Code: " + relayJoinCode);
#endif
            return relayJoinCode;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError(e);
            return null;
        }
    }

    public static string BuildVersionedLobbyCode(string relayJoinCode)
    {
        return $"{MultiplayerVersion}{CodeSeparator}{relayJoinCode}";
    }

    public static LobbyCodeValidationResult TryParseVersionedLobbyCode(string input, out string relayJoinCode)
    {
        relayJoinCode = string.Empty;

        if (string.IsNullOrWhiteSpace(input))
        {
            return LobbyCodeValidationResult.Empty;
        }

        string trimmed = input.Trim();
        int separatorIndex = trimmed.IndexOf(CodeSeparator);

        if (separatorIndex < 0)
        {
            relayJoinCode = trimmed.ToUpperInvariant();
            return LobbyCodeValidationResult.Valid;
        }

        string[] parts = trimmed.Split(CodeSeparator);
        if (parts.Length != 2)
        {
            return LobbyCodeValidationResult.InvalidFormat;
        }

        string version = parts[0].Trim();
        relayJoinCode = parts[1].Trim().ToUpperInvariant();

        if (string.IsNullOrEmpty(version) || string.IsNullOrEmpty(relayJoinCode))
        {
            return LobbyCodeValidationResult.InvalidFormat;
        }

        if (!version.Equals(MultiplayerVersion))
        {
            relayJoinCode = string.Empty;
            return LobbyCodeValidationResult.VersionMismatch;
        }

        return LobbyCodeValidationResult.Valid;
    }

    public static string GetDisplayLobbyCode(string input)
    {
        LobbyCodeValidationResult result = TryParseVersionedLobbyCode(input, out string relayJoinCode);
        if (result == LobbyCodeValidationResult.Valid && !string.IsNullOrWhiteSpace(relayJoinCode))
        {
            return relayJoinCode;
        }

        return string.IsNullOrWhiteSpace(input) ? string.Empty : input.Trim();
    }

    public async void JoinRelay(string joinCode)
    {
        try
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            if (networkManager == null)
            {
                Debug.LogError("[RelayManager] NetworkManager.Singleton is null.");
                return;
            }

            UnityTransport transport = EnsureUnityTransport(networkManager);
            if (transport == null)
            {
                return;
            }

            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            transport.SetClientRelayData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData
            );

            bool started = networkManager.StartClient();
            if (!started)
            {
                Debug.LogError("[RelayManager] Failed to start client.");
            }
        }
        catch (RelayServiceException e)
        {
            Debug.LogError(e);
        }
    }

    private static UnityTransport EnsureUnityTransport(NetworkManager networkManager)
    {
        UnityTransport transport = networkManager.GetComponent<UnityTransport>();
        if (transport != null)
        {
            return transport;
        }

        transport = networkManager.gameObject.AddComponent<UnityTransport>();
        if (transport == null)
        {
            Debug.LogError("[RelayManager] UnityTransport is missing and could not be added.");
            return null;
        }

        Debug.LogWarning("[RelayManager] UnityTransport was missing and added automatically.");
        return transport;
    }
}

