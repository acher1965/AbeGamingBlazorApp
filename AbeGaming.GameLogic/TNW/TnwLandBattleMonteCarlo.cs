namespace AbeGaming.GameLogic.TNW
{
    /// <summary>
    /// Monte Carlo estimate of Land Battle probabilities, for battles whose dice pools are too
    /// large for <see cref="TnwLandBattleExactStats"/> to enumerate in the browser.
    /// </summary>
    public static class TnwLandBattleMonteCarlo
    {
        /// <summary>2^16 = 65,536 trials: about +/-0.4 percentage points (95%) on a 50% probability.</summary>
        public const int DefaultTrialsExponent = 16;

        public static TnwLandBattleStats Run(TnwLandBattle battle, int trialsExponent = DefaultTrialsExponent, Random? random = null)
        {
            int trials = 1 << trialsExponent;
            Random rng = random ?? Random.Shared;
            TnwLandBattleStatsAccumulator accumulator = new(battle);

            for (int trial = 0; trial < trials; trial++)
                accumulator.Add(TnwLandBattleMethods.Simulate(battle, rng, log: null), 1);

            return accumulator.Build(isExact: false, monteCarloTrials: trials);
        }
    }
}
