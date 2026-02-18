namespace AbeGaming.GameLogic.PoG
{
    public record PoGBattleResult(
        Winner Winner,
        int HitsByAttacker,
        int HitsByDefender,
        int DefenderRetreatLength,
        int AttackerDieRoll,
        int DefenderDieRoll,
        int AttackerModifiedDieRoll,
        int DefenderModifiedDieRoll,
        int AdvanceMaxLength);
}
