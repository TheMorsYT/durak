using Durak.Architecture.Shared.FSM;
using UnityEngine;

namespace Durak.Architecture.Singleplayer.Core.States
{
    public sealed class ResolutionStateSP : SingleplayerMatchStateBase
    {
        public override MatchPhase Phase => MatchPhase.RoundResolution;

        public ResolutionStateSP(MatchControllerSP controller) : base(controller) { }

        public override void Enter()
        {
            Controller.StopTurnTimer();
            try
            {
                Controller.ResolveRound();
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
                Controller.SetResolutionMode(RoundResolutionModeSP.None);
            }

            if (Controller.TryFinalizeGameOver())
            {
                Controller.TryChangePhase(MatchPhase.GameOver);
                return;
            }

            Controller.TryChangePhase(MatchPhase.Attacking);
        }
    }
}
