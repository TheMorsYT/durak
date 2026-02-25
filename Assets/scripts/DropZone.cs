using UnityEngine;
using UnityEngine.EventSystems;

public class DropZone : MonoBehaviour, IDropHandler
{
    private GameManager gm;

    void Start()
    {
        gm = FindObjectOfType<GameManager>();
    }

    public void OnDrop(PointerEventData eventData)
    {
        CardMovement cardMove = eventData.pointerDrag.GetComponent<CardMovement>();
        Card cardData = eventData.pointerDrag.GetComponent<Card>();

        if (cardMove != null && cardData != null)
        {
            if (gm.isPlayerAttacker)
            {
                int maxCards = gm.GetMaxAttackCards();

                if (transform.childCount == 0)
                {
                    AcceptAttackCard(cardMove);
                }
                else if (transform.childCount < maxCards)
                {
                    bool canToss = false;
                    Card[] cardsOnTable = GetComponentsInChildren<Card>();
                    foreach (Card c in cardsOnTable)
                    {
                        if (c.value == cardData.value) { canToss = true; break; }
                    }

                    if (canToss) AcceptAttackCard(cardMove);

                }

            }
            else
            {
                bool hasDefended = false;

                foreach (Transform tableCard in transform)
                {
                    if (tableCard.childCount == 0)
                    {
                        Card botCardData = tableCard.GetComponent<Card>();

                        bool isSameSuitHigher = (cardData.suit == botCardData.suit) && (cardData.value > botCardData.value);
                        bool isTrumpBeat = (botCardData.suit != gm.trumpSuit) && (cardData.suit == gm.trumpSuit);

                        if (isSameSuitHigher || isTrumpBeat)
                        {
                            cardMove.defaultParent = tableCard;
                            cardMove.transform.SetParent(tableCard, false);
                            cardMove.transform.localPosition = new Vector3(30, -30, 0);

                            CanvasGroup cg = cardMove.GetComponent<CanvasGroup>();
                            if (cg != null) cg.blocksRaycasts = false;

                            hasDefended = true;

                            EnemyAI bot = gm.GetComponent<EnemyAI>();
                            if (bot != null) bot.TryToAttack();

                            break;
                        }
                    }
                }
            }
        }
    }

    void AcceptAttackCard(CardMovement cardMove)
    {
        cardMove.defaultParent = this.transform;
        CanvasGroup cg = cardMove.GetComponent<CanvasGroup>();
        if (cg != null) cg.blocksRaycasts = false;

        EnemyAI bot = gm.GetComponent<EnemyAI>();
        if (bot != null && !gm.isBotTaking)
        {
            bot.TryToDefend();
        }
    }
}