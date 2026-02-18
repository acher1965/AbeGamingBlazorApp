namespace AbeGaming.GameLogic.PoG
{
    public static class PoGCRT
    {
        public static PoGBattleResult Outcome(this PoGBattle battle, int attackerDieRoll, int defenderDieRoll)
        {
            /* TODO: deal with 
             * sieges (stopping advance if not 2 etc),
             * trenches and other terrain column shifts,
             * fortresses,
             * flank attacks,
             * what else
            */
            BattleSideInfo attacker = battle.Attacker;
            BattleSideInfo defender = battle.Defender;

            int attackerModifiedDieRoll = attackerDieRoll + attacker.DRM
                + (defender.OOS ? 2 : 0);
            int defenderModifiedDieRoll = defenderDieRoll + defender.DRM;
            int hitsByAttacker = Lookup[attacker.FireTable](attacker.StrengthFactors, attackerModifiedDieRoll);
            int hitsByDefender = Lookup[defender.FireTable](defender.StrengthFactors, defenderModifiedDieRoll);

            Winner winner = hitsByAttacker > hitsByDefender
                ? Winner.Attacker
                : Winner.Defender;

            int hitsDifference = hitsByAttacker - hitsByDefender;
            int defenderRetreatLength = winner == Winner.Defender
                ? 0
                : hitsDifference >= 2 ? 2 : 1;
            int advanceMaxLength = Math.Min(defenderRetreatLength, (battle.Terrain == Terrain.Clear ? 2 : 1));

            return new PoGBattleResult(
                winner,
                hitsByAttacker,
                hitsByDefender,
                hitsByAttacker,
                attackerDieRoll,
                defenderDieRoll,
                attackerModifiedDieRoll,
                defenderModifiedDieRoll,
                advanceMaxLength);
        }


        private static readonly Dictionary<FireTable, int[][]> FireTables = new Dictionary<FireTable, int[][]>
        {
            [FireTable.Corps] =
                [
                    [0, 0, 0, 0, 1, 1], // 0 factors
                    [0, 0, 0, 1, 1, 1], // 1 factor
                    [0, 1, 1, 1, 1, 1], // 2 factors
                    [1, 1, 1, 1, 1, 2], // 3 factors
                    [1, 1, 1, 2, 2, 2], // 4 factors
                    [1, 1, 2, 2, 2, 3], // 5 factors
                    [1, 2, 2, 2, 3, 3], // 6 factors
                    [1, 2, 2, 3, 3, 4], // 7 factors
                    [2, 2, 3, 3, 4, 4], // 8+ factors
                ],
            [FireTable.Army] =
                [
                    [0, 0, 0, 1, 1, 2, 3, 4, 5, 6], // 0-1 factors
                    [0, 0, 1, 1, 2, 3, 4, 5, 6, 7], // 2 factors
                    [0, 1, 1, 2, 3, 4, 5, 6, 7, 8], // 3 factors
                    [1, 1, 2, 3, 4, 5, 6, 7, 8, 9], // 4 factors
                    [1, 2, 3, 4, 5, 6, 7, 8, 9,10], // 5 factors
                    [2, 3, 4, 5, 6, 7, 8,10 ,11 ,12], //6-8 factors
                    [3 ,4 ,5 ,6 ,7 ,8 ,10 ,11 ,12 ,13], //9-11 factors
                    [4 ,5 ,6 ,7 ,8 ,10 ,11 ,12 ,13 ,14], //12-14 factors
                    [5 ,6 ,7 ,8 ,10 ,11 ,12 ,13 ,14 ,15], //15 factors
                    [6 ,7 ,8 ,9 ,10 ,11 ,12 ,13 ,14 ,16], //16+ factors
                ]
        };
        private static Dictionary<FireTable, Func<int, int, int>> Lookup = new Dictionary<FireTable, Func<int, int, int>>
        {
            [FireTable.Corps] = (factors, dieRoll) => FireTables[FireTable.Corps][CorpsFactorsToIndex(factors)][dieRoll - 1],
            [FireTable.Army] = (factors, dieRoll) => FireTables[FireTable.Army][ArmyFactorsToIndex(factors)][dieRoll - 1]
        };

        /// <summary>
        /// Maps firing factors to the CorpsFireTable array index.
        /// </summary>
        private static int CorpsFactorsToIndex(int factors) => Math.Clamp(factors, 0, 8);

        /// <summary>
        /// Maps firing factors to the ArmyFireTable array index.
        /// </summary>
        private static int ArmyFactorsToIndex(int factors) => factors switch
        {
            <= 1 => 0,
            2 => 1,
            3 => 2,
            4 => 3,
            5 => 4,
            <= 8 => 5,  // 6-8
            <= 11 => 6, // 9-11
            <= 14 => 7, // 12-14
            15 => 8,
            _ => 9      // 16+
        };
    }
}
