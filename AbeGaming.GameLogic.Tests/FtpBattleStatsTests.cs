using AbeGaming.GameLogic.FtP;

namespace AbeGaming.GameLogic.Tests
{
    /// <summary>
    /// Unit tests for FtP battle statistics calculations.
    /// Tests exact stats (ExactStats) and Monte Carlo simulations.
    /// </summary>
    public class FtpBattleStatsTests
    {
        /// <summary>
        /// Creates a default battle with specified attacker and defender sizes.
        /// All other parameters are default (false/0).
        /// </summary>
        private static FtpBattle CreateDefaultBattle(int attackerSize, int defenderSize)
        {
            return new FtpBattle(
                ResourceOrCapital: false,
                FortPresent: false,
                IsInterception: false,
                IsDefenderLeaderPresent: false,
                AttackerSize: attackerSize,
                DefenderSize: defenderSize,
                AttackerLeadersDRMIncludingCavalryIntelligence: 0,
                DefenderLeadersDRMIncludingCavalryIntelligence: 0,
                AttackerElitesCommitted: 0,
                DefenderElitesCommitted: 0,
                AttackerOOS: false,
                DefenderOOS: false,
                IsAmphibious: false);
        }

        #region Exact Stats (ExactStats) Tests

        [Theory]
        [InlineData(1, 1, BattleSize.Small)]
        [InlineData(2, 2, BattleSize.Small)]
        [InlineData(5, 5, BattleSize.Medium)]
        [InlineData(10, 10, BattleSize.Large)]
        public void ExactStats_EqualStrengths_ReturnsBattleSize(int attackerSize, int defenderSize, BattleSize expectedSize)
        {
            // Arrange
            FtpBattle battle = CreateDefaultBattle(attackerSize, defenderSize);

            // Act
            FtpStats stats = battle.ExactStats();

            // Assert - verify battle size is correct
            Assert.Equal(expectedSize, stats.BattleSize);
        }

        [Theory]
        [InlineData(1, 1)]
        [InlineData(2, 2)]
        [InlineData(5, 5)]
        [InlineData(10, 10)]
        public void ExactStats_EqualStrengths_Basics(int attackerSize, int defenderSize)
        {
            // Arrange
            FtpBattle battle = CreateDefaultBattle(attackerSize, defenderSize);

            // Act
            FtpStats stats = battle.ExactStats();
            HitStats hitStats = stats.HitsStats;

            // Assert
            Assert.True(hitStats.MeanHtoA >= 0, "Mean hits to attacker should be non-negative");
            Assert.True(hitStats.MeanHtoD >= 0, "Mean hits to defender should be non-negative");
            Assert.True(hitStats.StdDevHtoA >= 0, "StdDev hits to attacker should be non-negative");
            Assert.True(hitStats.StdDevHtoD >= 0, "StdDev hits to defender should be non-negative");
            Assert.True(hitStats.HitsToA_Prblty.Values.All(p => p >= 0 && p <= 1), "HitsToA probabilities should be between 0 and 1");
            Assert.True(hitStats.HitsToD_Prblty.Values.All(p => p >= 0 && p <= 1), "HitsToD probabilities should be between 0 and 1");
            Assert.True(hitStats.HitsToA_Prblty.Keys.Any(v => v >= 0), "bin should be non negative");
            Assert.True(hitStats.HitsToD_Prblty.Keys.Any(v => v >= 0), "bin should be non negative");
            
            
            double sumHtoA = hitStats.HitsToA_Prblty.Values.Sum();
            double sumHtoD = hitStats.HitsToD_Prblty.Values.Sum();
            Assert.Equal(1.0, sumHtoA, 3); // sum to 1.0 within 3 decimal places
            Assert.Equal(1.0, sumHtoD, 3); // sum to 1.0 within 3 decimal places
        }

        [Fact]
        public void ExactStats_SmallBattle_2v2_ExpectedValues()
        {
            // Arrange - 2 vs 2 is a Small battle with no ratio DRM
            FtpBattle battle = CreateDefaultBattle(2, 2);

            // Act
            FtpStats stats = battle.ExactStats();
            HitStats hitStats = stats.HitsStats;

            // Assert - verify we have a Small battle
            Assert.Equal(BattleSize.Small, battle.Size());

            // Small battle CRT (rolls 1-6 with no DRM):
            // HitsToD: [0,0,0,1,1,1] -> mean = 3/6 = 0.5
            // HitsToA: [0,1,1,1,1,1] -> mean = 5/6 ≈ 0.833
            Assert.Equal(0.5, hitStats.MeanHtoD, precision: 2);
            Assert.Equal(0.833, hitStats.MeanHtoA, precision: 2);
        }

