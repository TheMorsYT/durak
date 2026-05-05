using System.Collections.Generic;
using Durak.Architecture.Shared.Domain;
using Durak.Architecture.Shared.FSM;
using Durak.Architecture.Singleplayer.Core.States;
using UnityEngine;

namespace Durak.Architecture.Singleplayer.Core
{
    public sealed partial class MatchControllerSP
    {
        public void SetupProfilesVisuals()
        {
            SetAvatarSprite(playerAvatarImage, AvatarCatalog.GetAt(PlayerProfileStorage.GetAvatarIndex()));

            Sprite resolvedBotAvatar = botAvatarSprite;
            if (resolvedBotAvatar == null)
            {
                resolvedBotAvatar = AvatarCatalog.GetByName("bot");
            }

            if (resolvedBotAvatar == null)
            {
                resolvedBotAvatar = Resources.Load<Sprite>("bot");
            }

            botAvatarSprite = resolvedBotAvatar;
            SetAvatarSprite(botAvatarImage, botAvatarSprite);
        }

        public void StartTurnTimer(float durationSeconds, ulong ownerClientId, TurnTimerRole role)
        {
            if (LocalState == null)
            {
                return;
            }

            float duration = Mathf.Max(0f, durationSeconds);
            LocalState.SetTimer(duration, duration, ownerClientId, role, duration > 0f);
            LogDebug($"Timer start: duration={duration:0.00}, owner={ownerClientId}, role={role}, phase={GetCurrentPhase()}");

            if (ownerClientId == BotPlayerId)
            {
                enemyAI?.ForceScheduleNow();
            }
        }

        public void StopTurnTimer()
        {
            if (LocalState == null)
            {
                return;
            }

            if (LocalState.TurnTimerRunning)
            {
                LogDebug($"Timer stop: owner={LocalState.TurnTimerOwnerId}, role={LocalState.TurnTimerRole}, remain={LocalState.TurnTimerRemaining:0.00}");
            }

            LocalState.SetTimer(0f, 0f, MatchDefaults.InvalidClientId, TurnTimerRole.None, false);
        }

        public void RestartCurrentRoleTimer()
        {
            if (LocalState == null)
            {
                return;
            }

            if (LocalState.IsDefenderTaking)
            {
                StartTurnTimer(MatchDefaults.FollowUpTurnSeconds, LocalState.AttackerId, TurnTimerRole.FollowUp);
                return;
            }

            bool hasCards = HasCardsOnTable();
            bool allDefended = hasCards && AreAllCardsDefended();
            if (hasCards && !allDefended)
            {
                StartTurnTimer(MatchDefaults.FullTurnSeconds, LocalState.DefenderId, TurnTimerRole.Defend);
                return;
            }

            StartTurnTimer(MatchDefaults.FullTurnSeconds, LocalState.AttackerId, TurnTimerRole.Attack);
        }

        public void EnsurePhaseTimerRunning()
        {
            if (LocalState == null ||
                LocalState.IsGameOver ||
                LocalState.IsDealInProgress ||
                LocalState.TurnTimerRunning)
            {
                return;
            }

            MatchPhase phase = stateMachine != null ? stateMachine.CurrentPhase : LocalState.Phase;
            if (LocalState.Phase != phase)
            {
                LocalState.Phase = phase;
            }

            LogDebug($"EnsurePhaseTimerRunning: phase={phase}, attacker={LocalState.AttackerId}, defender={LocalState.DefenderId}");

            switch (phase)
            {
                case MatchPhase.Attacking:
                    StartTurnTimer(MatchDefaults.FullTurnSeconds, LocalState.AttackerId, TurnTimerRole.Attack);
                    break;

                case MatchPhase.Defending:
                    StartTurnTimer(MatchDefaults.FullTurnSeconds, LocalState.DefenderId, TurnTimerRole.Defend);
                    break;

                case MatchPhase.FollowUpThrowIn:
                    StartTurnTimer(MatchDefaults.FollowUpTurnSeconds, LocalState.AttackerId, TurnTimerRole.FollowUp);
                    break;
            }
        }

        public void TryRecoverRoundResolutionStall()
        {
            if (LocalState == null)
            {
                return;
            }

            SyncLocalPhaseWithStateMachine();
            if (GetCurrentPhase() != MatchPhase.RoundResolution)
            {
                roundResolutionStallTime = 0f;
                return;
            }

            if (roundResolutionStallTime < 0.35f)
            {
                return;
            }

            LogDebug($"RoundResolution stall detected: pendingMode={LocalState.PendingResolutionMode}, table={LocalState.AttackCardsCount}/{LocalState.DefendedCardsCount}");

            if (LocalState.PendingResolutionMode != RoundResolutionModeSP.None)
            {
                try
                {
                    ResolveRound();
                }
                catch (System.Exception exception)
                {
                    Debug.LogException(exception);
                    LocalState.PendingResolutionMode = RoundResolutionModeSP.None;
                }
            }

            if (TryFinalizeGameOver())
            {
                TryChangePhase(MatchPhase.GameOver);
                roundResolutionStallTime = 0f;
                return;
            }

            if (TryChangePhase(MatchPhase.Attacking))
            {
                EnsurePhaseTimerRunning();
            }

            roundResolutionStallTime = 0f;
        }

