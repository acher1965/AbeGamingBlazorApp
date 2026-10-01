namespace AbeGaming.GameLogic.PoG
{
    /// <param name="Detailed">
    /// Optional unit-level forces. When set, each side's Combat Strength and Fire Table come from
    /// its units (the factors and table in <paramref name="Attacker"/> and <paramref name="Defender"/>
    /// are then ignored, only their DRMs are used), and losses are taken step by step (12.4).
    /// </param>
    public record PoGBattle(
        BattleSideInfo Attacker,
        BattleSideInfo Defender,
        Terrain Terrain,
        FortressLevel FortressLevel,
        int Trench,
        bool AttemptFlankAttack = false,
        int FlankAttackDrm = 0,
        bool IsAllAttackersInSinai = false,
        PoGDetailedForces? Detailed = null);
}
