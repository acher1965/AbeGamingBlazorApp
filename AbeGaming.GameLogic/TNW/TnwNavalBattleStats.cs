namespace AbeGaming.GameLogic.TNW
{
    /// <summary>Probabilities for one Fleet in a naval battle.</summary>
    /// <param name="SquadronsLostDistribution">Squadrons sunk, plus - in a Port battle the Port loses - every Squadron of the defending Fleet (13.5).</param>
    /// <param name="EliminatedProbability">Every Squadron of this Fleet is lost.</param>
    public record TnwNavalSideStats(
        double MeanSquadronsLost,
        double StdDevSquadronsLost,
        Dictionary<int, double> SquadronsLostDistribution,
        double EliminatedProbability);

    /// <summary>Naval battle probabilities, either exact or estimated by Monte Carlo.</summary>
    public record TnwNavalBattleStats(
        double ActiveWinProbability,
        double InactiveWinProbability,
        double SecondRoundProbability,
        TnwNavalSideStats Active,
        TnwNavalSideStats Inactive,
        bool IsExact,
        int MonteCarloTrials);
}
