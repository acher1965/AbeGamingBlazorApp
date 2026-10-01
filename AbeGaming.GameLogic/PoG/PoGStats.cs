namespace AbeGaming.GameLogic.PoG
{
    /// <param name="IsDetailed">Computed from unit-level forces; the step and elimination figures below are then meaningful.</param>
    /// <param name="DefenderEliminatedProbability">Detailed mode: every defending unit eliminated.</param>
    /// <param name="FortDestroyedProbability">Detailed mode: the defending fort destroyed (12.4.6).</param>
    /// <param name="MeanAttackerStepsLost">Detailed mode: mean step losses of the attacker.</param>
    /// <param name="MeanDefenderStepsLost">Detailed mode: mean step losses of the defending units.</param>
    public record PoGStats(
        double AttackerWinProbability,
        double DefenderWinProbability,
        double DrawProbability,
        HitStats HitsStats,
        double DefenderRetreatProbability,
        double MeanDefenderRetreatLengthGivenDefenderLoses,
        double FlankAttackSuccessProbability,
        bool IsDetailed = false,
        double DefenderEliminatedProbability = 0,
        double FortDestroyedProbability = 0,
        double MeanAttackerStepsLost = 0,
        double MeanDefenderStepsLost = 0);
}
