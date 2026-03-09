namespace AbeGaming.GameLogic.PoG
{
    public static class PoGCRT
    {
        public static PoGBattleResult Outcome(this PoGBattle battle, int attackerDieRoll, int defenderDieRoll, int? flankAttackDieRoll = null)
        {
            if (!PoGBattleInputRules.IsBattleDefinitionConsistent(battle, out string? errorMessage))
                throw new InvalidOperationException(errorMessage);

            int normalizedTrench = PoGBattleInputRules.ClampTrench(battle.Trench);
            int attackerBaseFactors = PoGBattleInputRules.ClampFactors(battle.Attacker.StrengthFactors);
            int defenderBaseFactors = PoGBattleInputRules.ClampFactors(
                battle.Defender.StrengthFactors + FortressCombatFactors(battle.FortressLevel));

            int attackerModifiedDieRoll = PoGBattleInputRules.ClampModifiedDieRoll(
                PoGBattleInputRules.ClampDieRoll(attackerDieRoll)
                + battle.Attacker.DRM
                + (battle.IsAllAttackersInSinai ? -3 : 0));

            int defenderModifiedDieRoll = PoGBattleInputRules.ClampModifiedDieRoll(
                PoGBattleInputRules.ClampDieRoll(defenderDieRoll)
                + battle.Defender.DRM);

            int attackerFireColumnIndex = FireColumnIndex(
                battle.Attacker.FireTable,
                attackerBaseFactors,
                OffensiveColumnShift(battle.Terrain, normalizedTrench));

            int defenderFireColumnIndex = FireColumnIndex(
                battle.Defender.FireTable,
                defenderBaseFactors,
                DefensiveColumnShift(normalizedTrench));

            bool flankAttempted = battle.AttemptFlankAttack;
            int? flankRoll = null;
            int? flankModifiedRoll = null;
            bool flankSucceeded = false;

            if (flankAttempted)
            {
                if (flankAttackDieRoll is null)
                    throw new ArgumentException("Flank attack die roll is required when AttemptFlankAttack is true.");

                flankRoll = PoGBattleInputRules.ClampDieRoll(flankAttackDieRoll.Value);
                flankModifiedRoll = PoGBattleInputRules.ClampModifiedDieRoll(
                    flankRoll.Value + PoGBattleInputRules.ClampFlankAttackDrm(battle.FlankAttackDrm));

                flankSucceeded = flankModifiedRoll.Value >= 4;
            }

            int hitsByAttacker;
            int hitsByDefender;
            bool useFlankSequencing = flankAttempted;

            if (!useFlankSequencing)
            {
                hitsByAttacker = HitsFromColumn(battle.Attacker.FireTable, attackerFireColumnIndex, attackerModifiedDieRoll);
                hitsByDefender = HitsFromColumn(battle.Defender.FireTable, defenderFireColumnIndex, defenderModifiedDieRoll);
            }
            else if (flankSucceeded)
            {
                hitsByAttacker = HitsFromColumn(battle.Attacker.FireTable, attackerFireColumnIndex, attackerModifiedDieRoll);
                int defenderFactorsAfterLosses = Math.Max(0, defenderBaseFactors - hitsByAttacker);
                int defenderColumnAfterLosses = FireColumnIndex(
                    battle.Defender.FireTable,
                    defenderFactorsAfterLosses,
                    DefensiveColumnShift(normalizedTrench));
                hitsByDefender = HitsFromColumn(battle.Defender.FireTable, defenderColumnAfterLosses, defenderModifiedDieRoll);
            }
            else
            {
                hitsByDefender = HitsFromColumn(battle.Defender.FireTable, defenderFireColumnIndex, defenderModifiedDieRoll);
                int attackerFactorsAfterLosses = Math.Max(0, attackerBaseFactors - hitsByDefender);
                int attackerColumnAfterLosses = FireColumnIndex(
                    battle.Attacker.FireTable,
                    attackerFactorsAfterLosses,
                    OffensiveColumnShift(battle.Terrain, normalizedTrench));
                hitsByAttacker = HitsFromColumn(battle.Attacker.FireTable, attackerColumnAfterLosses, attackerModifiedDieRoll);
            }

            Winner winner = hitsByAttacker > hitsByDefender
                ? Winner.Attacker
                : hitsByAttacker < hitsByDefender
                    ? Winner.Defender
                    : Winner.Draw;

            int defenderRetreatLength = winner == Winner.Attacker
                ? (hitsByAttacker - hitsByDefender >= 2 ? 2 : 1)
                : 0;

            int advanceMaxLength = defenderRetreatLength == 0
                ? 0
                : Math.Min(defenderRetreatLength, battle.Terrain == Terrain.Clear ? 2 : 1);

            bool defenderCanIgnoreRetreat = defenderRetreatLength > 0
                && CanIgnoreRetreat(battle.Terrain, normalizedTrench)
                && (defenderBaseFactors - hitsByAttacker) >= 2;

            return new PoGBattleResult(
                winner,
                hitsByAttacker,
                hitsByDefender,
                defenderRetreatLength,
                PoGBattleInputRules.ClampDieRoll(attackerDieRoll),
                PoGBattleInputRules.ClampDieRoll(defenderDieRoll),
                attackerModifiedDieRoll,
                defenderModifiedDieRoll,
                advanceMaxLength,
                defenderCanIgnoreRetreat,
                attackerFireColumnIndex,
                defenderFireColumnIndex,
                flankAttempted,
                flankSucceeded,
                flankRoll,
                flankModifiedRoll);
        }

        private static bool CanIgnoreRetreat(Terrain terrain, int trench) =>
            trench > 0
            || terrain == Terrain.Forest
            || terrain == Terrain.Desert
            || terrain == Terrain.Mountain
            || terrain == Terrain.Marsh;

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

        private static int OffensiveColumnShift(Terrain terrain, int trench)
        {
            int terrainShift = terrain switch
            {
                Terrain.Mountain => -1,
                Terrain.Marsh => -1,
                _ => 0
            };

            int trenchShift = trench switch
            {
                1 => -1,
                2 => -2,
                _ => 0
            };

            return terrainShift + trenchShift;
        }

        private static int DefensiveColumnShift(int trench)
        {
            return trench switch
            {
                1 => 1,
                2 => 1,
                _ => 0
            };
        }

        private static int FireColumnIndex(FireTable fireTable, int factors, int shift)
        {
            int baseIndex = fireTable switch
            {
                FireTable.Corps => CorpsFactorsToIndex(factors),
                FireTable.Army => ArmyFactorsToIndex(factors),
                _ => throw new ArgumentOutOfRangeException(nameof(fireTable))
            };

            int maxIndex = fireTable switch
            {
                FireTable.Corps => CorpsFireTable.Length - 1,
                FireTable.Army => ArmyFireTable.Length - 1,
                _ => throw new ArgumentOutOfRangeException(nameof(fireTable))
            };

            return Math.Clamp(baseIndex + shift, 0, maxIndex);
        }

        private static int HitsFromColumn(FireTable fireTable, int columnIndex, int modifiedDieRoll)
        {
            int dieIndex = PoGBattleInputRules.ClampModifiedDieRoll(modifiedDieRoll) - 1;
            return fireTable switch
            {
                FireTable.Corps => CorpsFireTable[columnIndex][dieIndex],
                FireTable.Army => ArmyFireTable[columnIndex][dieIndex],
                _ => throw new ArgumentOutOfRangeException(nameof(fireTable))
            };
        }

        private static readonly int[][] CorpsFireTable =
        [
            [0, 0, 0, 0, 1, 1], // 0 factors
            [0, 0, 0, 1, 1, 1], // 1 factor
            [0, 1, 1, 1, 1, 1], // 2 factors
            [1, 1, 1, 1, 2, 2], // 3 factors
            [1, 1, 1, 2, 2, 2], // 4 factors
            [1, 1, 2, 2, 2, 3], // 5 factors
            [1, 1, 2, 2, 3, 3], // 6 factors
            [1, 2, 2, 3, 3, 4], // 7 factors
            [2, 2, 3, 3, 4, 4], // 8+ factors
        ];

        private static readonly int[][] ArmyFireTable =
        [
            [0, 1, 1, 1, 2, 2], // 1 factor
            [1, 1, 2, 2, 3, 3], // 2 factors
            [1, 2, 2, 3, 3, 4], // 3 factors
            [2, 2, 3, 3, 4, 4], // 4 factors
            [2, 3, 3, 4, 4, 5], // 5 factors
            [3, 3, 4, 4, 5, 5], // 6-8 factors
            [3, 4, 4, 5, 5, 7], // 9-11 factors
            [4, 4, 5, 5, 7, 7], // 12-14 factors
            [4, 5, 5, 7, 7, 7], // 15 factors
            [5, 5, 7, 7, 7, 7], // 16+ factors
        ];

        private static int CorpsFactorsToIndex(int factors) => Math.Clamp(factors, 0, 8);

        private static int ArmyFactorsToIndex(int factors) => factors switch
        {
            <= 1 => 0,
            2 => 1,
            3 => 2,
            4 => 3,
            5 => 4,
            <= 8 => 5,
            <= 11 => 6,
            <= 14 => 7,
            15 => 8,
            _ => 9
        };
    }
}
