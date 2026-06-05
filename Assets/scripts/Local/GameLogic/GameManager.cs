using Durak.Architecture.Singleplayer.Core;
using Durak.Architecture.Singleplayer.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Controller")]
    [SerializeField] private MatchControllerSP controller = null;
    [SerializeField] private MatchUIManagerSP uiManager = null;

    [Header("Board")]
    public GameObject cardPrefab;
    public Transform playerHand;
    public Transform enemyHand;
    public Transform deckArea;
    public Transform tableArea;
    public Transform discardPile;

    [Header("Actions UI")]
    public GameObject bitoVisual;
    public GameObject bitoButton;
    public GameObject passButton;
    public GameObject takeButton;
    public GameObject transferZone;
    public Image trumpCardUI;
    public Image mainDeckUI;
    public TMP_Text deckCardsText;
    public GameObject gameOverScreen;
    public Text gameOverText;

    [Header("Profile UI")]
    public Image playerAvatarImage;
    public Image botAvatarImage;
    public Sprite botAvatarSprite;
    public Image playerTimerRingAttack;
    public Image playerTimerRingDefend;
    public Image botTimerRingAttack;
    public Image botTimerRingDefend;

    [Header("Card Sprites")]
    public Sprite[] clubsSprites;
    public Sprite[] diamondsSprites;
    public Sprite[] heartsSprites;
    public Sprite[] spadesSprites;
    public Sprite[] jokerSprites;

    [Header("Legacy Mirror (Read-Only)")]
    public bool isDealing;
    public bool isTransferMode = true;
    public bool isFirstTurn = true;
    public bool isPlayerAttacker = true;
    public bool isBotTaking;
    public bool isGameOver;
    public bool isTurnChanging;
    public bool isPlayerTaking;

    public Card.CardSuit trumpSuit;

    private MatchControllerSP Controller => controller != null ? controller : MatchControllerSP.Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        controller ??= GetComponent<MatchControllerSP>();
        if (controller == null)
        {
            controller = FindFirstObjectByType<MatchControllerSP>();
        }

        if (controller == null)
        {
            controller = gameObject.AddComponent<MatchControllerSP>();
        }

        controller.ConfigureFromFacade(this);
        controller.Initialize();

        uiManager ??= GetComponent<MatchUIManagerSP>();
        if (uiManager == null)
        {
            uiManager = FindFirstObjectByType<MatchUIManagerSP>();
        }

        if (uiManager == null)
        {
            uiManager = gameObject.AddComponent<MatchUIManagerSP>();
        }

        uiManager.ConfigureFromFacade(this, controller);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void PlayerTakesCards() => Controller?.RequestTakeFromPlayer();

    public void SendToBito() => Controller?.RequestVoteBitoFromPlayer();

    public void OnPassButtonClicked() => Controller?.RequestPassFromPlayer();

    public void RestartGame() => Controller?.RestartGame();

    public void LoadMainMenu() => Controller?.LoadMainMenu();
}
