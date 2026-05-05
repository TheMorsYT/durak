using Durak.Architecture.Shared.Domain;

namespace Durak.Architecture.Shared.Interfaces
{
    public interface ITimerService
    {
        TimerContext Current { get; }
        void Start(float durationSeconds, ulong ownerClientId, TurnTimerRole role);
        void Sync(float durationSeconds, float remainingSeconds, ulong ownerClientId, TurnTimerRole role, bool isRunning);
        void Stop();
        void Tick(float deltaTime);
    }
}
