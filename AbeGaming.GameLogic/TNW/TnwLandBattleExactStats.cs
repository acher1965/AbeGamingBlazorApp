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
            (int Sixes, int Fives, double Probability)[] defenderRolls = Distribution(
                distributions, TnwLandBattleMethods.DiceForRound(battle, isAttacker: false, defender0, 1));

            int shoreDice = battle.AmphibiousLanding.ShoreBatteryDice();
            (int Sixes, int Fives, double Probability)[] shoreRolls = shoreDice > 0
                ? Distribution(distributions, shoreDice)
                : [(0, 0, 1.0)];

            // An Amphibious Assault's pre-battle shore fire (13.7) can only reduce the attacker's
            // Round 1 dice, never increase it, so bound the work across every shore outcome
            // before running the heavier per-outcome enumeration below.
            TnwBattleSideState[] attackerAfterShore = new TnwBattleSideState[shoreRolls.Length];
            long work = 0;
            for (int s = 0; s < shoreRolls.Length; s++)
            {
                attackerAfterShore[s] = shoreDice > 0
                    ? TnwLandBattleMethods.ApplyHits(attacker0, shoreRolls[s].Sixes, shoreRolls[s].Fives)
                    : attacker0;
                if (attackerAfterShore[s].Eliminated)
                    continue;

                work += (long)Distribution(distributions, TnwLandBattleMethods.DiceForRound(battle, true, attackerAfterShore[s], 1)).Length
                    * defenderRolls.Length;
                if (work > workBudget)
                    return false;
            }

            TnwLandBattleStatsAccumulator accumulator = new(battle);
            Dictionary<(TnwBattleSideState Attacker, TnwBattleSideState Defender), double> ties = [];

            for (int s = 0; s < shoreRolls.Length; s++)
            {
                if (attackerAfterShore[s].Eliminated)
                {
                    accumulator.Add(
                        TnwLandBattleMethods.Conclude(battle, attackerAfterShore[s], defender0, rounds: 0, attackerWins: false),
                        shoreRolls[s].Probability);
                    continue;
                }

                AccumulateRoundOne(battle, attackerAfterShore[s], defender0, shoreRolls[s].Probability, defenderRolls, distributions, accumulator, ties);
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

        /// <summary>
        /// Enumerates Round 1 for one starting attacker state (after any shore fire) weighted by
        /// <paramref name="weight"/>, adding concluded outcomes to <paramref name="accumulator"/>
        /// and tied state pairs to <paramref name="ties"/> for Round 2.
        /// </summary>
        private static void AccumulateRoundOne(
            TnwLandBattle battle,
            TnwBattleSideState attacker0,
            TnwBattleSideState defender0,
            double weight,
            (int Sixes, int Fives, double Probability)[] defenderRolls,
            Dictionary<int, (int Sixes, int Fives, double Probability)[]> distributions,
            TnwLandBattleStatsAccumulator accumulator,
            Dictionary<(TnwBattleSideState Attacker, TnwBattleSideState Defender), double> ties)
        {
            (int Sixes, int Fives, double Probability)[] attackerRolls = Distribution(
                distributions, TnwLandBattleMethods.DiceForRound(battle, isAttacker: true, attacker0, 1));

            // The attacker's state depends only on the defender's roll, so compute it once per roll.
            TnwBattleSideState[] attackerAfter = defenderRolls
                .Select(roll => TnwLandBattleMethods.ApplyHits(attacker0, roll.Sixes, roll.Fives))
                .ToArray();

            foreach ((int attackerSixes, int attackerFives, double pAttacker) in attackerRolls)
            {
                TnwBattleSideState defenderAfter = TnwLandBattleMethods.ApplyHits(defender0, attackerSixes, attackerFives);
                for (int d = 0; d < defenderRolls.Length; d++)
                {
                    double probability = weight * pAttacker * defenderRolls[d].Probability;
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
