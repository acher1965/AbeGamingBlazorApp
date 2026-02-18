namespace AbeGaming.GameLogic.FtP
{
    public static class FtpCRT
    {
        public static LossStats LossDistributions(this FtpLandBattle battle)
        {
            (int aDRM, int dDRM, _, _) = Extract(battle);
            return LossDistributions(battle.Size(), aDRM, dDRM);
        }

        /// <summary>
        /// Calculates hits to defender and attacker based on the battle conditions and die rolls.
        /// </summary>
        /// <param name="battle"></param>
        /// <param name="attackerDieRoll"></param>
        /// <param name="defenderDieRoll"></param>
        /// <returns>star already takes ResourceOrCapital into account</returns>
        public static (int hitsToD, int hitsToA, bool star, int leaderDeathTopD, int leaderDeathTopA) Outcome(
            this FtpLandBattle battle,
            int attackerDieRoll,
            int defenderDieRoll)
        {
            //Get out if it is not an overrun
            if (battle.IsOverrun())
                return (battle.DefenderSize, 0, false, 0, 0);

            (int aDRM, int dDRM, Ratio ratio, bool inAttackerFavour) = Extract(battle);
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

            (int hitsToD, int hitsToA) = RawTables[battle.Size()][modifiedRollA, modifiedRollD];
            bool star = !battle.ResourceOrCapital && modifiedRollA > 6;

            return (hitsToD, hitsToA, star, defenderLeaderDeathTop, attackerLeaderDeathTop);
        }

        #region private
        private static readonly Dictionary<BattleSize, (int hitsToD, int hitsToA)[,]> RawTables = PreCalculateRawTables();
        private static (int hitsToD, int hitsToA) RawTable(BattleSize battleSize, int attackerModifiedDieRoll, int defenderModifiedDieRoll)
        {
            int hitsToDefender = tablesHitToD[battleSize][Math.Clamp(attackerModifiedDieRoll - 1, 0, 9)];
            int hitsToAttacker = tablesHitToA[battleSize][Math.Clamp(defenderModifiedDieRoll - 1, 0, 9)];
            return (hitsToDefender, hitsToAttacker);
        }
        private static Dictionary<BattleSize, (int hitsToD, int hitsToA)[,]> PreCalculateRawTables()
        {
            var dict = new Dictionary<BattleSize, (int hitsToD, int hitsToA)[,]>();
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

        private static LossStats LossDistributions(BattleSize size, int aDRM, int dDRM)
        {
            int[] hitsToAVector = new int[36];
            int[] hitsToDVector = new int[36];
            (int hitsToD, int hitsToA)[,] rawTable = RawTables[size];
            foreach ((int black, int white) in Dice.TwoDice)
            {
                (int hitsToD, int hitsToA) = rawTable[black + aDRM, white + dDRM];
                hitsToAVector[white - 1] = hitsToA;
                hitsToDVector[black - 1] = hitsToD;
            }
            (double MeanA, double StdDevA) = IntArrayStatHelpers.MeanAndStdDevVectorised(hitsToAVector);
            (double MeanD, double StdDevD) = IntArrayStatHelpers.MeanAndStdDevVectorised(hitsToDVector);

            double[] distributionHtoA = IntArrayStatHelpers.CalculateDistribution(hitsToAVector, maxHitsToA[size]);
            double[] distributionHtoD = IntArrayStatHelpers.CalculateDistribution(hitsToDVector, maxHitsToD[size]);

            return (MeanA, StdDevA, MeanD, StdDevD,
                distributionHtoD.Select((prblty, hits) => (hits, prblty)).ToDictionary(x => x.hits, x => x.prblty),
                distributionHtoA.Select((prblty, hits) => (hits, prblty)).ToDictionary(x => x.hits, x => x.prblty));
        }


        private static (int aDRM, int dDRM, Ratio ratio, bool inAttackerFavour) Extract(FtpLandBattle battle)
        {
            (Ratio ratio, bool inAttackerFavour) = battle.RatioDRM();
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
            return (aDRM, dDRM, ratio, inAttackerFavour);
        }

        // Def column: Attacker's Roll -> Defender's Result (hits to defender)
        // Index 0-9 = die roll 1-10+
        private static readonly Dictionary<BattleSize, int[]> tablesHitToD = new()
        {
            [BattleSize.Small] = [0, 0, 0, 1, 1, 1, 1, 1, 1, 1],
            [BattleSize.Medium] = [0, 1, 1, 1, 1, 2, 2, 2, 2, 3],
            [BattleSize.Large] = [1, 2, 2, 3, 3, 3, 4, 4, 5, 5]
        };
        // Att column: Defender's Roll -> Attacker's Result (hits to attacker)
        // Index 0-9 = die roll 1-10+
        private static readonly Dictionary<BattleSize, int[]> tablesHitToA = new()
        {
            [BattleSize.Small] = [0, 1, 1, 1, 1, 1, 1, 1, 1, 2],
            [BattleSize.Medium] = [1, 1, 1, 1, 1, 1, 2, 3, 3, 3],
            [BattleSize.Large] = [1, 2, 3, 3, 3, 4, 4, 4, 5, 6]
        };
        private static readonly Dictionary<BattleSize, int> maxHitsToA = new()
        {
            [BattleSize.Small] = 2,
            [BattleSize.Medium] = 3,
            [BattleSize.Large] = 6
        };
        private static readonly Dictionary<BattleSize, int> maxHitsToD = new()
        {
            [BattleSize.Small] = 1,
            [BattleSize.Medium] = 3,
            [BattleSize.Large] = 5
        };
        #endregion
    }
}
