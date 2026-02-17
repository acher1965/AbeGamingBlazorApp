namespace AbeGamingBlazorApp.PoG
{
    /// <summary>
    /// Result of a Paths of Glory combat resolution.
    /// </summary>
    public record PoGCombatResult(
        CombatResultType ResultType,
        int AttackerLosses,
        int DefenderLosses,
        bool DefenderMustRetreat,
        bool BreakthroughAllowed,
        int DieRoll,
        int ModifiedDieRoll,
        int CombatDifferential);
}