        public void ClearBitoVotes()
        {
            LocalState?.BitoVotes.Clear();
        }

        public void RegisterBitoVote(ulong clientId)
        {
            if (LocalState != null)
            {
                LocalState.BitoVotes.Add(clientId);
            }
        }

        public bool AreAllThrowersReadyForResolution()
        {
            int throwers = GetActiveThrowerCount();
            return throwers <= 0 || (LocalState != null && LocalState.BitoVotes.Count >= throwers);
        }

        public bool HasCardsOnTable()
        {
            return CountTableCards().attackCardsCount > 0;
        }

        public bool AreAllCardsDefended()
        {
            (int attack, int defend) = CountTableCards();
            return attack > 0 && defend >= attack;
        }

        public bool IsEligibleThrower(ulong clientId)
        {
            if (LocalState == null || clientId == MatchDefaults.InvalidClientId)
            {
                return false;
            }

            return clientId != LocalState.DefenderId && HasCardsInHand(clientId);
        }

        public bool ValidateThrowCard(ulong senderId, Card card)
        {
            if (LocalState == null || card == null || LocalState.IsGameOver || LocalState.IsDealInProgress)
            {
                return false;
            }

            if (!IsOwnedByClient(card, senderId) || senderId == LocalState.DefenderId)
            {
                return false;
            }

            (int attackCards, _) = CountTableCards();
            if (attackCards == 0 && senderId != LocalState.AttackerId)
            {
                return false;
            }

            if (attackCards > 0)
            {
                if (!IsEligibleThrower(senderId))
                {
                    return false;
                }

                if (!IsCardValueAllowedForThrow(card.value))
                {
                    return false;
                }
            }

            return attackCards < GetMaxAttackCardsLimit();
        }

        public bool PlayAttackCard(ulong senderId, Card card)
        {
            if (!IsOwnedByClient(card, senderId) || tableArea == null)
            {
                return false;
            }

            MoveCardToTableRoot(card);
            LocalState.SetTurn(LocalState.AttackerId, LocalState.DefenderId, false, LocalState.IsDefenderTaking, false);
            ClearBitoVotes();
            UpdateTableStateFromScene();
            PublishCardPlayed(senderId, card, false);
            RestartCurrentRoleTimer();
            LogDebug($"PlayAttackCard: sender={senderId}, card={DescribeCard(card)}, table={LocalState.AttackCardsCount}/{LocalState.DefendedCardsCount}");
            return true;
        }

        public bool DefendCard(ulong senderId, Card defendCard, Card targetCard)
        {
            if (LocalState == null || LocalState.IsDefenderTaking || senderId != LocalState.DefenderId)
            {
                return false;
            }

            if (!IsOwnedByClient(defendCard, senderId) || targetCard == null || targetCard.transform.parent != tableArea)
            {
                return false;
            }

            Transform targetRoot = targetCard.transform;
            if (targetRoot.childCount > 0 || !CanCardBeat(defendCard, targetCard))
            {
                return false;
            }

            MoveCardToDefenseOverlay(defendCard, targetRoot);
            UpdateTableStateFromScene();
            PublishCardPlayed(senderId, defendCard, true);
            RestartCurrentRoleTimer();
            LogDebug($"DefendCard: sender={senderId}, defend={DescribeCard(defendCard)}, target={DescribeCard(targetCard)}, table={LocalState.AttackCardsCount}/{LocalState.DefendedCardsCount}");
            return true;
        }

        public bool ValidateTransfer(ulong senderId, Card card)
        {
            if (LocalState == null || card == null)
            {
                return false;
            }

            if (!LocalState.TransferModeEnabled || LocalState.IsDefenderTaking || senderId != LocalState.DefenderId)
            {
                return false;
            }

            if (!IsOwnedByClient(card, senderId))
            {
                return false;
            }

            List<Transform> roots = GetTableAttackCards();
            if (roots.Count == 0)
            {
                return false;
            }

            Card first = roots[0].GetComponent<Card>();
            if (first == null || card.value != first.value)
            {
                return false;
            }

            for (int i = 0; i < roots.Count; i++)
            {
                Card rootCard = roots[i].GetComponent<Card>();
                if (rootCard == null || roots[i].childCount > 0 || rootCard.value != first.value)
                {
                    return false;
                }
            }

            ulong nextDefender = GetNextActivePlayer(LocalState.DefenderId);
            return GetSeatCardCount(nextDefender) >= roots.Count + 1;
        }

        public bool ExecuteTransfer(ulong senderId, Card card)
        {
            if (!IsOwnedByClient(card, senderId))
            {
                return false;
            }

            MoveCardToTableRoot(card);
            ulong oldDefender = LocalState.DefenderId;
            ulong nextDefender = GetNextActivePlayer(oldDefender);
            LocalState.SetTurn(oldDefender, nextDefender, true, false, false);
            ClearBitoVotes();
            UpdateTableStateFromScene();
            PublishCardPlayed(senderId, card, false);
            LogDebug($"ExecuteTransfer: sender={senderId}, card={DescribeCard(card)}, newAttacker={LocalState.AttackerId}, newDefender={LocalState.DefenderId}");
            return true;
        }
    }
}
