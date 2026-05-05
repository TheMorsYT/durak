namespace Durak.Architecture.Shared.FSM
{
    public interface IMatchState
    {
        MatchPhase Phase { get; }
        void Enter();
        void Exit();
        void Tick(float deltaTime);
    }
}
