namespace AbeGaming.GameLogic.TNW
{
    /// <summary>
    /// Exact probability distribution for a Siege (rule 12.3), by enumerating every
    /// (besieger sixes, Fortress sixes, Fortress fives) combination each Round with its
    /// exact probability - a small, strictly bounded state space (at most
    /// <see cref="TnwSiegeMethods.FortressStrength"/> Rounds; see
    /// TNW-FEASIBILITY-2026-09-27.md §1.1-revised for the boundedness proof) - so no
    /// Monte Carlo cross-check is needed.
    /// </summary>
    public static class TnwSiegeExactStats
    {
        public static TnwSiegeStats Calculate(TnwSiegeBattle battle)
        {
            int strength = TnwSiegeMethods.FortressStrength(battle.IsGibraltar);
            List<(TnwSiegeRoundResult Outcome, double Probability)> terminalOutcomes = [];

            Dictionary<TnwSiegeState, double> frontier = new()
            {
                [TnwSiegeMethods.InitialState(battle)] = 1.0,
            };

            for (int round = 1; round <= strength && frontier.Count > 0; round++)
            {
                Dictionary<TnwSiegeState, double> nextFrontier = [];

                foreach ((TnwSiegeState state, double stateProbability) in frontier)
                {
                    int besiegerDice = TnwSiegeMethods.BesiegerDiceThisRound(battle, state.UnitsAvailable, state.CommanderAvailable);

                    foreach ((int besiegerSixes, double pBesieger) in TnwDicePool.SixesDistribution(besiegerDice))
                    {
                        if (pBesieger == 0)
                            continue;

                        foreach ((int fortressSixes, int fortressFives, double pFortress) in TnwDicePool.KillDisruptDistribution(strength))
                        {
                            if (pFortress == 0)
                                continue;

                            double branchProbability = stateProbability * pBesieger * pFortress;
                            TnwSiegeRoundResult outcome = TnwSiegeMethods.ResolveRound(
                                battle, state, round, besiegerSixes, fortressSixes, fortressFives);

                            if (outcome.Ended)
                            {
                                terminalOutcomes.Add((outcome, branchProbability));
                            }
                            else
                            {
                                nextFrontier[outcome.State] = nextFrontier.GetValueOrDefault(outcome.State) + branchProbability;
                            }
                        }
                    }
                }

                frontier = nextFrontier;
            }

            // Proven bound in TNW-FEASIBILITY-2026-09-27.md §1.1-revised: every branch has
            // ended by `strength` Rounds. Defensive fallback only - not expected to run.
            foreach ((TnwSiegeState state, double probability) in frontier)
            {
                terminalOutcomes.Add((new TnwSiegeRoundResult(state, true, false, false, false, strength), probability));
            }

            return Aggregate(battle, terminalOutcomes);
        }

        private static TnwSiegeStats Aggregate(TnwSiegeBattle battle, List<(TnwSiegeRoundResult Outcome, double Probability)> outcomes)
        {
            double fallsProbability = outcomes.Where(o => o.Outcome.FortressFalls).Sum(o => o.Probability);
            double overrunGivenFallsProbability = fallsProbability == 0
                ? 0
                : outcomes.Where(o => o.Outcome.FortressFalls && o.Outcome.Overrun).Sum(o => o.Probability) / fallsProbability;
            double eliminatedProbability = outcomes.Where(o => o.Outcome.BesiegersEliminated).Sum(o => o.Probability);
            double commanderLostProbability = battle.CommanderPresent
                ? outcomes.Where(o => !o.Outcome.State.CommanderAlive).Sum(o => o.Probability)
                : 0;

            int[] unitsLost = outcomes.Select(o => battle.Units - o.Outcome.State.UnitsAlive).ToArray();
            int[] roundsTaken = outcomes.Select(o => o.Outcome.Round).ToArray();
            double[] weights = outcomes.Select(o => o.Probability).ToArray();

            (double meanUnitsLost, double stdDevUnitsLost) = WeightedMeanAndStdDev(unitsLost, weights);
            (double meanRounds, _) = WeightedMeanAndStdDev(roundsTaken, weights);

            Dictionary<int, double> roundsDistribution = outcomes
                .GroupBy(o => o.Outcome.Round)
                .ToDictionary(g => g.Key, g => g.Sum(o => o.Probability));

            Dictionary<int, double> unitsLostDistribution = outcomes
                .GroupBy(o => battle.Units - o.Outcome.State.UnitsAlive)
                .ToDictionary(g => g.Key, g => g.Sum(o => o.Probability));

            return new TnwSiegeStats(
                FortressFallsProbability: fallsProbability,
                OverrunProbabilityGivenFalls: overrunGivenFallsProbability,
                BesiegersEliminatedProbability: eliminatedProbability,
                CommanderLostProbability: commanderLostProbability,
                MeanRoundsTaken: meanRounds,
                MeanBesiegerUnitsLost: meanUnitsLost,
                StdDevBesiegerUnitsLost: stdDevUnitsLost,
                RoundsDistribution: roundsDistribution,
                BesiegerUnitsLostDistribution: unitsLostDistribution);
        }

        private static (double Mean, double StdDev) WeightedMeanAndStdDev(int[] values, double[] weights)
        {
            double totalWeight = weights.Sum();
            if (totalWeight == 0)
                return (0, 0);

            double mean = 0;
            double meanOfSquares = 0;
            for (int i = 0; i < values.Length; i++)
            {
                mean += values[i] * weights[i];
                meanOfSquares += (double)values[i] * values[i] * weights[i];
            }
            mean /= totalWeight;
            meanOfSquares /= totalWeight;

            double variance = Math.Max(0, meanOfSquares - mean * mean);
            return (mean, Math.Sqrt(variance));
        }
    }
}
