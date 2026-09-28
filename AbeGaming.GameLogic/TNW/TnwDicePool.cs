namespace AbeGaming.GameLogic.TNW
{
    /// <summary>
    /// Shared dice-pool math for TNW's Battle/Siege system: roll N six-sided dice,
    /// a "6" kills, a "5" disrupts, anything else misses (rules 11.2-11.3, 12.3).
    /// Built for the Siege calculator; intended for reuse by a future Land Battle
    /// calculator, which shares the same underlying dice mechanic.
    /// </summary>
    public static class TnwDicePool
    {
        private const double SixProbability = 1.0 / 6.0;
        private const double FiveProbability = 1.0 / 6.0;

        /// <summary>
        /// P(exactly <paramref name="sixes"/> sixes) out of <paramref name="diceCount"/> d6
        /// (binomial, p=1/6). Use this when only kills/successes matter to the caller -
        /// e.g. a Siege's attack roll on the Fortress, where 5s and misses are equivalent.
        /// </summary>
        public static double SixesProbability(int diceCount, int sixes)
        {
            if (diceCount < 0 || sixes < 0 || sixes > diceCount)
                return 0;

            return BinomialCoefficient(diceCount, sixes)
                * Math.Pow(SixProbability, sixes)
                * Math.Pow(1 - SixProbability, diceCount - sixes);
        }

        /// <summary>Every (sixes, probability) outcome for <paramref name="diceCount"/> d6.</summary>
        public static IEnumerable<(int Sixes, double Probability)> SixesDistribution(int diceCount)
        {
            for (int sixes = 0; sixes <= diceCount; sixes++)
                yield return (sixes, SixesProbability(diceCount, sixes));
        }

        /// <summary>
        /// P(exactly <paramref name="sixes"/> sixes and <paramref name="fives"/> fives) out of
        /// <paramref name="diceCount"/> d6 (trinomial: 6 / 5 / other). Use this when both kills
        /// and disrupts matter to the caller - e.g. a Fortress's defensive fire on the besieger.
        /// </summary>
        public static double KillDisruptProbability(int diceCount, int sixes, int fives)
        {
            if (diceCount < 0 || sixes < 0 || fives < 0 || sixes + fives > diceCount)
                return 0;

            int misses = diceCount - sixes - fives;
            double coefficient = Factorial(diceCount) / (Factorial(sixes) * Factorial(fives) * Factorial(misses));
            return coefficient
                * Math.Pow(SixProbability, sixes)
                * Math.Pow(FiveProbability, fives)
                * Math.Pow(1 - SixProbability - FiveProbability, misses);
        }

        /// <summary>Every (sixes, fives, probability) outcome for <paramref name="diceCount"/> d6.</summary>
        public static IEnumerable<(int Sixes, int Fives, double Probability)> KillDisruptDistribution(int diceCount)
        {
            for (int sixes = 0; sixes <= diceCount; sixes++)
                for (int fives = 0; fives <= diceCount - sixes; fives++)
                    yield return (sixes, fives, KillDisruptProbability(diceCount, sixes, fives));
        }

        private static double BinomialCoefficient(int n, int k) => Factorial(n) / (Factorial(k) * Factorial(n - k));

        private static double Factorial(int n)
        {
            double result = 1;
            for (int i = 2; i <= n; i++)
                result *= i;
            return result;
        }
    }
}
