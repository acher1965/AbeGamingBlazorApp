namespace AbeGaming.GameLogic.PoG
{
    public enum FireTable
    {
        Corps,
        Army,
    }

    public enum Terrain
    {
        Clear,
        Forest,
        Marsh,
        Mountain,
        Desert,
    }

    /// <summary>Army or Corps; any Army firing means the Army Fire Table (12.2.8).</summary>
    public enum PoGUnitKind
    {
        Army,
        Corps,
    }

    /// <summary>The two sides of the war; in a combat the attacker and defender are on opposite sides.</summary>
    public enum PoGFaction
    {
        CentralPowers,
        AlliedPowers,
    }

    /// <summary>
    /// The fort in the defending space, by its printed CF. A besieged fort keeps its printed CF
    /// (besieging affects who may attack it, supply and surrender - rules 15.1.3, 15.2, 15.3),
    /// so there is no separate "besieged" level.
    /// </summary>
    public enum FortressLevel
    {
        None = 0,
        LevelOne,
        LevelTwo,
        LevelThree,
        Destroyed,
    }
}
