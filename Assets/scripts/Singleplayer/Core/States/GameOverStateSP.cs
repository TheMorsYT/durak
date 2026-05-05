using Durak.Architecture.Shared.FSM;

namespace Durak.Architecture.Singleplayer.Core.States
{
    public sealed class GameOverStateSP : SingleplayerMatchStateBase
    {
        public override MatchPhase Phase => MatchPhase.GameOver;

        public GameOverStateSP(MatchControllerSP controller) : base(controller) { }

        public override void Enter()
        {
            Controller.StopTurnTimer();
            Controller.LocalState.IsDealInProgress = false;
            Controller.LocalState.IsGameOver = true;
        }
    }
}
