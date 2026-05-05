using System;

namespace Durak.Architecture.Shared.Domain
{
    public sealed class TurnContext : IEquatable<TurnContext>
    {
        public ulong AttackerId { get; private set; } = MatchDefaults.InvalidClientId;
        public ulong DefenderId { get; private set; } = MatchDefaults.InvalidClientId;
        public bool FirstAttackWave { get; private set; } = true;
        public bool IsDefenderTaking { get; private set; }
        public bool AttackerPassed { get; private set; }

        public void Update(
            ulong attackerId,
            ulong defenderId,
            bool firstAttackWave,
            bool isDefenderTaking,
            bool attackerPassed)
        {
            AttackerId = attackerId;
            DefenderId = defenderId;
            FirstAttackWave = firstAttackWave;
            IsDefenderTaking = isDefenderTaking;
            AttackerPassed = attackerPassed;
        }

        public TurnContext Clone()
        {
            TurnContext copy = new TurnContext();
            copy.Update(AttackerId, DefenderId, FirstAttackWave, IsDefenderTaking, AttackerPassed);
            return copy;
        }

        public bool Equals(TurnContext other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return AttackerId == other.AttackerId &&
                   DefenderId == other.DefenderId &&
                   FirstAttackWave == other.FirstAttackWave &&
                   IsDefenderTaking == other.IsDefenderTaking &&
                   AttackerPassed == other.AttackerPassed;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as TurnContext);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = AttackerId.GetHashCode();
                hash = (hash * 397) ^ DefenderId.GetHashCode();
                hash = (hash * 397) ^ FirstAttackWave.GetHashCode();
                hash = (hash * 397) ^ IsDefenderTaking.GetHashCode();
                hash = (hash * 397) ^ AttackerPassed.GetHashCode();
                return hash;
            }
        }
    }
}
