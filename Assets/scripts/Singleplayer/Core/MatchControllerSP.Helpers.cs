using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Durak.Architecture.Singleplayer.Core
{
    public sealed partial class MatchControllerSP
    {
        public void SortPlayerHand()
        {
            if (playerHand == null)
            {
                return;
            }

            int sortType = PlayerPrefs.GetInt("SortMethod", 0);
            if (sortType == 0 || playerHand.childCount <= 0)
            {
                return;
            }

            List<Card> cards = new List<Card>();
            foreach (Transform child in playerHand)
            {
                Card card = child.GetComponent<Card>();
                if (card != null)
                {
                    cards.Add(card);
                }
            }

            Card.CardSuit trump = TrumpSuit;
            if (sortType == 1)
            {
                cards = cards.OrderBy(c => (int)c.value).ThenBy(c => c.suit).ToList();
            }
            else if (sortType == 2)
            {
                cards = cards
                    .OrderBy(c => c.suit == trump ? 1 : 0)
                    .ThenBy(c => c.suit)
                    .ThenBy(c => (int)c.value)
                    .ToList();
            }

            for (int i = 0; i < cards.Count; i++)
            {
                cards[i].transform.SetSiblingIndex(i);
            }
        }

        private IEnumerator DealRoutine()
        {
            if (LocalState == null)
            {
                yield break;
            }

            LogDebug("DealRoutine start.");
            LocalState.IsDealInProgress = true;
            LocalState.IsGameOver = false;
            LocalState.OutcomeRecorded = false;
            ClearBitoVotes();
            StopTurnTimer();

            while (LocalState.DeckObjects.Count > 0 &&
                   (GetSeatCardCount(LocalPlayerId) < handTargetCardCount || GetSeatCardCount(BotPlayerId) < handTargetCardCount))
            {
                if (GetSeatCardCount(LocalPlayerId) < handTargetCardCount)
                {
                    DrawTopDeckCardToPlayer(LocalPlayerId);
                    yield return new WaitForSeconds(dealCardDelaySeconds);
                }

                if (GetSeatCardCount(BotPlayerId) < handTargetCardCount)
                {
                    DrawTopDeckCardToPlayer(BotPlayerId);
                    yield return new WaitForSeconds(dealCardDelaySeconds);
                }
            }

            DetermineFirstAttacker();
            LocalState.IsDealInProgress = false;
            SortPlayerHand();
            RefreshDeckVisuals();

            dealRoutine = null;
            LogDebug($"DealRoutine complete: playerCards={GetSeatCardCount(LocalPlayerId)}, botCards={GetSeatCardCount(BotPlayerId)}, deck={LocalState.DeckObjects.Count}");
            TryChangePhase(Durak.Architecture.Shared.FSM.MatchPhase.Attacking);
        }

        private void CreateDeckLocal()
        {
            if (cardPrefab == null || deckArea == null || LocalState == null)
            {
                return;
            }

            ClearDeckObjects();

            int deckSize = PlayerPrefs.GetInt("DeckSize", 36);
            int startValue = deckSize == 24 ? 9 : deckSize == 36 ? 6 : 2;

            for (int suit = 0; suit < 4; suit++)
            {
                for (int value = startValue; value <= 14; value++)
                {
                    GameObject cardObject = CreateCardObject((Card.CardSuit)suit, (Card.CardValue)value);
                    if (cardObject != null)
                    {
                        LocalState.DeckObjects.Add(cardObject);
                    }
                }
            }

            if (deckSize == 54)
            {
                AddJokerCard(Card.CardSuit.Clubs, 0);
                AddJokerCard(Card.CardSuit.Hearts, 1);
            }
        }

        private void ShuffleDeckLocal()
        {
            if (LocalState == null)
            {
                return;
            }

            for (int i = 0; i < LocalState.DeckObjects.Count; i++)
            {
                int random = Random.Range(i, LocalState.DeckObjects.Count);
                (LocalState.DeckObjects[i], LocalState.DeckObjects[random]) = (LocalState.DeckObjects[random], LocalState.DeckObjects[i]);
            }

            SoundManager.Instance?.PlayShuffle();
        }

        private void SetTrumpFromDeckLocal()
        {
            if (LocalState == null || LocalState.DeckObjects.Count <= 0)
            {
                return;
            }

            int trumpIndex = LocalState.DeckObjects.Count - 1;
            while (trumpIndex >= 0)
            {
                Card card = LocalState.DeckObjects[trumpIndex].GetComponent<Card>();
                if (card != null && card.value != Card.CardValue.Joker)
                {
                    break;
                }

                trumpIndex--;
            }

            if (trumpIndex >= 0 && trumpIndex != LocalState.DeckObjects.Count - 1)
            {
                (LocalState.DeckObjects[trumpIndex], LocalState.DeckObjects[LocalState.DeckObjects.Count - 1]) =
                    (LocalState.DeckObjects[LocalState.DeckObjects.Count - 1], LocalState.DeckObjects[trumpIndex]);
            }

            Card trumpCard = LocalState.DeckObjects[LocalState.DeckObjects.Count - 1].GetComponent<Card>();
            if (trumpCard == null)
            {
                return;
            }

            LocalState.TrumpSuitCode = (int)trumpCard.suit;
            LocalState.TrumpValueCode = (int)trumpCard.value;

            if (trumpCardUI != null)
            {
                trumpCardUI.sprite = trumpCard.frontSprite;
                trumpCardUI.color = Color.white;
            }
        }

        private void DetermineFirstAttacker()
        {
            if (LocalState == null)
            {
                return;
            }

            int playerMinTrump = FindMinTrumpValue(LocalPlayerId);
            int botMinTrump = FindMinTrumpValue(BotPlayerId);
            ulong attacker = playerMinTrump <= botMinTrump ? LocalPlayerId : BotPlayerId;
            ulong defender = attacker == LocalPlayerId ? BotPlayerId : LocalPlayerId;

            LocalState.SetTurn(attacker, defender, true, false, false);
        }

        private void DrawTopDeckCardToPlayer(ulong clientId)
        {
            if (LocalState == null || LocalState.DeckObjects.Count <= 0)
            {
                return;
            }

            Transform hand = GetHandForClient(clientId);
            if (hand == null)
            {
                return;
            }

            GameObject cardObject = LocalState.DeckObjects[0];
            LocalState.DeckObjects.RemoveAt(0);

            if (cardObject == null)
            {
                return;
            }

            Card card = cardObject.GetComponent<Card>();
            cardObject.SetActive(true);
            cardObject.transform.SetParent(hand, false);
            cardObject.transform.SetAsLastSibling();
            SetCardVisualOwner(card, clientId);
            SoundManager.Instance?.PlayDeal();
            RefreshDeckVisuals();
        }

        private void DealCardsAfterRound(ulong attackerId)
        {
            if (LocalState == null)
            {
                return;
            }

            ulong[] order = { attackerId, GetNextActivePlayer(attackerId) };
            bool dealtAny;
            do
            {
                dealtAny = false;
                for (int i = 0; i < order.Length; i++)
                {
                    ulong playerId = order[i];
                    if (GetSeatCardCount(playerId) >= handTargetCardCount || LocalState.DeckObjects.Count <= 0)
                    {
                        continue;
                    }

                    DrawTopDeckCardToPlayer(playerId);
                    dealtAny = true;
                }
            } while (dealtAny && LocalState.DeckObjects.Count > 0);
        }

        private void TickTurnTimer(float deltaTime)
        {
            if (LocalState == null ||
                LocalState.IsGameOver ||
                LocalState.IsDealInProgress ||
                !LocalState.TurnTimerRunning)
            {
                return;
            }

            LocalState.TurnTimerRemaining = Mathf.Max(0f, LocalState.TurnTimerRemaining - Mathf.Max(0f, deltaTime));
            if (LocalState.TurnTimerRemaining > 0f)
            {
                return;
            }

            LocalState.TurnTimerRunning = false;
            LogDebug($"Timer expired: owner={LocalState.TurnTimerOwnerId}, role={LocalState.TurnTimerRole}, phase={LocalState.Phase}");
            CurrentActionHandler?.HandleTimerExpired();
        }

        private void MoveTableToDiscard()
        {
            List<Card> cards = CollectTableCards();
            for (int i = 0; i < cards.Count; i++)
            {
                Card card = cards[i];
                if (card == null)
                {
                    continue;
                }

                Transform transform = card.transform;
                if (discardPile != null)
                {
                    transform.SetParent(discardPile, false);
                }

                transform.gameObject.SetActive(false);
            }

            if (bitoVisual != null)
            {
                bitoVisual.SetActive(true);
            }

            SoundManager.Instance?.PlayBito();
        }

        private void MoveTableToDefenderHand(ulong defenderId)
        {
            Transform hand = GetHandForClient(defenderId);
            if (hand == null)
            {
                return;
            }

            List<Card> cards = CollectTableCards();
            for (int i = 0; i < cards.Count; i++)
            {
                Card card = cards[i];
                if (card == null)
                {
                    continue;
                }

                card.gameObject.SetActive(true);
                card.transform.SetParent(hand, false);
                card.transform.SetAsLastSibling();
                SetCardVisualOwner(card, defenderId);
            }

            SoundManager.Instance?.PlayTake();
        }

        private List<Card> CollectTableCards()
        {
            List<Card> cards = new List<Card>();
            if (tableArea == null)
            {
                return cards;
            }

            List<Transform> roots = GetTableAttackCards();
            for (int i = 0; i < roots.Count; i++)
            {
                Transform root = roots[i];
                Card attack = root.GetComponent<Card>();
                if (attack != null)
                {
                    cards.Add(attack);
                }

                for (int child = 0; child < root.childCount; child++)
                {
                    Card defense = root.GetChild(child).GetComponent<Card>();
                    if (defense != null)
                    {
                        cards.Add(defense);
                    }
                }
            }

            return cards;
        }
    }
}
