using System;
using System.Collections.Generic;
using Durak.Architecture.Shared.Domain;
using Durak.Architecture.Shared.Events;
using Durak.Architecture.Shared.FSM;
using UnityEngine;

namespace Durak.Architecture.Singleplayer.Core
{
    public sealed partial class MatchControllerSP
    {
        private void PublishSnapshotChanges()
        {
            MatchContextSnapshot snapshot = BuildSnapshot();
            MatchContextSnapshot previous = previousSnapshot;
            bool hadPrevious = hasSnapshot;

            Context.ApplySnapshot(snapshot);

            if (!hadPrevious || HaveTurnChanged(previous, snapshot))
            {
                EventBus.Publish(new TurnChangedEvent(
                    snapshot.AttackerId,
                    snapshot.DefenderId,
                    snapshot.FirstAttackWave,
                    snapshot.IsDefenderTaking,
                    snapshot.AttackerPassed));
            }

            if (!hadPrevious || HaveTimerChanged(previous, snapshot))
            {
                EventBus.Publish(new TimerChangedEvent(
                    snapshot.TimerDurationSeconds,
                    snapshot.TimerRemainingSeconds,
                    snapshot.TimerOwnerClientId,
                    snapshot.TimerRole,
                    snapshot.TimerIsRunning));
            }

            if (!hadPrevious || HaveTableChanged(previous, snapshot))
            {
                EventBus.Publish(new TableChangedEvent(
                    snapshot.AttackCardsCount,
                    snapshot.DefendedCardsCount,
                    snapshot.AttackCardsCount > 0 && snapshot.DefendedCardsCount >= snapshot.AttackCardsCount));
            }

            if (!hadPrevious || previous.DeckCardsRemaining != snapshot.DeckCardsRemaining)
            {
                EventBus.Publish(new DeckCountChangedEvent(snapshot.DeckCardsRemaining));
            }

            if (!hadPrevious || previous.IsDealInProgress != snapshot.IsDealInProgress)
            {
                EventBus.Publish(new DealStateChangedEvent(snapshot.IsDealInProgress));
            }

            if (!hadPrevious || previous.IsGameOver != snapshot.IsGameOver)
            {
                EventBus.Publish(new GameOverChangedEvent(snapshot.IsGameOver));
            }

            if (!hadPrevious || HaveSeatsChanged(previous, snapshot))
            {
                EventBus.Publish(new ProfilesChangedEvent(Context.Seats));
            }

            previousSnapshot = snapshot;
            hasSnapshot = true;
        }

        private MatchContextSnapshot BuildSnapshot()
        {
            MatchPhase phase = GetCurrentPhase();
            if (LocalState != null && LocalState.Phase != phase)
            {
                LocalState.Phase = phase;
            }

            return new MatchContextSnapshot(
                phase,
                LocalState != null ? LocalState.AttackerId : MatchDefaults.InvalidClientId,
                LocalState != null ? LocalState.DefenderId : MatchDefaults.InvalidClientId,
                LocalState != null && LocalState.FirstAttackWave,
                LocalState != null && LocalState.IsDefenderTaking,
                LocalState != null && LocalState.AttackerPassed,
                LocalState != null ? LocalState.TurnTimerDuration : 0f,
                LocalState != null ? LocalState.TurnTimerRemaining : 0f,
                LocalState != null ? LocalState.TurnTimerOwnerId : MatchDefaults.InvalidClientId,
                LocalState != null ? LocalState.TurnTimerRole : TurnTimerRole.None,
                LocalState != null && LocalState.TurnTimerRunning,
                LocalState != null ? LocalState.AttackCardsCount : 0,
                LocalState != null ? LocalState.DefendedCardsCount : 0,
                LocalState != null ? LocalState.DeckObjects.Count : 0,
                LocalState != null && LocalState.IsDealInProgress,
                LocalState != null && LocalState.IsGameOver,
                BuildSeatSnapshots());
        }

        private List<SeatSnapshot> BuildSeatSnapshots()
        {
            int playerCards = playerHand != null ? playerHand.childCount : 0;
            int botCards = enemyHand != null ? enemyHand.childCount : 0;
            int botAvatarIndex = 0;
            if (AvatarCatalog.TryGetIndexByName("bot", out int resolvedBotAvatarIndex))
            {
                botAvatarIndex = resolvedBotAvatarIndex;
            }

            List<SeatSnapshot> seats = new List<SeatSnapshot>(2)
            {
                new SeatSnapshot(0, LocalPlayerId, playerCards, PlayerProfileStorage.GetNickname(), PlayerProfileStorage.GetAvatarIndex()),
                new SeatSnapshot(1, BotPlayerId, botCards, "Bot", botAvatarIndex)
            };

            return seats;
        }

        private static bool HaveTurnChanged(MatchContextSnapshot previous, MatchContextSnapshot next)
        {
            return previous.AttackerId != next.AttackerId ||
                   previous.DefenderId != next.DefenderId ||
                   previous.FirstAttackWave != next.FirstAttackWave ||
                   previous.IsDefenderTaking != next.IsDefenderTaking ||
                   previous.AttackerPassed != next.AttackerPassed;
        }

        private static bool HaveTimerChanged(MatchContextSnapshot previous, MatchContextSnapshot next)
        {
            return !Mathf.Approximately(previous.TimerDurationSeconds, next.TimerDurationSeconds) ||
                   !Mathf.Approximately(previous.TimerRemainingSeconds, next.TimerRemainingSeconds) ||
                   previous.TimerOwnerClientId != next.TimerOwnerClientId ||
                   previous.TimerRole != next.TimerRole ||
                   previous.TimerIsRunning != next.TimerIsRunning;
        }

        private static bool HaveTableChanged(MatchContextSnapshot previous, MatchContextSnapshot next)
        {
            return previous.AttackCardsCount != next.AttackCardsCount ||
                   previous.DefendedCardsCount != next.DefendedCardsCount;
        }

        private static bool HaveSeatsChanged(MatchContextSnapshot previous, MatchContextSnapshot next)
        {
            if (ReferenceEquals(previous.Seats, next.Seats))
            {
                return false;
            }

            if (previous.Seats == null || next.Seats == null || previous.Seats.Count != next.Seats.Count)
            {
                return true;
            }

            for (int i = 0; i < previous.Seats.Count; i++)
            {
                if (!previous.Seats[i].Equals(next.Seats[i]))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
