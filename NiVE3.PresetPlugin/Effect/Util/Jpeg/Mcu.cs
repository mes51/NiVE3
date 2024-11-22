using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.PresetPlugin.Effect.Util.Jpeg
{
    [StructLayout(LayoutKind.Explicit)]
    struct Mcu
    {
        public const int LineSize = 8;

        public const int BlockSize = LineSize * LineSize;

        [FieldOffset(0)]
        public Vector256<int> Row1;

        [FieldOffset(32)]
        public Vector256<int> Row2;

        [FieldOffset(32 * 2)]
        public Vector256<int> Row3;

        [FieldOffset(32 * 3)]
        public Vector256<int> Row4;

        [FieldOffset(32 * 4)]
        public Vector256<int> Row5;

        [FieldOffset(32 * 5)]
        public Vector256<int> Row6;

        [FieldOffset(32 * 6)]
        public Vector256<int> Row7;

        [FieldOffset(32 * 7)]
        public Vector256<int> Row8;

        [FieldOffset(0)]
        public int DCElement;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReOrderZigZag(ref Mcu mcu)
        {
            if (Avx2.IsSupported)
            {
                ReOrderZigZagAvx2(ref mcu);
            }
            else
            {
                ReOrderZigZagSequential(ref mcu);
            }
        }

        // from: https://github.com/WojciechMula/toys/tree/master/avx512-jpeg-zizag
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void ReOrderZigZagAvx2(ref Mcu mcu)
        {
            var a0Shuffle = Vector256.Create(0, 1, -1, -1, -1, 2, 3, -1);
            var a0 = Avx2.PermuteVar8x32(mcu.Row1, a0Shuffle) & Vector256.Create(-1, -1, 0, 0, 0, -1, -1, 0);
            var b0Shuffle = Vector256.Create(-1, -1, 0, -1, 1, -1, -1, 2);
            var b0 = Avx2.PermuteVar8x32(mcu.Row2, b0Shuffle) & Vector256.Create(0, 0, -1, 0, -1, 0, 0, -1);
            var row1 = a0 | b0;
            var c0Shuffle = Vector256.Create(-1, -1, -1, 0, -1, -1, -1, -1);
            var c0 = Avx2.PermuteVar8x32(mcu.Row3, c0Shuffle) & Vector256.Create(0, 0, 0, -1, 0, 0, 0, 0);
            row1 |= c0;

            var a1Shuffle = Vector256.Create(-1, -1, -1, -1, -1, -1, 4, 5);
            var a1 = Avx2.PermuteVar8x32(mcu.Row1, a1Shuffle) & Vector256.Create(0, 0, 0, 0, 0, 0, -1, -1);
            var b1Shuffle = Vector256.Create(-1, -1, -1, -1, -1, 3, -1, -1);
            var b1 = Avx2.PermuteVar8x32(mcu.Row2, b1Shuffle) & Vector256.Create(0, 0, 0, 0, 0, -1, 0, 0);
            var row2 = a1 | b1;
            var c1Shuffle = Vector256.Create(1, -1, -1, -1, 2, -1, -1, -1);
            var c1 = Avx2.PermuteVar8x32(mcu.Row3, c1Shuffle) & Vector256.Create(-1, 0, 0, 0, -1, 0, 0, 0);
            row2 |= c1;
            var d1Shuffle = Vector256.Create(-1, 0, -1, 1, -1, -1, -1, -1);
            var d1 = Avx2.PermuteVar8x32(mcu.Row4, d1Shuffle) & Vector256.Create(0, -1, 0, -1, 0, 0, 0, 0);
            row2 |= d1;
            var e1Shuffle = Vector256.Create(-1, -1, 0, -1, -1, -1, -1, -1);
            var e1 = Avx2.PermuteVar8x32(mcu.Row5, e1Shuffle) & Vector256.Create(0, 0, -1, 0, 0, 0, 0, 0);
            row2 |= e1;

            var b2Shuffle = Vector256.Create(4, -1, -1, -1, -1, -1, -1, -1);
            var b2 = Avx2.PermuteVar8x32(mcu.Row2, b2Shuffle) & Vector256.Create(-1, 0, 0, 0, 0, 0, 0, 0);
            var c2Shuffle = Vector256.Create(-1, 3, -1, -1, -1, -1, -1, -1);
            var c2 = Avx2.PermuteVar8x32(mcu.Row3, c2Shuffle) & Vector256.Create(0, -1, 0, 0, 0, 0, 0, 0);
            var row3 = b2 | c2;
            var d2Shuffle = Vector256.Create(-1, -1, 2, -1, -1, -1, -1, -1);
            var d2 = Avx2.PermuteVar8x32(mcu.Row4, d2Shuffle) & Vector256.Create(0, 0, -1, 0, 0, 0, 0, 0);
            row3 |= d2;
            var e2Shuffle = Vector256.Create(-1, -1, -1, 1, -1, -1, -1, 2);
            var e2 = Avx2.PermuteVar8x32(mcu.Row5, e2Shuffle) & Vector256.Create(0, 0, 0, -1, 0, 0, 0, -1);
            row3 |= e2;
            var f2Shuffle = Vector256.Create(-1, -1, -1, -1, 0, -1, 1, -1);
            var f2 = Avx2.PermuteVar8x32(mcu.Row6, f2Shuffle) & Vector256.Create(0, 0, 0, 0, -1, 0, -1, 0);
            row3 |= f2;
            var g2Shuffle = Vector256.Create(-1, -1, -1, -1, -1, 0, -1, -1);
            var g2 = Avx2.PermuteVar8x32(mcu.Row7, g2Shuffle) & Vector256.Create(0, 0, 0, 0, 0, -1, 0, 0);
            row3 |= g2;

            var a3Shuffle = Vector256.Create(-1, -1, -1, 6, 7, -1, -1, -1);
            var a3 = Avx2.PermuteVar8x32(mcu.Row1, a3Shuffle) & Vector256.Create(0, 0, 0, -1, -1, 0, 0, 0);
            var b3Shuffle = Vector256.Create(-1, -1, 5, -1, -1, 6, -1, -1);
            var b3 = Avx2.PermuteVar8x32(mcu.Row2, b3Shuffle) & Vector256.Create(0, 0, -1, 0, 0, -1, 0, 0);
            var row4 = a3 | b3;
            var c3Shuffle = Vector256.Create(-1, 4, -1, -1, -1, -1, 5, -1);
            var c3 = Avx2.PermuteVar8x32(mcu.Row3, c3Shuffle) & Vector256.Create(0, -1, 0, 0, 0, 0, -1, 0);
            row4 |= c3;
            var d3Shuffle = Vector256.Create(3, -1, -1, -1, -1, -1, -1, 4);
            var d3 = Avx2.PermuteVar8x32(mcu.Row4, d3Shuffle) & Vector256.Create(-1, 0, 0, 0, 0, 0, 0, -1);
            row4 |= d3;

            var e4Shuffle = Vector256.Create(3, -1, -1, -1, -1, -1, -1, 4);
            var e4 = Avx2.PermuteVar8x32(mcu.Row5, e4Shuffle) & Vector256.Create(-1, 0, 0, 0, 0, 0, 0, -1);
            var f4Shuffle = Vector256.Create(-1, 2, -1, -1, -1, -1, 3, -1);
            var f4 = Avx2.PermuteVar8x32(mcu.Row6, f4Shuffle) & Vector256.Create(0, -1, 0, 0, 0, 0, -1, 0);
            var row5 = e4 | f4;
            var g4Shuffle = Vector256.Create(-1, -1, 1, -1, -1, 2, -1, -1);
            var g4 = Avx2.PermuteVar8x32(mcu.Row7, g4Shuffle) & Vector256.Create(0, 0, -1, 0, 0, -1, 0, 0);
            row5 |= g4;
            var h4Shuffle = Vector256.Create(-1, -1, -1, 0, 1, -1, -1, -1);
            var h4 = Avx2.PermuteVar8x32(mcu.Row8, h4Shuffle) & Vector256.Create(0, 0, 0, -1, -1, 0, 0, 0);
            row5 |= h4;

            var b5Shuffle = Vector256.Create(-1, -1, 7, -1, -1, -1, -1, -1);
            var b5 = Avx2.PermuteVar8x32(mcu.Row2, b5Shuffle) & Vector256.Create(0, 0, -1, 0, 0, 0, 0, 0);
            var c5Shuffle = Vector256.Create(-1, 6, -1, 7, -1, -1, -1, -1);
            var c5 = Avx2.PermuteVar8x32(mcu.Row3, c5Shuffle) & Vector256.Create(0, -1, 0, -1, 0, 0, 0, 0);
            var row6 = b5 | c5;
            var d5Shuffle = Vector256.Create(5, -1, -1, -1, 6, -1, -1, -1);
            var d5 = Avx2.PermuteVar8x32(mcu.Row4, d5Shuffle) & Vector256.Create(-1, 0, 0, 0, -1, 0, 0, 0);
            row6 |= d5;
            var e5Shuffle = Vector256.Create(-1, -1, -1, -1, -1, 5, -1, -1);
            var e5 = Avx2.PermuteVar8x32(mcu.Row5, e5Shuffle) & Vector256.Create(0, 0, 0, 0, 0, -1, 0, 0);
            row6 |= e5;
            var f5Shuffle = Vector256.Create(-1, -1, -1, -1, -1, -1, 4, -1);
            var f5 = Avx2.PermuteVar8x32(mcu.Row6, f5Shuffle) & Vector256.Create(0, 0, 0, 0, 0, 0, -1, 0);
            row6 |= f5;
            var g5Shuffle = Vector256.Create(-1, -1, -1, -1, -1, -1, -1, 3);
            var g5 = Avx2.PermuteVar8x32(mcu.Row7, g5Shuffle) & Vector256.Create(0, 0, 0, 0, 0, 0, 0, -1);
            row6 |= g5;

            var d6Shuffle = Vector256.Create(-1, -1, -1, -1, -1, 7, -1, -1);
            var d6 = Avx2.PermuteVar8x32(mcu.Row4, d6Shuffle) & Vector256.Create(0, 0, 0, 0, 0, -1, 0, 0);
            var e6Shuffle = Vector256.Create(-1, -1, -1, -1, 6, -1, 7, -1);
            var e6 = Avx2.PermuteVar8x32(mcu.Row5, e6Shuffle) & Vector256.Create(0, 0, 0, 0, -1, 0, -1, 0);
            var row7 = d6 | e6;
            var f6Shuffle = Vector256.Create(-1, -1, -1, 5, -1, -1, -1, 6);
            var f6 = Avx2.PermuteVar8x32(mcu.Row6, f6Shuffle) & Vector256.Create(0, 0, 0, -1, 0, 0, 0, -1);
            row7 |= f6;
            var g6Shuffle = Vector256.Create(-1, -1, 4, -1, -1, -1, -1, -1);
            var g6 = Avx2.PermuteVar8x32(mcu.Row7, g6Shuffle) & Vector256.Create(0, 0, -1, 0, 0, 0, 0, 0);
            row7 |= g6;
            var h6Shuffle = Vector256.Create(2, 3, -1, -1, -1, -1, -1, -1);
            var h6 = Avx2.PermuteVar8x32(mcu.Row8, h6Shuffle) & Vector256.Create(-1, -1, 0, 0, 0, 0, 0, 0);
            row7 |= h6;

            var f7Shuffle = Vector256.Create(-1, -1, -1, -1, 7, -1, -1, -1);
            var f7 = Avx2.PermuteVar8x32(mcu.Row6, f7Shuffle) & Vector256.Create(0, 0, 0, 0, -1, 0, 0, 0);
            var g7Shuffle = Vector256.Create(5, -1, -1, 6, -1, 7, -1, -1);
            var g7 = Avx2.PermuteVar8x32(mcu.Row7, g7Shuffle) & Vector256.Create(-1, 0, 0, -1, 0, -1, 0, 0);
            var row8 = f7 | g7;
            var h7Shuffle = Vector256.Create(-1, 4, 5, -1, -1, -1, 6, 7);
            var h7 = Avx2.PermuteVar8x32(mcu.Row8, h7Shuffle) & Vector256.Create(0, -1, -1, 0, 0, 0, -1, -1);
            row8 |= h7;

            mcu.Row1 = row1;
            mcu.Row2 = row2;
            mcu.Row3 = row3;
            mcu.Row4 = row4;
            mcu.Row5 = row5;
            mcu.Row6 = row6;
            mcu.Row7 = row7;
            mcu.Row8 = row8;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void ReOrderZigZagSequential(ref Mcu mcu)
        {
            ReadOnlySpan<int> ZigZag =
            [
                0, 1, 8, 16, 9, 2, 3, 10,
                17, 24, 32, 25, 18, 11, 4, 5,
                12, 19, 26, 33, 40, 48, 41, 34,
                27, 20, 13, 6, 7, 14, 21, 28,
                35, 42, 49, 56, 57, 50, 43, 36,
                29, 22, 15, 23, 30, 37, 44, 51,
                58, 59, 52, 45, 38, 31, 39, 46,
                53, 60, 61, 54, 47, 55, 62, 63
            ];
            Span<int> tmp = stackalloc int[ZigZag.Length];

            for (var i = 0; i < ZigZag.Length; i++)
            {
                tmp[i] = Unsafe.Add(ref mcu.DCElement, ZigZag[i]);
            }

            var tmpVector = MemoryMarshal.Cast<int, Vector256<int>>(tmp);
            mcu.Row1 = tmpVector[0];
            mcu.Row2 = tmpVector[1];
            mcu.Row3 = tmpVector[2];
            mcu.Row4 = tmpVector[3];
            mcu.Row5 = tmpVector[4];
            mcu.Row6 = tmpVector[5];
            mcu.Row7 = tmpVector[6];
            mcu.Row8 = tmpVector[7];
        }
    }
}
