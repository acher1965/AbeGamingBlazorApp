using System.Numerics;
using System.Runtime.InteropServices;

namespace AbeGaming.GameLogic
{
    /// <summary>
    /// Statistical helper methods for int arrays, including SIMD-accelerated operations.
    /// </summary>
    /// <remarks>
    /// <b>WebAssembly SIMD Note:</b> When running in Blazor WebAssembly, SIMD is limited to 128-bit vectors
    /// (WebAssembly's v128 type). Vector&lt;T&gt; will adapt to this width automatically.
    /// AVX2 (256-bit) and AVX-512 are only available in native/.NET server contexts.
    /// For small arrays (~100 elements or less), prefer the scalar MeanAndStdDev over vectorized versions
    /// as SIMD setup overhead may outweigh benefits.
    /// </remarks>
    public static class IntArrayStatHelpers
    {
        /// <summary>
        /// Calculates the probability distribution of values (0 to maxValue).
        /// </summary>
        /// <param name="array">Array of non-negative integer values to count (negative values will cause IndexOutOfRangeException)</param>
        /// <param name="maxValue">Maximum value to include in distribution (must be &lt;= 1024)</param>
        /// <returns>Array of probabilities for each value 0 to maxValue, or empty array if values is empty</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if maxValue exceeds 1024 (stackalloc limit)</exception>
        /// <exception cref="IndexOutOfRangeException">Thrown if any value in array is negative</exception>
        public static double[] CalculateDistribution(int[] array, int maxValue)
        {
            if (array.Length == 0)
                return [];

            const int MaxStackAllocSize = 1024;
            if (maxValue > MaxStackAllocSize)
                throw new ArgumentOutOfRangeException(nameof(maxValue), maxValue, $"maxValue must be <= {MaxStackAllocSize} to avoid stack overflow");

            int size = maxValue + 1;
            Span<int> counts = stackalloc int[size];
            foreach (int v in array)
                counts[Math.Min(v, maxValue)]++;

            int total = array.Length;
            double[] distribution = new double[size];
            for (int i = 0; i < size; i++)
                distribution[i] = (double)counts[i] / total;

            return distribution;
        }

        /// <summary>
        /// SIMD-accelerated mean and standard deviation from an int array.
        /// Computes both in a single pass, converting to double only at the final step.
        /// </summary>
        /// <returns>Mean and StdDev, or (0, 0) if array is empty</returns>
        public static (double Mean, double StdDev) MeanAndStdDevVectorised(int[] array)
        {
            if (array.Length == 0)
                return (0, 0);

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
        /// Simple scalar mean and standard deviation for small arrays (~100 elements or less).
        /// Single pass, avoids SIMD overhead for small datasets.
        /// </summary>
        /// <returns>Mean and StdDev, or (0, 0) if array is empty</returns>
        public static (double Mean, double StdDev) MeanAndStdDev(int[] array)
        {
            if (array.Length == 0)
                return (0, 0);

            long sum = 0;
            long sumSquares = 0;

            foreach (int v in array)
            {
                sum += v;
                sumSquares += (long)v * v;
            }

            double n = array.Length;
            double mean = sum / n;
            double variance = (sumSquares / n) - (mean * mean);
            double stdDev = Math.Sqrt(variance);

            return (mean, stdDev);
        }

        /// <summary>
        /// SIMD-accelerated sum of an int array using Vector&lt;int&gt;.
        /// Falls back to scalar for remainder elements.
        /// </summary>
        public static int SumVectorised(int[] array)
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
