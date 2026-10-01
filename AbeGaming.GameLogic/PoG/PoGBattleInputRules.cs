namespace AbeGaming.GameLogic.PoG
{
    public static class PoGBattleInputRules
    {
        public static int ClampFactors(int value) => Math.Clamp(value, 0, 16);

        public static int ClampDrm(int value) => Math.Clamp(value, 0, 9);

        public static int ClampDieRoll(int value) => Math.Clamp(value, 1, 6);

        public static int ClampModifiedDieRoll(int value) => Math.Clamp(value, 1, 6);

        public static int ClampTrench(int value) => Math.Clamp(value, 0, 2);

        public static int ClampFlankAttackDrm(int value) => Math.Clamp(value, 0, 3);

        public static bool IsBattleDefinitionConsistent(PoGBattle battle, out string? errorMessage)
        {
            if (battle.Detailed is PoGDetailedForces detailed && !IsDetailedForcesConsistent(battle, detailed, out errorMessage))
                return false;

            if (!battle.AttemptFlankAttack)
            {
                errorMessage = null;
                return true;
            }

            int trench = ClampTrench(battle.Trench);

            bool attackerHasArmy = battle.Detailed is null
                ? battle.Attacker.FireTable == FireTable.Army
                : battle.Detailed.Attackers.Any(u => u.Type.Kind == PoGUnitKind.Army);
            if (!attackerHasArmy)
            {
                errorMessage = "Flank attack requires at least one attacking Army (12.3.1).";
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

            if (battle.IsUnoccupiedFort())
            {
                errorMessage = "Flank attack is not allowed against an unoccupied fortified space.";
                return false;
            }

            errorMessage = null;
            return true;
        }

        private static bool IsDetailedForcesConsistent(PoGBattle battle, PoGDetailedForces detailed, out string? errorMessage)
        {
            if (detailed.Attackers.Count == 0)
                errorMessage = "The attacker needs at least one unit.";
            else if (detailed.Attackers.Count > PoGDetailedForces.MaxAttackers)
                errorMessage = $"At most {PoGDetailedForces.MaxAttackers} attacking units.";
            else if (detailed.Defenders.Count > PoGDetailedForces.MaxDefenders)
                errorMessage = $"At most {PoGDetailedForces.MaxDefenders} defending units (stacking limit).";
            else if (detailed.Defenders.Count == 0 && battle.FortressLevel.CombatFactors() == 0)
                errorMessage = "The defender needs at least one unit or a fort.";
            else
                errorMessage = null;

            return errorMessage is null;
        }
    }
}
