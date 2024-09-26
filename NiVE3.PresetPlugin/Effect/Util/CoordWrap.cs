using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using ComputeSharp;
using NiVE3.Shared.Extension;

namespace NiVE3.PresetPlugin.Effect.Util
{
    static class CoordWrap
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Mirror<T>(T x, T size) where T : INumber<T>
        {
            var two = (T.One + T.One);

            var ls = size - T.One;
            var a = T.Abs(x);
            var b = a % (ls * two);
            return b - T.Max(b - ls, T.Zero) * two;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Repeat<T>(T x, T size) where T : INumber<T>
        {
            return (((x % size) + size) % size);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Wrap<T>(T x, T size) where T : INumber<T>
        {
            return T.Clamp(x, T.Zero, size - T.One);
        }
    }

    static class CoordWrapGpu
    {
        public static int Mirror(int x, int size)
        {
            var ls = size - 1;
            var a = Hlsl.Abs(x);
            var b = a % (ls * 2);
            return b - Hlsl.Max(b - ls, 0) * 2;
        }

        public static float Mirror(float x, float size)
        {
            var ls = size - 1.0F;
            var a = Hlsl.Abs(x);
            var b = a % (ls * 2.0F);
            return b - Hlsl.Max(b - ls, 0) * 2.0F;
        }

        public static int Repeat(int x, int size)
        {
            return (((x % size) + size) % size);
        }

        public static float Repeat(float x, float size)
        {
            return (((x % size) + size) % size);
        }

        public static int Wrap(int x, int size)
        {
            return Hlsl.Clamp(x, 0, size - 1);
        }

        public static float Wrap(float x, float size)
        {
            return Hlsl.Clamp(x, 0.0F, size - 1.0F);
        }
    }
}
