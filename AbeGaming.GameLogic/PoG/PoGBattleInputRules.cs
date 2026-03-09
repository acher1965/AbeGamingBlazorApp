namespace AbeGaming.GameLogic.PoG
{
    public static class PoGBattleInputRules
    {
        public static int ClampFactors(int value) => Math.Clamp(value, 0, 50);

        public static int ClampDrm(int value) => Math.Clamp(value, -9, 9);

        public static int ClampDieRoll(int value) => Math.Clamp(value, 1, 6);

        public static int ClampModifiedDieRoll(int value) => Math.Clamp(value, 1, 6);

        public static int ClampTrench(int value) => Math.Clamp(value, 0, 2);

        public static int ClampFlankAttackDrm(int value) => Math.Clamp(value, 0, 5);

        public static bool IsBattleDefinitionConsistent(PoGBattle battle, out string? errorMessage)
        {
            if (!battle.AttemptFlankAttack)
            {
                errorMessage = null;
                return true;
            }

            int trench = ClampTrench(battle.Trench);

            if (battle.Attacker.FireTable != FireTable.Army)
            {
                errorMessage = "Flank attack requires attacker army fire table.";
                return false;
            }

            if (battle.Terrain is Terrain.Marsh or Terrain.Mountain)
            {
                errorMessage = "Flank attack is not allowed in Marsh or Mountain terrain.";
                return false;
            }

            if (trench > 0)
            {
                errorMessage = "Flank attack is not allowed against trenches.";
                return false;
            }

            bool unoccupiedFortBlocksFlank = battle.Defender.StrengthFactors <= 0
                && FortressCombatFactors(battle.FortressLevel) > 0;
            if (unoccupiedFortBlocksFlank)
            {
                errorMessage = "Flank attack is not allowed against an unoccupied fortified space.";
                return false;
            }

            errorMessage = null;
            return true;
        }

        private static int FortressCombatFactors(FortressLevel fortressLevel) => fortressLevel switch
        {
            FortressLevel.None => 0,
            FortressLevel.Destroyed => 0,
            FortressLevel.LevelOne => 1,
            FortressLevel.Besieged => 1,
            FortressLevel.LevelTwo => 2,
            FortressLevel.LevelThree => 3,
            _ => 0
        };
    }
}
