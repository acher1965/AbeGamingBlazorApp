namespace AbeGamingBlazorApp.PoG
{
    /// <summary>
    /// Represents a combat situation in Paths of Glory.
    /// Paths of Glory uses a differential CRT (attacker CF - defender CF).
    /// </summary>
    public record PoGCombat(
        int AttackerCombatFactors,
        int DefenderCombatFactors,
        Terrain DefenderTerrain,
        CombatType CombatType,
        bool AttackerHasGas,
        bool DefenderHasGasMasks,
        bool AttackerHasSiegeArtillery,
        bool DefenderInFortress,
        bool AttackerHasAirSupport,
        bool DefenderHasAirSupport,
        int AttackerLeaderDRM,
        int DefenderLeaderDRM);
}
