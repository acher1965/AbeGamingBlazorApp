namespace AbeGaming.GameLogic.TNW
{
    /// <summary>One Siege Round's dice, for display in a "Roll 1 Siege" result.</summary>
    public record TnwSiegeRoundLog(
        int Round,
        int BesiegerDice,
        int BesiegerSixes,
        int FortressDice,
        int FortressSixes,
        int FortressFives);

    /// <summary>The outcome of a single, randomly-rolled Siege attempt (rule 12).</summary>
    public record TnwSiegeRollResult(
        bool FortressFalls,
        bool Overrun,
        bool BesiegersEliminated,
        int Rounds,
        int UnitsLost,
        bool CommanderLost,
        List<TnwSiegeRoundLog> RoundLog);
}
