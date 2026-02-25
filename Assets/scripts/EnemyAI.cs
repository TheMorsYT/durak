using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class EnemyAI : MonoBehaviour
{
    private GameManager gm;
    private int difficulty;
    private List<string> seenCards = new List<string>();

    void Start()
    {
        gm = FindObjectOfType<GameManager>();
        difficulty = PlayerPrefs.GetInt("BotDifficulty", 0);
        Debug.Log("Бот ініціалізований. Поточна складність: " + difficulty);
    }

    public void UpdateMemory()
    {
        if (difficulty < 2) return;

        foreach (Transform card in gm.discardPile)
        {
            string cardID = card.name;
            if (!seenCards.Contains(cardID))
            {
                seenCards.Add(cardID);
                Debug.Log("[Висока Складність] Бот запам'ятав карту у відбої: " + cardID);
            }
        }
    }

    public void TryToDefend() => StartCoroutine(DefendRoutine());
    public void TryToAttack() => StartCoroutine(AttackRoutine());

    IEnumerator DefendRoutine()
    {
        yield return new WaitForSeconds(Random.Range(0.8f, 1.5f));
        if (gm.tableArea.childCount == 0) yield break;

        Transform attackCardTransform = gm.tableArea.GetChild(gm.tableArea.childCount - 1);
        Card attackCard = attackCardTransform.GetComponent<Card>();
        List<Card> hand = GetHand();

        Card bestDefense = null;

        if (difficulty == 0)
        {
            bestDefense = hand.FirstOrDefault(c => CanBotBeat(attackCard, c));
        }
        else if (difficulty == 1)
        {
            var valid = hand.Where(c => CanBotBeat(attackCard, c)).ToList();
            bestDefense = valid.OrderBy(c => c.suit == gm.trumpSuit).ThenBy(c => (int)c.value).FirstOrDefault();
        }
        else
        {
            var valid = hand.Where(c => CanBotBeat(attackCard, c)).OrderBy(c => c.suit == gm.trumpSuit).ThenBy(c => (int)c.value).ToList();

            if (gm.deckArea.childCount < 5 && valid.Count > 0 && valid[0].suit == gm.trumpSuit && (int)valid[0].value > 10)
            {
                Debug.Log("[Висока Складність] Бот вирішив НЕ відбиватися і прийняти карти, щоб зберегти великий козир!");
                bestDefense = null;
            }
            else
            {
                bestDefense = valid.FirstOrDefault();
                if (bestDefense != null)
                    Debug.Log("[Висока Складність] Бот відбивається картою: " + bestDefense.name);
            }
        }

        if (bestDefense != null) ExecuteMove(bestDefense.transform, attackCardTransform, true);
        else gm.StartBotTakeTimer();
    }

    IEnumerator AttackRoutine()
    {
        yield return new WaitForSeconds(Random.Range(1.0f, 2.0f));

        if (gm.tableArea.childCount == 0)
        {
            Transform attack = SelectBestInitialAttack();
            if (attack != null) TossCardToTable(attack);
        }
        else
        {
            if (gm.tableArea.childCount >= gm.GetMaxAttackCards())
            {
                gm.SendToBito();
                yield break;
            }

            Transform toss = GetBestTossCard();
            if (toss != null) TossCardToTable(toss);
            else gm.SendToBito();
        }
    }

    private Transform SelectBestInitialAttack()
    {
        var hand = GetHand();
        if (difficulty == 0) return hand[0].transform;

        var nonTrumps = hand.Where(c => c.suit != gm.trumpSuit).OrderBy(c => (int)c.value).ToList();
        if (nonTrumps.Count > 0) return nonTrumps[0].transform;

        return hand.OrderBy(c => (int)c.value).First().transform;
    }

    private Transform GetBestTossCard()
    {
        var tableValues = gm.tableArea.GetComponentsInChildren<Card>().Select(c => c.value).Distinct();
        var hand = GetHand();
        var candidates = hand.Where(c => tableValues.Contains(c.value)).ToList();

        if (candidates.Count == 0) return null;

        if (difficulty == 0) return candidates[0].transform;

        if (difficulty == 1)
        {
            return candidates.Where(c => c.suit != gm.trumpSuit).OrderBy(c => (int)c.value).FirstOrDefault()?.transform;
        }

        var best = candidates.Where(c => c.suit != gm.trumpSuit).OrderBy(c => (int)c.value).FirstOrDefault();
        if (best != null)
        {
            if (gm.playerHand.childCount < 3 && (int)best.value < 10)
            {
                Debug.Log("[Висока Складність] Блеф! Бот навмисно не підкидає дрібну карту (" + best.name + "), бо у гравця залишилося мало карт.");
                return null;
            }
            Debug.Log("[Висока Складність] Бот стратегічно підкидає карту: " + best.name);
            return best.transform;
        }

        return null;
    }

    private bool CanBotBeat(Card attack, Card defense)
    {
        if (defense.value == Card.CardValue.Joker)
        {
            bool isTrumpRed = (gm.trumpSuit == Card.CardSuit.Hearts || gm.trumpSuit == Card.CardSuit.Diamonds);
            bool defRed = (defense.suit == Card.CardSuit.Hearts || defense.suit == Card.CardSuit.Diamonds);
            bool atkRed = (attack.suit == Card.CardSuit.Hearts || attack.suit == Card.CardSuit.Diamonds);

            if (defRed) return isTrumpRed || atkRed;
            return !isTrumpRed || !atkRed;
        }

        if (attack.value == Card.CardValue.Joker) return false;

        if (defense.suit == gm.trumpSuit && attack.suit != gm.trumpSuit) return true;

        if (attack.suit == defense.suit) return defense.value > attack.value;
        return false;
    }

    private List<Card> GetHand() => gm.enemyHand.Cast<Transform>().Select(t => t.GetComponent<Card>()).ToList();

    private void ExecuteMove(Transform card, Transform parent, bool isDef)
    {
        card.SetParent(parent, false);
        if (isDef) card.localPosition = new Vector3(30, -30, 0);
        card.GetComponent<Card>().FlipCard(true);
        card.GetComponent<CardMovement>().defaultParent = parent;

        CanvasGroup cg = card.GetComponent<CanvasGroup>();
        if (cg != null) cg.blocksRaycasts = false;
    }

    void TossCardToTable(Transform cardTrans)
    {
        cardTrans.SetParent(gm.tableArea, false);
        cardTrans.GetComponent<Card>().FlipCard(true);
        cardTrans.GetComponent<CardMovement>().defaultParent = gm.tableArea;

        CanvasGroup cg = cardTrans.GetComponent<CanvasGroup>();
        if (cg != null) cg.blocksRaycasts = false;
    }

    public IEnumerator TossAllPossibleCardsCoroutine()
    {
        while (gm.tableArea.childCount < gm.GetMaxAttackCards())
        {
            Transform card = GetBestTossCard();
            if (card == null) break;
            TossCardToTable(card);
            yield return new WaitForSeconds(0.6f);
        }
    }
}