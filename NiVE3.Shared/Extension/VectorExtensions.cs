using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.Shared.Extension
{
    public static class Vector3Extensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float HorizontalAdd(this in Vector3 v)
        {
            return v.X + v.Y + v.Z;
        }
    }

    public static class Vector4Extensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float HorizontalAdd(this in Vector4 v)
        {
            return v.AsVector128().HorizontalAdd().GetElement(0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float HorizontalMaxBy3Element(this in Vector4 v)
        {
            return v.AsVector128().HorizontalMaxBy3Element().GetElement(0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float HorizontalMinBy3Element(this in Vector4 v)
        {
            return v.AsVector128().HorizontalMinBy3Element().GetElement(0);
        }
    }

    public static class Vector128Extensions
    {
        static readonly Vector128<float> MaxValue = Vector128.Create(float.MaxValue);

        static readonly Vector128<float> MinValue = Vector128.Create(float.MinValue);

        const byte MmPermuteAAAA = 0x00;
        const byte MmPermuteAAAB = 0x01;
        const byte MmPermuteAADC = 0x0E;
        const byte MmInsert0To3 = 3 << 4;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<float> HorizontalAdd(this in Vector128<float> v)
        {
            var a = Sse3.HorizontalAdd(v, v);
            return Sse3.HorizontalAdd(a, a);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<float> HorizontalMaxBy3Element(this in Vector128<float> v)
        {
            var x = Sse41.Insert(v, MinValue, MmInsert0To3);
            var max1 = Avx.Permute(x, MmPermuteAADC);
            var max2 = Sse.Max(x, max1);
            var max3 = Avx.Permute(max2, MmPermuteAAAB);
            var max4 = Sse.Max(max2, max3);
            return Avx.Permute(max4, MmPermuteAAAA);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<float> HorizontalMinBy3Element(this in Vector128<float> v)
        {
            var x = Sse41.Insert(v, MaxValue, MmInsert0To3);
            var min1 = Avx.Permute(x, MmPermuteAADC);
            var min2 = Sse.Min(x, min1);
            var min3 = Avx.Permute(min2, MmPermuteAAAB);
            var min4 = Sse.Min(min2, min3);
            return Avx.Permute(min4, MmPermuteAAAA);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<float> Exp(this in Vector128<float> x)
        {
            // https://stackoverflow.com/a/47025627
            var l2e = Vector128.Create(1.44269504088896F);
            var cvt = Vector128.Create(12582912.0F);
            var c0 = Vector128.Create(0.238428936F);
            var c1 = Vector128.Create(0.703448006F);
            var c2 = Vector128.Create(1.000443142F);

            var t = Sse.Multiply(x, l2e);
            var e = Sse41.Floor(t);
            var i = Sse2.ConvertToVector128Int32(e);
            var f = Sse.Subtract(t, e);
            var j = Sse2.ShiftLeftLogical(i, 23);

            Vector128<float> p;
            if (Fma.IsSupported)
            {
                p = Fma.MultiplyAdd(c0, f, c1);
                p = Fma.MultiplyAdd(p, f, c2);
            }
            else
            {
                p = Sse.Multiply(c0, f);
                p = Sse.Add(p, c1);
                p = Sse.Multiply(p, f);
                p = Sse.Add(p, c2);
            }
            return Sse2.Add(j, p.AsInt32()).AsSingle();
        }

        // Memo: 他の方法 https://codingforspeed.com/using-faster-exponential-approximation/
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<float> Log(this in Vector128<float> v)
        {
            var aInt = v.AsInt32();
            var e = Sse2.Subtract(aInt, Vector128.Create(0x3F2AAAAB));
            e = Sse2.And(e, Vector128.Create(0xFF800000).AsInt32());

            var subtr = Sse2.Subtract(aInt, e);
            var m = subtr.AsSingle();

            var i = Sse.Multiply(Sse2.ConvertToVector128Single(e), Vector128.Create(1.19209290E-7F));
            var f = Sse.Subtract(m, Vector128.Create(1.0F));
            var s = Sse.Multiply(f, f);

            Vector128<float> r;

            if (Fma.IsSupported)
            {
                r = Fma.MultiplyAdd(Vector128.Create(0.230836749F), f, Vector128.Create(-0.279208571F));
                var t = Fma.MultiplyAdd(Vector128.Create(0.331826031F), f, Vector128.Create(-0.498910338F));

                r = Fma.MultiplyAdd(r, s, t);
                r = Fma.MultiplyAdd(r, s, t);
                r = Fma.MultiplyAdd(i, Vector128.Create(0.693147182F), r);
            }
            else
            {
                r = Sse.Add(Sse.Multiply(Vector128.Create(0.230836749F), f), Vector128.Create(-0.279208571F));
                var t = Sse.Add(Sse.Multiply(Vector128.Create(0.331826031F), f), Vector128.Create(-0.498910338F));

                r = Sse.Add(Sse.Multiply(r, s), t);
                r = Sse.Add(Sse.Multiply(r, s), t);
                r = Sse.Add(Sse.Multiply(i, Vector128.Create(0.693147182F)), r);
            }

            return r;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<float> Pow(this in Vector128<float> x, in Vector128<float> y)
        {
            return Sse.Multiply(y, x.Log()).Exp();
        }
    }

    public static class Vector256Extension
    {
        const byte MmInsert0To3 = 3 << 4;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<double> Abs(this in Vector256<double> v)
        {
            return Avx.AndNot(Vector256.Create(-0.0), v);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<double> HorizontalAdd(this in Vector256<double> v)
        {
            if (Avx2.IsSupported)
            {
                var a = Avx.HorizontalAdd(v, v);
                a = Avx2.Permute4x64(a, 0b00100010);
                return Avx.HorizontalAdd(a, a);
            }
            else
            {
                var rv = Vector256.Create(Avx.ExtractVector128(v, 1), Avx.ExtractVector128(v, 0));
                var a = Avx.HorizontalAdd(v, rv);
                return Avx.HorizontalAdd(a, a);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<double> DotProduct(this in Vector256<double> a, in Vector256<double> b)
        {
            var v = Avx.Multiply(a, b);
            if (Avx2.IsSupported)
            {
                v = Avx.HorizontalAdd(v, v);
                v = Avx2.Permute4x64(v, 0b00100010);
                return Avx.HorizontalAdd(v, v);
            }
            else
            {
                var rv = Vector256.Create(Avx.ExtractVector128(v, 1), Avx.ExtractVector128(v, 0));
                v = Avx.HorizontalAdd(v, rv);
                return Avx.HorizontalAdd(v, v);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<double> DotProduct(this in Vector256<double> a, in Vector256<double> b, int control)
        {
            var v = Avx.Multiply(a, b);
            var addMask = Vector256.Create(
                ((control & 0b00010000) != 0) ? 0xFFFFFFFFFFFFFFFFUL : 0,
                ((control & 0b00100000) != 0) ? 0xFFFFFFFFFFFFFFFFUL : 0,
                ((control & 0b01000000) != 0) ? 0xFFFFFFFFFFFFFFFFUL : 0,
                ((control & 0b10000000) != 0) ? 0xFFFFFFFFFFFFFFFFUL : 0
            ).AsDouble();
            v = Avx.And(v, addMask);

            Vector256<double> result;
            if (Avx2.IsSupported)
            {
                v = Avx.HorizontalAdd(v, v);
                v = Avx2.Permute4x64(v, 0b00100010);
                result = Avx.HorizontalAdd(v, v);
            }
            else
            {
                var rv = Vector256.Create(Avx.ExtractVector128(v, 1), Avx.ExtractVector128(v, 0));
                v = Avx.HorizontalAdd(v, rv);
                result = Avx.HorizontalAdd(v, v);
            }

            var resultMask = Vector256.Create(
                ((control & 0b0001) != 0) ? 0xFFFFFFFFFFFFFFFFUL : 0,
                ((control & 0b0010) != 0) ? 0xFFFFFFFFFFFFFFFFUL : 0,
                ((control & 0b0100) != 0) ? 0xFFFFFFFFFFFFFFFFUL : 0,
                ((control & 0b1000) != 0) ? 0xFFFFFFFFFFFFFFFFUL : 0
            ).AsDouble();
            return Avx.And(result, resultMask);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<double> Normalize(this in Vector256<double> v)
        {
            var length = Avx.Sqrt(v.DotProduct(v));
            return Avx.Divide(v, length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<double> Invert(this in Vector256<double> v)
        {
            return Avx.Subtract(Vector256<double>.Zero, v);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 AsVector4(this in Vector256<double> v)
        {
            return Avx.ConvertToVector128Single(v).AsVector4();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 AsVector3(this in Vector256<double> v)
        {
            return Avx.ConvertToVector128Single(v).AsVector3();
        }

        // https://geometrian.com/programming/tutorials/cross-product/index.php
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<double> CrossProduct(this in Vector256<double> a, in Vector256<double> b)
        {
            var resultMask = Vector256.Create(0xFFFFFFFFFFFFFFFFUL, 0xFFFFFFFFFFFFFFFFUL, 0xFFFFFFFFFFFFFFFFUL, 0).AsDouble();
            Vector256<double> result;
            if (Avx.IsSupported)
            {
                var tmp0 = Avx2.Permute4x64(a, 0b11001001);
                var tmp1 = Avx2.Permute4x64(b, 0b11010010);
                var tmp2 = Avx.Multiply(tmp0, b);
                var tmp3 = Avx.Multiply(tmp0, tmp1);
                var tmp4 = Avx2.Permute4x64(tmp2, 0b11001001);
                result = Avx.Subtract(tmp3, tmp4);
            }
            else
            {
                var aLow = Avx.ExtractVector128(a, 0);
                var aHigh = Avx.ExtractVector128(a, 1);
                var bLow = Avx.ExtractVector128(b, 0);
                var bHigh = Avx.ExtractVector128(b, 1);

                var tmp0 = Vector256.Create(Sse2.Shuffle(aHigh, aLow, 0b10), Sse2.Shuffle(aHigh, aLow, 0b01));
                var tmp1 = Vector256.Create(Sse2.Shuffle(bHigh, bLow, 0b11), Sse2.Shuffle(bLow, bHigh, 0b00));
                var tmp2 = Avx.Multiply(tmp0, b);
                var tmp3 = Avx.Multiply(tmp0, tmp1);

                var tmp2Low = Avx.ExtractVector128(tmp2, 0);
                var tmp2High = Avx.ExtractVector128(tmp2, 1);
                var tmp4 = Vector256.Create(Sse2.Shuffle(tmp2High, tmp2Low, 0b10), Sse2.Shuffle(tmp2High, tmp2Low, 0b01));
                result = Avx.Subtract(tmp3, tmp4);
            }

            return Avx.And(result, resultMask);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<double> Shuffle4x64(this in Vector256<double> a, in Vector256<double> b, int control)
        {
            var en1 = (control & 0b00000011);
            var en2 = (control & 0b00001100) >> 2;
            var en3 = (control & 0b00110000) >> 4;
            var en4 = (control & 0b11000000) >> 6;

            var aLow = Avx.ExtractVector128(a, 0);
            var aHigh = Avx.ExtractVector128(a, 1);
            var bLow = Avx.ExtractVector128(b, 0);
            var bHigh = Avx.ExtractVector128(b, 1);

            var lo1 = en1 > 1 ? aHigh : aLow;
            var lo2 = en2 > 1 ? aHigh : aLow;
            var hi1 = en3 > 1 ? bHigh : bLow;
            var hi2 = en4 > 1 ? bHigh : bLow;

            var low = (en1, en2) switch
            {
                (_, _) when ((en1 & 1) == 0 && ((en2 & 1) == 0)) => Sse2.Shuffle(lo1, lo2, 0b00),
                (_, _) when ((en1 & 1) != 0 && ((en2 & 1) == 0)) => Sse2.Shuffle(lo1, lo2, 0b01),
                (_, _) when ((en1 & 1) == 0 && ((en2 & 1) != 0)) => Sse2.Shuffle(lo1, lo2, 0b10),
                (_, _) when ((en1 & 1) != 0 && ((en2 & 1) != 0)) => Sse2.Shuffle(lo1, lo2, 0b11),
                _ => throw new InvalidOperationException()
            };
            var high = (en3, en4) switch
            {
                (_, _) when ((en3 & 1) == 0 && ((en4 & 1) == 0)) => Sse2.Shuffle(hi1, hi2, 0b00),
                (_, _) when ((en3 & 1) != 0 && ((en4 & 1) == 0)) => Sse2.Shuffle(hi1, hi2, 0b01),
                (_, _) when ((en3 & 1) == 0 && ((en4 & 1) != 0)) => Sse2.Shuffle(hi1, hi2, 0b10),
                (_, _) when ((en3 & 1) != 0 && ((en4 & 1) != 0)) => Sse2.Shuffle(hi1, hi2, 0b11),
                _ => throw new InvalidOperationException()
            };

            return Vector256.Create(low, high);
        }
    }
}
