using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.ValueObject;
using NiVE3.Shared.Util;

namespace NiVE3.Expression.Utility
{
    // SEE: https://prng.di.unimi.it/xoshiro256starstar.c
    class ExpressionRandom
    {
        double Time { get; }

        ulong ObjectIdSeed { get; }

        Xoroshiro Random { get; set; }

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
                Random = new Xoroshiro(BitConverter.DoubleToUInt64Bits(seed + (isChangeByTime ? Time : 0.0)));
            }
            else
            {
                Random = new Xoroshiro(BitConverter.DoubleToUInt64Bits(seed + (isChangeByTime ? Time : 0.0)) ^ ObjectIdSeed);
            }
        }

#pragma warning restore IDE1006 // 命名スタイル
        #endregion Expression members
    }
}
