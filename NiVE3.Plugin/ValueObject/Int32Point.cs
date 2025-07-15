using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.Plugin.ValueObject
{
    /// <summary>
    /// 整数の2次元空間上の座標
    /// </summary>
    /// <param name="X">X座標</param>
    /// <param name="Y">Y座標</param>
    public readonly record struct Int32Point(int X, int Y)
    {
        public static readonly Int32Point Zero = new Int32Point(0, 0);

        public override string ToString()
        {
            return $"< X = {X}, Y = {Y} >";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int32Point operator +(Int32Point a)
        {
            return a;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int32Point operator +(Int32Point a, Int32Point b)
        {
            return new Int32Point(a.X + b.X, a.Y + b.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int32Point operator -(Int32Point a)
        {
            return new Int32Point(-a.X, -a.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int32Point operator -(Int32Point a, Int32Point b)
        {
            return new Int32Point(a.X - b.X, a.Y - b.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int32Point operator *(Int32Point a, int s)
        {
            return new Int32Point(a.X * s, a.Y * s);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int32Point operator *(int s, Int32Point a)
        {
            return new Int32Point(a.X * s, a.Y * s);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int32Point operator *(Int32Point a, Int32Point b)
        {
            return new Int32Point(a.X * b.X, a.Y * b.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int32Point operator /(Int32Point a, int s)
        {
            return new Int32Point(a.X / s, a.Y / s);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int32Point operator /(int s, Int32Point a)
        {
            return new Int32Point(s / a.X, s / a.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int32Point operator /(Int32Point a, Int32Point b)
        {
            return new Int32Point(a.X / b.X, a.Y / b.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int32Point operator %(Int32Point a, int s)
        {
            return new Int32Point(a.X % s, a.Y % s);
        }
    }
}
