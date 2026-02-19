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
        HitStats HitsStats,
        double AttackerLeaderDeathProbability,
        double DefenderLeaderDeathProbability,
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
        public static FtpMonteCarloResult Run(FtpBattle battle, int trialsExponent)
        {
            int trials = 1 << trialsExponent;

            // Allocate arrays for results - using int for SIMD-friendly counting
            int[] attackerWins = new int[trials];
            int[] defenderWins = new int[trials];
            int[] overruns = new int[trials];
            int[] stars = new int[trials];
            int[] attackerLeaderDeaths = new int[trials];
            int[] defenderLeaderDeaths = new int[trials];
            int[] attackerCasualties = new int[trials];
            int[] defenderCasualties = new int[trials];

            // Pre-roll all random numbers (SIMD-optimized in RollDice)
            Span<int> randoms = Dice.RollOneDieRepeatedly(trials << 2);

            // Main loop - minimal branching, just store results
            for (int i = 0; i < trials; i++)
            {
                FTPBattleResult result = FtpBattleMethods.BattleResult(battle, randoms.Slice(i << 2, 4));

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
            int totalAttackerWins = IntArrayStatHelpers.SumVectorised(attackerWins);
            int totalDefenderWins = IntArrayStatHelpers.SumVectorised(defenderWins);
            int totalOverruns = IntArrayStatHelpers.SumVectorised(overruns);
            int totalStars = IntArrayStatHelpers.SumVectorised(stars);
            int totalAttackerLeaderDeaths = IntArrayStatHelpers.SumVectorised(attackerLeaderDeaths);
            int totalDefenderLeaderDeaths = IntArrayStatHelpers.SumVectorised(defenderLeaderDeaths);

            // SIMD-accelerated mean and stddev directly from int arrays (no double[] allocation)
            (double avgAttackerCasualties, double stdAttackerCasualties) = IntArrayStatHelpers.MeanAndStdDevVectorised(attackerCasualties);
            (double avgDefenderCasualties, double stdDefenderCasualties) = IntArrayStatHelpers.MeanAndStdDevVectorised(defenderCasualties);

            // Calculate casualty distributions based on battle size (CRT max values)
            BattleSize battleSize = battle.Size();
            (int maxAttackerLoss, int maxDefenderLoss) = battleSize switch
            {
                BattleSize.Small => (2, 1),
                BattleSize.Medium => (3, 3),
                _ => (6, 5) // Large
            };
            Dictionary<int, double> attackerCasualtyDist = IntArrayStatHelpers.CalculateDistribution(attackerCasualties, maxAttackerLoss)
                .Index().ToDictionary(x => x.Index, x => x.Item);
            Dictionary<int, double> defenderCasualtyDist = IntArrayStatHelpers.CalculateDistribution(defenderCasualties, maxDefenderLoss)
                .Index().ToDictionary(x => x.Index, x => x.Item);

            HitStats hitsStats = new(avgDefenderCasualties, stdDefenderCasualties, avgAttackerCasualties, stdAttackerCasualties, defenderCasualtyDist, attackerCasualtyDist);

            return new FtpMonteCarloResult(
                trials,
                battleSize,
                (double)totalAttackerWins / trials,
                (double)totalDefenderWins / trials,
                hitsStats,
                (double)totalAttackerLeaderDeaths / trials,
                (double)totalDefenderLeaderDeaths / trials,
                (double)totalStars / trials);
        }
    }
}
