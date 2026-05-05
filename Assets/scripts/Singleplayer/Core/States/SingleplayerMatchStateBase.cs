using Durak.Architecture.Shared.FSM;

namespace Durak.Architecture.Singleplayer.Core.States
{
    public abstract class SingleplayerMatchStateBase : IMatchState, ISingleplayerMatchActionHandler
    {
        protected MatchControllerSP Controller { get; }

        public abstract MatchPhase Phase { get; }

        protected SingleplayerMatchStateBase(MatchControllerSP controller)
        {
            Controller = controller;
        }

        public virtual void Enter() { }
        public virtual void Exit() { }
        public virtual void Tick(float deltaTime) { }

        public virtual bool HandlePlayCard(ulong senderId, Card card) => false;
        public virtual bool HandleDefendCard(ulong senderId, Card defendCard, Card targetCard) => false;
        public virtual bool HandleTransfer(ulong senderId, Card card) => false;
        public virtual bool HandleTake(ulong senderId) => false;
        public virtual bool HandleVoteBito(ulong senderId) => false;
        public virtual void HandleTimerExpired() { }
    }
}
