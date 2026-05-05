using Durak.Architecture.Shared.FSM;

namespace Durak.Architecture.Singleplayer.Core.States
{
    public sealed class BootstrapStateSP : SingleplayerMatchStateBase
    {
        public override MatchPhase Phase => MatchPhase.Bootstrap;

        public BootstrapStateSP(MatchControllerSP controller) : base(controller) { }

        public override void Enter()
        {
            Controller.StopTurnTimer();
        }
    }
}
