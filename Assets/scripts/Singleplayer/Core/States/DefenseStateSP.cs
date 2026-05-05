using Durak.Architecture.Shared.Domain;
using Durak.Architecture.Shared.FSM;

namespace Durak.Architecture.Singleplayer.Core.States
{
    public sealed class DefenseStateSP : SingleplayerMatchStateBase
    {
        public override MatchPhase Phase => MatchPhase.Defending;

        public DefenseStateSP(MatchControllerSP controller) : base(controller) { }

        public override void Enter()
        {
            Controller.ClearBitoVotes();
            Controller.StartTurnTimer(MatchDefaults.FullTurnSeconds, Controller.LocalState.DefenderId, TurnTimerRole.Defend);
        }

        public override void Tick(float deltaTime)
        {
            if (!Controller.HasCardsOnTable())
            {
                Controller.TryChangePhase(MatchPhase.Attacking);
                return;
            }

            if (Controller.AreAllCardsDefended())
            {
                Controller.TryChangePhase(MatchPhase.Attacking);
            }
        }

        public override bool HandleDefendCard(ulong senderId, Card defendCard, Card targetCard)
        {
            if (senderId != Controller.LocalState.DefenderId)
            {
                return false;
            }

            if (!Controller.DefendCard(senderId, defendCard, targetCard))
            {
                return false;
            }

            if (Controller.AreAllCardsDefended())
            {
                Controller.TryChangePhase(MatchPhase.Attacking);
                return true;
            }

            Controller.StartTurnTimer(MatchDefaults.FullTurnSeconds, Controller.LocalState.DefenderId, TurnTimerRole.Defend);
            return true;
        }

        public override bool HandlePlayCard(ulong senderId, Card card)
        {
            if (!Controller.ValidateThrowCard(senderId, card))
            {
                return false;
            }

            if (!Controller.PlayAttackCard(senderId, card))
            {
                return false;
            }

            Controller.StartTurnTimer(MatchDefaults.FullTurnSeconds, Controller.LocalState.DefenderId, TurnTimerRole.Defend);
            return true;
        }

        public override bool HandleTransfer(ulong senderId, Card card)
        {
            if (!Controller.ValidateTransfer(senderId, card))
            {
                return false;
            }

            if (!Controller.ExecuteTransfer(senderId, card))
            {
                return false;
            }

            Controller.StartTurnTimer(MatchDefaults.FullTurnSeconds, Controller.LocalState.DefenderId, TurnTimerRole.Defend);
            return true;
        }

        public override bool HandleTake(ulong senderId)
        {
            if (senderId != Controller.LocalState.DefenderId)
            {
                return false;
            }

            if (!Controller.HasCardsOnTable())
            {
                return false;
            }

            Controller.BeginDefenderTakingMode();
            Controller.TryChangePhase(MatchPhase.FollowUpThrowIn);
            return true;
        }

        public override void HandleTimerExpired()
        {
            Controller.BeginDefenderTakingMode();
            Controller.TryChangePhase(MatchPhase.FollowUpThrowIn);
        }
    }
}
