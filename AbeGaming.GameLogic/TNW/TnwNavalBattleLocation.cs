namespace AbeGaming.GameLogic.TNW
{
    /// <summary>Where a naval battle is fought (rules 13.4, 13.5).</summary>
    public enum TnwNavalBattleLocation
    {
        OpenSea,
        EnemyPort,
        EnemyFortressPort,
    }

    public static class TnwNavalBattleLocationExtensions
    {
        /// <summary>Shore battery dice of a Port the Active Fleet enters: two, or four if a Fortress (13.5).</summary>
        public static int ShoreBatteryDice(this TnwNavalBattleLocation location) => location switch
        {
            TnwNavalBattleLocation.EnemyPort => 2,
            TnwNavalBattleLocation.EnemyFortressPort => 4,
            _ => 0,
        };

        public static bool IsPort(this TnwNavalBattleLocation location) => location != TnwNavalBattleLocation.OpenSea;
    }
}
