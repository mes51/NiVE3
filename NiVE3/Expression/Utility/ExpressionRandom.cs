using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.ValueObject;
using NiVE3.Shared.Util;
using static System.Windows.Forms.AxHost;

namespace NiVE3.Expression.Utility
{
    // SEE: https://prng.di.unimi.it/xoshiro256starstar.c
    class ExpressionRandom
    {
        double Time { get; }

        ulong ObjectIdSeed { get; }

        Xoroshiro Random { get; set; }

        ulong NoTimeChangedSeed { get; set; }

        public ExpressionRandom(Time time, Int128 objectId)
        {
            Time = (double)time;
            ObjectIdSeed = (ulong)((objectId >> 64) & 0xFFFFFFFFFFFFFFFFUL) ^ (ulong)(objectId & 0xFFFFFFFFFFFFFFFFUL);
            setSeed(0);
        }

        #region Expression members
#pragma warning disable IDE1006 // NOTE: エクスプレッション用メソッドのため、命名規則は camelCase を許容する

        [ExpressionPublicMember]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double next()
        {
            return Random.NextDouble();
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
            return Random.NextDouble() * range + min;
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
        [MemberNotNull(nameof(Random))]
        public void setSeed(double seed, bool isChangeByTime, bool isGlobalSeed)
        {
            if (isGlobalSeed)
            {
                NoTimeChangedSeed = BitConverter.DoubleToUInt64Bits(seed);
                Random = new Xoroshiro(BitConverter.DoubleToUInt64Bits(seed + (isChangeByTime ? Time : 0.0)));
            }
            else
            {
                NoTimeChangedSeed = BitConverter.DoubleToUInt64Bits(seed) ^ ObjectIdSeed;
                Random = new Xoroshiro(BitConverter.DoubleToUInt64Bits(seed + (isChangeByTime ? Time : 0.0)) ^ ObjectIdSeed);
            }
        }

        [ExpressionPublicMember]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double wiggle(double value, double amplitude, int octaves, double amplitudeMultiply, double t)
        {
            return Turbulence.Apply(value, amplitude, octaves, amplitudeMultiply, t, NoTimeChangedSeed);
        }

        [ExpressionPublicMember]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double[] wiggle(double[] value, double amplitude, int octaves, double amplitudeMultiply, double t)
        {
            return [..value.Select((v, i) => Turbulence.Apply(v, amplitude, octaves, amplitudeMultiply, t, NoTimeChangedSeed + (ulong)i))];
        }

#pragma warning restore IDE1006 // 命名スタイル
        #endregion Expression members
    }

    file static class Turbulence
    {
        const int MaxLoop = 1024;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Apply(double value, double amplitude, int octaves, double amplitudeMultiply, double t, ulong seed)
        {
            amplitudeMultiply = Math.Clamp(amplitudeMultiply, 0.0, 10.0);
            octaves = Math.Clamp(octaves, 1, 20);

            var noise = 0.0;
            var octaveMult = 1.0;
            for (var i = 0; i < octaves; i++, t *= 2.0, octaveMult *= amplitudeMultiply)
            {
                var t1RandomIndex = ((int)Math.Floor(t) % MaxLoop);
                var t0RandomIndex = (t1RandomIndex - 1) % MaxLoop;
                var t2RandomIndex = (t1RandomIndex + 1) % MaxLoop;
                var t3RandomIndex = (t2RandomIndex + 1) % MaxLoop;
                var diff = t - Math.Floor(t);

                var t0Value = (Pcg64.GenerateDouble(seed, t0RandomIndex) * 2.0) - 1.0;
                var t1Value = (Pcg64.GenerateDouble(seed, t1RandomIndex) * 2.0) - 1.0;
                var t2Value = (Pcg64.GenerateDouble(seed, t2RandomIndex) * 2.0) - 1.0;
                var t3Value = (Pcg64.GenerateDouble(seed, t3RandomIndex) * 2.0) - 1.0;

                noise += Interpolation.CatmullRom(t0Value, t1Value, t2Value, t3Value, 0.0, 1.0, diff) * octaveMult;
            }

            return noise * amplitude + value;
        }
    }

    // from: https://github.com/imneme/pcg-c/blob/master/src/pcg-global-64.c
    file static class Pcg64
    {
        static readonly UInt128 Multiplier = new UInt128(2549297995355413924UL, 4865540595714422341UL);

        static readonly UInt128 Increment = new UInt128(1UL, 0xDA3E39CB94B95BDBUL);

        public static ulong GenerateUInt64(UInt128 seed, int skip)
        {
            for (var i = 0; i < skip; i++)
            {
                unchecked
                {
                    seed = seed * Multiplier + Increment;
                }
            }

            return BitOperations.RotateRight((ulong)(seed >> 64) ^ (ulong)seed, (int)(seed >> 122));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double GenerateDouble(UInt128 seed, int skip)
        {
            // SEE: https://prng.di.unimi.it/
            //      "Generating uniform doubles in the unit interval"
            return (GenerateUInt64(seed, skip) >> 11) / (double)(1UL << 53);
        }
    }
}
