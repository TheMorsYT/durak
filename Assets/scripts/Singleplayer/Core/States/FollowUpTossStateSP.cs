using Durak.Architecture.Shared.Domain;
using Durak.Architecture.Shared.FSM;

namespace Durak.Architecture.Singleplayer.Core.States
{
    public sealed class FollowUpTossStateSP : SingleplayerMatchStateBase
    {
        public override MatchPhase Phase => MatchPhase.FollowUpThrowIn;

        public FollowUpTossStateSP(MatchControllerSP controller) : base(controller) { }

        public override void Enter()
        {
            Controller.ClearBitoVotes();
            if (!Controller.IsEligibleThrower(Controller.LocalState.AttackerId))
            {
                Controller.SetResolutionMode(RoundResolutionModeSP.DefenderCollectsTable);
                Controller.TryChangePhase(MatchPhase.RoundResolution);
                return;
            }

            Controller.StartTurnTimer(MatchDefaults.FollowUpTurnSeconds, Controller.LocalState.AttackerId, TurnTimerRole.FollowUp);
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

            Controller.StartTurnTimer(MatchDefaults.FollowUpTurnSeconds, Controller.LocalState.AttackerId, TurnTimerRole.FollowUp);
            return true;
        }

        public override bool HandleVoteBito(ulong senderId)
        {
            return HandlePassOrBito(senderId);
        }

        public override bool HandlePass(ulong senderId)
        {
            return HandlePassOrBito(senderId);
        }

        private bool HandlePassOrBito(ulong senderId)
        {
            if (!Controller.IsEligibleThrower(senderId))
            {
                return false;
            }

            Controller.RegisterBitoVote(senderId);
            if (Controller.AreAllThrowersReadyForResolution())
            {
                Controller.SetResolutionMode(RoundResolutionModeSP.DefenderCollectsTable);
                Controller.TryChangePhase(MatchPhase.RoundResolution);
            }

            return true;
        }

        public override void HandleTimerExpired()
        {
            Controller.SetResolutionMode(RoundResolutionModeSP.DefenderCollectsTable);
            Controller.TryChangePhase(MatchPhase.RoundResolution);
        }
    }
}
