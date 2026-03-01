using UnityEngine;
using UnityEngine.EventSystems;
using System.Linq;
using System.Collections.Generic;

public class DropZone : MonoBehaviour, IDropHandler
{
    private GameManager gm;

    void Start()
    {
        gm = GameManager.Instance;
        if (gm == null) gm = Object.FindFirstObjectByType<GameManager>();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerEnter != null && eventData.pointerEnter.name == "TransferZone")
        {
            return;
        }

        CardMovement cardMove = eventData.pointerDrag.GetComponent<CardMovement>();
        Card cardData = eventData.pointerDrag.GetComponent<Card>();

        if (cardMove != null && cardData != null)
        {
            if (gm.isPlayerAttacker)
            {
                int maxCards = gm.GetMaxAttackCards();
                bool canToss = false;

                var cardsOnTableTransforms = gm.GetTableAttackCards();
                int attackCardsCount = cardsOnTableTransforms.Count;

                if (attackCardsCount == 0)
                {
                    canToss = true;
                }
                else if (attackCardsCount < maxCards)
                {
                    var allCardsOnTable = gm.tableArea.GetComponentsInChildren<Card>();
                    var tableRanks = allCardsOnTable.Select(c => c.value).Distinct();

                    if (tableRanks.Contains(cardData.value))
                    {
                        canToss = true;
                    }
                }

                if (canToss)
                {
                    AcceptAttackCard(cardMove);
                    gm.CheckWinCondition();
                }
            }
            else
            {
                if (gm.isPlayerAttacker) return;

                foreach (Transform tableCard in gm.GetTableAttackCards())
                {
                    Card botCardData = tableCard.GetComponent<Card>();

                    if (tableCard.childCount == 0)
                    {
                        if (CanPlayerBeat(cardData, botCardData))
                        {
                            if (SoundManager.Instance != null) SoundManager.Instance.PlayCardToTable();

                            cardMove.defaultParent = tableCard;
                            cardMove.transform.SetParent(tableCard, false);
                            cardMove.transform.localPosition = new Vector3(30, -30, 0);
                            cardMove.transform.SetAsLastSibling();

                            CanvasGroup cg = cardMove.GetComponent<CanvasGroup>();
                            if (cg != null) cg.blocksRaycasts = false;

                            gm.CheckWinCondition();

                            EnemyAI bot = gm.GetComponent<EnemyAI>();
                            if (bot != null && !gm.isBotTaking) bot.TryToAttack();

                            break;
                        }
                    }
                }
            }
        }
    }

    bool CanPlayerBeat(Card playerCard, Card botCard)
    {
        if (playerCard.value == Card.CardValue.Joker)
        {
            bool isTrumpRed = (gm.trumpSuit == Card.CardSuit.Hearts || gm.trumpSuit == Card.CardSuit.Diamonds);
            bool pRed = (playerCard.suit == Card.CardSuit.Hearts || playerCard.suit == Card.CardSuit.Diamonds);
            bool bRed = (botCard.suit == Card.CardSuit.Hearts || botCard.suit == Card.CardSuit.Diamonds);
            if (pRed) return isTrumpRed || bRed;
            return !isTrumpRed || !bRed;
        }

        if (botCard.value == Card.CardValue.Joker) return false;
        if (playerCard.suit == gm.trumpSuit && botCard.suit != gm.trumpSuit) return true;
        if (playerCard.suit == botCard.suit) return playerCard.value > botCard.value;
        return false;
    }

    void AcceptAttackCard(CardMovement cardMove)
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayCardToTable();
        cardMove.defaultParent = this.transform;
        cardMove.transform.SetParent(this.transform, false);
        cardMove.transform.SetAsLastSibling();

        CanvasGroup cg = cardMove.GetComponent<CanvasGroup>();
        if (cg != null) cg.blocksRaycasts = false;

        EnemyAI bot = gm.GetComponent<EnemyAI>();
        if (bot != null && !gm.isBotTaking) bot.TryToDefend();
    }
}