        [Fact]
        public void ExactStats_MediumBattle_5v5_ExpectedValues()
        {
            // Arrange - 5 vs 5 is a Medium battle with no ratio DRM
            FtpBattle battle = CreateDefaultBattle(5, 5);

            // Act
            FtpStats stats = battle.ExactStats();
            HitStats hitStats = stats.HitsStats;

            // Assert - verify we have a Medium battle
            Assert.Equal(BattleSize.Medium, battle.Size());

            // Medium battle CRT (rolls 1-6 with no DRM):
            // HitsToD: [0,1,1,1,1,2] -> mean = 6/6 = 1.0
            // HitsToA: [1,1,1,1,1,1] -> mean = 6/6 = 1.0
            Assert.Equal(1.0, hitStats.MeanHtoD, precision: 2);
            Assert.Equal(1.0, hitStats.MeanHtoA, precision: 2);
        }

        [Fact]
        public void ExactStats_LargeBattle_10v10_ExpectedValues()
        {
            // Arrange - 10 vs 10 is a Large battle with no ratio DRM
            FtpBattle battle = CreateDefaultBattle(10, 10);

            // Act
            FtpStats stats = battle.ExactStats();
            HitStats hitStats = stats.HitsStats;

            // Assert - verify we have a Large battle
            Assert.Equal(BattleSize.Large, battle.Size());

            // Large battle CRT (rolls 1-6 with no DRM):
            // HitsToD: [1,2,2,3,3,3] -> mean = 14/6 ≈ 2.333
            // HitsToA: [1,2,3,3,3,4] -> mean = 16/6 ≈ 2.667
            Assert.Equal(2.333, hitStats.MeanHtoD, precision: 2);
            Assert.Equal(2.667, hitStats.MeanHtoA, precision: 2);
        }

        #endregion

        #region Monte Carlo Tests

        [Theory]
        [InlineData(1, 1)]
        [InlineData(2, 2)]
        [InlineData(5, 5)]
        [InlineData(10, 10)]
        public void MonteCarlo_EqualStrengths_WinProbabilitiesSumToOne(int attackerSize, int defenderSize)
        {
            // Arrange
            FtpBattle battle = CreateDefaultBattle(attackerSize, defenderSize);
            int trialsExponent = 14; // 16,384 trials

            // Act
            (int _, FtpStats stats) = FtpMonteCarlo.Run(battle, trialsExponent);

            // Assert - win probabilities sum to 1.0
            double totalProbability = stats.AttackerWinProbability + stats.DefenderWinProbability;

            Assert.Equal( 1.0, totalProbability, 0.001);
        }

        [Theory]
        [InlineData(1, 1)]
        [InlineData(2, 2)]
        [InlineData(5, 5)]
        [InlineData(10, 10)]
        public void MonteCarlo_EqualStrengths_HitsStatsAreValid(int attackerSize, int defenderSize)
        {
            // Arrange
            FtpBattle battle = CreateDefaultBattle(attackerSize, defenderSize);
            int trialsExponent = 14;

            // Act
            (_, FtpStats stats) = FtpMonteCarlo.Run(battle, trialsExponent);

            // Assert
            Assert.True(stats.HitsStats.MeanHtoA >= 0, "Mean hits to attacker should be non-negative");
            Assert.True(stats.HitsStats.MeanHtoD >= 0, "Mean hits to defender should be non-negative");
            Assert.True(stats.HitsStats.StdDevHtoA >= 0, "StdDev should be non-negative");
            Assert.True(stats.HitsStats.StdDevHtoD >= 0, "StdDev should be non-negative");
        }

        [Fact]
        public void MonteCarlo_SmallBattle_TrialsCountCorrect()
        {
            // Arrange
            FtpBattle battle = CreateDefaultBattle(1, 1);
            int trialsExponent = 14; // 2^14 = 16,384

            // Act
            (int trials, FtpStats stats) = FtpMonteCarlo.Run(battle, trialsExponent);

            // Assert
            Assert.Equal(16384, trials);
            Assert.Equal(BattleSize.Small, stats.BattleSize);
        }

