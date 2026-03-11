namespace AbeGaming.GameLogic.PoG
{
    public record PoGStats(
        double AttackerWinProbability,
        double DefenderWinProbability,
        double DrawProbability,
        HitStats HitsStats,
        double DefenderRetreatProbability,
        double MeanDefenderRetreatLengthGivenDefenderLoses,
        double FlankAttackSuccessProbability);
}
