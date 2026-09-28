namespace AbeGaming.GameLogic.TNW
{
    /// <summary>
    /// Exact Land Battle probabilities by enumerating every (sixes, fives) result of both
    /// sides' dice in Round 1, and - for each tied Round-1 state pair - both sides' dice in
    /// Round 2. Round 2 is where the cost is: each tie state pair needs a joint enumeration of
    /// both sides' rolls, so large dice pools become too slow for the browser. The work is
    /// estimated before Round 2 is enumerated, and <see cref="TryCalculate"/> declines above
    /// the budget so the caller can fall back to <see cref="TnwLandBattleMonteCarlo"/>.
    /// </summary>
    public static class TnwLandBattleExactStats
    {
        /// <summary>Outcome evaluations allowed before giving up on exactness (sized for Blazor WebAssembly).</summary>
        public const long DefaultWorkBudget = 1_000_000;

        /// <summary>
        /// Computes exact statistics if the enumeration fits in <paramref name="workBudget"/>
        /// outcome evaluations; otherwise returns false and <paramref name="stats"/> is null.
        /// </summary>
        public static bool TryCalculate(TnwLandBattle battle, long workBudget, out TnwLandBattleStats? stats)
        {
            stats = null;
            Dictionary<int, (int Sixes, int Fives, double Probability)[]> distributions = [];

            TnwBattleSideState attacker0 = TnwLandBattleMethods.InitialState(battle.Attacker);
            TnwBattleSideState defender0 = TnwLandBattleMethods.InitialState(battle.Defender);
            (int Sixes, int Fives, double Probability)[] attackerRolls = Distribution(
                distributions, TnwLandBattleMethods.DiceForRound(battle, isAttacker: true, attacker0, 1));
            (int Sixes, int Fives, double Probability)[] defenderRolls = Distribution(
                distributions, TnwLandBattleMethods.DiceForRound(battle, isAttacker: false, defender0, 1));

            long work = (long)attackerRolls.Length * defenderRolls.Length;
            if (work > workBudget)
                return false;

            TnwLandBattleStatsAccumulator accumulator = new(battle);
            Dictionary<(TnwBattleSideState Attacker, TnwBattleSideState Defender), double> ties = [];

            // The attacker's state depends only on the defender's roll, so compute it once per roll.
            TnwBattleSideState[] attackerAfter = defenderRolls
                .Select(roll => TnwLandBattleMethods.ApplyHits(attacker0, roll.Sixes, roll.Fives))
                .ToArray();

            foreach ((int attackerSixes, int attackerFives, double pAttacker) in attackerRolls)
            {
                TnwBattleSideState defenderAfter = TnwLandBattleMethods.ApplyHits(defender0, attackerSixes, attackerFives);
                for (int d = 0; d < defenderRolls.Length; d++)
                {
                    double probability = pAttacker * defenderRolls[d].Probability;
                    TnwRoundVerdict verdict = TnwLandBattleMethods.Verdict(attackerAfter[d], defenderAfter, 1);
                    if (verdict == TnwRoundVerdict.SecondRound)
                    {
                        (TnwBattleSideState, TnwBattleSideState) key = (attackerAfter[d], defenderAfter);
                        ties[key] = ties.GetValueOrDefault(key) + probability;
                    }
                    else
                    {
                        accumulator.Add(
                            TnwLandBattleMethods.Conclude(battle, attackerAfter[d], defenderAfter, 1, verdict == TnwRoundVerdict.AttackerWins),
                            probability);
                    }
                }
            }

            foreach ((TnwBattleSideState attacker, TnwBattleSideState defender) in ties.Keys)
            {
                work += (long)Distribution(distributions, TnwLandBattleMethods.DiceForRound(battle, true, attacker, 2)).Length
                    * Distribution(distributions, TnwLandBattleMethods.DiceForRound(battle, false, defender, 2)).Length;
                if (work > workBudget)
                    return false;
            }

            foreach (((TnwBattleSideState attacker, TnwBattleSideState defender), double tieProbability) in ties)
            {
                (int Sixes, int Fives, double Probability)[] attackerRolls2 = Distribution(
                    distributions, TnwLandBattleMethods.DiceForRound(battle, true, attacker, 2));
                (int Sixes, int Fives, double Probability)[] defenderRolls2 = Distribution(
                    distributions, TnwLandBattleMethods.DiceForRound(battle, false, defender, 2));

                TnwBattleSideState[] attackerAfter2 = defenderRolls2
                    .Select(roll => TnwLandBattleMethods.ApplyHits(attacker, roll.Sixes, roll.Fives))
                    .ToArray();

                foreach ((int attackerSixes, int attackerFives, double pAttacker) in attackerRolls2)
                {
                    TnwBattleSideState defenderAfter2 = TnwLandBattleMethods.ApplyHits(defender, attackerSixes, attackerFives);
                    for (int d = 0; d < defenderRolls2.Length; d++)
                    {
                        TnwRoundVerdict verdict = TnwLandBattleMethods.Verdict(attackerAfter2[d], defenderAfter2, 2);
                        accumulator.Add(
                            TnwLandBattleMethods.Conclude(battle, attackerAfter2[d], defenderAfter2, 2, verdict == TnwRoundVerdict.AttackerWins),
                            tieProbability * pAttacker * defenderRolls2[d].Probability);
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
