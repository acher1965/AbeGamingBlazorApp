namespace AbeGaming.GameLogic.TNW
{
    /// <summary>Whether a Land Battle is the result of an Amphibious Assault (rule 13.7), and if so where.</summary>
    public enum TnwAmphibiousLanding
    {
        None,
        EnemyPort,
        EnemyFortressPort,
    }

    public static class TnwAmphibiousLandingExtensions
    {
        /// <summary>
        /// Shore battery dice fired once at the landing attacker before Round 1 (13.7, via 13.5):
        /// two, or four for a Fortress-Port. Unlike a naval Port battle, the batteries do not
        /// fire again once the Land Battle itself begins.
        /// </summary>
        public static int ShoreBatteryDice(this TnwAmphibiousLanding landing) => landing switch
        {
            TnwAmphibiousLanding.EnemyPort => 2,
            TnwAmphibiousLanding.EnemyFortressPort => 4,
            _ => 0,
        };
    }
}
