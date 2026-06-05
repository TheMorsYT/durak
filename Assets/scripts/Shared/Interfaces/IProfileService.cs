namespace Durak.Architecture.Shared.Interfaces
{
    public interface IProfileService
    {
        string GetLocalNickname();
        int GetLocalAvatarIndex();
        void SaveLocalProfile(string nickname, int avatarIndex);
        PlayerStatsSnapshot GetStatsSnapshot();
        string BuildSingleStatsText(bool useUkrainian);
    }
}
