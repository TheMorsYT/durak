using System;
using UnityEngine;
using UnityEngine.SceneManagement;

#if DISCORD_RICH_PRESENCE
using DiscordRPC;
#endif

public sealed class DiscordRichPresenceManager : MonoBehaviour
{
    [SerializeField] private string applicationId = "1501304456837464184";
    [SerializeField] private bool persistBetweenScenes = true;
    [SerializeField] private bool enableLogs = true;

    private static DiscordRichPresenceManager instance;
    private float refreshTimer;

#if DISCORD_RICH_PRESENCE
    private DiscordRpcClient client;
    private DateTime startedAtUtc;
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null)
        {
            return;
        }

        GameObject root = new GameObject("DiscordRichPresenceManager");
        instance = root.AddComponent<DiscordRichPresenceManager>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        if (persistBetweenScenes)
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    private void OnEnable()
    {
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
        InitializeRpcIfNeeded();
        UpdatePresence(SceneManager.GetActiveScene().name);
    }

    private void Update()
    {
        refreshTimer -= Time.unscaledDeltaTime;
        if (refreshTimer > 0f)
        {
            return;
        }

        refreshTimer = 2f;
        UpdatePresence(SceneManager.GetActiveScene().name);
    }

    private void OnDisable()
    {
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }

#if DISCORD_RICH_PRESENCE
        if (client != null)
        {
            try
            {
                client.ClearPresence();
                client.Dispose();
            }
            catch
            {
            }

            client = null;
        }
#endif
    }

    private void OnActiveSceneChanged(Scene oldScene, Scene newScene)
    {
        UpdatePresence(newScene.name);
    }

    private void InitializeRpcIfNeeded()
    {
#if DISCORD_RICH_PRESENCE
        if (client != null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(applicationId))
        {
            Log("Discord RPC disabled: applicationId is empty.");
            return;
        }


        try
        {
            client = new DiscordRpcClient(applicationId.Trim());
            

            client.Logger = new DiscordRPC.Logging.NullLogger();
            
            client.Initialize();
            startedAtUtc = DateTime.UtcNow;
            Log($"Discord RPC initialized successfully on {Application.platform}.");
        }
        catch (Exception exception)
        {

            Log($"Discord RPC init failed (this is normal if Discord is not running): {exception.Message}");
            client = null;
        }
#else
        Log("DISCORD_RICH_PRESENCE define is missing. RPC is disabled.");
#endif
    }

    private void UpdatePresence(string sceneName)
    {
#if DISCORD_RICH_PRESENCE
        if (client == null)
        {
            return;
        }

        try
        {
            client.SetPresence(new RichPresence
            {
                Details = GetDetails(sceneName),
                State = GetState(sceneName),
                Timestamps = new Timestamps(startedAtUtc)
            });
        }
        catch (Exception exception)
        {
            Log($"Discord RPC update failed: {exception.Message}");
        }
#endif
    }

    private static string GetDetails(string sceneName)
    {
        bool isUkr = PlayerPrefs.GetInt("GameLanguage", 0) == 0;
        string nickname = PlayerProfileStorage.GetNickname();
        string version = string.IsNullOrWhiteSpace(Application.version) ? "dev" : Application.version;
        string playingAs = isUkr ? $"Грає як {nickname}" : $"Playing as {nickname}";

        if (string.Equals(sceneName, "MainMenu", StringComparison.OrdinalIgnoreCase))
        {
            return isUkr ? $"Головне меню v{version} | {playingAs}" : $"Main Menu v{version} | {playingAs}";
        }

        if (string.Equals(sceneName, "game", StringComparison.OrdinalIgnoreCase))
        {
            return isUkr ? $"Одиночна гра v{version} | {playingAs}" : $"Singleplayer v{version} | {playingAs}";
        }

        return isUkr ? $"Гра Durak v{version} | {playingAs}" : $"Durak v{version} | {playingAs}";
    }

    private static string GetState(string sceneName)
    {
        bool isUkr = PlayerPrefs.GetInt("GameLanguage", 0) == 0;

        if (string.Equals(sceneName, "MainMenu", StringComparison.OrdinalIgnoreCase))
        {
            return isUkr ? "У головному меню" : "In Main Menu";
        }

        if (!string.Equals(sceneName, "game", StringComparison.OrdinalIgnoreCase))
        {
            return isUkr ? "У грі" : "In Game";
        }

        int gameMode = PlayerPrefs.GetInt("GameMode", 0);
        int deckSize = PlayerPrefs.GetInt("DeckSize", 36);
        int difficulty = PlayerPrefs.GetInt("BotDifficulty", 0);

        string mode = isUkr
            ? (gameMode == 1 ? "Переводний" : "Підкидний")
            : (gameMode == 1 ? "Passing" : "Throw-in");

        string deck = isUkr ? $"Колода {deckSize}" : $"Deck {deckSize}";

        string diff = isUkr
            ? difficulty switch
            {
                0 => "Легка",
                1 => "Нормальна",
                2 => "Складна",
                _ => $"Рівень {difficulty}"
            }
            : difficulty switch
            {
                0 => "Easy",
                1 => "Normal",
                2 => "Hard",
                _ => $"Level {difficulty}"
            };

        return $"{mode} | {deck} | {diff}";
    }

    private void Log(string message)
    {
        if (!enableLogs)
        {
            return;
        }

        Debug.Log($"[DiscordRichPresenceManager] {message}");
    }
}
