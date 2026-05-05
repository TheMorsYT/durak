using System.Collections.Generic;
using Durak.Architecture.Shared.Events;
using UnityEngine;
using UnityEngine.UI;

namespace Durak.Architecture.Singleplayer.Core
{
    public sealed partial class MatchControllerSP
    {
        private void MoveCardToTableRoot(Card card)
        {
            if (card == null || tableArea == null)
            {
                return;
            }

            Transform cardTransform = card.transform;
            cardTransform.SetParent(tableArea, false);
            cardTransform.localPosition = Vector3.zero;
            cardTransform.SetAsLastSibling();
            card.FlipCard(true);
            SetCardInteractable(cardTransform, false);
            UpdateCardDefaultParent(cardTransform, tableArea);
            SoundManager.Instance?.PlayCardToTable();
        }

        private void MoveCardToDefenseOverlay(Card card, Transform targetRoot)
        {
            if (card == null || targetRoot == null)
            {
                return;
            }

            Transform cardTransform = card.transform;
            cardTransform.SetParent(targetRoot, false);
            cardTransform.localPosition = new Vector3(30f, -30f, 0f);
            cardTransform.SetAsLastSibling();
            card.FlipCard(true);
            SetCardInteractable(cardTransform, false);
            UpdateCardDefaultParent(cardTransform, targetRoot);
            SoundManager.Instance?.PlayCardToTable();
        }

        private void UpdateTableStateFromScene()
        {
            if (LocalState == null)
            {
                return;
            }

            (int attack, int defend) = CountTableCards();
            LocalState.SetTable(attack, defend);
        }

        private (int attackCardsCount, int defendedCardsCount) CountTableCards()
        {
            int attack = 0;
            int defend = 0;
            if (tableArea == null)
            {
                return (attack, defend);
            }

            foreach (Transform root in tableArea)
            {
                Card attackCard = root.GetComponent<Card>();
                if (attackCard == null)
                {
                    continue;
                }

                attack++;
                if (root.childCount > 0 && root.GetChild(0).GetComponent<Card>() != null)
                {
                    defend++;
                }
            }

            return (attack, defend);
        }

        private int GetActiveThrowerCount()
        {
            int count = 0;
            if (IsEligibleThrower(LocalState.AttackerId))
            {
                count++;
            }

            return count;
        }

        private bool IsOwnedByClient(Card card, ulong clientId)
        {
            Transform hand = GetHandForClient(clientId);
            if (card == null || hand == null)
            {
                return false;
            }

            if (card.transform.parent == hand)
            {
                return true;
            }

            CardMovement movement = card.GetComponent<CardMovement>();
            return movement != null &&
                   movement.IsDraggingCard &&
                   movement.DragStartParent == hand;
        }

        private bool IsCardValueAllowedForThrow(Card.CardValue value)
        {
            if (tableArea == null)
            {
                return false;
            }

            Card[] cards = tableArea.GetComponentsInChildren<Card>(true);
            if (cards.Length == 0)
            {
                return true;
            }

            for (int i = 0; i < cards.Length; i++)
            {
                if (cards[i].value == value)
                {
                    return true;
                }
            }

            return false;
        }

        private Transform GetHandForClient(ulong clientId)
        {
            if (LocalState != null && LocalState.HandsByClientId.TryGetValue(clientId, out Transform hand) && hand != null)
            {
                return hand;
            }

            return clientId == LocalPlayerId ? playerHand : enemyHand;
        }

        private int GetSeatCardCount(ulong clientId)
        {
            Transform hand = GetHandForClient(clientId);
            return hand != null ? hand.childCount : 0;
        }

        private bool HasCardsInHand(ulong clientId)
        {
            return GetSeatCardCount(clientId) > 0;
        }

        private ulong GetNextActivePlayer(ulong currentClientId)
        {
            ulong next = currentClientId == LocalPlayerId ? BotPlayerId : LocalPlayerId;
            if (GetSeatCardCount(next) > 0 || (LocalState != null && LocalState.DeckObjects.Count > 0))
            {
                return next;
            }

            return currentClientId;
        }

        private int FindMinTrumpValue(ulong clientId)
        {
            List<Card> cards = GetHandCards(clientId);
            int min = int.MaxValue;
            for (int i = 0; i < cards.Count; i++)
            {
                Card card = cards[i];
                if (card.suit == TrumpSuit)
                {
                    min = Mathf.Min(min, (int)card.value);
                }
            }

            return min;
        }

        private void RefreshDeckVisuals()
        {
            int cardsLeft = LocalState != null ? LocalState.DeckObjects.Count : 0;
            if (deckCardsText != null)
            {
                bool show = cardsLeft > 0;
                deckCardsText.gameObject.SetActive(show);
                if (show)
                {
                    deckCardsText.text = cardsLeft.ToString();
                }
            }

            if (mainDeckUI != null && cardsLeft <= 1)
            {
                mainDeckUI.gameObject.SetActive(false);
            }

            if (trumpCardUI != null && cardsLeft == 0)
            {
                Color color = trumpCardUI.color;
                color.a = 0.35f;
                trumpCardUI.color = color;
            }
        }

