using System.Collections.Generic;
using Durak.Architecture.Shared.Domain;
using Durak.Architecture.Shared.FSM;

namespace Durak.Architecture.Shared.Events
{
    public readonly struct MatchPhaseChangedEvent
    {
        public readonly MatchPhase PreviousPhase;
        public readonly MatchPhase CurrentPhase;

        public MatchPhaseChangedEvent(MatchPhase previousPhase, MatchPhase currentPhase)
        {
            PreviousPhase = previousPhase;
            CurrentPhase = currentPhase;
        }
    }

    public readonly struct TurnChangedEvent
    {
        public readonly ulong AttackerId;
        public readonly ulong DefenderId;
        public readonly bool FirstAttackWave;
        public readonly bool IsDefenderTaking;
        public readonly bool AttackerPassed;

        public TurnChangedEvent(
            ulong attackerId,
            ulong defenderId,
            bool firstAttackWave,
            bool isDefenderTaking,
            bool attackerPassed)
        {
            AttackerId = attackerId;
            DefenderId = defenderId;
            FirstAttackWave = firstAttackWave;
            IsDefenderTaking = isDefenderTaking;
            AttackerPassed = attackerPassed;
        }
    }

    public readonly struct TimerChangedEvent
    {
        public readonly float DurationSeconds;
        public readonly float RemainingSeconds;
        public readonly ulong OwnerClientId;
        public readonly TurnTimerRole Role;
        public readonly bool IsRunning;

        public TimerChangedEvent(
            float durationSeconds,
            float remainingSeconds,
            ulong ownerClientId,
            TurnTimerRole role,
            bool isRunning)
        {
            DurationSeconds = durationSeconds;
            RemainingSeconds = remainingSeconds;
            OwnerClientId = ownerClientId;
            Role = role;
            IsRunning = isRunning;
        }
    }

    public readonly struct TableChangedEvent
    {
        public readonly int AttackCardsCount;
        public readonly int DefendedCardsCount;
        public readonly bool AllCardsDefended;

        public TableChangedEvent(int attackCardsCount, int defendedCardsCount, bool allCardsDefended)
        {
            AttackCardsCount = attackCardsCount;
            DefendedCardsCount = defendedCardsCount;
            AllCardsDefended = allCardsDefended;
        }
    }

    public readonly struct CardPlayedEvent
    {
        public readonly ulong PlayerId;
        public readonly ulong CardNetworkObjectId;
        public readonly bool IsDefenseCard;

        public CardPlayedEvent(ulong playerId, ulong cardNetworkObjectId, bool isDefenseCard)
        {
            PlayerId = playerId;
            CardNetworkObjectId = cardNetworkObjectId;
            IsDefenseCard = isDefenseCard;
        }
    }

    public readonly struct DeckCountChangedEvent
    {
        public readonly int CardsRemaining;

        public DeckCountChangedEvent(int cardsRemaining)
        {
            CardsRemaining = cardsRemaining;
        }
    }

    public readonly struct DealStateChangedEvent
    {
        public readonly bool IsDealInProgress;

        public DealStateChangedEvent(bool isDealInProgress)
        {
            IsDealInProgress = isDealInProgress;
        }
    }

    public readonly struct ProfilesChangedEvent
    {
        public readonly IReadOnlyList<SeatSnapshot> Seats;

        public ProfilesChangedEvent(IReadOnlyList<SeatSnapshot> seats)
        {
            Seats = seats;
        }
    }

    public readonly struct GameOverChangedEvent
    {
        public readonly bool IsGameOver;

        public GameOverChangedEvent(bool isGameOver)
        {
            IsGameOver = isGameOver;
        }
    }
}
