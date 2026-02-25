using UnityEngine;
using UnityEngine.EventSystems;

public class DropZone : MonoBehaviour, IDropHandler
{
    private GameManager gm;

    void Start() { gm = FindObjectOfType<GameManager>(); }

    public void OnDrop(PointerEventData eventData)
    {
        CardMovement cardMove = eventData.pointerDrag.GetComponent<CardMovement>();
        Card cardData = eventData.pointerDrag.GetComponent<Card>();

        if (cardMove != null && cardData != null)
        {
            if (gm.isPlayerAttacker)
            {
                int maxCards = gm.GetMaxAttackCards();
                bool canToss = false;

                if (transform.childCount == 0)
                {
                    canToss = true;
                }
                else if (transform.childCount < maxCards)
                {
                    Card[] cardsOnTable = gm.tableArea.GetComponentsInChildren<Card>();
                    foreach (Card c in cardsOnTable)
                    {
                        if (c.value == cardData.value) { canToss = true; break; }
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
                foreach (Transform tableCard in transform)
                {
                    if (tableCard.childCount == 0)
                    {
                        Card botCardData = tableCard.GetComponent<Card>();

                        if (botCardData != null && CanPlayerBeat(cardData, botCardData))
                        {
                            cardMove.defaultParent = tableCard;
                            cardMove.transform.SetParent(tableCard, false);
                            cardMove.transform.localPosition = new Vector3(30, -30, 0);
                            cardMove.transform.SetAsLastSibling();

                            CanvasGroup cg = cardMove.GetComponent<CanvasGroup>();
                            if (cg != null) cg.blocksRaycasts = false;

                            gm.CheckWinCondition(); // Миттєва перевірка
                            EnemyAI bot = gm.GetComponent<EnemyAI>();
                            if (bot != null) bot.TryToAttack();

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
        cardMove.defaultParent = this.transform;
        cardMove.transform.SetAsLastSibling();

        CanvasGroup cg = cardMove.GetComponent<CanvasGroup>();
        if (cg != null) cg.blocksRaycasts = false;

        EnemyAI bot = gm.GetComponent<EnemyAI>();
        if (bot != null && !gm.isBotTaking) bot.TryToDefend();
    }
}