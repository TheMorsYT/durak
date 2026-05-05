using UnityEngine;

namespace Durak.Architecture.Shared.Domain
{
    public sealed class TimerContext
    {
        public float DurationSeconds { get; private set; } = MatchDefaults.FullTurnSeconds;
        public float RemainingSeconds { get; private set; } = MatchDefaults.FullTurnSeconds;
        public ulong OwnerClientId { get; private set; } = MatchDefaults.InvalidClientId;
        public TurnTimerRole Role { get; private set; } = TurnTimerRole.None;
        public bool IsRunning { get; private set; }

        public float NormalizedFill =>
            DurationSeconds > 0.001f
                ? Mathf.Clamp01(RemainingSeconds / DurationSeconds)
                : 0f;

        public void Start(float durationSeconds, ulong ownerClientId, TurnTimerRole role)
        {
            DurationSeconds = Mathf.Max(0f, durationSeconds);
            RemainingSeconds = DurationSeconds;
            OwnerClientId = ownerClientId;
            Role = role;
            IsRunning = DurationSeconds > 0f;
        }

        public void Synchronize(
            float durationSeconds,
            float remainingSeconds,
            ulong ownerClientId,
            TurnTimerRole role,
            bool isRunning)
        {
            DurationSeconds = Mathf.Max(0f, durationSeconds);
            RemainingSeconds = Mathf.Clamp(remainingSeconds, 0f, DurationSeconds);
            OwnerClientId = ownerClientId;
            Role = role;
            IsRunning = isRunning && DurationSeconds > 0f;
        }

        public void Stop()
        {
            IsRunning = false;
            RemainingSeconds = 0f;
            Role = TurnTimerRole.None;
            OwnerClientId = MatchDefaults.InvalidClientId;
        }

        public void Tick(float deltaTime)
        {
            if (!IsRunning)
            {
                return;
            }

            RemainingSeconds = Mathf.Max(0f, RemainingSeconds - Mathf.Max(0f, deltaTime));
            if (RemainingSeconds <= 0f)
            {
                IsRunning = false;
            }
        }

        public TimerContext Clone()
        {
            TimerContext copy = new TimerContext();
            copy.Synchronize(DurationSeconds, RemainingSeconds, OwnerClientId, Role, IsRunning);
            return copy;
        }
    }
}
