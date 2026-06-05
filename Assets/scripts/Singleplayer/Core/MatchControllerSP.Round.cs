using System.Collections.Generic;
using Durak.Architecture.Shared.Domain;
using Durak.Architecture.Shared.FSM;
using Durak.Architecture.Singleplayer.Core.States;
using UnityEngine;

namespace Durak.Architecture.Singleplayer.Core
{
    public sealed partial class MatchControllerSP
    {
        public void BeginDefenderTakingMode()
        {
            if (LocalState == null)
            {
                return;
            }

            LocalState.SetTurn(LocalState.AttackerId, LocalState.DefenderId, false, true, false);
            ClearBitoVotes();
            LogDebug($"BeginDefenderTakingMode: attacker={LocalState.AttackerId}, defender={LocalState.DefenderId}");
            StartTurnTimer(MatchDefaults.FollowUpTurnSeconds, LocalState.AttackerId, TurnTimerRole.FollowUp);
        }

        public void PassOpeningAttackByTimeout()
        {
            if (LocalState == null)
            {
                return;
            }

            ulong nextAttacker = GetNextActivePlayer(LocalState.AttackerId);
            ulong nextDefender = GetNextActivePlayer(nextAttacker);
            LocalState.SetTurn(nextAttacker, nextDefender, true, false, false);
            ClearBitoVotes();
            LogDebug($"PassOpeningAttackByTimeout: nextAttacker={nextAttacker}, nextDefender={nextDefender}");
            StartTurnTimer(MatchDefaults.FullTurnSeconds, nextAttacker, TurnTimerRole.Attack);
        }

        public void ResolveRound()
        {
            if (LocalState == null || LocalState.PendingResolutionMode == RoundResolutionModeSP.None)
            {
                return;
            }

            ulong attacker = LocalState.AttackerId;
            ulong defender = LocalState.DefenderId;
            LogDebug($"ResolveRound start: mode={LocalState.PendingResolutionMode}, attacker={attacker}, defender={defender}, table={LocalState.AttackCardsCount}/{LocalState.DefendedCardsCount}");

            if (LocalState.PendingResolutionMode == RoundResolutionModeSP.DiscardToPile)
            {
                MoveTableToDiscard();
                attacker = defender;
            }
            else if (LocalState.PendingResolutionMode == RoundResolutionModeSP.DefenderCollectsTable)
            {
                MoveTableToDefenderHand(defender);
                attacker = GetNextActivePlayer(defender);
            }

            LocalState.PendingResolutionMode = RoundResolutionModeSP.None;
            LocalState.IsFirstTurn = false;

            ulong nextDefender = GetNextActivePlayer(attacker);
            LocalState.SetTurn(attacker, nextDefender, true, false, false);
            ClearBitoVotes();

            DealCardsAfterRound(attacker);
            UpdateTableStateFromScene();
            SortPlayerHand();

            if (!TryFinalizeGameOver())
            {
                StartTurnTimer(MatchDefaults.FullTurnSeconds, attacker, TurnTimerRole.Attack);
            }

            LogDebug($"ResolveRound end: nextAttacker={LocalState.AttackerId}, nextDefender={LocalState.DefenderId}, deck={LocalState.DeckObjects.Count}, table={LocalState.AttackCardsCount}/{LocalState.DefendedCardsCount}");
        }

        public bool TryFinalizeGameOver()
        {
            if (LocalState == null || LocalState.DeckObjects.Count > 0)
            {
                return false;
            }




            if (LocalState.AttackCardsCount > 0 || LocalState.DefendedCardsCount > 0)
            {
                LogDebug($"TryFinalizeGameOver: Cards still on table (attack={LocalState.AttackCardsCount}, defended={LocalState.DefendedCardsCount}). Waiting for resolution.");
                return false;
            }

            int playerCards = GetSeatCardCount(LocalPlayerId);
            int botCards = GetSeatCardCount(BotPlayerId);
            MatchResultType? outcome = ResolveOutcome(playerCards, botCards);
            if (!outcome.HasValue)
            {
                return false;
            }

            LocalState.IsGameOver = true;
            LocalState.IsDealInProgress = false;
            LocalState.LastOutcome = outcome.Value;
            StopTurnTimer();

            if (!LocalState.OutcomeRecorded)
            {
                PlayerProfileStorage.RecordSingleResult(outcome.Value);
                LocalState.OutcomeRecorded = true;
            }

            ShowGameOver(outcome.Value);
            LogDebug($"Game Over: outcome={outcome.Value}, playerCards={playerCards}, botCards={botCards}");
            return true;
        }

