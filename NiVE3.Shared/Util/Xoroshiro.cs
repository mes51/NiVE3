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
    public struct Xoroshiro
    {
        ulong S1;

        ulong S2;

        public Xoroshiro()
        {
            S1 = unchecked((ulong)Random.Shared.NextInt64());
            S2 = unchecked((ulong)Random.Shared.NextInt64());
        }

        public Xoroshiro(ulong seed1, ulong seed2)
        {
            S1 = seed1;
            S2 = seed2;
        }

        public Xoroshiro(int seed)
        {
            var seedRand = new Random(seed);
            S1 = (ulong)seedRand.NextInt64();
            S2 = (ulong)seedRand.NextInt64();
        }

        public ulong NextUInt64()
        {
            var s0 = S1;
            var s1 = S2;
            var result = BitOperations.RotateLeft(s0 * 5, 7) * 9;

            s1 ^= s0;
            S1 = BitOperations.RotateLeft(s0, 24) ^ s1 ^ (s1 << 16);
            S2 = BitOperations.RotateLeft(s1, 37);

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
            return (NextUInt64() >> 11) / (double)(1U << 53);
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
    }
}