        [Fact]
        public void MonteCarlo_MediumBattle_TrialsCountCorrect()
        {
            // Arrange
            FtpBattle battle = CreateDefaultBattle(5, 5);
            int trialsExponent = 14;

            // Act
            (int trials, FtpStats stats) = FtpMonteCarlo.Run(battle, trialsExponent);

            // Assert
            Assert.Equal(16384, trials);
            Assert.Equal(BattleSize.Medium, stats.BattleSize);
        }

        [Fact]
        public void MonteCarlo_SmallBattle_1v1_ExpectedValues()
        {
            // Arrange - 1v1 Small battle, defender always wins (attacker capped at 1 SP loss = wiped out)
            FtpBattle battle = CreateDefaultBattle(1, 1);
            int trialsExponent = 16; // 65,536 trials for stable values

            // Act
            (_, FtpStats stats) = FtpMonteCarlo.Run(battle, trialsExponent);

            // Assert - expected values from Monte Carlo with full rules
            // 1v1: Attacker almost always loses (capped to 1 hit wipes them out)
            Assert.Equal(0.083, stats.AttackerWinProbability, tolerance: 0.02);
            Assert.Equal(0.917, stats.DefenderWinProbability, tolerance: 0.02);
            Assert.Equal(0.833, stats.HitsStats.MeanHtoA, tolerance: 0.02);
            Assert.Equal(0.083, stats.HitsStats.MeanHtoD, tolerance: 0.02);
            Assert.Equal(0.0, stats.StarResultProbability, tolerance: 0.01);
        }

        [Fact]
        public void MonteCarlo_SmallBattle_2v2_ExpectedValues()
        {
            // Arrange - 2v2 Small battle
            FtpBattle battle = CreateDefaultBattle(2, 2);
            int trialsExponent = 16;

            // Act
            (_, FtpStats stats) = FtpMonteCarlo.Run(battle, trialsExponent);

            // Assert - expected values from Monte Carlo with full rules
            Assert.Equal(0.083, stats.AttackerWinProbability, tolerance: 0.02);
            Assert.Equal(0.917, stats.DefenderWinProbability, tolerance: 0.02);
            Assert.Equal(0.833, stats.HitsStats.MeanHtoA, tolerance: 0.02);
            Assert.Equal(0.50, stats.HitsStats.MeanHtoD, tolerance: 0.02);
            Assert.Equal(0.0, stats.StarResultProbability, tolerance: 0.01);
        }

        [Fact]
        public void MonteCarlo_MediumBattle_5v5_ExpectedValues()
        {
            // Arrange - 5v5 Medium battle
            FtpBattle battle = CreateDefaultBattle(5, 5);
            int trialsExponent = 16;

            // Act
            (_, FtpStats stats) = FtpMonteCarlo.Run(battle, trialsExponent);

            // Assert - expected values from Monte Carlo with full rules
            Assert.Equal(0.167, stats.AttackerWinProbability, tolerance: 0.02);
            Assert.Equal(0.833, stats.DefenderWinProbability, tolerance: 0.02);
            Assert.Equal(1.0, stats.HitsStats.MeanHtoA, tolerance: 0.02);
            Assert.Equal(1.0, stats.HitsStats.MeanHtoD, tolerance: 0.02);
            Assert.Equal(0.0, stats.StarResultProbability, tolerance: 0.01);
        }

        [Fact]
        public void MonteCarlo_LargeBattle_10v10_ExpectedValues()
        {
            // Arrange - 10v10 Large battle
            FtpBattle battle = CreateDefaultBattle(10, 10);
            int trialsExponent = 16;

            // Act
            (_, FtpStats stats) = FtpMonteCarlo.Run(battle, trialsExponent);

            // Assert - expected values from Monte Carlo with full rules
            Assert.Equal(0.222, stats.AttackerWinProbability, tolerance: 0.02);
            Assert.Equal(0.778, stats.DefenderWinProbability, tolerance: 0.02);
            Assert.Equal(2.667, stats.HitsStats.MeanHtoA, tolerance: 0.05);
            Assert.Equal(2.333, stats.HitsStats.MeanHtoD, tolerance: 0.05);
            Assert.Equal(0.0, stats.StarResultProbability, tolerance: 0.01);
        }

