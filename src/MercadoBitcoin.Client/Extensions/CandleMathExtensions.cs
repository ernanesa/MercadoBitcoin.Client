using System;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using MercadoBitcoin.Client.Models;

namespace MercadoBitcoin.Client.Extensions
{
    /// <summary>
    /// High-performance SIMD-accelerated math extensions for CandleData.
    /// Uses AVX2/Vector256 where available.
    /// </summary>
    public static class CandleMathExtensions
    {
        /// <summary>
        /// Calculates the average Close price using SIMD if available.
        /// </summary>
        public static decimal CalculateAverageClose(this ReadOnlySpan<CandleData> candles)
        {
            if (candles.IsEmpty) return 0m;

            // Fallback for small arrays or no SIMD support
            if (!Avx2.IsSupported || candles.Length < Vector256<double>.Count)
            {
                decimal sum = 0;
                foreach (var candle in candles)
                {
                    sum += candle.Close;
                }
                return sum / candles.Length;
            }

            // SIMD Implementation
            // Note: Converting decimal to double for SIMD processing. 
            // This trades a tiny bit of precision for massive speed in technical analysis.
            // For exact financial accounting, use the scalar decimal loop.

            double sumDouble = 0;
            int vectorSize = Vector256<double>.Count; // 4 doubles fit in 256 bits

            // We need to extract values to a contiguous memory block for efficient loading
            // Or we can load gather/scatter if we had struct of arrays (SoA).
            // Since CandleData is Array of Structs (AoS), we have a stride.
            // Loading from AoS to Vector is tricky without gather support (AVX2 gather is slow).
            // So we might need to copy to a temporary buffer or just do scalar if the copy overhead is too high.

            // However, GAP-21 suggests: "Span<double> values = stackalloc double[candles.Length];"
            // This copy step allows us to use SIMD on the contiguous doubles.
            // For very large arrays, stackalloc might blow the stack.
            // We should chunk it.

            const int ChunkSize = 1024;
            Span<double> buffer = stackalloc double[ChunkSize];

            int remaining = candles.Length;
            int offset = 0;

            while (remaining > 0)
            {
                int currentChunkSize = Math.Min(remaining, ChunkSize);

                // Copy and convert to double
                for (int j = 0; j < currentChunkSize; j++)
                {
                    buffer[j] = (double)candles[offset + j].Close;
                }

                // Process chunk with SIMD
                int chunkIndex = 0;
                var vSum = Vector256<double>.Zero;

                while (chunkIndex <= currentChunkSize - vectorSize)
                {
                    var v = Vector256.LoadUnsafe(ref buffer[chunkIndex]);
                    vSum = Avx2.Add(vSum, v);
                    chunkIndex += vectorSize;
                }

                // Horizontal sum of the vector
                sumDouble += vSum.GetElement(0) + vSum.GetElement(1) + vSum.GetElement(2) + vSum.GetElement(3);

                // Process remaining elements in chunk
                while (chunkIndex < currentChunkSize)
                {
                    sumDouble += buffer[chunkIndex];
                    chunkIndex++;
                }

                remaining -= currentChunkSize;
                offset += currentChunkSize;
            }

            return (decimal)(sumDouble / candles.Length);
        }

        /// <summary>
        /// Calculates the maximum High price using SIMD if available.
        /// </summary>
        public static decimal CalculateMaxHigh(this ReadOnlySpan<CandleData> candles)
        {
            if (candles.IsEmpty) return 0m;

            if (!Avx2.IsSupported || candles.Length < Vector256<double>.Count)
            {
                decimal max = decimal.MinValue;
                foreach (var candle in candles)
                {
                    if (candle.High > max) max = candle.High;
                }
                return max;
            }

            double maxDouble = double.MinValue;
            const int ChunkSize = 1024;
            Span<double> buffer = stackalloc double[ChunkSize];

            int remaining = candles.Length;
            int offset = 0;

            while (remaining > 0)
            {
                int currentChunkSize = Math.Min(remaining, ChunkSize);
                for (int j = 0; j < currentChunkSize; j++)
                {
                    buffer[j] = (double)candles[offset + j].High;
                }

                int chunkIndex = 0;
                var vMax = Vector256.Create(double.MinValue);
                int vectorSize = Vector256<double>.Count;

                while (chunkIndex <= currentChunkSize - vectorSize)
                {
                    var v = Vector256.LoadUnsafe(ref buffer[chunkIndex]);
                    vMax = Avx2.Max(vMax, v);
                    chunkIndex += vectorSize;
                }

                // Horizontal max
                double m0 = Math.Max(vMax.GetElement(0), vMax.GetElement(1));
                double m1 = Math.Max(vMax.GetElement(2), vMax.GetElement(3));
                double chunkMax = Math.Max(m0, m1);

                if (chunkMax > maxDouble) maxDouble = chunkMax;

                while (chunkIndex < currentChunkSize)
                {
                    if (buffer[chunkIndex] > maxDouble) maxDouble = buffer[chunkIndex];
                    chunkIndex++;
                }

                remaining -= currentChunkSize;
                offset += currentChunkSize;
            }

            return (decimal)maxDouble;
        }

        /// <summary>
        /// Calculates the minimum Low price using SIMD if available.
        /// </summary>
        public static decimal CalculateMinLow(this ReadOnlySpan<CandleData> candles)
        {
            if (candles.IsEmpty) return 0m;

            if (!Avx2.IsSupported || candles.Length < Vector256<double>.Count)
            {
                decimal min = decimal.MaxValue;
                foreach (var candle in candles)
                {
                    if (candle.Low < min) min = candle.Low;
                }
                return min;
            }

            double minDouble = double.MaxValue;
            const int ChunkSize = 1024;
            Span<double> buffer = stackalloc double[ChunkSize];

            int remaining = candles.Length;
            int offset = 0;

            while (remaining > 0)
            {
                int currentChunkSize = Math.Min(remaining, ChunkSize);
                for (int j = 0; j < currentChunkSize; j++)
                {
                    buffer[j] = (double)candles[offset + j].Low;
                }

                int chunkIndex = 0;
                var vMin = Vector256.Create(double.MaxValue);
                int vectorSize = Vector256<double>.Count;

                while (chunkIndex <= currentChunkSize - vectorSize)
                {
                    var v = Vector256.LoadUnsafe(ref buffer[chunkIndex]);
                    vMin = Avx2.Min(vMin, v);
                    chunkIndex += vectorSize;
                }

                double m0 = Math.Min(vMin.GetElement(0), vMin.GetElement(1));
                double m1 = Math.Min(vMin.GetElement(2), vMin.GetElement(3));
                double chunkMin = Math.Min(m0, m1);

                if (chunkMin < minDouble) minDouble = chunkMin;

                while (chunkIndex < currentChunkSize)
                {
                    if (buffer[chunkIndex] < minDouble) minDouble = buffer[chunkIndex];
                    chunkIndex++;
                }

                remaining -= currentChunkSize;
                offset += currentChunkSize;
            }

            return (decimal)minDouble;
        }
    }
}
