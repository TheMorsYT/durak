namespace Durak.Architecture.Shared.FSM
{
    public enum MatchPhase
    {
        Bootstrap = 0,
        Dealing = 1,
        Attacking = 2,
        Defending = 3,
        FollowUpThrowIn = 4,
        RoundResolution = 5,
        GameOver = 6
    }
}
