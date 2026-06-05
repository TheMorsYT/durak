using System;
using System.Collections.Generic;
using Durak.Architecture.Shared.Domain;
using Durak.Architecture.Shared.Events;
using Durak.Architecture.Shared.UI.Presenters;
using Durak.Architecture.Singleplayer.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Durak.Architecture.Singleplayer.UI
{
    public sealed class MatchUIManagerSP : MonoBehaviour
    {
        [Header("Controller")]
        [SerializeField] private MatchControllerSP controller = null;

        [Header("Profiles")]
        [SerializeField] private GameObject[] profileObjects = null;
        [SerializeField] private TMP_Text[] nicknameTexts = null;
        [SerializeField] private Image[] avatarImages = null;
        [SerializeField] private Sprite botAvatarSprite = null;

        [Header("Timers")]
        [SerializeField] private Image[] timerRingsAttack = null;
        [SerializeField] private Image[] timerRingsDefend = null;

        [Header("Actions")]
        [SerializeField] private GameObject takeButton = null;
        [SerializeField] private GameObject bitoButton = null;
        [SerializeField] private GameObject passButton = null;
        [SerializeField] private GameObject transferZone = null;

        [Header("Deck")]
        [SerializeField] private TMP_Text deckCardsText = null;

        private MatchProfilesPresenter profilesPresenter;
        private MatchTimerPresenter timerPresenter;
        private MatchActionsPresenter actionsPresenter;
        private readonly List<IDisposable> subscriptions = new List<IDisposable>();
        private bool started;

        private void Awake()
        {
            ResolveReferences();
            BuildPresenters();
            BindActionButtons();
        }

        private void Start()
        {
            started = true;
            if (controller == null)
            {
                return;
            }

            SubscribeEvents();
            RefreshAll();
        }

        private void Update()
        {
            timerPresenter?.Tick(Time.deltaTime);
            if (controller != null && controller.Context != null)
            {
                RefreshActionAndTimerState();
            }
        }

        private void OnDestroy()
        {
            for (int i = 0; i < subscriptions.Count; i++)
            {
                subscriptions[i]?.Dispose();
            }

            subscriptions.Clear();
        }

        private void ResolveReferences()
        {
            controller ??= MatchControllerSP.Instance;
            if (controller == null)
            {
                controller = FindFirstObjectByType<MatchControllerSP>();
            }

            if (controller == null)
            {
                GameManager facade = GameManager.Instance;
                if (facade == null)
                {
                    facade = FindFirstObjectByType<GameManager>();
                }

                if (facade != null)
                {
                    controller = facade.GetComponent<MatchControllerSP>();
                }
            }
        }

        public void ConfigureFromFacade(GameManager facade, MatchControllerSP configuredController = null)
        {
            if (configuredController != null)
            {
                controller = configuredController;
            }

            if (facade != null)
            {
                if (profileObjects == null || profileObjects.Length < 2)
                {
                    profileObjects = new[]
                    {
                        facade.playerAvatarImage != null ? facade.playerAvatarImage.transform.parent.gameObject : null,
                        facade.botAvatarImage != null ? facade.botAvatarImage.transform.parent.gameObject : null
                    };
                }

                if (nicknameTexts == null || nicknameTexts.Length < 2)
                {
                    nicknameTexts = new TMP_Text[2];
                }

                if (avatarImages == null || avatarImages.Length < 2)
                {
                    avatarImages = new[] { facade.playerAvatarImage, facade.botAvatarImage };
                }

                botAvatarSprite ??= facade.botAvatarSprite;

                if (timerRingsAttack == null || timerRingsAttack.Length < 2)
                {
                    timerRingsAttack = new[] { facade.playerTimerRingAttack, facade.botTimerRingAttack };
                }

                if (timerRingsDefend == null || timerRingsDefend.Length < 2)
                {
                    timerRingsDefend = new[] { facade.playerTimerRingDefend, facade.botTimerRingDefend };
                }

                takeButton ??= facade.takeButton;
                bitoButton ??= facade.bitoButton;
                passButton ??= facade.passButton;
                transferZone ??= facade.transferZone;
                deckCardsText ??= facade.deckCardsText;
            }

            BuildPresenters();
            BindActionButtons();

            if (started)
            {
                RefreshAll();
            }
        }

        private void BuildPresenters()
        {
            profilesPresenter = new MatchProfilesPresenter(profileObjects, nicknameTexts, avatarImages, hideSeatZeroNickname: false);
            timerPresenter = new MatchTimerPresenter(timerRingsAttack, timerRingsDefend);
            ResolvePassButton();
            actionsPresenter = new MatchActionsPresenter(takeButton, bitoButton, passButton, transferZone, allowFollowUpBito: true);
        }

        private void BindActionButtons()
        {
            ResolvePassButton();

            Button take = takeButton != null ? takeButton.GetComponent<Button>() : null;
            if (take != null && take.onClick.GetPersistentEventCount() == 0)
            {
                take.onClick.RemoveListener(OnTakeClicked);
                take.onClick.AddListener(OnTakeClicked);
            }

            Button bito = bitoButton != null ? bitoButton.GetComponent<Button>() : null;
            if (bito != null && bito.onClick.GetPersistentEventCount() == 0)
            {
                bito.onClick.RemoveListener(OnBitoClicked);
                bito.onClick.AddListener(OnBitoClicked);
            }

            Button pass = passButton != null ? passButton.GetComponent<Button>() : null;
            if (pass != null && pass.onClick.GetPersistentEventCount() == 0)
            {
                pass.onClick.RemoveListener(OnPassClicked);
                pass.onClick.AddListener(OnPassClicked);
            }
        }

        private void ResolvePassButton()
        {
            if (passButton != null)
            {
                return;
            }

            GameObject found = GameObject.Find("PassButton");
            if (found != null)
            {
                passButton = found;
                return;
            }

            GameObject[] objects = Resources.FindObjectsOfTypeAll<GameObject>();
            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i].name == "PassButton" &&
                    objects[i].hideFlags == HideFlags.None &&
                    objects[i].scene.IsValid() &&
                    objects[i].scene.isLoaded)
                {
                    passButton = objects[i];
                    return;
                }
            }
        }

        private void SubscribeEvents()
        {
            subscriptions.Add(controller.EventBus.Subscribe<ProfilesChangedEvent>(OnProfilesChanged));
            subscriptions.Add(controller.EventBus.Subscribe<TimerChangedEvent>(OnTimerChanged));
            subscriptions.Add(controller.EventBus.Subscribe<TurnChangedEvent>(_ => RefreshActionAndTimerState()));
            subscriptions.Add(controller.EventBus.Subscribe<TableChangedEvent>(_ => RefreshActionAndTimerState()));
            subscriptions.Add(controller.EventBus.Subscribe<MatchPhaseChangedEvent>(_ => RefreshActionAndTimerState()));
            subscriptions.Add(controller.EventBus.Subscribe<DealStateChangedEvent>(_ => RefreshActionAndTimerState()));
            subscriptions.Add(controller.EventBus.Subscribe<GameOverChangedEvent>(_ => RefreshActionAndTimerState()));
            subscriptions.Add(controller.EventBus.Subscribe<CardPlayedEvent>(_ => RefreshActionAndTimerState()));
            subscriptions.Add(controller.EventBus.Subscribe<DeckCountChangedEvent>(OnDeckCountChanged));
        }

        private void OnProfilesChanged(ProfilesChangedEvent evt)
        {
            profilesPresenter?.Apply(evt.Seats);
            ApplySingleplayerBotAvatar();
            RefreshActionAndTimerState();
        }

        private void OnTimerChanged(TimerChangedEvent evt)
        {
            if (controller == null || controller.Context == null)
            {
                timerPresenter?.HideAll();
                return;
            }

            MatchContext context = controller.Context;
            timerPresenter?.SetVisibilityFromContext(context);
            timerPresenter?.ApplyTimer(evt, context);
        }

        private void OnDeckCountChanged(DeckCountChangedEvent evt)
        {
            if (deckCardsText == null)
            {
                return;
            }

            bool show = evt.CardsRemaining > 0;
            deckCardsText.gameObject.SetActive(show);
            if (show)
            {
                deckCardsText.text = evt.CardsRemaining.ToString();
            }
        }

        private void RefreshAll()
        {
            if (controller == null || controller.Context == null)
            {
                return;
            }

            profilesPresenter?.Apply(controller.Context.Seats);
            ApplySingleplayerBotAvatar();
            RefreshActionAndTimerState();
            OnDeckCountChanged(new DeckCountChangedEvent(controller.Context.DeckCardsRemaining));
        }

        private void RefreshActionAndTimerState()
        {
            if (controller == null || controller.Context == null)
            {
                timerPresenter?.HideAll();
                actionsPresenter?.HideAll();
                return;
            }

            MatchContext context = controller.Context;
            timerPresenter?.SetVisibilityFromContext(context);
            bool canTransferNow = controller.CanLocalPlayerTransferNow() || CardMovement.IsDraggingTransferCandidate;
            actionsPresenter?.Refresh(context, MatchControllerSP.LocalPlayerId, canTransferNow);
        }

        private void OnTakeClicked()
        {
            controller?.RequestTakeFromPlayer();
        }

        private void OnBitoClicked()
        {
            actionsPresenter?.MarkLocalBitoVote();
            controller?.RequestVoteBitoFromPlayer();
            RefreshActionAndTimerState();
        }

        private void OnPassClicked()
        {
            actionsPresenter?.MarkLocalBitoVote();
            controller?.RequestPassFromPlayer();
            RefreshActionAndTimerState();
        }

        private void ApplySingleplayerBotAvatar()
        {
            if (avatarImages == null || avatarImages.Length < 2 || avatarImages[1] == null)
            {
                return;
            }

            Sprite botSprite = botAvatarSprite;
            if (botSprite == null)
            {
                botSprite = AvatarCatalog.GetByName("bot");
            }

            if (botSprite == null)
            {
                botSprite = Resources.Load<Sprite>("bot");
            }

            if (botSprite == null)
            {
                return;
            }

            botAvatarSprite = botSprite;
            avatarImages[1].sprite = botSprite;
            avatarImages[1].preserveAspect = true;
        }
    }
}
