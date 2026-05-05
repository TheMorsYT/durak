using System.Collections.Generic;
using Durak.Architecture.Shared.FSM;

namespace Durak.Architecture.Shared.Domain
{
    public readonly struct MatchContextSnapshot
    {
        public readonly MatchPhase Phase;

        public readonly ulong AttackerId;
        public readonly ulong DefenderId;
        public readonly bool FirstAttackWave;
        public readonly bool IsDefenderTaking;
        public readonly bool AttackerPassed;

        public readonly float TimerDurationSeconds;
        public readonly float TimerRemainingSeconds;
        public readonly ulong TimerOwnerClientId;
        public readonly TurnTimerRole TimerRole;
        public readonly bool TimerIsRunning;

        public readonly int AttackCardsCount;
        public readonly int DefendedCardsCount;
        public readonly int DeckCardsRemaining;
        public readonly bool IsDealInProgress;
        public readonly bool IsGameOver;

        public readonly IReadOnlyList<SeatSnapshot> Seats;

        public MatchContextSnapshot(
            MatchPhase phase,
            ulong attackerId,
            ulong defenderId,
            bool firstAttackWave,
            bool isDefenderTaking,
            bool attackerPassed,
            float timerDurationSeconds,
            float timerRemainingSeconds,
            ulong timerOwnerClientId,
            TurnTimerRole timerRole,
            bool timerIsRunning,
            int attackCardsCount,
            int defendedCardsCount,
            int deckCardsRemaining,
            bool isDealInProgress,
            bool isGameOver,
            IReadOnlyList<SeatSnapshot> seats)
        {
            Phase = phase;
            AttackerId = attackerId;
            DefenderId = defenderId;
            FirstAttackWave = firstAttackWave;
            IsDefenderTaking = isDefenderTaking;
            AttackerPassed = attackerPassed;

            TimerDurationSeconds = timerDurationSeconds;
            TimerRemainingSeconds = timerRemainingSeconds;
            TimerOwnerClientId = timerOwnerClientId;
            TimerRole = timerRole;
            TimerIsRunning = timerIsRunning;

            AttackCardsCount = attackCardsCount;
            DefendedCardsCount = defendedCardsCount;
            DeckCardsRemaining = deckCardsRemaining;
            IsDealInProgress = isDealInProgress;
            IsGameOver = isGameOver;

            Seats = seats;
        }
    }
}