        [Theory]
        [InlineData(1, 1)]
        [InlineData(2, 2)]
        [InlineData(5, 5)]
        [InlineData(10, 10)]
        public void MonteCarlo_And_ExactStats_BothProduceValidResults(int attackerSize, int defenderSize)
        {
            // Note: Monte Carlo uses full battle rules (caps, floors via Outcome())
            // while ExactStats uses raw CRT probabilities only.
            // They should be reasonably close for larger battles where caps don't apply as much.
            // For small battles (like 1v1), they may differ significantly due to damage caps.

            // Arrange
            FtpBattle battle = CreateDefaultBattle(attackerSize, defenderSize);
            int trialsExponent = 16; // 65,536 trials for better convergence

            // Act
            FtpStats exactStats = battle.ExactStats();
            (_, FtpStats stats) = FtpMonteCarlo.Run(battle, trialsExponent);

            // Assert - just verify both produce valid results, not that they match
            Assert.True(exactStats.HitsStats.MeanHtoA >= 0);
            Assert.True(exactStats.HitsStats.MeanHtoD >= 0);
            Assert.True(stats.HitsStats.MeanHtoA >= 0);
            Assert.True(stats.HitsStats.MeanHtoD >= 0);

            //TODO when ready, compare the 2 and tests they are v close
        }

        #endregion

        #region Comprehensive Comparison Tests (ExactStats vs MonteCarlo)

        /// <summary>
        /// Creates a battle with all parameters specified for comprehensive testing.
        /// </summary>
        private static FtpBattle CreateBattle(
            int attackerSize, int defenderSize,
            bool resourceOrCapital = false, bool fortPresent = false,
            bool isInterception = false, bool isDefenderLeaderPresent = false,
            int attackerLeaderDRM = 0, int defenderLeaderDRM = 0,
            int attackerElites = 0, int defenderElites = 0,
            bool attackerOOS = false, bool defenderOOS = false,
            bool isAmphibious = false)
        {
            return new FtpBattle(
                ResourceOrCapital: resourceOrCapital,
                FortPresent: fortPresent,
                IsInterception: isInterception,
                IsDefenderLeaderPresent: isDefenderLeaderPresent,
                AttackerSize: attackerSize,
                DefenderSize: defenderSize,
                AttackerLeadersDRMIncludingCavalryIntelligence: attackerLeaderDRM,
                DefenderLeadersDRMIncludingCavalryIntelligence: defenderLeaderDRM,
                AttackerElitesCommitted: attackerElites,
                DefenderElitesCommitted: defenderElites,
                AttackerOOS: attackerOOS,
                DefenderOOS: defenderOOS,
                IsAmphibious: isAmphibious);
        }

