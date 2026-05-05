using Durak.Architecture.Shared.FSM;

namespace Durak.Architecture.Singleplayer.Core.States
{
    public sealed class DealStateSP : SingleplayerMatchStateBase
    {
        public override MatchPhase Phase => MatchPhase.Dealing;

        public DealStateSP(MatchControllerSP controller) : base(controller) { }

        public override void Enter()
        {
            Controller.BeginDealState();
        }
    }
}
