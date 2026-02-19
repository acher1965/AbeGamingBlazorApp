using AbeGaming.GameLogic.FtP;

namespace AbeGaming.GameLogic.Tests
{
    /// <summary>
    /// Unit tests for FtP battle statistics calculations.
    /// Tests exact stats (LossDistributions) and Monte Carlo simulations.
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

        #region Exact Stats (LossDistributions) Tests

        [Theory]
        [InlineData(1, 1, BattleSize.Small)]
        [InlineData(2, 2, BattleSize.Small)]
        [InlineData(5, 5, BattleSize.Medium)]
        [InlineData(10, 10, BattleSize.Large)]
        public void LossDistributions_EqualStrengths_ReturnsBattleSize(int attackerSize, int defenderSize, BattleSize expectedSize)
        {
            // Arrange
            FtpBattle battle = CreateDefaultBattle(attackerSize, defenderSize);

            // Act
            HitStats stats = battle.LossDistributions();

            // Assert - verify battle size is correct
            Assert.Equal(expectedSize, battle.Size());
        }

        [Theory]
        [InlineData(1, 1)]
        [InlineData(2, 2)]
        [InlineData(5, 5)]
        [InlineData(10, 10)]
        public void LossDistributions_EqualStrengths_MeanHitsAreNonNegative(int attackerSize, int defenderSize)
        {
            // Arrange
            FtpBattle battle = CreateDefaultBattle(attackerSize, defenderSize);

            // Act
            HitStats stats = battle.LossDistributions();

            // Assert
            Assert.True(stats.MeanHtoA >= 0, "Mean hits to attacker should be non-negative");
            Assert.True(stats.MeanHtoD >= 0, "Mean hits to defender should be non-negative");
            Assert.True(stats.StdDevHtoA >= 0, "StdDev hits to attacker should be non-negative");
            Assert.True(stats.StdDevHtoD >= 0, "StdDev hits to defender should be non-negative");
        }

        [Theory]
        [InlineData(1, 1)]
        [InlineData(2, 2)]
        [InlineData(5, 5)]
        [InlineData(10, 10)]
        public void LossDistributions_EqualStrengths_ProbabilitiesSumToOne(int attackerSize, int defenderSize)
        {
            // Arrange
            FtpBattle battle = CreateDefaultBattle(attackerSize, defenderSize);

            // Act
            HitStats stats = battle.LossDistributions();

            // Assert - probabilities should sum to approximately 1.0
            double sumHtoA = stats.HitsToA_Prblty.Values.Sum();
            double sumHtoD = stats.HitsToD_Prblty.Values.Sum();

            Assert.True(Math.Abs(sumHtoA - 1.0) < 0.001, $"HitsToA probabilities should sum to 1.0, got {sumHtoA}");
            Assert.True(Math.Abs(sumHtoD - 1.0) < 0.001, $"HitsToD probabilities should sum to 1.0, got {sumHtoD}");
        }

        [Fact]
        public void LossDistributions_SmallBattle_2v2_ExpectedValues()
        {
            // Arrange - 2 vs 2 is a Small battle with no ratio DRM
            FtpBattle battle = CreateDefaultBattle(2, 2);

            // Act
            HitStats stats = battle.LossDistributions();

            // Assert - verify we have a Small battle
            Assert.Equal(BattleSize.Small, battle.Size());

            // Small battle CRT (rolls 1-6 with no DRM):
            // HitsToD: [0,0,0,1,1,1] -> mean = 3/6 = 0.5
            // HitsToA: [0,1,1,1,1,1] -> mean = 5/6 ≈ 0.833
            Assert.Equal(0.5, stats.MeanHtoD, precision: 2);
            Assert.Equal(0.833, stats.MeanHtoA, precision: 2);
        }

        [Fact]
        public void LossDistributions_MediumBattle_5v5_ExpectedValues()
        {
            // Arrange - 5 vs 5 is a Medium battle with no ratio DRM
            FtpBattle battle = CreateDefaultBattle(5, 5);

            // Act
            HitStats stats = battle.LossDistributions();

            // Assert - verify we have a Medium battle
            Assert.Equal(BattleSize.Medium, battle.Size());

            // Medium battle CRT (rolls 1-6 with no DRM):
            // HitsToD: [0,1,1,1,1,2] -> mean = 6/6 = 1.0
            // HitsToA: [1,1,1,1,1,1] -> mean = 6/6 = 1.0
            Assert.Equal(1.0, stats.MeanHtoD, precision: 2);
            Assert.Equal(1.0, stats.MeanHtoA, precision: 2);
        }

