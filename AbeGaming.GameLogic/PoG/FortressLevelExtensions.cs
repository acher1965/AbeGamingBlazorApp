namespace AbeGaming.GameLogic.PoG
{
    public static class FortressLevelExtensions
    {
        /// <summary>The fort's CF, added to the defender's Combat Strength (12.2.3); 0 if none or destroyed.</summary>
        public static int CombatFactors(this FortressLevel fortressLevel) => fortressLevel switch
        {
            FortressLevel.LevelOne => 1,
            FortressLevel.LevelTwo => 2,
            FortressLevel.LevelThree => 3,
            _ => 0,
        };

        /// <summary>Label for the Fortress picker.</summary>
        public static string DisplayName(this FortressLevel fortressLevel) => fortressLevel switch
        {
            FortressLevel.None => "None",
            FortressLevel.LevelOne => "Fort CF 1",
            FortressLevel.LevelTwo => "Fort CF 2",
            FortressLevel.LevelThree => "Fort CF 3",
            FortressLevel.Destroyed => "Destroyed",
            _ => fortressLevel.ToString(),
        };

        /// <summary>
        /// True if the defending space holds a fort but no defending units. Such a fort cannot be
        /// flanked and gets no trench benefit (15.1.6). In factor mode "no units" is taken as no
        /// defending combat factors; in detailed mode it is an empty defender list.
        /// </summary>
        public static bool IsUnoccupiedFort(this PoGBattle battle) =>
            battle.FortressLevel.CombatFactors() > 0
            && (battle.Detailed is null ? battle.Defender.StrengthFactors <= 0 : battle.Detailed.Defenders.Count == 0);
    }
}
