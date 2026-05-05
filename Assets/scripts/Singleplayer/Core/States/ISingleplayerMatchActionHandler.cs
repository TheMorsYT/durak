namespace Durak.Architecture.Singleplayer.Core.States
{
    public interface ISingleplayerMatchActionHandler
    {
        bool HandlePlayCard(ulong senderId, Card card);
        bool HandleDefendCard(ulong senderId, Card defendCard, Card targetCard);
        bool HandleTransfer(ulong senderId, Card card);
        bool HandleTake(ulong senderId);
        bool HandleVoteBito(ulong senderId);
        void HandleTimerExpired();
    }
}
