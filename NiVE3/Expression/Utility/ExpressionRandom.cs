using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.Expression.Utility
{
    // SEE: https://prng.di.unimi.it/xoshiro256starstar.c
    class ExpressionRandom
    {
        ulong S1 = 0UL;
        ulong S2 = 0UL;
        ulong S3 = 0UL;
        ulong S4 = 0UL;

        double Time { get; }

        ulong ObjectIdSeed { get; }

        public ExpressionRandom(double time, Int128 objectId)
        {
            Time = time;
            ObjectIdSeed = (ulong)((objectId >> 64) & 0xFFFFFFFFFFFFFFFFUL) ^ (ulong)(objectId & 0xFFFFFFFFFFFFFFFFUL);
            setSeed(0);
        }

        #region Expression members
#pragma warning disable IDE1006 // NOTE: エクスプレッション用メソッドのため、命名規則は camelCase を許容する

        [ExpressionPublicMember]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double next()
        {
            var result = BitOperations.RotateLeft(S2 * 5, 7) * 9;
            var t = S2 << 17;

            S3 ^= S1;
            S4 ^= S2;
            S2 ^= S3;
            S1 ^= S4;

            S3 ^= t;
            S4 = BitOperations.RotateLeft(S4, 45);

            // SEE: https://prng.di.unimi.it/
            //      "Generating uniform doubles in the unit interval"
            return (result >> 11) * (1.0 / (1UL << 53));
        }


        [ExpressionPublicMember]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double next(double max)
        {
            return next(max, 0.0);
        }

        [ExpressionPublicMember]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double next(double max, double min)
        {
            if (max < min)
            {
                (min, max) = (max, min);
            }

            var range = max - min;
            return next() * range + min;
        }

        [ExpressionPublicMember]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void setSeed(double seed)
        {
            setSeed(seed, true, false);
        }

        [ExpressionPublicMember]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void setSeed(double seed, bool isChangeByTime)
        {
            setSeed(seed, isChangeByTime, false);
        }

        [ExpressionPublicMember]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void setSeed(double seed, bool isChangeByTime, bool isGlobalSeed)
        {
            if (isGlobalSeed)
            {
                GeneratePRNGParameters(BitConverter.DoubleToUInt64Bits(seed + (isChangeByTime ? Time : 0.0)));
            }
            else
            {
                GeneratePRNGParameters(BitConverter.DoubleToUInt64Bits(seed + (isChangeByTime ? Time : 0.0)) ^ ObjectIdSeed);
            }
        }

#pragma warning restore IDE1006 // 命名スタイル
        #endregion Expression members

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
