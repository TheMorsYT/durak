using System;
using System.Collections.Generic;

namespace Durak.Architecture.Shared.FSM
{
    public sealed class MatchStateMachine
    {
        private readonly Dictionary<MatchPhase, IMatchState> states = new Dictionary<MatchPhase, IMatchState>();

        public IMatchState CurrentState { get; private set; }
        public MatchPhase CurrentPhase => CurrentState != null ? CurrentState.Phase : MatchPhase.Bootstrap;

        public event Action<MatchPhase, MatchPhase> StateChanged;

        public void RegisterState(IMatchState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            states[state.Phase] = state;
        }

        public bool TryChangeState(MatchPhase nextPhase)
        {
            if (!states.TryGetValue(nextPhase, out IMatchState nextState))
            {
                return false;
            }

            MatchPhase previousPhase = CurrentPhase;
            if (CurrentState == nextState)
            {
                return true;
            }

            CurrentState?.Exit();
            CurrentState = nextState;
            CurrentState.Enter();

            StateChanged?.Invoke(previousPhase, nextPhase);
            return true;
        }

        public void Tick(float deltaTime)
        {
            CurrentState?.Tick(deltaTime);
        }
    }
}
