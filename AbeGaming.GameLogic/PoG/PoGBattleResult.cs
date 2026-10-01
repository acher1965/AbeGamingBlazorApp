namespace AbeGaming.GameLogic.PoG
{
    /// <param name="DefenderFireTable">The table the defender actually fired on (it can change after a flank attack in detailed mode).</param>
    /// <param name="AttackerStepsLost">Detailed mode only: step losses taken by the attacker.</param>
    /// <param name="DefenderStepsLost">Detailed mode only: step losses taken by the defending units.</param>
    /// <param name="DefenderEliminated">Detailed mode only: every defending unit was eliminated.</param>
    /// <param name="FortDestroyed">Detailed mode only: the defending fort was destroyed (12.4.6).</param>
    /// <param name="AttackerHasFullStrengthUnit">A full strength attacking unit remains (always true in factor mode).</param>
    public record PoGBattleResult(
        Winner Winner,
        int HitsByAttacker,
        int HitsByDefender,
        int DefenderRetreatLength,
        int AttackerDieRoll,
        int DefenderDieRoll,
        int AttackerModifiedDieRoll,
        int DefenderModifiedDieRoll,
        int AdvanceMaxLength,
        bool DefenderCanIgnoreRetreat,
        int AttackerFireColumnIndex,
        int DefenderFireColumnIndex,
        bool FlankAttackAttempted,
        bool FlankAttackSucceeded,
        int? FlankAttackDieRoll,
        int? FlankAttackModifiedDieRoll,
        FireTable? DefenderFireTable = null,
        int AttackerStepsLost = 0,
        int DefenderStepsLost = 0,
        bool DefenderEliminated = false,
        bool FortDestroyed = false,
        bool AttackerHasFullStrengthUnit = true);
}
