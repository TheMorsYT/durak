using Durak.Architecture.Shared.Interfaces;

namespace Durak.Architecture.Shared.Domain
{
    public sealed class PlayerProfileService : IProfileService
    {
        public string GetLocalNickname()
        {
            return PlayerProfileStorage.GetNickname();
        }

        public int GetLocalAvatarIndex()
        {
            return PlayerProfileStorage.GetAvatarIndex();
        }

        public void SaveLocalProfile(string nickname, int avatarIndex)
        {
            PlayerProfileStorage.SetNickname(PlayerProfileStorage.SanitizeNickname(nickname));
            PlayerProfileStorage.SetAvatarIndex(avatarIndex);
        }

        public PlayerStatsSnapshot GetStatsSnapshot()
        {
            return PlayerProfileStorage.GetStatsSnapshot();
        }

        public string BuildSingleStatsText(bool useUkrainian)
        {
            PlayerStatsSnapshot stats = PlayerProfileStorage.GetStatsSnapshot();
            if (!useUkrainian)
            {
                return $"Singleplayer\nWins: {stats.SingleWins}\nLosses: {stats.SingleLosses}\nDraws: {stats.SingleDraws}";
            }

            return $"Singleplayer (UA)\nPeremoh: {stats.SingleWins}\nPorazok: {stats.SingleLosses}\nNichyikh: {stats.SingleDraws}";
        }

        public string BuildMultiplayerStatsText(bool useUkrainian)
        {
            PlayerStatsSnapshot stats = PlayerProfileStorage.GetStatsSnapshot();
            if (!useUkrainian)
            {
                return $"Multiplayer\nWins: {stats.MultiWins}\nLosses: {stats.MultiLosses}\nDraws: {stats.MultiDraws}";
            }

            return $"Multypleier\nPeremoh: {stats.MultiWins}\nPorazok: {stats.MultiLosses}\nNichyikh: {stats.MultiDraws}";
        }
    }
}
