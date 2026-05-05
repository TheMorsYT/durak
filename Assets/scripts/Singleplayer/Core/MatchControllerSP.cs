using Durak.Architecture.Shared.Domain;
using Durak.Architecture.Shared.Events;
using Durak.Architecture.Shared.FSM;
using Durak.Architecture.Shared.Interfaces;
using Durak.Architecture.Singleplayer.Core.States;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Durak.Architecture.Singleplayer.Core
{
    public sealed partial class MatchControllerSP : MonoBehaviour, IMatchController
    {
        public const ulong LocalPlayerId = 0;
        public const ulong BotPlayerId = 1;

        public static MatchControllerSP Instance { get; private set; }

        [Header("Runtime")]
        [SerializeField] private EnemyAI enemyAI = null;
        [SerializeField] private bool autoStartOnAwake = true;
        [SerializeField] private bool enableDebugLogs = false;

        [Header("Board References")]
        [SerializeField] private GameObject cardPrefab = null;
        [SerializeField] private Transform playerHand = null;
        [SerializeField] private Transform enemyHand = null;
        [SerializeField] private Transform deckArea = null;
        [SerializeField] private Transform tableArea = null;
        [SerializeField] private Transform discardPile = null;

        [Header("Gameplay UI")]
        [SerializeField] private GameObject bitoVisual = null;
        [SerializeField] private GameObject bitoButton = null;
        [SerializeField] private GameObject takeButton = null;
        [SerializeField] private GameObject transferZone = null;
        [SerializeField] private Image trumpCardUI = null;
        [SerializeField] private Image mainDeckUI = null;
        [SerializeField] private TMP_Text deckCardsText = null;
        [SerializeField] private GameObject gameOverScreen = null;
        [SerializeField] private Text gameOverText = null;

        [Header("Profiles")]
        [SerializeField] private Image playerAvatarImage = null;
        [SerializeField] private Image botAvatarImage = null;
        [SerializeField] private Sprite botAvatarSprite = null;

        [Header("Card Sprites")]
        [SerializeField] private Sprite[] clubsSprites = null;
        [SerializeField] private Sprite[] diamondsSprites = null;
        [SerializeField] private Sprite[] heartsSprites = null;
        [SerializeField] private Sprite[] spadesSprites = null;
        [SerializeField] private Sprite[] jokerSprites = null;

        [Header("Rules")]
        [SerializeField] private int handTargetCardCount = 6;
        [SerializeField] private int maxAttackCards = 6;
        [SerializeField] private float dealCardDelaySeconds = 0.15f;

        private MatchStateMachine stateMachine;
        private bool initialized;
        private bool hasSnapshot;
        private MatchContextSnapshot previousSnapshot;
        private Coroutine dealRoutine;
        private float roundResolutionStallTime;
        private ISingleplayerMatchActionHandler CurrentActionHandler => stateMachine?.CurrentState as ISingleplayerMatchActionHandler;

        public MatchContext Context { get; private set; }
        public IMatchEventBus EventBus { get; private set; }
        public MatchLocalState LocalState { get; private set; }

        public Transform PlayerHand => playerHand;
        public Transform BotHand => enemyHand;
        public Transform TableArea => tableArea;
        public Transform DeckArea => deckArea;
        public Transform DiscardPile => discardPile;
        public GameObject TransferZoneObject => transferZone;

        public Card.CardSuit TrumpSuit => (Card.CardSuit)Mathf.Max(0, LocalState != null ? LocalState.TrumpSuitCode : 0);
        public bool IsTransferModeEnabled => LocalState != null && LocalState.TransferModeEnabled;
        public bool IsDealInProgress => LocalState != null && LocalState.IsDealInProgress;
        public bool IsGameOver => LocalState != null && LocalState.IsGameOver;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            EnsureInitialized();
            TryApplyFacadeBindings();
        }

        private void Start()
        {
            if (autoStartOnAwake)
            {
                TryApplyFacadeBindings();
                Initialize();
            }
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        private void OnDestroy()
        {
            if (stateMachine != null)
            {
                stateMachine.StateChanged -= OnStateChanged;
            }

            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void Initialize()
        {
            EnsureInitialized();
            if (initialized)
            {
                return;
            }

            TryApplyFacadeBindings();
            ValidateSerializedReferences();
            if (!HasRequiredSerializedReferences())
            {
                return;
            }

            SetupProfilesVisuals();
            CreateDeckLocal();
            ShuffleDeckLocal();
            SetTrumpFromDeckLocal();
            RefreshDeckVisuals();

            LocalState.TransferModeEnabled = PlayerPrefs.GetInt("GameMode", 1) == 1;
            LocalState.IsFirstTurn = true;
            LocalState.SetTurn(LocalPlayerId, BotPlayerId, true, false, false);
            LocalState.Phase = MatchPhase.Bootstrap;
            BuildStateMachine();

            initialized = true;
            TryChangePhase(MatchPhase.Dealing);
            PublishSnapshotChanges();
        }

        public void Tick(float deltaTime)
        {
            if (!initialized)
            {
                return;
            }

            stateMachine?.Tick(deltaTime);
            SyncLocalPhaseWithStateMachine(logIfChanged: true);
            MatchPhase activePhase = GetCurrentPhase();

            if (LocalState != null && activePhase == MatchPhase.RoundResolution)
            {
                roundResolutionStallTime += Mathf.Max(0f, deltaTime);
            }
            else
            {
                roundResolutionStallTime = 0f;
            }

            EnsurePhaseTimerRunning();
            TryRecoverRoundResolutionStall();
            TickTurnTimer(deltaTime);
            PublishSnapshotChanges();
        }

        public bool RequestPlayCardFromPlayer(Card card)
        {
            return RequestPlayCard(LocalPlayerId, card);
        }

        public bool RequestDefendCardFromPlayer(Card defendCard, Card targetCard)
        {
            return RequestDefendCard(LocalPlayerId, defendCard, targetCard);
        }

        public bool RequestTransferFromPlayer(Card card)
        {
            return RequestTransfer(LocalPlayerId, card);
        }

        public bool RequestTakeFromPlayer()
        {
            return RequestTake(LocalPlayerId);
        }

        public bool RequestVoteBitoFromPlayer()
        {
            return RequestVoteBito(LocalPlayerId);
        }

        public bool RequestPlayCardFromBot(Card card)
        {
            return RequestPlayCard(BotPlayerId, card);
        }

        public bool RequestDefendCardFromBot(Card defendCard, Card targetCard)
        {
            return RequestDefendCard(BotPlayerId, defendCard, targetCard);
        }

        public bool RequestTransferFromBot(Card card)
        {
            return RequestTransfer(BotPlayerId, card);
        }

        public bool RequestTakeFromBot()
        {
            return RequestTake(BotPlayerId);
        }

        public bool RequestVoteBitoFromBot()
        {
            return RequestVoteBito(BotPlayerId);
        }

        public void RestartGame()
        {
            SoundManager.Instance?.PlayClick();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void LoadMainMenu()
        {
            SoundManager.Instance?.PlayClick();
            SceneManager.LoadScene("MainMenu");
        }

        public bool TryChangePhase(MatchPhase phase)
        {
            if (stateMachine == null)
            {
                LogDebug($"TryChangePhase({phase}) ignored: stateMachine is null");
                return false;
            }

            MatchPhase before = stateMachine.CurrentPhase;
            bool changed = stateMachine.TryChangeState(phase);
            MatchPhase after = stateMachine.CurrentPhase;
            SyncLocalPhaseWithStateMachine();
            LogDebug($"TryChangePhase {before} -> {phase}, result={changed}, now={after}");
            return changed;
        }

        public void BeginDealState()
        {
            if (dealRoutine != null)
            {
                StopCoroutine(dealRoutine);
            }

            dealRoutine = StartCoroutine(DealRoutine());
        }

        public void SetResolutionMode(RoundResolutionModeSP mode)
        {
            if (LocalState != null)
            {
                LocalState.PendingResolutionMode = mode;
            }
        }

        private bool RequestPlayCard(ulong senderId, Card card)
        {
            bool result = CurrentActionHandler != null && CurrentActionHandler.HandlePlayCard(senderId, card);
            LogDebug($"RequestPlayCard sender={senderId}, card={DescribeCard(card)}, phase={GetCurrentPhase()}, result={result}");
            return result;
        }

        private bool RequestDefendCard(ulong senderId, Card defendCard, Card targetCard)
        {
            bool result = CurrentActionHandler != null && CurrentActionHandler.HandleDefendCard(senderId, defendCard, targetCard);
            LogDebug($"RequestDefend sender={senderId}, defend={DescribeCard(defendCard)}, target={DescribeCard(targetCard)}, phase={GetCurrentPhase()}, result={result}");
            return result;
        }

        private bool RequestTransfer(ulong senderId, Card card)
        {
            bool result = CurrentActionHandler != null && CurrentActionHandler.HandleTransfer(senderId, card);
            LogDebug($"RequestTransfer sender={senderId}, card={DescribeCard(card)}, phase={GetCurrentPhase()}, result={result}");
            return result;
        }

        private bool RequestTake(ulong senderId)
        {
            bool result = CurrentActionHandler != null && CurrentActionHandler.HandleTake(senderId);
            LogDebug($"RequestTake sender={senderId}, phase={GetCurrentPhase()}, result={result}");
            return result;
        }

        private bool RequestVoteBito(ulong senderId)
        {
            bool result = CurrentActionHandler != null && CurrentActionHandler.HandleVoteBito(senderId);
            LogDebug($"RequestBito sender={senderId}, phase={GetCurrentPhase()}, result={result}");
            return result;
        }

        private MatchPhase GetCurrentPhase()
        {
            if (stateMachine != null)
            {
                return stateMachine.CurrentPhase;
            }

            return LocalState != null ? LocalState.Phase : MatchPhase.Bootstrap;
        }

        private void SyncLocalPhaseWithStateMachine(bool logIfChanged = false)
        {
            if (LocalState == null)
            {
                return;
            }

            MatchPhase activePhase = GetCurrentPhase();
            if (LocalState.Phase == activePhase)
            {
                return;
            }

            if (logIfChanged)
            {
                LogDebug($"Phase sync: LocalState={LocalState.Phase} -> FSM={activePhase}");
            }

            LocalState.Phase = activePhase;
        }

        private void LogDebug(string message)
        {
            if (!enableDebugLogs)
            {
                return;
            }

#if DURAK_VERBOSE_LOGS
            Debug.Log($"[MatchControllerSP] {message}");
#endif
        }

        private static string DescribeCard(Card card)
        {
            if (card == null)
            {
                return "null";
            }

            return $"{card.suit}/{card.value}";
        }
    }
}
