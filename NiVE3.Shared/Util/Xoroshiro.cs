using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.Shared.Util
{
    // https://prng.di.unimi.it/
    public class Xoroshiro
    {
        ulong S1 = 0UL;
        ulong S2 = 0UL;
        ulong S3 = 0UL;
        ulong S4 = 0UL;

        public Xoroshiro()
        {
            GeneratePRNGParameters(unchecked((ulong)(Random.Shared.NextInt64())));
        }

        public Xoroshiro(long seed)
        {
            GeneratePRNGParameters(unchecked((ulong)seed));
        }

        public Xoroshiro(ulong seed)
        {
            GeneratePRNGParameters(seed);
        }

        public ulong NextUInt64()
        {
            var result = BitOperations.RotateLeft(S2 * 5, 7) * 9;
            var t = S2 << 17;

            S3 ^= S1;
            S4 ^= S2;
            S2 ^= S3;
            S1 ^= S4;

            S3 ^= t;
            S4 = BitOperations.RotateLeft(S4, 45);

            return result;
        }

        public uint NextUInt()
        {
            return unchecked((uint)NextUInt64());
        }

        public int Next()
        {
            return (int)(NextUInt64() >> 33);
        }

        public int Next(int maxValue)
        {
            if (maxValue < 0)
            {
                throw new ArgumentException(null, nameof(maxValue));
            }

            return (int)(NextUInt64() % (ulong)maxValue);
        }

        public int Next(int maxValue, int minValue)
        {
            if (minValue >= maxValue)
            {
                throw new ArgumentException(null, nameof(minValue));
            }

            return (int)(NextUInt64() % (ulong)(maxValue - minValue)) + minValue;
        }

        public long NextInt64()
        {
            return (long)(NextUInt64() >> 1);
        }

        public long NextInt64(long maxValue)
        {
            if (maxValue < 0)
            {
                throw new ArgumentException(null, nameof(maxValue));
            }

            return (long)(NextUInt64() % (ulong)maxValue);
        }

        public long NextInt64(long maxValue, long minValue)
        {
            if (minValue >= maxValue)
            {
                throw new ArgumentException(null, nameof(minValue));
            }

            return (long)(NextUInt64() % (ulong)(maxValue - minValue)) + minValue;
        }

        public double NextDouble()
        {
            // SEE: https://prng.di.unimi.it/
            //      "Generating uniform doubles in the unit interval"
            return (NextUInt64() >> 11) / (double)(1UL << 53);
        }

        public float NextSingle()
        {
            return (NextUInt64() >> 40) / (float)(1U << 24);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Shuffle<T>(T[] array)
        {
            Shuffle(array.AsSpan());
        }

        public void Shuffle<T>(Span<T> span)
        {
            for (int i = 0, limit = span.Length - 1; i < limit; i++)
            {
                var j = Next(span.Length, i);
                if (i != j)
                {
                    (span[j], span[i]) = (span[i], span[j]);
                }
            }
        }

        // SEE: https://xoshiro.di.unimi.it/splitmix64.c
        void GeneratePRNGParameters(ulong seed)
        {
            var z = (seed += 0x9E3779B97F4A7C15UL);
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            S1 = z ^ (z >> 31);

            z = (seed += 0x9E3779B97F4A7C15UL);
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            S2 = z ^ (z >> 31);

            z = (seed += 0x9E3779B97F4A7C15UL);
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            S3 = z ^ (z >> 31);

            z = (seed += 0x9E3779B97F4A7C15UL);
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            S4 = z ^ (z >> 31);
        }
    }
}
