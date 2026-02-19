using System.Drawing;

namespace AbeGaming.GameLogic.FtP
{

    public static class FtpCRT
    {
        /// <summary>
        /// Calculates hits to defender and attacker based on the battle conditions and die rolls.
        /// </summary>
        /// <param name="battle"></param>
        /// <param name="attackerDieRoll"></param>
        /// <param name="defenderDieRoll"></param>
        /// <returns>star already takes ResourceOrCapital into account</returns>
        public static (int hitsToD, int hitsToA, bool star, int leaderDeathTopD, int leaderDeathTopA) Outcome(
            this FtpBattle battle,
            int attackerDieRoll,
            int defenderDieRoll)
        {
            //Get out if it is not an overrun
            if (battle.IsOverrun())
                return (battle.DefenderSize, 0, false, 0, 0);

            (int aDRM, int dDRM, Ratio ratio, bool inAttackerFavour, BattleSize size) = Extract(battle);
            int modifiedRollA = attackerDieRoll + aDRM;
            int modifiedRollD = defenderDieRoll + dDRM;

            // Leaders death changes: determine leader death roll threshold
            // Rolled modified 10 or greater: Leader killed on 1-3
            // Rolled less than 10: Leader killed on 1
            int defenderLeaderDeathTop = (!inAttackerFavour && ratio > Ratio.Low)
                ? 0
                : modifiedRollD >= 10
                    ? 3
                    : modifiedRollA >= 10
                        ? 1
                        : 0;
            int attackerLeaderDeathTop = (inAttackerFavour && ratio > Ratio.Low)
                ? 0
                : modifiedRollA >= 10
                    ? 3
                    : modifiedRollD >= 10
                        ? 1
                        : 0;

            // Clamp indices to table bounds (0-9 for modified rolls 1-10+)
            int tableIndexA = Math.Clamp(modifiedRollA - 1, 0, 9);
            int tableIndexD = Math.Clamp(modifiedRollD - 1, 0, 9);
            (int hitsToD, int hitsToA) = RawTables[size][tableIndexA, tableIndexD];

            bool star = !battle.ResourceOrCapital && modifiedRollA > 6;

            return (hitsToD, hitsToA, star, defenderLeaderDeathTop, attackerLeaderDeathTop);
        }

        #region private methods
        private static (int aDRM, int dDRM, Ratio ratio, bool inAttackerFavour, BattleSize size) Extract(FtpBattle battle)
        {
            (Ratio ratio, bool inAttackerFavour) = battle.BattleRatio();
            int aDRM = (inAttackerFavour ? ratio.DRM_fromRatio() : 0)
                 + battle.AttackerLeadersDRMIncludingCavalryIntelligence
                 + battle.AttackerElitesCommitted
                 + (battle.DefenderOOS ? 2 : 0);
            int dDRM = (inAttackerFavour ? 0 : ratio.DRM_fromRatio())
                 + battle.DefenderLeadersDRMIncludingCavalryIntelligence
                 + battle.DefenderElitesCommitted
                 + (battle.IsInterception ? 2 : 0)
                 + (battle.FortPresent ? 2 : 0)
                 + (battle.AttackerOOS ? 2 : 0);
            return (aDRM, dDRM, ratio, inAttackerFavour, battle.Size());
        }
        #endregion

        #region private data, some caching
        // CRT lookup tables - must be declared BEFORE RawTables to ensure proper static initialization order

        // Def column: Attacker's Roll -> Defender's Result (hits to defender)
        // Index 0-9 = die roll 1-10+
        private static readonly Dictionary<BattleSize, int[]> tablesHitToD = new()
        {
            [BattleSize.Small] = [0, 0, 0, 1, 1, 1, 1, 1, 1, 1],
            [BattleSize.Medium] = [0, 1, 1, 1, 1, 2, 2, 2, 2, 3],
            [BattleSize.Large] = [1, 2, 2, 3, 3, 3, 4, 4, 5, 5]
        };
        internal static readonly Dictionary<BattleSize, int> MaxHitsToD = new()
        {
            [BattleSize.Small] = 1,
            [BattleSize.Medium] = 3,
            [BattleSize.Large] = 5
        };
        // Att column: Defender's Roll -> Attacker's Result (hits to attacker)
        // Index 0-9 = die roll 1-10+
        private static readonly Dictionary<BattleSize, int[]> tablesHitToA = new()
        {
            [BattleSize.Small] = [0, 1, 1, 1, 1, 1, 1, 1, 1, 2],
            [BattleSize.Medium] = [1, 1, 1, 1, 1, 1, 2, 3, 3, 3],
            [BattleSize.Large] = [1, 2, 3, 3, 3, 4, 4, 4, 5, 6]
        };
        internal static readonly Dictionary<BattleSize, int> MaxHitsToA = new()
        {
            [BattleSize.Small] = 2,
            [BattleSize.Medium] = 3,
            [BattleSize.Large] = 6
        };

        // Pre-calculated tables - depends on tablesHitToD/tablesHitToA being initialized first
        private static readonly Dictionary<BattleSize, (int hitsToD, int hitsToA)[,]> RawTables = PreCalculateRawTables();

        private static (int hitsToD, int hitsToA) RawTable(BattleSize battleSize, int attackerModifiedDieRoll, int defenderModifiedDieRoll)
        {
            int hitsToDefender = tablesHitToD[battleSize][Math.Clamp(attackerModifiedDieRoll - 1, 0, 9)];
            int hitsToAttacker = tablesHitToA[battleSize][Math.Clamp(defenderModifiedDieRoll - 1, 0, 9)];
            return (hitsToDefender, hitsToAttacker);
        }
        private static Dictionary<BattleSize, (int hitsToD, int hitsToA)[,]> PreCalculateRawTables()
        {
            Dictionary<BattleSize, (int hitsToD, int hitsToA)[,]> dict = new();
            foreach (BattleSize size in Enum.GetValues<BattleSize>())
            {
                (int hitsToD, int hitsToA)[,] table = new (int hitsToD, int hitsToA)[10, 10];
                for (int a = 0; a < 10; a++)
                {
                    for (int d = 0; d < 10; d++)
                    {
                        table[a, d] = RawTable(size, a + 1, d + 1);
                    }
                    dict[size] = table;
                }
            }
            return dict;
        }
        #endregion
    }
}
