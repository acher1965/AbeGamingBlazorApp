using AbeGaming.GameLogic.PoG;

namespace AbeGaming.GameLogic.Tests
{
    public class PoGBattleRulesTests
    {
        [Fact]
        public void Outcome_CorpsTableBaseline_UsesExpectedHitsAndRetreat()
        {
            PoGBattle battle = new(
                new BattleSideInfo(FireTable.Corps, StrengthFactors: 3, DRM: 0),
                new BattleSideInfo(FireTable.Corps, StrengthFactors: 3, DRM: 0),
                Terrain.Clear,
                FortressLevel.None,
                Trench: 0);

            PoGBattleResult result = battle.Outcome(attackerDieRoll: 6, defenderDieRoll: 1);

            Assert.Equal(2, result.HitsByAttacker);
            Assert.Equal(1, result.HitsByDefender);
            Assert.Equal(Winner.Attacker, result.Winner);
            Assert.Equal(1, result.DefenderRetreatLength);
            Assert.Equal(1, result.AdvanceMaxLength);
        }

        [Fact]
        public void Outcome_TerrainAndTrenchShift_MoveAttackerLeftAndDefenderRight()
        {
            PoGBattle battle = new(
                new BattleSideInfo(FireTable.Army, StrengthFactors: 12, DRM: 0),
                new BattleSideInfo(FireTable.Army, StrengthFactors: 12, DRM: 0),
                Terrain.Mountain,
                FortressLevel.None,
                Trench: 2);

            PoGBattleResult result = battle.Outcome(attackerDieRoll: 6, defenderDieRoll: 1);

            Assert.Equal(4, result.AttackerFireColumnIndex); // 12-14 shifted left by 3 -> 5 factors column
            Assert.Equal(8, result.DefenderFireColumnIndex); // 12-14 shifted right by 1 -> 15 factors column
            Assert.Equal(5, result.HitsByAttacker);
        }

        [Fact]
        public void Outcome_SuccessfulFlank_ResolvesAttackerFireFirst()
        {
            PoGBattle battle = new(
                new BattleSideInfo(FireTable.Army, StrengthFactors: 16, DRM: 0),
                new BattleSideInfo(FireTable.Corps, StrengthFactors: 7, DRM: 0),
                Terrain.Clear,
                FortressLevel.None,
                Trench: 0,
                AttemptFlankAttack: true,
                FlankAttackDrm: 2);

            PoGBattleResult result = battle.Outcome(attackerDieRoll: 6, defenderDieRoll: 6, flankAttackDieRoll: 2);

            Assert.True(result.FlankAttackAttempted);
            Assert.True(result.FlankAttackSucceeded);
            Assert.Equal(7, result.HitsByAttacker);
            Assert.Equal(1, result.HitsByDefender); // reduced defender factors fire from 0 column
        }

        [Fact]
        public void Outcome_TrenchBlocksFlank_WhenNotNegated_Throws()
        {
            PoGBattle battle = new(
                new BattleSideInfo(FireTable.Army, StrengthFactors: 10, DRM: 0),
                new BattleSideInfo(FireTable.Corps, StrengthFactors: 4, DRM: 0),
                Terrain.Clear,
                FortressLevel.None,
                Trench: 1,
                AttemptFlankAttack: true,
                FlankAttackDrm: 3);

            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
                () => battle.Outcome(attackerDieRoll: 5, defenderDieRoll: 5, flankAttackDieRoll: 6));
            Assert.Contains("trenches", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ExactStats_ProbabilitiesSumToOne()
        {
            PoGBattle battle = new(
                new BattleSideInfo(FireTable.Army, StrengthFactors: 6, DRM: 0),
                new BattleSideInfo(FireTable.Corps, StrengthFactors: 4, DRM: 0),
                Terrain.Forest,
                FortressLevel.None,
                Trench: 0);

            PoGStats stats = PoGExactStats.Calculate(battle);

            double total = stats.AttackerWinProbability + stats.DefenderWinProbability + stats.DrawProbability;
            Assert.InRange(total, 0.999999, 1.000001);
        }
    }
}
