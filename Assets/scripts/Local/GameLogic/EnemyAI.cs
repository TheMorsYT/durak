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

    private Coroutine currentThinkRoutine;

    void Start()
    {
        gm = Object.FindFirstObjectByType<GameManager>();
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

    public void TryToDefend()
    {
        if (currentThinkRoutine != null) StopCoroutine(currentThinkRoutine);
        currentThinkRoutine = StartCoroutine(DefendRoutine());
    }

    public void TryToAttack()
    {
        if (currentThinkRoutine != null) StopCoroutine(currentThinkRoutine);
        currentThinkRoutine = StartCoroutine(AttackRoutine());
    }

    IEnumerator DefendRoutine()
    {
        yield return new WaitForSeconds(Random.Range(1.2f, 2.0f));

        var tableCards = gm.GetTableAttackCards();
        if (tableCards.Count == 0 || gm.isTurnChanging) yield break;

        Transform attackCardTransform = null;
        foreach (Transform t in tableCards)
        {
            if (t.childCount == 0)
            {
                attackCardTransform = t;
                break;
            }
        }

        if (attackCardTransform == null) yield break;

        Card attackCard = attackCardTransform.GetComponent<Card>();
        List<Card> hand = GetHand();

        if (gm.isTransferMode && gm.IsTableUncovered() && gm.playerHand.childCount >= (tableCards.Count + 1))
        {
            Card transferCard = TryFindTransferCard(attackCard, hand);

            if (transferCard != null)
            {
                TossCardToTable(transferCard.transform);

                gm.isPlayerAttacker = false;
                gm.isBotTaking = false;
                yield break;
            }
        }

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

            bool needsMoreDefense = false;
            foreach (Transform t in gm.GetTableAttackCards())
            {
                if (t.childCount == 0)
                {
                    needsMoreDefense = true;
                    break;
                }
            }

            if (needsMoreDefense && !gm.isTurnChanging)
            {
                TryToDefend();
            }
        }
        else
        {
            gm.StartBotTakeTimer();
        }
    }

    private Card TryFindTransferCard(Card attackCard, List<Card> hand)
    {
        var possibleTransfers = hand.Where(c => c.value == attackCard.value).ToList();
        if (possibleTransfers.Count == 0)
        {
            return null;
        }

        var nonTrumpTransfer = possibleTransfers.FirstOrDefault(c => c.suit != gm.trumpSuit && c.value != Card.CardValue.Joker);
        if (nonTrumpTransfer != null)
        {
            return nonTrumpTransfer;
        }

        Card trumpTransfer = possibleTransfers.FirstOrDefault(c => c.suit == gm.trumpSuit);
        if (trumpTransfer != null)
        {
            if (difficulty == 0)
            {
                return trumpTransfer;
            }

            int dangerLevel = gm.GetTableAttackCards().Count;
            bool isEndGame = gm.deckArea.childCount == 0;
            int trumpValue = (int)trumpTransfer.value;

            if (isEndGame)
            {
                return trumpTransfer;
            }

            if (dangerLevel >= 3)
            {
                return trumpTransfer;
            }

            if (difficulty == 1)
            {
                if (trumpValue < 11)
                {
                    return trumpTransfer;
                }
                else
                {
                    return null;
                }
            }

            if (difficulty == 2)
            {
                if (trumpValue <= 9 && dangerLevel >= 2)
                {
                    return trumpTransfer;
                }
                else
                {
                    return null;
                }
            }
        }

        return null;
    }

    IEnumerator AttackRoutine()
    {
        yield return new WaitForSeconds(Random.Range(1.0f, 2.0f));
        if (gm.isTurnChanging) yield break;

        var tableCards = gm.GetTableAttackCards();

        if (difficulty == 2 && tableCards.Count > 0)
        {
            foreach (Transform attackT in tableCards)
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
                            if (knownPlayerVoids.Contains(atk.suit)) knownPlayerVoids.Remove(atk.suit);
                        }
                    }
                }
            }
        }

        if (tableCards.Count == 0)
        {
            if (gm.isPlayerAttacker)
            {
                yield break;
            }

            List<Transform> attacks = SelectInitialAttacks();

            if (attacks.Count > 0)
            {
                foreach (Transform atkCard in attacks)
                {
                    TossCardToTable(atkCard);
                    yield return new WaitForSeconds(0.4f);
                }
            }
        }
        else
        {
            if (tableCards.Count >= gm.GetMaxAttackCards())
            {
                gm.SendToBito();
                yield break;
            }

            List<Transform> tosses = GetTossCards();

            if (tosses.Count > 0)
            {
                foreach (Transform toss in tosses)
                {
                    if (gm.GetTableAttackCards().Count >= gm.GetMaxAttackCards()) break;
                    TossCardToTable(toss);
                    yield return new WaitForSeconds(0.4f);
                }
            }
            else
            {
                yield return new WaitForSeconds(0.8f);
                if (gm.AreAllCardsDefended())
                {
                    gm.SendToBito();
                }
            }
        }
    }

    private List<Card> GetKnownPlayerCards()
    {
        return gm.playerHand.Cast<Transform>().Select(t => t.GetComponent<Card>()).ToList();
    }

    private List<Transform> SelectInitialAttacks()
    {
        var hand = GetHand();
        List<Transform> selectedCards = new List<Transform>();
        if (hand.Count == 0) return selectedCards;

        if (difficulty == 0)
        {
            selectedCards.Add(hand[Random.Range(0, hand.Count)].transform);
            return selectedCards;
        }

        var safeCards = hand.Where(c => c.value != Card.CardValue.Joker).ToList();
        if (safeCards.Count == 0) safeCards = hand;
        var nonTrumps = safeCards.Where(c => c.suit != gm.trumpSuit).ToList();

        if (difficulty >= 1 && !gm.isTransferMode)
        {
            var grouped = nonTrumps.GroupBy(c => c.value)
                                   .Where(g => g.Count() > 1)
                                   .OrderByDescending(g => g.Count())
                                   .ThenBy(g => (int)g.Key)
                                   .FirstOrDefault();

            if (grouped != null)
            {
                int maxCanAttack = Mathf.Min(grouped.Count(), gm.GetMaxAttackCards());
                foreach (var card in grouped.Take(maxCanAttack))
                {
                    selectedCards.Add(card.transform);
                }
                return selectedCards;
            }
        }

        if (difficulty == 2)
        {
            bool isEndGame = gm.deckArea.childCount == 0;
            if (isEndGame)
            {
                var playerCards = GetKnownPlayerCards();
                var unblockableCards = hand.Where(myCard => !playerCards.Any(pCard => CanBeat(myCard, pCard))).ToList();
                if (unblockableCards.Count > 0)
                {
                    var nonTrumpUnblockable = unblockableCards.Where(c => c.suit != gm.trumpSuit && c.value != Card.CardValue.Joker).OrderBy(c => (int)c.value).FirstOrDefault();
                    if (nonTrumpUnblockable != null) { selectedCards.Add(nonTrumpUnblockable.transform); return selectedCards; }
                    selectedCards.Add(unblockableCards.OrderBy(c => (int)c.value).First().transform);
                    return selectedCards;
                }
            }

            var exploitVoids = nonTrumps.Where(c => knownPlayerVoids.Contains(c.suit)).OrderByDescending(c => (int)c.value).ToList();
            if (exploitVoids.Count > 0)
            {
                selectedCards.Add(exploitVoids.First().transform);
                return selectedCards;
            }
        }

        if (nonTrumps.Count > 0)
        {
            selectedCards.Add(nonTrumps.OrderBy(c => (int)c.value).First().transform);
        }
        else
        {
            selectedCards.Add(safeCards.OrderBy(c => (int)c.value).First().transform);
        }

        return selectedCards;
    }

    private List<Transform> GetTossCards()
    {
        var tableValues = gm.tableArea.GetComponentsInChildren<Card>().Select(c => c.value).Distinct().ToList();
        var hand = GetHand();
        List<Transform> tosses = new List<Transform>();

        var candidates = hand.Where(c => tableValues.Contains(c.value)).ToList();
        if (candidates.Count == 0) return tosses;

        if (difficulty == 0)
        {
            tosses.Add(candidates.FirstOrDefault()?.transform);
            return tosses;
        }

        bool isEndGame = gm.deckArea.childCount == 0;
        var safeTossCandidates = isEndGame ? candidates : candidates.Where(c => c.suit != gm.trumpSuit && c.value != Card.CardValue.Joker).ToList();

        if (safeTossCandidates.Count > 0)
        {
            var sortedCandidates = safeTossCandidates
                                    .OrderByDescending(c => hand.Count(h => h.value == c.value && h.suit != gm.trumpSuit))
                                    .ThenBy(c => (int)c.value)
                                    .ToList();

            int maxCanToss = gm.GetMaxAttackCards() - gm.GetTableAttackCards().Count;

            foreach (var cand in sortedCandidates.Take(maxCanToss))
            {
                if (difficulty == 2 && !isEndGame && (int)cand.value >= 11 && hand.Count(h => h.value == cand.value) == 1) continue;

                tosses.Add(cand.transform);
            }
        }

        return tosses;
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
        while (gm.GetTableAttackCards().Count < gm.GetMaxAttackCards())
        {
            List<Transform> tosses = GetTossCards();
            if (tosses.Count == 0) break;

            TossCardToTable(tosses[0]);
            yield return new WaitForSeconds(0.6f);
        }
    }
}