        public bool CanLocalPlayerPlayCard(Card card)
        {
            return ValidateThrowCard(LocalPlayerId, card);
        }

        public bool TryFindLocalDefenseTarget(Card defendingCard, out Card targetCard)
        {
            targetCard = null;
            if (LocalState == null || defendingCard == null || LocalState.DefenderId != LocalPlayerId || tableArea == null)
            {
                return false;
            }

            List<Transform> roots = GetTableAttackCards();
            for (int i = 0; i < roots.Count; i++)
            {
                Transform root = roots[i];
                if (IsAttackRootDefended(root))
                {
                    continue;
                }

                Card attackCard = root.GetComponent<Card>();
                if (attackCard == null || !CanCardBeat(defendingCard, attackCard))
                {
                    continue;
                }

                targetCard = attackCard;
                return true;
            }

            return false;
        }

        public bool CanLocalPlayerTransfer(Card card)
        {
            return ValidateTransfer(LocalPlayerId, card);
        }

        public bool CanLocalPlayerTransferNow()
        {
            if (LocalState == null || LocalState.DefenderId != LocalPlayerId || !LocalState.TransferModeEnabled)
            {
                return false;
            }

            List<Card> playerCards = GetHandCards(LocalPlayerId);
            for (int i = 0; i < playerCards.Count; i++)
            {
                if (ValidateTransfer(LocalPlayerId, playerCards[i]))
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsInLocalPlayerHand(Transform cardTransform)
        {
            return cardTransform != null && playerHand != null && cardTransform.parent == playerHand;
        }

        public bool IsCardOnTable(Transform cardTransform)
        {
            if (cardTransform == null || tableArea == null)
            {
                return false;
            }

            if (cardTransform.parent == tableArea)
            {
                return true;
            }

            Transform parent = cardTransform.parent;
            return parent != null && parent.parent == tableArea && parent.GetComponent<Card>() != null;
        }

        public bool CanCardBeat(Card defenseCard, Card attackCard)
        {
            if (defenseCard == null || attackCard == null || LocalState == null)
            {
                return false;
            }

            if (defenseCard.value == Card.CardValue.Joker)
            {
                bool trumpRed = IsRedSuit((Card.CardSuit)LocalState.TrumpSuitCode);
                bool defenseRed = IsRedSuit(defenseCard.suit);
                bool attackRed = IsRedSuit(attackCard.suit);
                return defenseRed ? trumpRed || attackRed : !trumpRed || !attackRed;
            }

            if (attackCard.value == Card.CardValue.Joker)
            {
                return false;
            }

            Card.CardSuit trumpSuit = (Card.CardSuit)Mathf.Max(0, LocalState.TrumpSuitCode);
            if (defenseCard.suit == trumpSuit && attackCard.suit != trumpSuit)
            {
                return true;
            }

            return defenseCard.suit == attackCard.suit && defenseCard.value > attackCard.value;
        }

        public List<Card> GetHandCards(ulong clientId)
        {
            Transform hand = GetHandForClient(clientId);
            if (hand == null)
            {
                return new List<Card>();
            }

            List<Card> cards = new List<Card>(hand.childCount);
            foreach (Transform child in hand)
            {
                Card card = child.GetComponent<Card>();
                if (card != null)
                {
                    cards.Add(card);
                }
            }

            return cards;
        }

        public List<Transform> GetTableAttackCards()
        {
            List<Transform> roots = new List<Transform>();
            if (tableArea == null)
            {
                return roots;
            }

            foreach (Transform root in tableArea)
            {
                if (root.GetComponent<Card>() != null)
                {
                    roots.Add(root);
                }
            }

            return roots;
        }

        public int GetMaxAttackCardsLimit()
        {
            int roundCap = LocalState != null && LocalState.IsFirstTurn ? Mathf.Max(1, maxAttackCards - 1) : maxAttackCards;
            int defenderCards = GetSeatCardCount(LocalState != null ? LocalState.DefenderId : MatchDefaults.InvalidClientId);
            int attackCards = CountTableCards().attackCardsCount;
            return Mathf.Min(roundCap, defenderCards + attackCards);
        }
    }
}
