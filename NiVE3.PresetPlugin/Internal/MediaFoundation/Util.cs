using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Vortice.MediaFoundation;

namespace NiVE3.PresetPlugin.Internal.MediaFoundation
{
    static class Util
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (int, int) GetDoubleInt32(IMFAttributes attributes, Guid guidKey)
        {
            var result = attributes.GetUInt64(guidKey);
            return SplitDoubleInt32(result);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetDoubleInt32(IMFAttributes attributes, Guid guidKey, int value1, int value2)
        {
            attributes.Set(guidKey, unchecked(((ulong)value1 << 32) | (uint)value2));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetDoubleUInt32(IMFAttributes attributes, Guid guidKey, uint value1, uint value2)
        {
            attributes.Set(guidKey, (ulong)(value1 << 32) | value2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int MulDiv(int number, int numerator, int denominator)
        {
            return unchecked((int)(number * (long)numerator / denominator));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (int, int) SplitDoubleInt32(ulong value)
        {
            return (
                unchecked((int)((value >> 32) & 0xFFFFFFFF)),
                unchecked((int)(value & 0xFFFFFFFF))
            );
        }
    }
}
