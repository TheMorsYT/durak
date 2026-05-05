using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Durak.Architecture.Shared.Events;
using Durak.Architecture.Shared.FSM;
using Durak.Architecture.Singleplayer.Core;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    private const float MinThinkDelaySeconds = 2.2f;
    private const float ThinkWatchdogIntervalSeconds = 0.25f;
    private const float StuckThinkTimeoutSeconds = 8f;

    private MatchControllerSP controller;
    private int difficulty;
    private readonly HashSet<string> seenCards = new HashSet<string>();
    private readonly HashSet<Card.CardSuit> knownPlayerVoids = new HashSet<Card.CardSuit>();
    private readonly List<IDisposable> subscriptions = new List<IDisposable>();

    [SerializeField] private bool enableDebugLogs = false;
    [SerializeField] private Vector2 thinkDelaySecondsRange = new Vector2(2.2f, 3.6f);
    [SerializeField] private Vector2 postActionDelaySecondsRange = new Vector2(0.9f, 1.6f);

    private Coroutine currentThinkRoutine;
    private bool subscribed;
    private float thinkWatchdogTimer;
    private float currentThinkElapsed;
    private float nextAllowedThinkAt;

    private void Awake()
    {
        difficulty = PlayerPrefs.GetInt("BotDifficulty", 0);
        TryResolveController();
    }

    private void Start()
    {
        SubscribeIfPossible();
        ScheduleIfBotNeedsAction();
    }

    private void OnEnable()
    {
        TryResolveController();
        SubscribeIfPossible();
    }

    private void OnDisable()
    {
        UnsubscribeAll();
        StopThinkRoutine();
    }

    private void Update()
    {
        if (currentThinkRoutine != null)
        {
            currentThinkElapsed += Mathf.Max(0f, Time.deltaTime);
            if (currentThinkElapsed >= StuckThinkTimeoutSeconds)
            {
                LogDebug($"Think coroutine timeout ({currentThinkElapsed:0.00}s). Restarting scheduler. {DescribeContext()}");
                StopThinkRoutine();
            }
        }

        thinkWatchdogTimer -= Time.deltaTime;
        if (thinkWatchdogTimer > 0f)
        {
            return;
        }

        thinkWatchdogTimer = ThinkWatchdogIntervalSeconds;
        if (currentThinkRoutine == null)
        {
            ScheduleIfBotNeedsAction();
        }
    }

    public void ForceScheduleNow()
    {
        TryResolveController();
        ScheduleIfBotNeedsAction();
    }

    public void UpdateMemory()
    {
        if (difficulty < 2 || controller == null || controller.DiscardPile == null)
        {
            return;
        }

        foreach (Transform card in controller.DiscardPile)
        {
            if (card != null)
            {
                seenCards.Add(card.name);
            }
        }
    }

    private void TryResolveController()
    {
        controller ??= MatchControllerSP.Instance;
        if (controller == null)
        {
            controller = FindFirstObjectByType<MatchControllerSP>();
        }
    }

    private void SubscribeIfPossible()
    {
        if (subscribed || controller == null || controller.EventBus == null)
        {
            return;
        }

        subscriptions.Add(controller.EventBus.Subscribe<MatchPhaseChangedEvent>(_ => ScheduleIfBotNeedsAction()));
        subscriptions.Add(controller.EventBus.Subscribe<TurnChangedEvent>(_ => ScheduleIfBotNeedsAction()));
        subscriptions.Add(controller.EventBus.Subscribe<TableChangedEvent>(_ => ScheduleIfBotNeedsAction()));
        subscriptions.Add(controller.EventBus.Subscribe<DealStateChangedEvent>(_ => ScheduleIfBotNeedsAction()));
        subscriptions.Add(controller.EventBus.Subscribe<GameOverChangedEvent>(_ => ScheduleIfBotNeedsAction()));
        subscriptions.Add(controller.EventBus.Subscribe<CardPlayedEvent>(_ => ScheduleIfBotNeedsAction()));
        subscribed = true;
    }

    private void UnsubscribeAll()
    {
        for (int i = 0; i < subscriptions.Count; i++)
        {
            subscriptions[i]?.Dispose();
        }

        subscriptions.Clear();
        subscribed = false;
    }

    private void ScheduleIfBotNeedsAction()
    {
        if (controller == null || controller.Context == null || controller.Context.IsGameOver || controller.Context.IsDealInProgress)
        {
            LogDebug("Schedule skipped: invalid context/game over/dealing.");
            StopThinkRoutine();
            return;
        }

        if (Time.time < nextAllowedThinkAt)
        {
            return;
        }

        bool botAttacker = controller.Context.Turn.AttackerId == MatchControllerSP.BotPlayerId;
        bool botDefender = controller.Context.Turn.DefenderId == MatchControllerSP.BotPlayerId;
        MatchPhase phase = controller.Context.Phase;

        bool shouldAct = phase switch
        {
            MatchPhase.Attacking => botAttacker,
            MatchPhase.Defending => botAttacker || botDefender,
            MatchPhase.FollowUpThrowIn => botAttacker,
            _ => false
        };

        LogDebug($"Schedule check: shouldAct={shouldAct}, phase={phase}, botAttacker={botAttacker}, botDefender={botDefender}, thinkRunning={currentThinkRoutine != null}");

        if (!shouldAct)
        {
            StopThinkRoutine();
            return;
        }

        RestartThinkRoutine(BotThinkAndActRoutine());
    }

    private IEnumerator BotThinkAndActRoutine()
    {
        float minDelay = Mathf.Max(MinThinkDelaySeconds, Mathf.Min(thinkDelaySecondsRange.x, thinkDelaySecondsRange.y));
        float maxDelay = Mathf.Max(minDelay, Mathf.Max(thinkDelaySecondsRange.x, thinkDelaySecondsRange.y));
        float thinkDelay = UnityEngine.Random.Range(minDelay, maxDelay);
        LogDebug($"Think scheduled in {thinkDelay:0.00}s. {DescribeContext()}");
        yield return new WaitForSeconds(thinkDelay);

        bool acted = false;
        try
        {
            if (controller == null || controller.Context == null || controller.Context.IsGameOver || controller.Context.IsDealInProgress)
            {
                LogDebug("Think canceled after delay due to invalid context/game over/dealing.");
            }
            else
            {
                ObserveDefenseOutcomes();

                MatchPhase phase = controller.Context.Phase;
                bool botAttacker = controller.Context.Turn.AttackerId == MatchControllerSP.BotPlayerId;
                bool botDefender = controller.Context.Turn.DefenderId == MatchControllerSP.BotPlayerId;
                LogDebug($"Think start: phase={phase}, botAttacker={botAttacker}, botDefender={botDefender}");

                if (phase == MatchPhase.Defending && botDefender)
                {
                    acted = ExecuteDefendDecision();
                }
                else if (phase == MatchPhase.FollowUpThrowIn && botAttacker)
                {
                    acted = ExecuteFollowUpDecision();
                }
                else if ((phase == MatchPhase.Attacking || phase == MatchPhase.Defending) && botAttacker)
                {
                    acted = ExecuteAttackDecision();
                }

                if (!acted)
                {
                    LogDebug($"Think produced no action. Ensuring phase timer. {DescribeContext()}");
                    controller.EnsurePhaseTimerRunning();
                }
                else
                {
                    float minPostDelay = Mathf.Max(0f, Mathf.Min(postActionDelaySecondsRange.x, postActionDelaySecondsRange.y));
                    float maxPostDelay = Mathf.Max(minPostDelay, Mathf.Max(postActionDelaySecondsRange.x, postActionDelaySecondsRange.y));
                    nextAllowedThinkAt = Time.time + UnityEngine.Random.Range(minPostDelay, maxPostDelay);
                }
            }
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            LogDebug($"Think exception: {exception.GetType().Name}: {exception.Message}");
        }
        finally
        {
            currentThinkRoutine = null;
            currentThinkElapsed = 0f;
            ScheduleIfBotNeedsAction();
        }
    }

    private bool ExecuteDefendDecision()
    {
        List<Transform> roots = controller.GetTableAttackCards();
        Transform uncoveredRoot = roots.FirstOrDefault(root => root != null && root.childCount == 0);
        if (uncoveredRoot == null)
        {
            LogDebug("Defend decision: no uncovered attack card.");
            return false;
        }

        Card attackCard = uncoveredRoot.GetComponent<Card>();
        if (attackCard == null)
        {
            LogDebug("Defend decision: uncovered root has no Card.");
            return false;
        }

        List<Card> botHand = controller.GetHandCards(MatchControllerSP.BotPlayerId);
        if (botHand.Count <= 0)
        {
            bool takeNoCards = controller.RequestTakeFromBot();
            LogDebug($"Defend decision: no cards in hand, take result={takeNoCards}");
            return takeNoCards;
        }

        Card transferCard = TryFindTransferCard(attackCard, botHand, roots.Count);
        if (transferCard != null)
        {
            bool transferAccepted = controller.RequestTransferFromBot(transferCard);
            LogDebug($"Defend decision: try transfer {DescribeCard(transferCard)}, result={transferAccepted}");
            if (transferAccepted)
            {
                return true;
            }
        }

        List<Card> validDefenseCards = botHand.Where(card => controller.CanCardBeat(card, attackCard)).ToList();
        Card bestDefense = SelectBestDefenseCard(validDefenseCards, attackCard, botHand);
        if (bestDefense != null)
        {
            bool defendAccepted = controller.RequestDefendCardFromBot(bestDefense, attackCard);
            LogDebug($"Defend decision: defend with {DescribeCard(bestDefense)} vs {DescribeCard(attackCard)}, result={defendAccepted}");
            if (defendAccepted)
            {
                return true;
            }
        }

        bool takeAccepted = controller.RequestTakeFromBot();
        LogDebug($"Defend decision: fallback take result={takeAccepted}");
        return takeAccepted;
    }

    private bool ExecuteAttackDecision()
    {
        List<Transform> roots = controller.GetTableAttackCards();
        if (roots.Count == 0)
        {
            Card openingCard = SelectInitialAttackCard();
            if (openingCard != null)
            {
                bool playedOpening = controller.RequestPlayCardFromBot(openingCard);
                LogDebug($"Attack decision: opening {DescribeCard(openingCard)}, result={playedOpening}");
                return playedOpening;
            }

            LogDebug("Attack decision: no opening card selected.");
            return false;
        }

        if (roots.Count >= controller.GetMaxAttackCardsLimit())
        {
            bool voteAtLimit = controller.RequestVoteBitoFromBot();
            LogDebug($"Attack decision: max attack reached, vote bito result={voteAtLimit}");
            return voteAtLimit;
        }

        Card tossCard = SelectTossCard();
        if (tossCard != null)
        {
            bool tossed = controller.RequestPlayCardFromBot(tossCard);
            LogDebug($"Attack decision: toss {DescribeCard(tossCard)}, result={tossed}");
            return tossed;
        }

        if (controller.AreAllCardsDefended())
        {
            bool voteAllDefended = controller.RequestVoteBitoFromBot();
            LogDebug($"Attack decision: all defended, vote bito result={voteAllDefended}");
            return voteAllDefended;
        }

        LogDebug("Attack decision: no legal toss and not all defended.");
        return false;
    }

    private bool ExecuteFollowUpDecision()
    {
        Card tossCard = SelectTossCard();
        if (tossCard != null)
        {
            bool tossed = controller.RequestPlayCardFromBot(tossCard);
            LogDebug($"Follow-up decision: toss {DescribeCard(tossCard)}, result={tossed}");
            if (tossed)
            {
                return true;
            }
        }

        bool vote = controller.RequestVoteBitoFromBot();
        LogDebug($"Follow-up decision: vote bito result={vote}");
        return vote;
    }

    private Card SelectInitialAttackCard()
    {
        List<Card> hand = controller.GetHandCards(MatchControllerSP.BotPlayerId);
        if (hand.Count <= 0)
        {
            return null;
        }

        List<Card> nonTrump = hand
            .Where(c => c.value != Card.CardValue.Joker && c.suit != controller.TrumpSuit)
            .OrderBy(c => (int)c.value)
            .ToList();

        if (difficulty <= 0)
        {
            if (nonTrump.Count > 0)
            {
                int candidateCount = Mathf.Min(3, nonTrump.Count);
                return nonTrump[UnityEngine.Random.Range(0, candidateCount)];
            }

            List<Card> lowRisk = hand
                .Where(c => c.value != Card.CardValue.Joker)
                .OrderBy(c => c.suit == controller.TrumpSuit ? 1 : 0)
                .ThenBy(c => (int)c.value)
                .ToList();

            return lowRisk.Count > 0 ? lowRisk[0] : hand[0];
        }

        if (difficulty >= 1 && !controller.IsTransferModeEnabled)
        {
            IGrouping<Card.CardValue, Card> grouped = nonTrump
                .GroupBy(c => c.value)
                .OrderByDescending(g => g.Count())
                .ThenBy(g => (int)g.Key)
                .FirstOrDefault(g => g.Count() > 1);

            if (grouped != null)
            {
                return grouped.FirstOrDefault();
            }
        }

        if (difficulty >= 2)
        {
            Card exploitVoid = nonTrump
                .Where(card => knownPlayerVoids.Contains(card.suit))
                .OrderByDescending(card => (int)card.value)
                .FirstOrDefault();

            if (exploitVoid != null)
            {
                return exploitVoid;
            }

            bool isEndGame = controller.DeckArea == null || controller.DeckArea.childCount == 0;
            if (isEndGame)
            {
                List<Card> playerCards = controller.GetHandCards(MatchControllerSP.LocalPlayerId);
                List<Card> unblockableCards = hand
                    .Where(myCard => !playerCards.Any(playerCard => controller.CanCardBeat(playerCard, myCard)))
                    .OrderBy(card => (int)card.value)
                    .ToList();

                if (unblockableCards.Count > 0)
                {
                    Card nonTrumpUnblockable = unblockableCards
                        .Where(card => card.suit != controller.TrumpSuit && card.value != Card.CardValue.Joker)
                        .OrderBy(card => (int)card.value)
                        .FirstOrDefault();

                    return nonTrumpUnblockable ?? unblockableCards[0];
                }
            }
        }

        if (nonTrump.Count > 0)
        {
            return nonTrump[0];
        }

        List<Card> lowRiskFallback = hand
            .Where(c => c.value != Card.CardValue.Joker)
            .OrderBy(c => c.suit == controller.TrumpSuit ? 1 : 0)
            .ThenBy(c => (int)c.value)
            .ToList();

        return lowRiskFallback.Count > 0 ? lowRiskFallback[0] : hand[0];
    }

    private Card SelectTossCard()
    {
        List<Card> hand = controller.GetHandCards(MatchControllerSP.BotPlayerId);
        if (hand.Count <= 0)
        {
            return null;
        }

        HashSet<Card.CardValue> tableValues = new HashSet<Card.CardValue>();
        Card[] tableCards = controller.TableArea != null
            ? controller.TableArea.GetComponentsInChildren<Card>(true)
            : Array.Empty<Card>();
        for (int i = 0; i < tableCards.Length; i++)
        {
            tableValues.Add(tableCards[i].value);
        }

        List<Card> candidates = hand.Where(c => tableValues.Contains(c.value)).ToList();
        if (candidates.Count <= 0)
        {
            return null;
        }

        if (difficulty <= 0)
        {
            return candidates[0];
        }

        bool endGame = controller.DeckArea == null || controller.DeckArea.childCount == 0;
        List<Card> safe = endGame
            ? candidates
            : candidates.Where(c => c.suit != controller.TrumpSuit && c.value != Card.CardValue.Joker).ToList();
        if (safe.Count <= 0)
        {
            safe = candidates;
        }

        return safe
            .OrderByDescending(c => hand.Count(h => h.value == c.value && h.suit != controller.TrumpSuit))
            .ThenBy(c => (int)c.value)
            .FirstOrDefault();
    }

    private Card TryFindTransferCard(Card attackCard, List<Card> hand, int currentAttackRoots)
    {
        if (!controller.IsTransferModeEnabled)
        {
            return null;
        }

        List<Card> candidates = hand.Where(c => c.value == attackCard.value).ToList();
        if (candidates.Count <= 0)
        {
            return null;
        }

        if (controller.GetHandCards(MatchControllerSP.LocalPlayerId).Count < currentAttackRoots + 1)
        {
            return null;
        }

        Card nonTrump = candidates.FirstOrDefault(c => c.suit != controller.TrumpSuit && c.value != Card.CardValue.Joker);
        if (nonTrump != null)
        {
            return nonTrump;
        }

        Card trump = candidates.FirstOrDefault(c => c.suit == controller.TrumpSuit);
        if (trump == null || difficulty <= 0)
        {
            return trump;
        }

        bool endGame = controller.DeckArea == null || controller.DeckArea.childCount == 0;
        int trumpValue = (int)trump.value;
        if (endGame || currentAttackRoots >= 3)
        {
            return trump;
        }

        if (difficulty == 1 && trumpValue < 11)
        {
            return trump;
        }

        if (difficulty >= 2 && trumpValue <= 9 && currentAttackRoots >= 2)
        {
            return trump;
        }

        return null;
    }

    private Card SelectBestDefenseCard(List<Card> validDefenseCards, Card attackCard, List<Card> hand)
    {
        if (validDefenseCards == null || validDefenseCards.Count <= 0)
        {
            return null;
        }

        if (difficulty <= 0)
        {
            return validDefenseCards[0];
        }

        if (difficulty == 1)
        {
            return validDefenseCards
                .OrderBy(card => card.suit == controller.TrumpSuit)
                .ThenBy(card => (int)card.value)
                .FirstOrDefault();
        }

        List<Card> duplicateValues = hand
            .Where(card => card.suit != controller.TrumpSuit && card.value != Card.CardValue.Joker)
            .GroupBy(card => card.value)
            .Where(group => group.Count() > 1)
            .SelectMany(group => group)
            .ToList();

        Card best = validDefenseCards
            .OrderBy(card => card.value == Card.CardValue.Joker)
            .ThenBy(card => card.suit == controller.TrumpSuit)
            .ThenBy(card => duplicateValues.Contains(card))
            .ThenBy(card => (int)card.value)
            .FirstOrDefault();

        if (best == null)
        {
            return null;
        }

        bool isJoker = best.value == Card.CardValue.Joker;
        bool isHighTrump = best.suit == controller.TrumpSuit && (int)best.value >= 11;
        bool cheapAttack = attackCard.suit != controller.TrumpSuit && (int)attackCard.value < 11;
        bool deckRich = controller.DeckArea != null && controller.DeckArea.childCount > 6;

        return (isJoker || isHighTrump) && cheapAttack && deckRich ? null : best;
    }

    private void ObserveDefenseOutcomes()
    {
        if (difficulty < 2)
        {
            return;
        }

        List<Transform> roots = controller.GetTableAttackCards();
        for (int i = 0; i < roots.Count; i++)
        {
            Transform attackRoot = roots[i];
            if (attackRoot == null || attackRoot.childCount <= 0)
            {
                continue;
            }

            Card attack = attackRoot.GetComponent<Card>();
            Card defense = attackRoot.GetChild(0).GetComponent<Card>();
            if (attack == null || defense == null)
            {
                continue;
            }

            if (attack.suit != controller.TrumpSuit && defense.suit == controller.TrumpSuit)
            {
                knownPlayerVoids.Add(attack.suit);
            }
            else if (defense.suit == attack.suit && defense.value != Card.CardValue.Joker)
            {
                knownPlayerVoids.Remove(attack.suit);
            }
        }
    }

    private void RestartThinkRoutine(IEnumerator routine)
    {
        StopThinkRoutine();
        currentThinkElapsed = 0f;
        currentThinkRoutine = StartCoroutine(routine);
        LogDebug("Think routine started.");
    }

    private void StopThinkRoutine()
    {
        if (currentThinkRoutine != null)
        {
            StopCoroutine(currentThinkRoutine);
            currentThinkRoutine = null;
            currentThinkElapsed = 0f;
            LogDebug("Think routine stopped.");
        }
    }

    private void LogDebug(string message)
    {
        if (!enableDebugLogs)
        {
            return;
        }

#if DURAK_VERBOSE_LOGS
        Debug.Log($"[EnemyAI] {message}");
#endif
    }

    private string DescribeContext()
    {
        if (controller == null || controller.Context == null)
        {
            return "ctx=null";
        }

        MatchPhase phase = controller.Context.Phase;
        ulong attacker = controller.Context.Turn.AttackerId;
        ulong defender = controller.Context.Turn.DefenderId;
        bool timerRunning = controller.Context.Timer.IsRunning;
        float timerRemain = controller.Context.Timer.RemainingSeconds;
        return $"phase={phase}, attacker={attacker}, defender={defender}, timerRunning={timerRunning}, timerRemain={timerRemain:0.00}";
    }

    private static string DescribeCard(Card card)
    {
        return card == null ? "null" : $"{card.suit}/{card.value}";
    }
}
