using System.Numerics;
using System.Runtime.InteropServices;

namespace AbeGaming.GameLogic
{
    /// <summary>
    /// SIMD-accelerated mathematical operations for Monte Carlo simulations.
    /// </summary>
    public static class VectorizedMath
    {
        /// <summary>
        /// Calculates the probability distribution of values (0 to maxValue).
        /// </summary>
        /// <param name="values">Array of integer values to count</param>
        /// <param name="total">Total count for probability calculation</param>
        /// <param name="maxValue">Maximum value to include in distribution</param>
        /// <returns>Array of probabilities for each value 0 to maxValue</returns>
        public static double[] CalculateDistribution(int[] values, int total, int maxValue)
        {
            Span<int> counts = stackalloc int[maxValue + 1];
            foreach (var v in values)
                if (v <= maxValue)
                    counts[v]++;

            int size = maxValue + 1;
            var distribution = new double[size];
            for (int i = 0; i < size; i++)
                distribution[i] = (double)counts[i] / total;

            return distribution;
        }

        /// <summary>
        /// SIMD-accelerated mean and standard deviation from an int array.
        /// Computes both in a single pass, converting to double only at the final step.
        /// </summary>
        public static (double Mean, double StdDev) MeanAndStdDev(int[] array)
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
        public static int Sum(int[] array)
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