        public static IEnumerable<object[]> ComprehensiveBattleScenarios()
        {
            // ===== BATTLE SIZE EDGE CASES =====
            // Small battles (total SP <= 5)
            yield return new object[] { CreateBattle(1, 1) };  // Minimum
            yield return new object[] { CreateBattle(2, 2) };
            yield return new object[] { CreateBattle(3, 2) };  // Edge: total = 5
            yield return new object[] { CreateBattle(1, 4) };  // Unequal small

            // Medium battles (total SP 6-19)
            yield return new object[] { CreateBattle(3, 3) };  // Edge: total = 6
            yield return new object[] { CreateBattle(5, 5) };
            yield return new object[] { CreateBattle(8, 8) };
            yield return new object[] { CreateBattle(10, 9) }; // Edge: total = 19

            // Large battles (total SP >= 20)
            yield return new object[] { CreateBattle(10, 10) }; // Edge: total = 20
            yield return new object[] { CreateBattle(12, 12) };
            yield return new object[] { CreateBattle(15, 15) }; // Maximum typical

            // ===== RATIO EDGE CASES =====
            // Low ratio (< 3:1)
            yield return new object[] { CreateBattle(5, 3) };
            yield return new object[] { CreateBattle(3, 5) };

            // 3:1 ratio edge
            yield return new object[] { CreateBattle(6, 2) };  // Exactly 3:1
            yield return new object[] { CreateBattle(2, 6) };  // Defender 3:1

            // 4:1 ratio edge
            yield return new object[] { CreateBattle(8, 2) };  // Exactly 4:1
            yield return new object[] { CreateBattle(2, 8) };  // Defender 4:1

            // 5:1+ ratio edge
            yield return new object[] { CreateBattle(10, 2) }; // Exactly 5:1
            yield return new object[] { CreateBattle(2, 10) }; // Defender 5:1

            // 10:1+ ratio (overrun territory)
            yield return new object[] { CreateBattle(10, 1) }; // Exactly 10:1 - overrun!
            yield return new object[] { CreateBattle(15, 1) }; // > 10:1 - overrun!

            // ===== DRM COMBINATIONS =====
            // Leader DRMs
            yield return new object[] { CreateBattle(5, 5, attackerLeaderDRM: 1) };
            yield return new object[] { CreateBattle(5, 5, attackerLeaderDRM: 2) };
            yield return new object[] { CreateBattle(5, 5, attackerLeaderDRM: 3) };
            yield return new object[] { CreateBattle(5, 5, defenderLeaderDRM: 1) };
            yield return new object[] { CreateBattle(5, 5, defenderLeaderDRM: 2) };
            yield return new object[] { CreateBattle(5, 5, defenderLeaderDRM: 3) };
            yield return new object[] { CreateBattle(5, 5, attackerLeaderDRM: 2, defenderLeaderDRM: 2) };

            // Elites (max 2 each)
            yield return new object[] { CreateBattle(5, 5, attackerElites: 1) };
            yield return new object[] { CreateBattle(5, 5, attackerElites: 2) };
            yield return new object[] { CreateBattle(5, 5, defenderElites: 1) };
            yield return new object[] { CreateBattle(5, 5, defenderElites: 2) };
            yield return new object[] { CreateBattle(5, 5, attackerElites: 2, defenderElites: 2) };

            // ===== TERRAIN MODIFIERS =====
            yield return new object[] { CreateBattle(5, 5, fortPresent: true) };
            yield return new object[] { CreateBattle(5, 5, isInterception: true) };
            yield return new object[] { CreateBattle(5, 5, fortPresent: true, isInterception: true) };

            // Fort prevents overrun
            yield return new object[] { CreateBattle(10, 1, fortPresent: true) }; // Would be overrun without fort

            // ===== SUPPLY STATUS =====
            yield return new object[] { CreateBattle(5, 5, attackerOOS: true) };
            yield return new object[] { CreateBattle(5, 5, defenderOOS: true) };
            yield return new object[] { CreateBattle(5, 5, attackerOOS: true, defenderOOS: true) };

            // ===== RESOURCE/CAPITAL (affects star results) =====
            yield return new object[] { CreateBattle(5, 5, resourceOrCapital: true) };
            yield return new object[] { CreateBattle(10, 10, resourceOrCapital: true) };
            yield return new object[] { CreateBattle(5, 5, resourceOrCapital: false) }; // Star possible

            // ===== COMPLEX COMBINATIONS =====
            // High attacker advantage
            yield return new object[] { CreateBattle(8, 5, attackerLeaderDRM: 3, attackerElites: 2, defenderOOS: true) };

            // High defender advantage
            yield return new object[] { CreateBattle(5, 8, defenderLeaderDRM: 3, defenderElites: 2, fortPresent: true, isInterception: true) };

            // Balanced with many modifiers
            yield return new object[] { CreateBattle(8, 6, attackerLeaderDRM: 2, defenderLeaderDRM: 1, attackerElites: 1, fortPresent: true) };
            yield return new object[] { CreateBattle(6, 8, defenderLeaderDRM: 3, isInterception: true, attackerOOS: true) };
            yield return new object[] { CreateBattle(10, 10, attackerLeaderDRM: 2, defenderLeaderDRM: 2, attackerElites: 1, defenderElites: 1) };

            // Maximum DRMs stacked
            yield return new object[] { CreateBattle(5, 5, defenderLeaderDRM: 3, defenderElites: 2, fortPresent: true, isInterception: true, attackerOOS: true) };

            // ===== EXTREME/EDGE CASES =====
            // Extreme defender advantage (2v20)
            yield return new object[] { CreateBattle(2, 20) };

            // Minimum battle (1v1)
            yield return new object[] { CreateBattle(1, 1) };

            // Overrun scenario (10v1)
            yield return new object[] { CreateBattle(10, 1) };

            // Maximum DRM modifiers on both sides (3v3 with DRM+9 each, both OOS, interception)
            yield return new object[] { CreateBattle(3, 3, isInterception: true, attackerLeaderDRM: 9, defenderLeaderDRM: 9, attackerOOS: true, defenderOOS: true) };
        }

