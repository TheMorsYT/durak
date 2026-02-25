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
        Debug.Log($"--- БОТ ЗАПУЩЕНИЙ. Складність: {difficulty} ---");
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
            }
        }

        int trumpsGone = seenCards.Count(c => c.Contains(gm.trumpSuit.ToString()) || c.Contains("Joker"));
        Debug.Log($"[Аналітика] Бот знає, що у відбій пішло козирів: {trumpsGone}");
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

        var validCards = hand.Where(c => CanBeat(attackCard, c)).ToList();
        Card bestDefense = null;

        if (validCards.Count > 0)
        {
            if (difficulty == 0)
            {
                bestDefense = validCards.FirstOrDefault();
                Debug.Log($"[Легкий] Відбиваюсь тим, що перше під руку попалося: {bestDefense.name}");
            }
            else if (difficulty == 1)
            {
                bestDefense = validCards.OrderBy(c => c.suit == gm.trumpSuit).ThenBy(c => (int)c.value).FirstOrDefault();
            }
            else
            {
                bestDefense = validCards.OrderBy(c => c.value == Card.CardValue.Joker)
                                        .ThenBy(c => c.suit == gm.trumpSuit)
                                        .ThenBy(c => (int)c.value)
                                        .FirstOrDefault();

                if (bestDefense != null)
                {
                    bool isJoker = bestDefense.value == Card.CardValue.Joker;
                    bool isHighTrump = bestDefense.suit == gm.trumpSuit && (int)bestDefense.value >= 11;

                    if ((isJoker || isHighTrump) && gm.deckArea.childCount > 4)
                    {
                        Debug.Log($"[Високий] Ти хочеш витягнути з мене {bestDefense.name}? Не вийде, я краще заберу карти.");
                        bestDefense = null;
                    }
                    else
                    {
                        Debug.Log($"[Високий] Ідеальна карта для захисту: {bestDefense.name}");
                    }
                }
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

    private List<Card> GetKnownPlayerCards()
    {
        return gm.playerHand.Cast<Transform>().Select(t => t.GetComponent<Card>()).ToList();
    }

    private Transform SelectBestInitialAttack()
    {
        var hand = GetHand();

        if (difficulty == 0) return hand[Random.Range(0, hand.Count)].transform;

        if (difficulty == 2 && gm.deckArea.childCount == 0)
        {
            var playerCards = GetKnownPlayerCards();
            var mySortedCards = hand.OrderBy(c => c.suit == gm.trumpSuit).ThenBy(c => (int)c.value).ToList();

            foreach (var myCard in mySortedCards)
            {
                bool playerCanBeat = playerCards.Any(pCard => CanBeat(myCard, pCard));
                if (!playerCanBeat)
                {
                    Debug.Log($"[ЕНДШПІЛЬ] Я знаю, що тобі нічим бити {myCard.name}. Отримуй!");
                    return myCard.transform;
                }
            }
        }

        var safeCards = hand.Where(c => c.value != Card.CardValue.Joker).ToList();
        if (safeCards.Count == 0) safeCards = hand;

        var nonTrumps = safeCards.Where(c => c.suit != gm.trumpSuit).ToList();

        if (difficulty == 2 && nonTrumps.Count > 0)
        {
            // 1. Пошук парних карт
            var paired = nonTrumps.GroupBy(c => c.value)
                                  .Where(g => g.Count() > 1)
                                  .OrderBy(g => (int)g.Key)
                                  .FirstOrDefault();

            var lowestCard = nonTrumps.OrderBy(c => (int)c.value).First();

            if (paired != null)
            {
                int pairValue = (int)paired.Key;
                int lowestValue = (int)lowestCard.value;

                // ФІКС: Б'ємо парою тільки якщо це дрібна пара (менше Валета) 
                // АБО якщо ця пара і так є нашими найменшими картами в руці
                if (pairValue < 11 || pairValue <= lowestValue + 1)
                {
                    Debug.Log($"[Високий] Атакую ПАРНОЮ картою {paired.First().name}, щоб потім гарантовано підкинути другу!");
                    return paired.First().transform;
                }
                else
                {
                    Debug.Log($"[Високий] Маю пару {paired.First().name}, але на початку гри це занадто жирно. Піду з найменшої: {lowestCard.name}");
                }
            }

            // 2. Витягування козирів великими картами (Туз/Король)
            var highCards = nonTrumps.Where(c => (int)c.value >= 13).OrderByDescending(c => (int)c.value).ToList();
            foreach (var highCard in highCards)
            {
                int suitGone = seenCards.Count(c => c.Contains(highCard.suit.ToString()));
                if (suitGone > 3)
                {
                    Debug.Log($"[Високий] Кидаю {highCard.name}. Знаю, що цієї масті мало, зараз ти витратиш козира!");
                    return highCard.transform;
                }
            }
        }

        if (nonTrumps.Count > 0) return nonTrumps.OrderBy(c => (int)c.value).First().transform;

        return safeCards.OrderBy(c => (int)c.value).First().transform;
    }

    private Transform GetBestTossCard()
    {
        var tableValues = gm.tableArea.GetComponentsInChildren<Card>().Select(c => c.value).Distinct();
        var hand = GetHand();
        var candidates = hand.Where(c => tableValues.Contains(c.value)).ToList();

        if (candidates.Count == 0) return null;

        if (difficulty == 0) return candidates.FirstOrDefault()?.transform;

        if (difficulty == 1)
        {
            return candidates.Where(c => c.suit != gm.trumpSuit && c.value != Card.CardValue.Joker).OrderBy(c => (int)c.value).FirstOrDefault()?.transform;
        }

        if (difficulty == 2 && gm.deckArea.childCount == 0)
        {
            var playerCards = GetKnownPlayerCards();
            foreach (var cand in candidates)
            {
                bool playerCanBeat = playerCards.Any(pCard => CanBeat(cand, pCard));
                if (!playerCanBeat)
                {
                    Debug.Log($"[ЕНДШПІЛЬ] Підкидаю {cand.name}! Тобі кінець.");
                    return cand.transform;
                }
            }
        }

        var best = candidates.Where(c => c.suit != gm.trumpSuit && c.value != Card.CardValue.Joker).OrderBy(c => (int)c.value).FirstOrDefault();

        if (best == null)
        {
            best = candidates.Where(c => c.suit == gm.trumpSuit && c.value != Card.CardValue.Joker && (int)c.value < 11).OrderBy(c => (int)c.value).FirstOrDefault();
        }

        if (best != null && difficulty == 2)
        {
            if (gm.playerHand.childCount < 3 && (int)best.value < 10 && gm.deckArea.childCount == 0)
            {
                Debug.Log($"[Високий] БЛЕФ! Маю {best.name}, але не підкину, щоб ти не відбився легкою картою.");
                return null;
            }
            return best.transform;
        }

        return best?.transform;
    }

    private bool CanBeat(Card attack, Card defense)
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

        gm.CheckWinCondition();
    }

    void TossCardToTable(Transform cardTrans)
    {
        cardTrans.SetParent(gm.tableArea, false);
        cardTrans.GetComponent<Card>().FlipCard(true);
        cardTrans.GetComponent<CardMovement>().defaultParent = gm.tableArea;

        CanvasGroup cg = cardTrans.GetComponent<CanvasGroup>();
        if (cg != null) cg.blocksRaycasts = false;

        gm.CheckWinCondition();
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