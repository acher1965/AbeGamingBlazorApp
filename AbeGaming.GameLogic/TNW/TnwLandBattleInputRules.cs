namespace AbeGaming.GameLogic.TNW
{
    public static class TnwLandBattleInputRules
    {
        public static int ClampUnits(int value) => Math.Clamp(value, 0, 30);

        /// <summary>0 means no leader; a leader's printed Battle Rating is 1 to 4.</summary>
        public static int ClampCommanderBattleRating(int value) => Math.Clamp(value, 0, 4);

        public static int ClampEventDice(int value) => Math.Clamp(value, -5, 5);

        public static int ClampFailedEvasions(int value) => Math.Clamp(value, 0, 3);

        /// <summary>Each side needs at least one Unit or a Commander to fight.</summary>
        public static bool IsBattleDefinitionConsistent(TnwLandBattle battle, out string? errorMessage)
        {
            if (battle.Attacker.Pieces == 0)
            {
                errorMessage = "The attacker needs at least one Unit or a Commander.";
                return false;
            }

            if (battle.Defender.Pieces == 0)
            {
                errorMessage = "The defender needs at least one Unit or a Commander.";
                return false;
            }

            if (battle.AmphibiousLanding != TnwAmphibiousLanding.None && battle.Terrain != TnwTerrain.None)
            {
                errorMessage = "An Amphibious Assault crosses no rough/pass/marsh line, so there is no terrain bonus to apply.";
                return false;
            }

            errorMessage = null;
            return true;
        }
    }
}