        [Theory]
        [MemberData(nameof(ComprehensiveBattleScenarios))]
        public void ExactStats_And_MonteCarlo_ProduceSimilarResults(FtpBattle battle)
        {
            // Both ExactStats and MonteCarlo now use the same full battle rules,
            // so all statistics should be close.

            // Arrange
            int trialsExponent = 17; // 131,072 trials for good convergence

            // Act
            FtpStats exactStats = battle.ExactStats();
            (int trials, FtpStats monteCarloStats) = FtpMonteCarlo.Run(battle, trialsExponent);

            // Assert - battle size should match exactly
            Assert.Equal(exactStats.BattleSize, monteCarloStats.BattleSize);

            // All statistics should be close (5% tolerance for Monte Carlo variance)
            double tolerance = 0.05;

            // Win probabilities
            Assert.Equal(exactStats.AttackerWinProbability, monteCarloStats.AttackerWinProbability, tolerance);
            Assert.Equal(exactStats.DefenderWinProbability, monteCarloStats.DefenderWinProbability, tolerance);

            // Hit statistics
            Assert.Equal(exactStats.HitsStats.MeanHtoA, monteCarloStats.HitsStats.MeanHtoA, tolerance);
            Assert.Equal(exactStats.HitsStats.MeanHtoD, monteCarloStats.HitsStats.MeanHtoD, tolerance);
            Assert.Equal(exactStats.HitsStats.StdDevHtoA, monteCarloStats.HitsStats.StdDevHtoA, tolerance);
            Assert.Equal(exactStats.HitsStats.StdDevHtoD, monteCarloStats.HitsStats.StdDevHtoD, tolerance);

            // Star result probability
            Assert.Equal(exactStats.StarResultProbability, monteCarloStats.StarResultProbability, tolerance);

            // Leader death probabilities
            Assert.Equal(exactStats.AttackerLeaderDeathProbability, monteCarloStats.AttackerLeaderDeathProbability, tolerance);
            Assert.Equal(exactStats.DefenderLeaderDeathProbability, monteCarloStats.DefenderLeaderDeathProbability, tolerance);

            // Distribution probabilities should be close for each bucket
            foreach (int key in exactStats.HitsStats.HitsToA_Prblty.Keys)
            {
                if (monteCarloStats.HitsStats.HitsToA_Prblty.TryGetValue(key, out double mcValue))
                {
                    Assert.Equal(exactStats.HitsStats.HitsToA_Prblty[key], mcValue, tolerance);
                }
            }
            foreach (int key in exactStats.HitsStats.HitsToD_Prblty.Keys)
            {
                if (monteCarloStats.HitsStats.HitsToD_Prblty.TryGetValue(key, out double mcValue))
                {
                    Assert.Equal(exactStats.HitsStats.HitsToD_Prblty[key], mcValue, tolerance);
                }
            }
        }

        #endregion

        #region ExactStats Regression Tests (Golden Values)

        /// <summary>
        /// Regression test to ensure ExactStats calculations remain consistent.
        /// These expected values are FROZEN golden values.
        /// Only update these if the calculation logic intentionally changes.
        /// </summary>
        [Theory]
        [MemberData(nameof(ExactStatsGoldenTestCases))]
        public void ExactStats_RegressionTest_MatchesGoldenValues(
            FtpBattle battle,
            BattleSize expectedBattleSize,
            double expectedAttackerWinProb,
            double expectedDefenderWinProb,
            double expectedMeanHtoA,
            double expectedMeanHtoD,
            double expectedStdDevHtoA,
            double expectedStdDevHtoD,
            double expectedStarProb)
        {
            // Act
            FtpStats stats = battle.ExactStats();

            // Assert - exact match for battle size
            Assert.Equal(expectedBattleSize, stats.BattleSize);

            // Assert - close match for probabilities (within 0.0001 for regression)
            double tolerance = 0.0001;
            Assert.Equal(expectedAttackerWinProb, stats.AttackerWinProbability, tolerance);
            Assert.Equal(expectedDefenderWinProb, stats.DefenderWinProbability, tolerance);
            Assert.Equal(expectedMeanHtoA, stats.HitsStats.MeanHtoA, tolerance);
            Assert.Equal(expectedMeanHtoD, stats.HitsStats.MeanHtoD, tolerance);
            Assert.Equal(expectedStdDevHtoA, stats.HitsStats.StdDevHtoA, tolerance);
            Assert.Equal(expectedStdDevHtoD, stats.HitsStats.StdDevHtoD, tolerance);
            Assert.Equal(expectedStarProb, stats.StarResultProbability, tolerance);
        }