        private void ClearDeckObjects()
        {
            if (LocalState == null)
            {
                return;
            }

            for (int i = 0; i < LocalState.DeckObjects.Count; i++)
            {
                GameObject cardObject = LocalState.DeckObjects[i];
                if (cardObject != null)
                {
                    Destroy(cardObject);
                }
            }

            LocalState.DeckObjects.Clear();
        }

        private GameObject CreateCardObject(Card.CardSuit suit, Card.CardValue value)
        {
            if (cardPrefab == null || deckArea == null)
            {
                return null;
            }

            GameObject cardObject = Instantiate(cardPrefab, deckArea);
            cardObject.name = $"{suit}_{value}";
            cardObject.SetActive(false);

            Card card = cardObject.GetComponent<Card>();
            if (card == null)
            {
                Destroy(cardObject);
                return null;
            }

            card.suit = suit;
            card.value = value;
            card.frontSprite = ResolveFrontSprite(suit, value);
            return cardObject;
        }

        private void AddJokerCard(Card.CardSuit suit, int spriteIndex)
        {
            if (cardPrefab == null || deckArea == null || LocalState == null)
            {
                return;
            }

            GameObject cardObject = Instantiate(cardPrefab, deckArea);
            cardObject.name = $"Joker_{suit}";
            cardObject.SetActive(false);

            Card card = cardObject.GetComponent<Card>();
            if (card == null)
            {
                Destroy(cardObject);
                return;
            }

            card.suit = suit;
            card.value = Card.CardValue.Joker;
            if (jokerSprites != null && spriteIndex >= 0 && spriteIndex < jokerSprites.Length)
            {
                card.frontSprite = jokerSprites[spriteIndex];
            }

            LocalState.DeckObjects.Add(cardObject);
        }

        private Sprite ResolveFrontSprite(Card.CardSuit suit, Card.CardValue value)
        {
            if (value == Card.CardValue.Joker)
            {
                int jokerIndex = suit == Card.CardSuit.Clubs ? 0 : 1;
                return jokerSprites != null && jokerIndex >= 0 && jokerIndex < jokerSprites.Length
                    ? jokerSprites[jokerIndex]
                    : null;
            }

            int spriteIndex = (int)value - 2;
            Sprite[] source = suit switch
            {
                Card.CardSuit.Clubs => clubsSprites,
                Card.CardSuit.Diamonds => diamondsSprites,
                Card.CardSuit.Hearts => heartsSprites,
                Card.CardSuit.Spades => spadesSprites,
                _ => null
            };

            return source != null && spriteIndex >= 0 && spriteIndex < source.Length
                ? source[spriteIndex]
                : null;
        }

        private void SetCardVisualOwner(Card card, ulong ownerId)
        {
            if (card == null)
            {
                return;
            }

            bool faceUp = ownerId == LocalPlayerId;
            card.FlipCard(faceUp);
            SetCardInteractable(card.transform, faceUp);
            Transform hand = GetHandForClient(ownerId);
            UpdateCardDefaultParent(card.transform, hand);
        }

        private void SetCardInteractable(Transform cardTransform, bool interactable)
        {
            if (cardTransform == null)
            {
                return;
            }

            CanvasGroup canvasGroup = cardTransform.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = interactable;
            }
        }

        private static void UpdateCardDefaultParent(Transform cardTransform, Transform parent)
        {
            CardMovement movement = cardTransform != null ? cardTransform.GetComponent<CardMovement>() : null;
            if (movement != null)
            {
                movement.defaultParent = parent;
            }
        }

        private void PublishCardPlayed(ulong playerId, Card card, bool isDefenseCard)
        {
            ulong cardId = card != null ? (ulong)Mathf.Abs(card.GetInstanceID()) : 0;
            EventBus.Publish(new CardPlayedEvent(playerId, cardId, isDefenseCard));
        }

        private static bool IsRedSuit(Card.CardSuit suit)
        {
            return suit == Card.CardSuit.Hearts || suit == Card.CardSuit.Diamonds;
        }

        private static void SetAvatarSprite(Image targetImage, Sprite sprite)
        {
            if (targetImage == null)
            {
                return;
            }

            targetImage.sprite = sprite;
            targetImage.preserveAspect = true;
        }

        private MatchResultType? ResolveOutcome(int playerCards, int botCards)
        {
            if (playerCards == 0 && botCards == 0)
            {
                return MatchResultType.Draw;
            }

            if (playerCards == 0)
            {
                return MatchResultType.Win;
            }

            if (botCards == 0)
            {
                return MatchResultType.Loss;
            }

            return null;
        }

        private void ShowGameOver(MatchResultType outcome)
        {
            if (gameOverScreen != null)
            {
                gameOverScreen.SetActive(true);
            }

            if (gameOverText != null)
            {
                gameOverText.text = BuildOutcomeText(outcome);
            }

            if (takeButton != null)
            {
                takeButton.SetActive(false);
            }

            if (bitoButton != null)
            {
                bitoButton.SetActive(false);
            }
        }

        private static string BuildOutcomeText(MatchResultType outcome)
        {
            bool ukrainian = PlayerPrefs.GetInt("GameLanguage", 0) == 0;
            if (outcome == MatchResultType.Win)
            {
                return ukrainian ? "Перемога!" : "You Win!";
            }

            if (outcome == MatchResultType.Loss)
            {
                return ukrainian ? "Поразка!" : "You Lose!";
            }

            return ukrainian ? "Нічия!" : "Draw!";
        }
    }
}
