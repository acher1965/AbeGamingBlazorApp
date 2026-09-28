namespace AbeGaming.GameLogic.TNW
{
    /// <summary>Exact probability results for a Siege (rule 12.3).</summary>
    public record TnwSiegeStats(
        double FortressFallsProbability,
        double OverrunProbabilityGivenFalls,
        double BesiegersEliminatedProbability,
        double CommanderLostProbability,
        double MeanRoundsTaken,
        double MeanBesiegerUnitsLost,
        double StdDevBesiegerUnitsLost,
        Dictionary<int, double> RoundsDistribution,
        Dictionary<int, double> BesiegerUnitsLostDistribution);
}
