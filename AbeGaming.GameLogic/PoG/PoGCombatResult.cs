namespace AbeGaming.GameLogic.PoG
{
    /// <summary>
    /// Result of a Paths of Glory combat resolution.
    /// </summary>
    public record PoGCombatResult(
        int AttackerLosses,
        int DefenderLosses,
        int DefenderRetreatLength,
        int AttackerDieRoll,
        int DefenderDieRoll,
        int AttackerModifiedDieRoll,
        int DefenderModifiedDieRoll,
        int AdvanceMaxLength);
}
