using Durak.Architecture.Shared.Interfaces;

namespace Durak.Architecture.Shared.Domain
{
    public sealed class MatchTimerService : ITimerService
    {
        private readonly TimerContext timerContext = new TimerContext();

        public TimerContext Current => timerContext;

        public void Start(float durationSeconds, ulong ownerClientId, TurnTimerRole role)
        {
            timerContext.Start(durationSeconds, ownerClientId, role);
        }

        public void Sync(float durationSeconds, float remainingSeconds, ulong ownerClientId, TurnTimerRole role, bool isRunning)
        {
            timerContext.Synchronize(durationSeconds, remainingSeconds, ownerClientId, role, isRunning);
        }

        public void Stop()
        {
            timerContext.Stop();
        }

        public void Tick(float deltaTime)
        {
            timerContext.Tick(deltaTime);
        }
    }
}
