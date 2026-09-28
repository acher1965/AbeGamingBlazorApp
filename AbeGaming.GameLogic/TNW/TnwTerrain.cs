namespace AbeGaming.GameLogic.TNW
{
    /// <summary>
    /// The terrain crossed by the Active (attacking) Formation to enter the battle Duchy,
    /// which gives the defender bonus dice in the first Round (rule 11.22).
    /// </summary>
    public enum TnwTerrain
    {
        None,
        Rough,
        Pass,
        Marsh,
    }

    public static class TnwTerrainExtensions
    {
        /// <summary>Defender's first-Round bonus dice: Rough +1, Pass +2, Marsh +3 (rule 11.22).</summary>
        public static int DefenderBonusDice(this TnwTerrain terrain) => terrain switch
        {
            TnwTerrain.Rough => 1,
            TnwTerrain.Pass => 2,
            TnwTerrain.Marsh => 3,
            _ => 0,
        };
    }
}