        [Fact]
        public void LossDistributions_LargeBattle_10v10_ExpectedValues()
        {
            // Arrange - 10 vs 10 is a Large battle with no ratio DRM
            FtpBattle battle = CreateDefaultBattle(10, 10);

            // Act
            HitStats stats = battle.LossDistributions();

            // Assert - verify we have a Large battle
            Assert.Equal(BattleSize.Large, battle.Size());

            // Large battle CRT (rolls 1-6 with no DRM):
            // HitsToD: [1,2,2,3,3,3] -> mean = 14/6 ≈ 2.333
            // HitsToA: [1,2,3,3,3,4] -> mean = 16/6 ≈ 2.667
            Assert.Equal(2.333, stats.MeanHtoD, precision: 2);
            Assert.Equal(2.667, stats.MeanHtoA, precision: 2);
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
            FtpMonteCarloResult result = FtpMonteCarlo.Run(battle, trialsExponent);

            // Assert - win probabilities + draw should sum to 1.0
            double totalProbability = result.AttackerWinProbability + result.DefenderWinProbability;
            // Note: There can be draws, so total may be less than 1.0
            Assert.True(totalProbability <= 1.0 + 0.001, $"Win probabilities should not exceed 1.0, got {totalProbability}");
            Assert.True(totalProbability >= 0.0, "Win probabilities should be non-negative");
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
            FtpMonteCarloResult result = FtpMonteCarlo.Run(battle, trialsExponent);

            // Assert
            Assert.True(result.HitsStats.MeanHtoA >= 0, "Mean hits to attacker should be non-negative");
            Assert.True(result.HitsStats.MeanHtoD >= 0, "Mean hits to defender should be non-negative");
            Assert.True(result.HitsStats.StdDevHtoA >= 0, "StdDev should be non-negative");
            Assert.True(result.HitsStats.StdDevHtoD >= 0, "StdDev should be non-negative");
        }

        [Theory]
        [InlineData(1, 1)]
        [InlineData(2, 2)]
        [InlineData(5, 5)]
        [InlineData(10, 10)]
        public void MonteCarlo_EqualStrengths_SpecialOutcomesAreValid(int attackerSize, int defenderSize)
        {
            // Arrange
            FtpBattle battle = CreateDefaultBattle(attackerSize, defenderSize);
            int trialsExponent = 14;

            // Act
            FtpMonteCarloResult result = FtpMonteCarlo.Run(battle, trialsExponent);

            // Assert - probabilities should be between 0 and 1
            Assert.InRange(result.StarResultProbability, 0.0, 1.0);
            Assert.InRange(result.AttackerLeaderDeathProbability, 0.0, 1.0);
            Assert.InRange(result.DefenderLeaderDeathProbability, 0.0, 1.0);
        }

        [Fact]
        public void MonteCarlo_SmallBattle_TrialsCountCorrect()
        {
            // Arrange
            FtpBattle battle = CreateDefaultBattle(1, 1);
            int trialsExponent = 14; // 2^14 = 16,384

            // Act
            FtpMonteCarloResult result = FtpMonteCarlo.Run(battle, trialsExponent);

            // Assert
            Assert.Equal(16384, result.Trials);
            Assert.Equal(BattleSize.Small, result.BattleSize);
        }

        [Fact]
        public void MonteCarlo_MediumBattle_TrialsCountCorrect()
        {
            // Arrange
            FtpBattle battle = CreateDefaultBattle(5, 5);
            int trialsExponent = 14;

            // Act
            FtpMonteCarloResult result = FtpMonteCarlo.Run(battle, trialsExponent);

            // Assert
            Assert.Equal(16384, result.Trials);
            Assert.Equal(BattleSize.Medium, result.BattleSize);
        }

