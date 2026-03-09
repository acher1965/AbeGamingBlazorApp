namespace AbeGaming.GameLogic.PoG
{
    public static class PoGExactStats
    {
        public static PoGStats Calculate(PoGBattle battle)
        {
            List<PoGBattleResult> results = new();

            if (battle.AttemptFlankAttack)
            {
                foreach ((int attackerDie, int defenderDie, int flankDie) in Dice.ThreeDice)
                {
                    results.Add(battle.Outcome(attackerDie, defenderDie, flankDie));
                }
            }
            else
            {
                foreach ((int attackerDie, int defenderDie) in Dice.TwoDice)
                {
                    results.Add(battle.Outcome(attackerDie, defenderDie));
                }
            }

            int total = results.Count;
            int attackerWins = results.Count(r => r.Winner == Winner.Attacker);
            int defenderWins = results.Count(r => r.Winner == Winner.Defender);
            int draws = results.Count(r => r.Winner == Winner.Draw);
            int retreats = results.Count(r => r.DefenderRetreatLength > 0);
            int flankSuccesses = results.Count(r => r.FlankAttackSucceeded);

            int[] hitsToDefender = results.Select(r => r.HitsByAttacker).ToArray();
            int[] hitsToAttacker = results.Select(r => r.HitsByDefender).ToArray();

            (double meanHtoD, double stdDevHtoD) = IntArrayStatHelpers.MeanAndStdDev(hitsToDefender);
            (double meanHtoA, double stdDevHtoA) = IntArrayStatHelpers.MeanAndStdDev(hitsToAttacker);

            int maxHitsToD = hitsToDefender.Length == 0 ? 0 : hitsToDefender.Max();
            int maxHitsToA = hitsToAttacker.Length == 0 ? 0 : hitsToAttacker.Max();

            Dictionary<int, double> hitsToDDistribution = IntArrayStatHelpers
                .CalculateDistribution(hitsToDefender, maxHitsToD)
                .Select((probability, hits) => new { hits, probability })
                .ToDictionary(x => x.hits, x => x.probability);

            Dictionary<int, double> hitsToADistribution = IntArrayStatHelpers
                .CalculateDistribution(hitsToAttacker, maxHitsToA)
                .Select((probability, hits) => new { hits, probability })
                .ToDictionary(x => x.hits, x => x.probability);

            double meanRetreatGivenDefenderLoses = attackerWins == 0
                ? 0
                : results
                    .Where(r => r.Winner == Winner.Attacker)
                    .Average(r => r.DefenderRetreatLength);

            return new PoGStats(
                AttackerWinProbability: (double)attackerWins / total,
                DefenderWinProbability: (double)defenderWins / total,
                DrawProbability: (double)draws / total,
                HitsStats: new HitStats(
                    meanHtoD,
                    stdDevHtoD,
                    meanHtoA,
                    stdDevHtoA,
                    hitsToDDistribution,
                    hitsToADistribution),
                DefenderRetreatProbability: (double)retreats / total,
                MeanDefenderRetreatLengthGivenDefenderLoses: meanRetreatGivenDefenderLoses,
                FlankAttackSuccessProbability: battle.AttemptFlankAttack
                    ? (double)flankSuccesses / total
                    : 0);
        }
    }
}
