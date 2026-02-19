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
    }
}
