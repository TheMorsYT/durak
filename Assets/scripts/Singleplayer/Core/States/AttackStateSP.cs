using Durak.Architecture.Shared.Domain;
using Durak.Architecture.Shared.FSM;

namespace Durak.Architecture.Singleplayer.Core.States
{
    public sealed class AttackStateSP : SingleplayerMatchStateBase
    {
        public override MatchPhase Phase => MatchPhase.Attacking;

        public AttackStateSP(MatchControllerSP controller) : base(controller) { }

        public override void Enter()
        {
            Controller.ClearBitoVotes();
            Controller.StartTurnTimer(MatchDefaults.FullTurnSeconds, Controller.LocalState.AttackerId, TurnTimerRole.Attack);
        }

        public override void Tick(float deltaTime)
        {
            if (!Controller.HasCardsOnTable())
            {
                return;
            }

            if (!Controller.AreAllCardsDefended())
            {
                Controller.TryChangePhase(MatchPhase.Defending);
            }
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

            Controller.TryChangePhase(MatchPhase.Defending);
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
            if (!Controller.HasCardsOnTable() || !Controller.AreAllCardsDefended())
            {
                return false;
            }

            if (!Controller.IsEligibleThrower(senderId))
            {
                return false;
            }

            Controller.RegisterBitoVote(senderId);
            if (senderId == Controller.LocalState.AttackerId)
            {
                Controller.LocalState.SetTurn(
                    Controller.LocalState.AttackerId,
                    Controller.LocalState.DefenderId,
                    false,
                    false,
                    true);
            }

            if (Controller.AreAllThrowersReadyForResolution())
            {
                Controller.SetResolutionMode(RoundResolutionModeSP.DiscardToPile);
                Controller.TryChangePhase(MatchPhase.RoundResolution);
                return true;
            }

            Controller.RestartCurrentRoleTimer();
            return true;
        }

        public override void HandleTimerExpired()
        {
            bool hasCards = Controller.HasCardsOnTable();
            if (!hasCards)
            {
                Controller.PassOpeningAttackByTimeout();
                return;
            }

            if (Controller.AreAllCardsDefended())
            {
                Controller.SetResolutionMode(RoundResolutionModeSP.DiscardToPile);
                Controller.TryChangePhase(MatchPhase.RoundResolution);
            }
        }
    }
}