        /// <summary>
        /// FROZEN golden values for ExactStats regression tests.
        /// These values were captured from the implementation and should NOT be recalculated.
        /// Only update these values when intentionally changing the calculation logic.
        /// To regenerate: run the battle through ExactStats and copy the output values.
        /// </summary>
        public static IEnumerable<object[]> ExactStatsGoldenTestCases()
        {
            // Format: battle, expectedBattleSize, attackerWinProb, defenderWinProb, 
            //         meanHtoA, meanHtoD, stdDevHtoA, stdDevHtoD, starProb

            // Small battle 2v2 (verified)
            yield return new object[] { 
                CreateBattle(2, 2), 
                BattleSize.Small, 
                0.083333333333333329, 0.91666666666666663,
                0.83333333333333337, 0.5,
                0.37267799624996495, 0.5,
                0.0 
            };

            // Medium battle 5v5 (verified)
            yield return new object[] { 
                CreateBattle(5, 5), 
                BattleSize.Medium, 
                0.16666666666666666, 0.83333333333333337,
                1.0, 1.0,
                0.0, 0.57735026918962573,
                0.0 
            };

            // Large battle 10v10 (verified)
            yield return new object[] { 
                CreateBattle(10, 10), 
                BattleSize.Large, 
                0.22222222222222221, 0.77777777777777779,
                2.6666666666666665, 2.3333333333333335,
                0.94280904158206336, 0.74535599249992989,
                0.0 
            };

            // Medium 5v5 at resource/capital - no star results (verified)
            yield return new object[] { 
                CreateBattle(5, 5, resourceOrCapital: true), 
                BattleSize.Medium, 
                0.16666666666666666, 0.83333333333333337,
                1.0, 1.0,
                0.0, 0.57735026918962573,
                0.0 
            };

            // Medium 5v5 with elites (attacker +2, defender +1) (verified)
            yield return new object[] { 
                CreateBattle(5, 5, attackerElites: 2, defenderElites: 1), 
                BattleSize.Medium, 
                0.47222222222222221, 0.52777777777777779,
                1.1666666666666667, 1.5,
                0.37267799624996495, 0.5,
                0.33333333333333331 
            };

            // Medium 5v5 with defender OOS (attacker +2 DRM) (verified)
            yield return new object[] { 
                CreateBattle(5, 5, defenderOOS: true), 
                BattleSize.Medium, 
                0.5, 0.5,
                1.0, 1.5,
                0.0, 0.5,
                0.33333333333333331 
            };

            // Complex: 8v6, attacker DRM +2, defender DRM +1, attacker elite +1, fort (verified)
            yield return new object[] { 
                CreateBattle(8, 6, attackerLeaderDRM: 2, defenderLeaderDRM: 1, attackerElites: 1, fortPresent: true), 
                BattleSize.Medium, 
                0.41666666666666669, 0.58333333333333337,
                1.8333333333333333, 1.6666666666666667,
                0.89752746785575113, 0.47140452079103168,
                0.5 
            };

            // Large battle 2v20 - extreme defender advantage
            yield return new object[] { 
                CreateBattle(2, 20), 
                BattleSize.Large, 
                0.0, 1.0,
                2.0, 2.3333333333333335,
                0.0, 0.7453559924999291,
                0.0 
            };

            // Small battle 1v1 - minimum battle
            yield return new object[] { 
                CreateBattle(1, 1), 
                BattleSize.Small, 
                0.08333333333333333, 0.9166666666666666,
                0.8333333333333334, 0.08333333333333333,
                0.3726779962499649, 0.2763853991962833,
                0.0 
            };

            // Medium battle 10v1 - overrun scenario
            yield return new object[] { 
                CreateBattle(10, 1), 
                BattleSize.Medium, 
                1.0, 0.0,
                0.0, 1.0,
                0.0, 0.0,
                0.0 
            };

            // Medium 3v3 with max DRM modifiers - extreme case
            yield return new object[] { 
                CreateBattle(3, 3, isInterception: true, attackerLeaderDRM: 9, defenderLeaderDRM: 9, attackerOOS: true, defenderOOS: true), 
                BattleSize.Medium, 
                1.0, 0.0,
                2.0, 3.0,
                0.0, 0.0,
                1.0 
            };
        }

        #endregion
    }
}
