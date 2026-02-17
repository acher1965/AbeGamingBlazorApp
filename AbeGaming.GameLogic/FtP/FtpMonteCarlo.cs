using System.Numerics;
using System.Runtime.InteropServices;

namespace AbeGaming.GameLogic.FtP
{
    /// <summary>
    /// Monte Carlo simulation results for FtP battle outcomes.
    /// </summary>
    public record FtpMonteCarloResult(
        int Trials,
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
        double StarResultProbability);

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
            int totalAttackerWins = SumVectorized(attackerWins);
            int totalDefenderWins = SumVectorized(defenderWins);
            int totalOverruns = SumVectorized(overruns);
            int totalStars = SumVectorized(stars);
            int totalAttackerLeaderDeaths = SumVectorized(attackerLeaderDeaths);
            int totalDefenderLeaderDeaths = SumVectorized(defenderLeaderDeaths);

            // SIMD-accelerated mean and stddev directly from int arrays (no double[] allocation)
            var (avgAttackerCasualties, stdAttackerCasualties) = MeanAndStdDevVectorized(attackerCasualties);
            var (avgDefenderCasualties, stdDefenderCasualties) = MeanAndStdDevVectorized(defenderCasualties);

            return new FtpMonteCarloResult(
                Trials: trials,
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
                StarResultProbability: (double)totalStars / trials);
        }

        /// <summary>
        /// SIMD-accelerated mean and standard deviation from an int array.
        /// Computes both in a single pass, converting to double only at the final step.
        /// </summary>
        private static (double Mean, double StdDev) MeanAndStdDevVectorized(int[] array)
        {
            var span = array.AsSpan();
            var vectors = MemoryMarshal.Cast<int, Vector<int>>(span);

            var sum = Vector<int>.Zero;
            var sumSquares = Vector<int>.Zero;

            foreach (var v in vectors)
            {
                sum += v;
                sumSquares += v * v;
            }

            // Sum the vector lanes (use long to avoid overflow when combining)
            long totalSum = 0;
            long totalSumSquares = 0;
            for (int i = 0; i < Vector<int>.Count; i++)
            {
                totalSum += sum[i];
                totalSumSquares += sumSquares[i];
            }

            // Handle remainder (if array length not divisible by vector width)
            int remainder = span.Length % Vector<int>.Count;
            for (int i = span.Length - remainder; i < span.Length; i++)
            {
                totalSum += span[i];
                totalSumSquares += (long)span[i] * span[i];
            }

            // Final double arithmetic
            double n = array.Length;
            double mean = totalSum / n;
            double variance = (totalSumSquares / n) - (mean * mean);
            double stdDev = Math.Sqrt(variance);

            return (mean, stdDev);
        }

        /// <summary>
        /// SIMD-accelerated sum of an int array using Vector&lt;int&gt;.
        /// Falls back to scalar for remainder elements.
        /// </summary>
        private static int SumVectorized(int[] array)
        {
            var span = array.AsSpan();
            var vectors = MemoryMarshal.Cast<int, Vector<int>>(span);

            var sum = Vector<int>.Zero;
            foreach (var v in vectors)
                sum += v;

            // Sum the vector lanes
            int total = 0;
            for (int i = 0; i < Vector<int>.Count; i++)
                total += sum[i];

            // Handle remainder (if array length not divisible by vector width)
            int remainder = span.Length % Vector<int>.Count;
            for (int i = span.Length - remainder; i < span.Length; i++)
                total += span[i];

            return total;
        }
    }
}
