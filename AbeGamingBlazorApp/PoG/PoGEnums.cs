namespace AbeGamingBlazorApp.PoG
{
    /// <summary>
    /// Represents the combat type in Paths of Glory
    /// </summary>
    public enum CombatType
    {
        FireCombat,
        Assault
    }

    /// <summary>
    /// Terrain types that affect combat
    /// </summary>
    public enum Terrain
    {
        Clear,
        Forest,
        Marsh,
        Mountain,
        Trench,
        Fortress
    }

    /// <summary>
    /// Combat result types from the CRT
    /// </summary>
    public enum CombatResultType
    {
        AttackerEliminated,  // AE
        AttackerLoss,        // AL
        Exchange,            // EX
        DefenderLoss,        // DL
        DefenderRouted,      // DR
        Breakthrough         // BT
    }
}
