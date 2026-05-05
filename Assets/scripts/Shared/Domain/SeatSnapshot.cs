using System;

namespace Durak.Architecture.Shared.Domain
{
    public sealed class SeatSnapshot : IEquatable<SeatSnapshot>
    {
        public int SeatIndex { get; }
        public ulong ClientId { get; }
        public int CardCount { get; }
        public string Nickname { get; }
        public int AvatarIndex { get; }

        public bool IsOccupied => ClientId != MatchDefaults.InvalidClientId;

        public SeatSnapshot(int seatIndex, ulong clientId, int cardCount, string nickname, int avatarIndex)
        {
            SeatIndex = seatIndex;
            ClientId = clientId;
            CardCount = Math.Max(0, cardCount);
            Nickname = nickname ?? string.Empty;
            AvatarIndex = Math.Max(0, avatarIndex);
        }

        public static SeatSnapshot Empty(int seatIndex)
        {
            return new SeatSnapshot(seatIndex, MatchDefaults.InvalidClientId, 0, string.Empty, 0);
        }

        public bool Equals(SeatSnapshot other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return SeatIndex == other.SeatIndex &&
                   ClientId == other.ClientId &&
                   CardCount == other.CardCount &&
                   AvatarIndex == other.AvatarIndex &&
                   string.Equals(Nickname, other.Nickname, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as SeatSnapshot);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = SeatIndex;
                hash = (hash * 397) ^ ClientId.GetHashCode();
                hash = (hash * 397) ^ CardCount;
                hash = (hash * 397) ^ AvatarIndex;
                hash = (hash * 397) ^ (Nickname != null ? Nickname.GetHashCode() : 0);
                return hash;
            }
        }
    }
}
