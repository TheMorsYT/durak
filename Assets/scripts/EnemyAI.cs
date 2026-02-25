using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    private GameManager gm;

    void Start()
    {
        gm = FindObjectOfType<GameManager>();
    }

    public void TryToDefend()
    {
        StartCoroutine(DefendRoutine());
    }

    IEnumerator DefendRoutine()
    {
        float thinkTime = Random.Range(1.0f, 2.0f);
        yield return new WaitForSeconds(thinkTime);

        if (gm.tableArea.childCount == 0) yield break;

        Transform attackCardTransform = gm.tableArea.GetChild(gm.tableArea.childCount - 1);
        Card attackCard = attackCardTransform.GetComponent<Card>();

        bool hasDefended = false;

        for (int i = 0; i < gm.enemyHand.childCount; i++)
        {
            Transform botCardTransform = gm.enemyHand.GetChild(i);
            Card botCard = botCardTransform.GetComponent<Card>();

            bool isSameSuitHigherValue = (botCard.suit == attackCard.suit) && (botCard.value > attackCard.value);
            bool isTrumpBeat = (attackCard.suit != gm.trumpSuit) && (botCard.suit == gm.trumpSuit);

            if (isSameSuitHigherValue || isTrumpBeat)
            {
                botCardTransform.SetParent(attackCardTransform, false);
                botCardTransform.localPosition = new Vector3(30, -30, 0);
                botCard.FlipCard(true);
                botCardTransform.GetComponent<CardMovement>().defaultParent = attackCardTransform;

                CanvasGroup cg = botCardTransform.GetComponent<CanvasGroup>();
                if (cg != null) cg.blocksRaycasts = false;

                hasDefended = true;
                break;
            }
        }

        if (!hasDefended)
        {

            gm.StartBotTakeTimer();
        }
    }

    public void TryToAttack()
    {
        StartCoroutine(AttackRoutine());
    }

    IEnumerator AttackRoutine()
    {
        yield return new WaitForSeconds(Random.Range(1.0f, 2.0f));

        if (gm.tableArea.childCount == 0)
        {
            if (gm.enemyHand.childCount > 0) TossCardToTable(gm.enemyHand.GetChild(0));
        }
        else
        {

            if (gm.tableArea.childCount >= gm.GetMaxAttackCards())
            {
                gm.SendToBito();
                yield break;
            }

            bool tossed = false;
            Card[] tableCards = gm.tableArea.GetComponentsInChildren<Card>();

            for (int i = 0; i < gm.enemyHand.childCount; i++)
            {
                Transform botCardTrans = gm.enemyHand.GetChild(i);
                Card botCard = botCardTrans.GetComponent<Card>();

                foreach (Card tc in tableCards)
                {
                    if (botCard.value == tc.value)
                    {
                        TossCardToTable(botCardTrans);
                        tossed = true;
                        break;
                    }
                }
                if (tossed) break;
            }

            if (!tossed) gm.SendToBito();
        }
    }


    public IEnumerator TossAllPossibleCardsCoroutine()
    {
        bool tossed = true;
        while (tossed && gm.tableArea.childCount < gm.GetMaxAttackCards())
        {
            tossed = false;
            Card[] tableCards = gm.tableArea.GetComponentsInChildren<Card>();

            for (int i = 0; i < gm.enemyHand.childCount; i++)
            {
                Transform botCardTrans = gm.enemyHand.GetChild(i);
                Card botCard = botCardTrans.GetComponent<Card>();

                foreach (Card tc in tableCards)
                {
                    if (botCard.value == tc.value)
                    {
                        TossCardToTable(botCardTrans);
                        tossed = true;
                        yield return new WaitForSeconds(0.6f);
                        break;
                    }
                }
                if (tossed) break;
            }
        }
    }

    void TossCardToTable(Transform cardTrans)
    {
        Card cardData = cardTrans.GetComponent<Card>();
        cardTrans.SetParent(gm.tableArea, false);
        cardData.FlipCard(true);
        cardTrans.GetComponent<CardMovement>().defaultParent = gm.tableArea;

        CanvasGroup cg = cardTrans.GetComponent<CanvasGroup>();
        if (cg != null) cg.blocksRaycasts = false;
    }
}