namespace AbeGaming.GameLogic.TNW
{
    /// <summary>
    /// The Besieging Army's state between Siege Rounds (rule 12.3). "Alive" counts are
    /// permanent losses (kills); "Available" counts also exclude units disrupted earlier
    /// in this same Siege - disrupts have no lasting effect once the Siege ends (11.44),
    /// but do remove a unit from rolling again while the Siege continues.
    /// </summary>
    public readonly record struct TnwSiegeState(
        int UnitsAlive,
        int UnitsAvailable,
        bool CommanderAlive,
        bool CommanderAvailable,
        int CumulativeSixes);

    /// <summary>The outcome of resolving one Siege Round (rule 12.3, 12.31).</summary>
    public readonly record struct TnwSiegeRoundResult(
        TnwSiegeState State,
        bool Ended,
        bool FortressFalls,
        bool Overrun,
        bool BesiegersEliminated,
        int Round);
}
