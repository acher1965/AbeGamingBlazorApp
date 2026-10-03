namespace AbeGaming.GameLogic.TNW
{
    /// <summary>
    /// Exact naval battle probabilities by enumerating every (sixes, fives) result of any
    /// pre-battle shore battery fire, of both Fleets' dice in Round 1, and - for each tied
    /// state pair - in Round 2. As for land battles (see <see cref="TnwLandBattleExactStats"/>)
    /// large dice pools make Round 2 too slow for the browser, so the work is estimated
    /// before it is enumerated and <see cref="TryCalculate"/> declines above the budget.
    /// </summary>
    public static class TnwNavalBattleExactStats
    {
        /// <summary>Outcome evaluations allowed before giving up on exactness (sized for Blazor WebAssembly).</summary>
        public const long DefaultWorkBudget = 1_000_000;

        public static bool TryCalculate(TnwNavalBattle battle, long workBudget, out TnwNavalBattleStats? stats)
        {
            stats = null;
            Dictionary<int, (int Sixes, int Fives, double Probability)[]> distributions = [];
            TnwNavalBattleStatsAccumulator accumulator = new(battle);

            TnwFleetState active0 = TnwNavalBattleMethods.InitialState(battle.Active);
            TnwFleetState inactive0 = TnwNavalBattleMethods.InitialState(battle.Inactive);

            // Pre-battle shore battery fire on the Active Fleet (13.5); a single certain state at sea.
            Dictionary<TnwFleetState, double> activeStarts = [];
            int shoreDice = battle.Location.ShoreBatteryDice();
            if (shoreDice == 0)
            {
                activeStarts[active0] = 1.0;
            }
            else
            {
                foreach ((int sixes, int fives, double probability) in Distribution(distributions, shoreDice))
                {
                    TnwFleetState afterFire = TnwNavalBattleMethods.ShoreBatteryFire(battle, active0, sixes, fives);
                    if (afterFire.Remaining.TotalSquadrons == 0)
                        accumulator.Add(TnwNavalBattleMethods.Conclude(battle, afterFire, inactive0, 0, activeWins: false), probability);
                    else
                        activeStarts[afterFire] = activeStarts.GetValueOrDefault(afterFire) + probability;
                }
            }

            (int Sixes, int Fives, double Probability)[] inactiveRolls = Distribution(
                distributions, TnwNavalBattleMethods.DiceForRound(battle, isActive: false, inactive0, 1));

            long work = 0;
            foreach (TnwFleetState start in activeStarts.Keys)
            {
                work += (long)Distribution(distributions, TnwNavalBattleMethods.DiceForRound(battle, true, start, 1)).Length * inactiveRolls.Length;
                if (work > workBudget)
                    return false;
            }

            Dictionary<(TnwFleetState Active, TnwFleetState Inactive), double> ties = [];
            foreach ((TnwFleetState start, double startProbability) in activeStarts)
            {
                (int Sixes, int Fives, double Probability)[] activeRolls = Distribution(
                    distributions, TnwNavalBattleMethods.DiceForRound(battle, true, start, 1));
                TnwFleetState[] activeAfter = inactiveRolls
                    .Select(roll => TnwNavalBattleMethods.ApplyHits(battle.Active, start, roll.Sixes, roll.Fives, battle.ActiveHasFischer))
                    .ToArray();

                foreach ((int activeSixes, int activeFives, double pActive) in activeRolls)
                {
                    TnwFleetState inactiveAfter = TnwNavalBattleMethods.ApplyHits(battle.Inactive, inactive0, activeSixes, activeFives, battle.InactiveHasFischer);
                    for (int i = 0; i < inactiveRolls.Length; i++)
                    {
                        double probability = startProbability * pActive * inactiveRolls[i].Probability;
                        TnwNavalRoundVerdict verdict = TnwNavalBattleMethods.Verdict(battle, activeAfter[i], inactiveAfter, 1);
                        if (verdict == TnwNavalRoundVerdict.SecondRound)
                        {
                            (TnwFleetState, TnwFleetState) key = (activeAfter[i], inactiveAfter);
                            ties[key] = ties.GetValueOrDefault(key) + probability;
                        }
                        else
                        {
                            accumulator.Add(
                                TnwNavalBattleMethods.Conclude(battle, activeAfter[i], inactiveAfter, 1, verdict == TnwNavalRoundVerdict.ActiveWins),
                                probability);
                        }
                    }
                }
            }

            foreach ((TnwFleetState active, TnwFleetState inactive) in ties.Keys)
            {
                work += (long)Distribution(distributions, TnwNavalBattleMethods.DiceForRound(battle, true, active, 2)).Length
                    * Distribution(distributions, TnwNavalBattleMethods.DiceForRound(battle, false, inactive, 2)).Length;
                if (work > workBudget)
                    return false;
            }

            foreach (((TnwFleetState active, TnwFleetState inactive), double tieProbability) in ties)
            {
                (int Sixes, int Fives, double Probability)[] activeRolls2 = Distribution(
                    distributions, TnwNavalBattleMethods.DiceForRound(battle, true, active, 2));
                (int Sixes, int Fives, double Probability)[] inactiveRolls2 = Distribution(
                    distributions, TnwNavalBattleMethods.DiceForRound(battle, false, inactive, 2));
                TnwFleetState[] activeAfter2 = inactiveRolls2
                    .Select(roll => TnwNavalBattleMethods.ApplyHits(battle.Active, active, roll.Sixes, roll.Fives, battle.ActiveHasFischer))
                    .ToArray();

                foreach ((int activeSixes, int activeFives, double pActive) in activeRolls2)
                {
                    TnwFleetState inactiveAfter2 = TnwNavalBattleMethods.ApplyHits(battle.Inactive, inactive, activeSixes, activeFives, battle.InactiveHasFischer);
                    for (int i = 0; i < inactiveRolls2.Length; i++)
                    {
                        TnwNavalRoundVerdict verdict = TnwNavalBattleMethods.Verdict(battle, activeAfter2[i], inactiveAfter2, 2);
                        accumulator.Add(
                            TnwNavalBattleMethods.Conclude(battle, activeAfter2[i], inactiveAfter2, 2, verdict == TnwNavalRoundVerdict.ActiveWins),
                            tieProbability * pActive * inactiveRolls2[i].Probability);
                    }
                }
            }

            stats = accumulator.Build(isExact: true, monteCarloTrials: 0);
            return true;
        }

        private static (int Sixes, int Fives, double Probability)[] Distribution(
            Dictionary<int, (int Sixes, int Fives, double Probability)[]> cache, int dice)
        {
            if (!cache.TryGetValue(dice, out (int Sixes, int Fives, double Probability)[]? distribution))
            {
                distribution = TnwDicePool.KillDisruptDistribution(dice).ToArray();
                cache[dice] = distribution;
            }
            return distribution;
        }
    }
}
