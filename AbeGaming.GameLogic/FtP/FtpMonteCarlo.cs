using System.Numerics;

namespace AbeGaming.GameLogic.FtP
{
    /// <summary>
    /// Monte Carlo simulation results for FtP battle outcomes.
    /// </summary>
    public record FtpMonteCarloResult(
        int Trials,
        BattleSize BattleSize,
        double AttackerWinProbability,
        double DefenderWinProbability,
        double DrawProbability,
        double AverageAttackerCasualties,
        double AverageDefenderCasualties,
        double AttackerCasualtiesStdDev,
        double DefenderCasualtiesStdDev,
        double AttackerLeaderDeathProbability,
        double DefenderLeaderDeathProbability,
        double OverrunProbability,
        double StarResultProbability,
        double[] AttackerCasualtyDistribution,
        double[] DefenderCasualtyDistribution);

    /// <summary>
    /// Monte Carlo simulation for FtP battle outcomes.
    /// Runs multiple battle trials to calculate probability distributions.
    /// Optimized with SIMD for counting operations.
    /// </summary>
    public static class FtpMonteCarlo
    {
        /// <summary>
        /// Runs Monte Carlo trials for the given battle configuration.
        /// </summary>
        /// <param name="battle">The battle configuration to simulate</param>
        /// <param name="trialsExponent">Power of 2 exponent for number of trials (e.g., 14 = 2^14 = 16,384 trials)</param>
        /// <returns>Statistical results of the trials</returns>
        public static FtpMonteCarloResult Run(FtpLandBattle battle, int trialsExponent)
        {
            int trials = 1 << trialsExponent;

            // Allocate arrays for results - using int for SIMD-friendly counting
            var attackerWins = new int[trials];
            var defenderWins = new int[trials];
            var overruns = new int[trials];
            var stars = new int[trials];
            var attackerLeaderDeaths = new int[trials];
            var defenderLeaderDeaths = new int[trials];
            var attackerCasualties = new int[trials];
            var defenderCasualties = new int[trials];

            // Pre-roll all random numbers (SIMD-optimized in RollDice)
            var randoms = FtpBattleMethods.RollDice(trials << 2);

            // Main loop - minimal branching, just store results
            for (int i = 0; i < trials; i++)
            {
                var result = FtpBattleMethods.BattleResult(battle, randoms.Slice(i << 2, 4));

                // Store as 0/1 for SIMD summation (branchless where possible)
                attackerWins[i] = result.Winner == Winner.Attacker ? 1 : 0;
                defenderWins[i] = result.Winner == Winner.Defender ? 1 : 0;
                overruns[i] = result.Overrun ? 1 : 0;
                stars[i] = result.Star ? 1 : 0;
                attackerLeaderDeaths[i] = result.AttackerLeaderDeath ? 1 : 0;
                defenderLeaderDeaths[i] = result.DefenderLeaderDeath ? 1 : 0;
                attackerCasualties[i] = result.DamageToAttacker;
                defenderCasualties[i] = result.DamageToDefender;
            }

            // SIMD-accelerated summation
            int totalAttackerWins = VectorizedMath.Sum(attackerWins);
            int totalDefenderWins = VectorizedMath.Sum(defenderWins);
            int totalOverruns = VectorizedMath.Sum(overruns);
            int totalStars = VectorizedMath.Sum(stars);
            int totalAttackerLeaderDeaths = VectorizedMath.Sum(attackerLeaderDeaths);
            int totalDefenderLeaderDeaths = VectorizedMath.Sum(defenderLeaderDeaths);

            // SIMD-accelerated mean and stddev directly from int arrays (no double[] allocation)
            var (avgAttackerCasualties, stdAttackerCasualties) = VectorizedMath.MeanAndStdDev(attackerCasualties);
            var (avgDefenderCasualties, stdDefenderCasualties) = VectorizedMath.MeanAndStdDev(defenderCasualties);

            // Calculate casualty distributions based on battle size (CRT max values)
            // Small: Attacker 0-2, Defender 0-1 | Medium: Both 0-3 | Large: Attacker 0-6, Defender 0-5
            var battleSize = battle.Size();
            var (maxAttackerLoss, maxDefenderLoss) = battleSize switch
            {
                BattleSize.Small => (2, 1),
                BattleSize.Medium => (3, 3),
                _ => (6, 5) // Large
            };
            var attackerCasualtyDist = VectorizedMath.CalculateDistribution(attackerCasualties, trials, maxAttackerLoss);
            var defenderCasualtyDist = VectorizedMath.CalculateDistribution(defenderCasualties, trials, maxDefenderLoss);

            return new FtpMonteCarloResult(
                Trials: trials,
                BattleSize: battleSize,
                AttackerWinProbability: (double)totalAttackerWins / trials,
                DefenderWinProbability: (double)totalDefenderWins / trials,
                DrawProbability: (double)(trials - totalAttackerWins - totalDefenderWins) / trials,
                AverageAttackerCasualties: avgAttackerCasualties,
                AverageDefenderCasualties: avgDefenderCasualties,
                AttackerCasualtiesStdDev: stdAttackerCasualties,
                DefenderCasualtiesStdDev: stdDefenderCasualties,
                AttackerLeaderDeathProbability: (double)totalAttackerLeaderDeaths / trials,
                DefenderLeaderDeathProbability: (double)totalDefenderLeaderDeaths / trials,
                OverrunProbability: (double)totalOverruns / trials,
                StarResultProbability: (double)totalStars / trials,
                AttackerCasualtyDistribution: attackerCasualtyDist,
                DefenderCasualtyDistribution: defenderCasualtyDist);
        }
    }
}
