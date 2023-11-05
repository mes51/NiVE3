using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.Plugin.ValueObject
{
    public readonly struct Int32Point : IEquatable<Int32Point>
    {
        public readonly int X;

        public readonly int Y;

        public Int32Point(int x, int y)
        {
            X = x;
            Y = y;
        }

        public bool Equals(Int32Point other)
        {
            return X == other.X && Y == other.Y;
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is Int32Point p)
            {
                return Equals(p);
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(X);
            hashCode.Add(Y);
            return hashCode.ToHashCode();
        }

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
