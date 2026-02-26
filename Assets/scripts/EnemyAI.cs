using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class EnemyAI : MonoBehaviour
{
    private GameManager gm;
    private int difficulty;
    private List<string> seenCards = new List<string>();
    private List<Card.CardSuit> knownPlayerVoids = new List<Card.CardSuit>();

    void Start()
    {
        gm = FindObjectOfType<GameManager>();
        difficulty = PlayerPrefs.GetInt("BotDifficulty", 0);
        knownPlayerVoids.Clear();
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
            }
            else if (difficulty == 1)
            {
                bestDefense = validCards.OrderBy(c => c.suit == gm.trumpSuit).ThenBy(c => (int)c.value).FirstOrDefault();
            }
            else
            {
                var allPairs = hand.Where(c => c.suit != gm.trumpSuit && c.value != Card.CardValue.Joker)
                                   .GroupBy(c => c.value)
                                   .Where(g => g.Count() > 1)
                                   .SelectMany(g => g)
                                   .ToList();

                bestDefense = validCards.OrderBy(c => c.value == Card.CardValue.Joker)
                                        .ThenBy(c => c.suit == gm.trumpSuit)
                                        .ThenBy(c => allPairs.Contains(c))
                                        .ThenBy(c => (int)c.value)
                                        .FirstOrDefault();

                if (bestDefense != null)
                {
                    bool isJoker = bestDefense.value == Card.CardValue.Joker;
                    bool isHighTrump = bestDefense.suit == gm.trumpSuit && (int)bestDefense.value >= 11;
                    bool isAttackCheap = attackCard.suit != gm.trumpSuit && (int)attackCard.value < 11;

                    if ((isJoker || isHighTrump) && isAttackCheap && gm.deckArea.childCount > 6)
                    {
                        bestDefense = null;
                    }
                }
            }
        }

        if (bestDefense != null)
        {
            ExecuteMove(bestDefense.transform, attackCardTransform, true);
        }
        else
        {
            gm.StartBotTakeTimer();
        }
    }

    IEnumerator AttackRoutine()
    {
        yield return new WaitForSeconds(Random.Range(1.0f, 2.0f));

        if (difficulty == 2 && gm.tableArea.childCount > 0)
        {
            foreach (Transform attackT in gm.tableArea)
            {
                if (attackT.childCount > 0)
                {
                    Card atk = attackT.GetComponent<Card>();
                    Card def = attackT.GetChild(0).GetComponent<Card>();
                    if (atk != null && def != null)
                    {
                        if (atk.suit != gm.trumpSuit && def.suit == gm.trumpSuit)
                        {
                            if (!knownPlayerVoids.Contains(atk.suit))
                            {
                                knownPlayerVoids.Add(atk.suit);
                            }
                        }
                        else if (def.suit == atk.suit && def.value != Card.CardValue.Joker)
                        {
                            if (knownPlayerVoids.Contains(atk.suit))
                            {
                                knownPlayerVoids.Remove(atk.suit);
                            }
                        }
                    }
                }
            }
        }

        if (gm.tableArea.childCount == 0)
        {
            Transform attack = SelectBestInitialAttack();
            if (attack != null)
            {
                TossCardToTable(attack);
            }
        }
        else
        {
            if (gm.tableArea.childCount >= gm.GetMaxAttackCards())
            {
                gm.SendToBito();
                yield break;
            }

            Transform toss = GetBestTossCard();
            if (toss != null)
            {
                TossCardToTable(toss);
            }
            else
            {
                gm.SendToBito();
            }
        }
    }

    private List<Card> GetKnownPlayerCards()
    {
        return gm.playerHand.Cast<Transform>().Select(t => t.GetComponent<Card>()).ToList();
    }

    private Transform SelectBestInitialAttack()
    {
        var hand = GetHand();

        if (difficulty == 0)
        {
            return hand[Random.Range(0, hand.Count)].transform;
        }

        var safeCards = hand.Where(c => c.value != Card.CardValue.Joker).ToList();
        if (safeCards.Count == 0) safeCards = hand;
        var nonTrumps = safeCards.Where(c => c.suit != gm.trumpSuit).ToList();

        if (difficulty == 2)
        {
            bool isEndGame = gm.deckArea.childCount == 0;

            var grouped = nonTrumps.GroupBy(c => c.value)
                                   .Where(g => g.Count() > 1)
                                   .OrderByDescending(g => g.Count())
                                   .ThenBy(g => (int)g.Key)
                                   .FirstOrDefault();
            if (grouped != null)
            {
                return grouped.First().transform;
            }

            if (isEndGame)
            {
                var playerCards = GetKnownPlayerCards();
                var unblockableCards = hand.Where(myCard => !playerCards.Any(pCard => CanBeat(myCard, pCard))).ToList();

                if (unblockableCards.Count > 0)
                {
                    var nonTrumpUnblockable = unblockableCards.Where(c => c.suit != gm.trumpSuit && c.value != Card.CardValue.Joker).OrderBy(c => (int)c.value).FirstOrDefault();
                    if (nonTrumpUnblockable != null) return nonTrumpUnblockable.transform;

                    return unblockableCards.OrderBy(c => (int)c.value).First().transform;
                }
            }

            var exploitVoids = nonTrumps.Where(c => knownPlayerVoids.Contains(c.suit)).OrderByDescending(c => (int)c.value).ToList();
            if (exploitVoids.Count > 0)
            {
                return exploitVoids.First().transform;
            }

            if (nonTrumps.Count > 0)
            {
                return nonTrumps.OrderBy(c => (int)c.value).First().transform;
            }
        }

        var mediumPaired = nonTrumps.GroupBy(c => c.value).Where(g => g.Count() > 1).OrderBy(g => (int)g.Key).FirstOrDefault();
        if (mediumPaired != null)
        {
            return mediumPaired.First().transform;
        }

        if (nonTrumps.Count > 0)
        {
            return nonTrumps.OrderBy(c => (int)c.value).First().transform;
        }

        return safeCards.OrderBy(c => (int)c.value).First().transform;
    }

    private Transform GetBestTossCard()
    {
        var tableValues = gm.tableArea.GetComponentsInChildren<Card>().Select(c => c.value).Distinct();
        var hand = GetHand();
        var candidates = hand.Where(c => tableValues.Contains(c.value)).ToList();

        if (candidates.Count == 0) return null;

        if (difficulty == 0)
        {
            return candidates.FirstOrDefault()?.transform;
        }

        if (difficulty == 1)
        {
            return candidates.Where(c => c.suit != gm.trumpSuit && c.value != Card.CardValue.Joker).OrderBy(c => (int)c.value).FirstOrDefault()?.transform;
        }

        if (difficulty == 2)
        {
            bool isEndGame = gm.deckArea.childCount == 0;
            var safeTossCandidates = isEndGame ? candidates : candidates.Where(c => c.suit != gm.trumpSuit && c.value != Card.CardValue.Joker).ToList();

            if (safeTossCandidates.Count == 0) return null;

            if (isEndGame)
            {
                var playerCards = GetKnownPlayerCards();
                var unblockableToss = safeTossCandidates.Where(cand => !playerCards.Any(p => CanBeat(cand, p))).OrderBy(c => (int)c.value).FirstOrDefault();
                if (unblockableToss != null) return unblockableToss.transform;
            }

            var best = safeTossCandidates.OrderByDescending(c => hand.Count(h => h.value == c.value && h.suit != gm.trumpSuit && h.value != Card.CardValue.Joker))
                                         .ThenBy(c => (int)c.value)
                                         .FirstOrDefault();

            if (best != null)
            {
                if (!isEndGame && (int)best.value >= 11 && hand.Count(h => h.value == best.value) == 1) return null;
                return best.transform;
            }
        }

        return null;
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
        if (SoundManager.Instance != null) SoundManager.Instance.PlayCardToTable();
    }

    void TossCardToTable(Transform cardTrans)
    {
        cardTrans.SetParent(gm.tableArea, false);
        cardTrans.GetComponent<Card>().FlipCard(true);
        cardTrans.GetComponent<CardMovement>().defaultParent = gm.tableArea;

        CanvasGroup cg = cardTrans.GetComponent<CanvasGroup>();
        if (cg != null) cg.blocksRaycasts = false;

        gm.CheckWinCondition();
        if (SoundManager.Instance != null) SoundManager.Instance.PlayCardToTable();
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