using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.Plugin.ValueObject
{
    /// <summary>
    /// 整数の2次元空間上のサイズ
    /// </summary>
    /// <param name="Width">幅</param>
    /// <param name="Height">高さ</param>
    public readonly record struct Int32Size(int Width, int Height)
    {
        public override string ToString()
        {
            return $"< Width = {Width}, Height = {Height} >";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int32Size operator +(Int32Size a)
        {
            return a;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int32Size operator +(Int32Size a, Int32Size b)
        {
            return new Int32Size(a.Width + b.Width, a.Height + b.Height);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int32Size operator -(Int32Size a)
        {
            return new Int32Size(-a.Width, -a.Height);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int32Size operator -(Int32Size a, Int32Size b)
        {
            return new Int32Size(a.Width - b.Width, a.Height - b.Height);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int32Size operator *(Int32Size a, int s)
        {
            return new Int32Size(a.Width * s, a.Height * s);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int32Size  operator *(int s, Int32Size a)
        {
            return new Int32Size    (a.Width * s, a.Height * s);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int32Size operator *(Int32Size a, Int32Size b)
        {
            return new Int32Size(a.Width * b.Width, a.Height * b.Height);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int32Size operator /(Int32Size a, int s)
        {
            return new Int32Size(a.Width / s, a.Height / s);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int32Size operator /(int s, Int32Size a)
        {
            return new Int32Size(s / a.Width, s / a.Height);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int32Size operator /(Int32Size a, Int32Size b)
        {
            return new Int32Size(a.Width / b.Width, a.Height / b.Height);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int32Size operator %(Int32Size a, int s)
        {
            return new Int32Size(a.Width % s, a.Height % s);
        }
    }
}
