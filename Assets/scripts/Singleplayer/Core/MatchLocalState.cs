using System.Collections.Generic;
using Durak.Architecture.Shared.Domain;
using Durak.Architecture.Shared.FSM;
using Durak.Architecture.Singleplayer.Core.States;
using UnityEngine;

namespace Durak.Architecture.Singleplayer.Core
{
    public sealed class MatchLocalState
    {
        public readonly List<GameObject> DeckObjects = new List<GameObject>();
        public readonly HashSet<ulong> BitoVotes = new HashSet<ulong>();
        public readonly Dictionary<ulong, Transform> HandsByClientId = new Dictionary<ulong, Transform>();

        public MatchPhase Phase = MatchPhase.Bootstrap;
        public bool IsGameOver;
        public bool IsDealInProgress;
        public bool TransferModeEnabled = true;

        public bool IsFirstTurn = true;
        public bool FirstAttackWave = true;
        public bool IsDefenderTaking;
        public bool AttackerPassed;

        public ulong AttackerId = MatchDefaults.InvalidClientId;
        public ulong DefenderId = MatchDefaults.InvalidClientId;

        public float TurnTimerDuration;
        public float TurnTimerRemaining;
        public ulong TurnTimerOwnerId = MatchDefaults.InvalidClientId;
        public TurnTimerRole TurnTimerRole = TurnTimerRole.None;
        public bool TurnTimerRunning;

        public int TrumpSuitCode = -1;
        public int TrumpValueCode = -1;
        public int AttackCardsCount;
        public int DefendedCardsCount;
        public RoundResolutionModeSP PendingResolutionMode = RoundResolutionModeSP.None;

        public bool OutcomeRecorded;
        public MatchResultType LastOutcome = MatchResultType.Draw;

        public void SetTurn(ulong attackerId, ulong defenderId, bool firstAttackWave, bool isDefenderTaking, bool attackerPassed)
        {
            AttackerId = attackerId;
            DefenderId = defenderId;
            FirstAttackWave = firstAttackWave;
            IsDefenderTaking = isDefenderTaking;
            AttackerPassed = attackerPassed;
        }

        public void SetTimer(float durationSeconds, float remainingSeconds, ulong ownerId, TurnTimerRole role, bool running)
        {
            TurnTimerDuration = Mathf.Max(0f, durationSeconds);
            TurnTimerRemaining = Mathf.Clamp(remainingSeconds, 0f, TurnTimerDuration);
            TurnTimerOwnerId = ownerId;
            TurnTimerRole = role;
            TurnTimerRunning = running && TurnTimerDuration > 0f;
        }

        public void SetTable(int attackCards, int defendedCards)
        {
            AttackCardsCount = Mathf.Max(0, attackCards);
            DefendedCardsCount = Mathf.Clamp(defendedCards, 0, AttackCardsCount);
        }
    }
}
