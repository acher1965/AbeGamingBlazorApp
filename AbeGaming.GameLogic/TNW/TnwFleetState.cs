namespace AbeGaming.GameLogic.TNW
{
    /// <summary>
    /// One Fleet's state during a naval battle. Squadrons are sunk by "6"s; each "5" takes a
    /// die away from the Fleet in later Rounds, however many times a Squadron is hit (13.4).
    /// <see cref="HitsReceived"/> counts every 5 and 6 rolled against the Fleet, including
    /// shore battery fire, as the victor is decided on rolled casualties (13.4, 11.32).
    /// </summary>
    public readonly record struct TnwFleetState(
        TnwFleetComposition Remaining,
        int FivesReceived,
        int HitsReceived);

    /// <summary>What a naval Round's results mean for the battle (rules 13.4, 11.32).</summary>
    public enum TnwNavalRoundVerdict
    {
        ActiveWins,
        InactiveWins,
        SecondRound,
    }
}
