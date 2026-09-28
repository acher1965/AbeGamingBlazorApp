namespace AbeGaming.GameLogic.TNW
{
    /// <summary>The concluded result of one naval battle.</summary>
    public readonly record struct TnwNavalBattleOutcome(
        bool ActiveWins,
        int Rounds,
        int ActiveSquadronsLost,
        int InactiveSquadronsLost,
        bool ActiveEliminated,
        bool InactiveEliminated);

    /// <summary>One Round's dice, for display in a "Roll 1 Battle" result. Round 0 is the pre-battle shore battery fire.</summary>
    public record TnwNavalRoundLog(
        int Round,
        int ActiveDice,
        int ActiveSixes,
        int ActiveFives,
        int InactiveDice,
        int InactiveSixes,
        int InactiveFives);

    /// <summary>A single, randomly rolled naval battle.</summary>
    public record TnwNavalBattleRollResult(
        TnwNavalBattleOutcome Outcome,
        List<TnwNavalRoundLog> RoundLog);
}
