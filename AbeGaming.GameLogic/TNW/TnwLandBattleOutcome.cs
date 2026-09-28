namespace AbeGaming.GameLogic.TNW
{
    /// <summary>The concluded result of one Land Battle (rules 11.32-11.7).</summary>
    /// <param name="ResourceChance">
    /// Probability that the victor gains a Resource from the rout (11.5): the routed
    /// Commander's printed Battle Rating out of 6, or 0 if there was no rout or no Commander.
    /// </param>
    public readonly record struct TnwLandBattleOutcome(
        bool AttackerWins,
        int Rounds,
        int AttackerUnitsLost,
        int DefenderUnitsLost,
        bool AttackerCommanderKilled,
        bool DefenderCommanderKilled,
        bool LoserRouted,
        bool AttackerEliminated,
        bool DefenderEliminated,
        bool FlagOverrun,
        double ResourceChance);

    /// <summary>One Round's dice, for display in a "Roll 1 Battle" result.</summary>
    public record TnwLandBattleRoundLog(
        int Round,
        int AttackerDice,
        int AttackerSixes,
        int AttackerFives,
        int DefenderDice,
        int DefenderSixes,
        int DefenderFives);

    /// <summary>A single, randomly rolled Land Battle.</summary>
    /// <param name="ResourceDieRoll">The victor's rout die roll (11.5), or null if no roll was made.</param>
    public record TnwLandBattleRollResult(
        TnwLandBattleOutcome Outcome,
        List<TnwLandBattleRoundLog> RoundLog,
        int? ResourceDieRoll,
        bool ResourceGained);
}
