using Durak.Architecture.Shared.Events;
using Durak.Architecture.Shared.FSM;
using Durak.Architecture.Shared.Domain;
using Durak.Architecture.Singleplayer.Core.States;
using UnityEngine;

namespace Durak.Architecture.Singleplayer.Core
{
    public sealed partial class MatchControllerSP
    {
        private void TryApplyFacadeBindings()
        {
            GameManager facade = GameManager.Instance;
            if (facade == null)
            {
                facade = FindFirstObjectByType<GameManager>();
            }

            if (facade == null)
            {
                return;
            }

            ConfigureFromFacade(facade);
        }

        private bool HasRequiredSerializedReferences()
        {
            return cardPrefab != null &&
                   playerHand != null &&
                   enemyHand != null &&
                   deckArea != null &&
                   tableArea != null &&
                   discardPile != null;
        }

        public void ConfigureFromFacade(GameManager facade)
        {
            if (facade == null)
            {
                return;
            }

            cardPrefab ??= facade.cardPrefab;
            playerHand ??= facade.playerHand;
            enemyHand ??= facade.enemyHand;
            deckArea ??= facade.deckArea;
            tableArea ??= facade.tableArea;
            discardPile ??= facade.discardPile;

            bitoVisual ??= facade.bitoVisual;
            bitoButton ??= facade.bitoButton;
            takeButton ??= facade.takeButton;
            transferZone ??= facade.transferZone;
            trumpCardUI ??= facade.trumpCardUI;
            mainDeckUI ??= facade.mainDeckUI;
            deckCardsText ??= facade.deckCardsText;
            gameOverScreen ??= facade.gameOverScreen;
            gameOverText ??= facade.gameOverText;
            playerAvatarImage ??= facade.playerAvatarImage;
            botAvatarImage ??= facade.botAvatarImage;
            botAvatarSprite ??= facade.botAvatarSprite;

            clubsSprites ??= facade.clubsSprites;
            diamondsSprites ??= facade.diamondsSprites;
            heartsSprites ??= facade.heartsSprites;
            spadesSprites ??= facade.spadesSprites;
            jokerSprites ??= facade.jokerSprites;

            if (LocalState != null)
            {
                LocalState.HandsByClientId[LocalPlayerId] = playerHand;
                LocalState.HandsByClientId[BotPlayerId] = enemyHand;
            }
        }

        private void EnsureInitialized()
        {
            if (Context != null && EventBus != null && LocalState != null)
            {
                return;
            }

            if (Instance == null)
            {
                Instance = this;
            }

            Context = new MatchContext();
            EventBus = new MatchEventBus();
            LocalState = new MatchLocalState();
            LocalState.HandsByClientId[LocalPlayerId] = playerHand;
            LocalState.HandsByClientId[BotPlayerId] = enemyHand;
        }

        private void ValidateSerializedReferences()
        {
            TryApplyFacadeBindings();

            if (cardPrefab == null)
            {
                Debug.LogError("[MatchControllerSP] 'Card Prefab' is not assigned.", this);
            }

            if (playerHand == null)
            {
                Debug.LogError("[MatchControllerSP] 'Player Hand' is not assigned.", this);
            }

            if (enemyHand == null)
            {
                Debug.LogError("[MatchControllerSP] 'Enemy Hand' is not assigned.", this);
            }

            if (deckArea == null)
            {
                Debug.LogError("[MatchControllerSP] 'Deck Area' is not assigned.", this);
            }

            if (tableArea == null)
            {
                Debug.LogError("[MatchControllerSP] 'Table Area' is not assigned.", this);
            }

            if (discardPile == null)
            {
                Debug.LogError("[MatchControllerSP] 'Discard Pile' is not assigned.", this);
            }

            enemyAI ??= GetComponent<EnemyAI>();
            LocalState.HandsByClientId[LocalPlayerId] = playerHand;
            LocalState.HandsByClientId[BotPlayerId] = enemyHand;
        }

        private void BuildStateMachine()
        {
            stateMachine = new MatchStateMachine();
            stateMachine.RegisterState(new BootstrapStateSP(this));
            stateMachine.RegisterState(new DealStateSP(this));
            stateMachine.RegisterState(new AttackStateSP(this));
            stateMachine.RegisterState(new DefenseStateSP(this));
            stateMachine.RegisterState(new FollowUpTossStateSP(this));
            stateMachine.RegisterState(new ResolutionStateSP(this));
            stateMachine.RegisterState(new GameOverStateSP(this));
            stateMachine.StateChanged += OnStateChanged;
            stateMachine.TryChangeState(MatchPhase.Bootstrap);
        }

        private void OnStateChanged(MatchPhase previous, MatchPhase current)
        {
            MatchPhase actualCurrent = GetCurrentPhase();
            if (LocalState != null)
            {
                LocalState.Phase = actualCurrent;
            }

            if (actualCurrent != current)
            {
                LogDebug($"StateChanged stale callback ignored: {previous} -> {current}, actual={actualCurrent}");
                return;
            }

            LogDebug($"StateChanged: {previous} -> {actualCurrent}, attacker={LocalState?.AttackerId}, defender={LocalState?.DefenderId}, table={LocalState?.AttackCardsCount}/{LocalState?.DefendedCardsCount}");
            EventBus.Publish(new MatchPhaseChangedEvent(previous, actualCurrent));

            if (actualCurrent == MatchPhase.Attacking ||
                actualCurrent == MatchPhase.Defending ||
                actualCurrent == MatchPhase.FollowUpThrowIn)
            {
                enemyAI?.ForceScheduleNow();
            }
        }
    }
}
