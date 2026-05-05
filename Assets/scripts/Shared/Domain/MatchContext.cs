using System.Collections.Generic;
using Durak.Architecture.Shared.FSM;

namespace Durak.Architecture.Shared.Domain
{
    public sealed class MatchContext
    {
        private readonly List<SeatSnapshot> seats = new List<SeatSnapshot>(MatchDefaults.MaxSeats);

        public MatchPhase Phase { get; private set; } = MatchPhase.Bootstrap;
        public TurnContext Turn { get; } = new TurnContext();
        public TimerContext Timer { get; } = new TimerContext();
        public TableContext Table { get; } = new TableContext();

        public int DeckCardsRemaining { get; private set; }
        public bool IsDealInProgress { get; private set; }
        public bool IsGameOver { get; private set; }

        public IReadOnlyList<SeatSnapshot> Seats => seats;

        public void ApplySnapshot(MatchContextSnapshot snapshot)
        {
            Phase = snapshot.Phase;

            Turn.Update(
                snapshot.AttackerId,
                snapshot.DefenderId,
                snapshot.FirstAttackWave,
                snapshot.IsDefenderTaking,
                snapshot.AttackerPassed);

            Timer.Synchronize(
                snapshot.TimerDurationSeconds,
                snapshot.TimerRemainingSeconds,
                snapshot.TimerOwnerClientId,
                snapshot.TimerRole,
                snapshot.TimerIsRunning);

            Table.SetCounts(snapshot.AttackCardsCount, snapshot.DefendedCardsCount);

            DeckCardsRemaining = snapshot.DeckCardsRemaining;
            IsDealInProgress = snapshot.IsDealInProgress;
            IsGameOver = snapshot.IsGameOver;

            seats.Clear();
            if (snapshot.Seats != null)
            {
                seats.AddRange(snapshot.Seats);
            }
        }
    }
}
