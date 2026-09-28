namespace AbeGaming.GameLogic.TNW
{
    /// <summary>Probabilities for one side of a Land Battle.</summary>
    /// <param name="UnitsLostDistribution">Lasting Unit losses: kills, plus disrupted Units eliminated by a rout or by being unable to retreat. Disrupts alone are not lasting (11.44).</param>
    /// <param name="RoutedProbability">This side loses and is routed (11.5).</param>
    /// <param name="EliminatedProbability">Every piece of this side is eliminated - an Overrun for the other side (11.6).</param>
    /// <param name="FlagOverrunProbability">This side wins and gains a free Flag placement (11.7).</param>
    /// <param name="ResourceGainProbability">This side wins by rout and gains a Resource (11.5).</param>
    public record TnwLandBattleSideStats(
        double MeanUnitsLost,
        double StdDevUnitsLost,
        Dictionary<int, double> UnitsLostDistribution,
        double RoutedProbability,
        double EliminatedProbability,
        double CommanderKilledProbability,
        double FlagOverrunProbability,
        double ResourceGainProbability);

    /// <summary>Land Battle probabilities, either exact or estimated by Monte Carlo.</summary>
    /// <param name="IsExact">True if every dice outcome was enumerated; false for a Monte Carlo estimate.</param>
    /// <param name="MonteCarloTrials">Number of simulated battles when <paramref name="IsExact"/> is false, else 0.</param>
    public record TnwLandBattleStats(
        double AttackerWinProbability,
        double DefenderWinProbability,
        double SecondRoundProbability,
        TnwLandBattleSideStats Attacker,
        TnwLandBattleSideStats Defender,
        bool IsExact,
        int MonteCarloTrials);
}
