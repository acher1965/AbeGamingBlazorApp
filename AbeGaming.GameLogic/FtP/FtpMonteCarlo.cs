using MathNet.Numerics.Statistics;

namespace AbeGaming.GameLogic.FtP
{
    /// <summary>
    /// Monte Carlo simulation results for FtP battle outcomes.
    /// </summary>
    public record FtpMonteCarloResult(
        int SimulationCount,
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
    /// Runs multiple battle simulations to calculate probability distributions.
    /// </summary>
    public static class FtpMonteCarlo
    {
        /// <summary>
        /// Runs a Monte Carlo simulation for the given battle configuration.
        /// </summary>
        /// <param name="battle">The battle configuration to simulate</param>
        /// <param name="simulationCount">Number of simulations to run (default: 10,000)</param>
        /// <param name="seed">Optional random seed for reproducibility</param>
        /// <returns>Statistical results of the simulation</returns>
        public static FtpMonteCarloResult Simulate(
            FtpLandBattle battle,
            int simulationCount = 10_000,
            int? seed = null)
        {
            var rng = seed.HasValue ? new Random(seed.Value) : new Random();

            int attackerWins = 0;
            int defenderWins = 0;
            int draws = 0;
            int overruns = 0;
            int starResults = 0;
            int attackerLeaderDeaths = 0;
            int defenderLeaderDeaths = 0;

            var attackerCasualties = new double[simulationCount];
            var defenderCasualties = new double[simulationCount];

            for (int i = 0; i < simulationCount; i++)
            {
                var result = FtpBattleMethods.BattleResult(battle, rng);

                // Count outcomes
                switch (result.Winner)
                {
                    case Winner.Attacker:
                        attackerWins++;
                        break;
                    case Winner.Defender:
                        defenderWins++;
                        break;
                    default:
                        draws++;
                        break;
                }

                // Track casualties
                attackerCasualties[i] = result.DamageToAttacker;
                defenderCasualties[i] = result.DamageToDefender;

                // Track special outcomes
                if (result.Overrun) overruns++;
                if (result.Star) starResults++;
                if (result.AttackerLeaderDeath) attackerLeaderDeaths++;
                if (result.DefenderLeaderDeath) defenderLeaderDeaths++;
            }

            // Calculate statistics using MathNet.Numerics
            return new FtpMonteCarloResult(
                SimulationCount: simulationCount,
                AttackerWinProbability: (double)attackerWins / simulationCount,
                DefenderWinProbability: (double)defenderWins / simulationCount,
                DrawProbability: (double)draws / simulationCount,
                AverageAttackerCasualties: attackerCasualties.Mean(),
                AverageDefenderCasualties: defenderCasualties.Mean(),
                AttackerCasualtiesStdDev: attackerCasualties.StandardDeviation(),
                DefenderCasualtiesStdDev: defenderCasualties.StandardDeviation(),
                AttackerLeaderDeathProbability: (double)attackerLeaderDeaths / simulationCount,
                DefenderLeaderDeathProbability: (double)defenderLeaderDeaths / simulationCount,
                OverrunProbability: (double)overruns / simulationCount,
                StarResultProbability: (double)starResults / simulationCount);
        }

        /// <summary>
        /// Runs a quick simulation with fewer iterations for responsive UI.
        /// </summary>
        public static FtpMonteCarloResult SimulateQuick(FtpLandBattle battle, int? seed = null)
            => Simulate(battle, simulationCount: 1_000, seed: seed);

        /// <summary>
        /// Runs a detailed simulation with more iterations for accuracy.
        /// </summary>
        public static FtpMonteCarloResult SimulateDetailed(FtpLandBattle battle, int? seed = null)
            => Simulate(battle, simulationCount: 100_000, seed: seed);
    }
}