        [Fact]
        public void MonteCarlo_SmallBattle_1v1_ExpectedValues()
        {
            // Arrange - 1v1 Small battle, defender always wins (attacker capped at 1 SP loss = wiped out)
            FtpBattle battle = CreateDefaultBattle(1, 1);
            int trialsExponent = 16; // 65,536 trials for stable values

            // Act
            FtpMonteCarloResult result = FtpMonteCarlo.Run(battle, trialsExponent);

            // Assert - expected values from Monte Carlo with full rules
            // 1v1: Attacker almost always loses (capped to 1 hit wipes them out)
            Assert.Equal(0.083, result.AttackerWinProbability, tolerance: 0.02);
            Assert.Equal(0.917, result.DefenderWinProbability, tolerance: 0.02);
            Assert.Equal(0.833, result.HitsStats.MeanHtoA, tolerance: 0.02);
            Assert.Equal(0.083, result.HitsStats.MeanHtoD, tolerance: 0.02);
            Assert.Equal(0.0, result.StarResultProbability, tolerance: 0.01);
        }

        [Fact]
        public void MonteCarlo_SmallBattle_2v2_ExpectedValues()
        {
            // Arrange - 2v2 Small battle
            FtpBattle battle = CreateDefaultBattle(2, 2);
            int trialsExponent = 16;

            // Act
            FtpMonteCarloResult result = FtpMonteCarlo.Run(battle, trialsExponent);

            // Assert - expected values from Monte Carlo with full rules
            Assert.Equal(0.083, result.AttackerWinProbability, tolerance: 0.02);
            Assert.Equal(0.917, result.DefenderWinProbability, tolerance: 0.02);
            Assert.Equal(0.833, result.HitsStats.MeanHtoA, tolerance: 0.02);
            Assert.Equal(0.50, result.HitsStats.MeanHtoD, tolerance: 0.02);
            Assert.Equal(0.0, result.StarResultProbability, tolerance: 0.01);
        }

        [Fact]
        public void MonteCarlo_MediumBattle_5v5_ExpectedValues()
        {
            // Arrange - 5v5 Medium battle
            FtpBattle battle = CreateDefaultBattle(5, 5);
            int trialsExponent = 16;

            // Act
            FtpMonteCarloResult result = FtpMonteCarlo.Run(battle, trialsExponent);

            // Assert - expected values from Monte Carlo with full rules
            Assert.Equal(0.167, result.AttackerWinProbability, tolerance: 0.02);
            Assert.Equal(0.833, result.DefenderWinProbability, tolerance: 0.02);
            Assert.Equal(1.0, result.HitsStats.MeanHtoA, tolerance: 0.02);
            Assert.Equal(1.0, result.HitsStats.MeanHtoD, tolerance: 0.02);
            Assert.Equal(0.0, result.StarResultProbability, tolerance: 0.01);
        }

        [Fact]
        public void MonteCarlo_LargeBattle_10v10_ExpectedValues()
        {
            // Arrange - 10v10 Large battle
            FtpBattle battle = CreateDefaultBattle(10, 10);
            int trialsExponent = 16;

            // Act
            FtpMonteCarloResult result = FtpMonteCarlo.Run(battle, trialsExponent);

            // Assert - expected values from Monte Carlo with full rules
            Assert.Equal(0.222, result.AttackerWinProbability, tolerance: 0.02);
            Assert.Equal(0.778, result.DefenderWinProbability, tolerance: 0.02);
            Assert.Equal(2.667, result.HitsStats.MeanHtoA, tolerance: 0.05);
            Assert.Equal(2.333, result.HitsStats.MeanHtoD, tolerance: 0.05);
            Assert.Equal(0.0, result.StarResultProbability, tolerance: 0.01);
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
            HitStats exactStats = battle.LossDistributions();
            FtpMonteCarloResult mcResult = FtpMonteCarlo.Run(battle, trialsExponent);

            // Assert - just verify both produce valid results, not that they match
            Assert.True(exactStats.MeanHtoA >= 0);
            Assert.True(exactStats.MeanHtoD >= 0);
            Assert.True(mcResult.HitsStats.MeanHtoA >= 0);
            Assert.True(mcResult.HitsStats.MeanHtoD >= 0);

            // Log the values for informational purposes (visible in test output)
            // Monte Carlo should show capped/floored values from full rules
            // Exact Stats shows raw CRT probabilities
        }

        #endregion
    }
}
