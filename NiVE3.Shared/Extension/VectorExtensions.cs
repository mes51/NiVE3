using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace NiVE3.Shared.Extension
{
    public static class Vector3Extensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float HorizontalAdd(this in Vector3 v)
        {
            return v.X + v.Y + v.Z;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 AsVector2(this in Vector3 v)
        {
            return v.AsVector128().AsVector2();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 AsVector4(this in Vector3 v)
        {
            return v.AsVector128().AsVector4();
        }
    }

    public static class Vector4Extensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float HorizontalAdd(this in Vector4 v)
        {
            return Vector128.Sum(v.AsVector128());
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CompareGreaterThanBy3Element(this in Vector4 a, in Vector3 b)
        {
            return !Avx.TestZ(Sse.CompareGreaterThan(a.AsVector128().AsVector3().AsVector128(), b.AsVector128()), Vector128.Create(float.NaN));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 AsVector2(this in Vector4 v)
        {
            return v.AsVector128().AsVector2();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 AsVector3(this in Vector4 v)
        {
            return v.AsVector128().AsVector3();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Color ToColor(this in Vector4 v)
        {
            var converted = Vector4.Clamp(v, Vector4.Zero, Vector4.One) * 255.0F;
            return Color.FromArgb((byte)converted.W, (byte)converted.Z, (byte)converted.Y, (byte)converted.X);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToIntColor(this in Vector4 v)
        {
            var p32 = Sse2.ConvertToVector128Int32(Vector4.Clamp(v, Vector4.Zero, Vector4.One).AsVector128() * 255.0F);
            var p16 = Sse2.PackSignedSaturate(p32, Vector128<int>.Zero);
            var p8 = Sse2.PackUnsignedSaturate(p16, Vector128<short>.Zero);
            return Sse2.ConvertToInt32(p8.AsInt32());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Mod(this in Vector4 a, float b)
        {
            return a.AsVector128().Mod(b).AsVector4();
        }
    }

    public static class Vector64Extensions
    {
        public static Vector2 AsVector2(this Vector64<float> v)
        {
            return Unsafe.As<Vector64<float>, Vector2>(ref v);
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
        public static Vector128<float> Not(this in Vector128<float> v)
        {
            return Vector128.Xor(v, Vector128<float>.AllBitsSet);
        }

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

        // https://stackoverflow.com/a/76302863
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<float> Exp(this in Vector128<float> x)
        {
            var xd = Avx.ConvertToVector256Double(x) * Vector256.Create(1.442695040888963387);
            var a = Vector256.Create(0.000217549227054);
            var b = Vector256.Create(0.00124218531444);
            var c = Vector256.Create(0.00968102455999);
            var d = Vector256.Create(0.0554821818101);
            var e = Vector256.Create(0.240230073528);
            var f = Vector256.Create(0.693146979806);
            var fx = Vector256.Floor(xd);
            var X = xd - fx;

            var result = Vector256<double>.Zero;
            if (Fma.IsSupported)
            {
                var y = Fma.MultiplyAdd(a, X, b);
                y = Fma.MultiplyAdd(y, X, c);
                y = Fma.MultiplyAdd(y, X, d);
                y = Fma.MultiplyAdd(y, X, e);
                y = Fma.MultiplyAdd(y, X, f);
                result = Fma.MultiplyAdd(y, X, Vector256<double>.One);
            }
            else
            {
                var y = a * X + b;
                y = y * X + c;
                y = y * X + d;
                y = y * X + e;
                y = y * X + f;
                result = y * X + Vector256<double>.One;
            }

            if (Avx512F.VL.IsSupported)
            {
                result = Avx512F.VL.Scale(result, fx);
            }
            else if (Avx2.IsSupported)
            {
                var scale = Avx.ConvertToVector256Double(
                    Avx2.ShiftLeftLogicalVariable(Vector128<int>.One,
                    Avx.ConvertToVector128Int32(Vector256.Abs(fx)).AsUInt32())
                );
                var divMask = Avx.CompareLessThan(fx, Vector256<double>.Zero);

                result = Avx.Or(
                    Avx.And(result * scale, divMask.Not()),
                    Avx.And(result / scale, divMask)
                );
            }
            else
            {
                var count = Avx.ConvertToVector128Int32(Vector256.Abs(fx));
                var countHigh = Sse2.UnpackHigh(count, count);
                var shiftLow = Sse2.ShiftLeftLogical(Vector128<long>.One, Sse41.ConvertToVector128Int64(count)).AsInt32();
                var shiftHigh = Sse2.ShiftLeftLogical(Vector128<long>.One, Sse41.ConvertToVector128Int64(countHigh)).AsInt32();
                var scale = Avx.ConvertToVector256Double(
                    Sse.Shuffle(shiftLow.AsSingle(), shiftHigh.AsSingle(), 0b10001000).AsInt32()
                );
                var divMask = Avx.CompareLessThan(fx, Vector256<double>.Zero);

                result = Avx.Or(
                    Avx.And(result * scale, divMask.Not()),
                    Avx.And(result / scale, divMask)
                );
            }

            return Avx.ConvertToVector128Single(result);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<float> Log(this in Vector128<float> v)
        {
            return Vector128.Log(v);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<float> Pow(this in Vector128<float> x, in Vector128<float> y)
        {
            return Sse.And(Sse.Multiply(y, x.Log()).Exp(), Sse.CompareNotEqual(x, Vector128<float>.Zero));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<float> Cbrt(this in Vector128<float> x)
        {
            return x.Pow(Vector128.Create(1.0F / 3.0F));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<float> SignWithoutZero(this in Vector128<float> v)
        {
            var gteMask = Vector128.GreaterThanOrEqual(v, Vector128<float>.Zero);
            var ltMask = Vector128.LessThan(v, Vector128<float>.Zero);

            return (Vector128<float>.One & gteMask) | (Vector128.Create(-1.0F) & ltMask);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double CrossProduct(this in Vector128<double> x, in Vector128<double> y)
        {
            var v = Avx.Permute(x, 0b01) * y;
            return -Sse3.HorizontalSubtract(v, v).GetElement(0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double LengthSquared(this in Vector128<double> v)
        {
            return Vector128.Dot(v, v);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<double> Normalize(this in Vector128<double> v)
        {
            var length = Math.Sqrt(Vector128.Dot(v, v));
            return v / length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<float> Mod(this in Vector128<float> a, float b)
        {
            return a - Sse41.RoundToZero(a / b) * b;
        }
    }

    public static class Vector256Extension
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<double> Not(this in Vector256<double> v)
        {
            return Vector256.Xor(v, Vector256<double>.AllBitsSet);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<double> DotProduct(this in Vector256<double> a, in Vector256<double> b)
        {
            var v = a * b;
            v = Avx.HorizontalAdd(v, v);
            var r = Avx.Permute2x128(v, v, 1);
            return v + r;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double LengthSquared(this in Vector256<double> v)
        {
            return Vector256.Dot(v, v);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Length(this in Vector256<double> v)
        {
            return Math.Sqrt(Vector256.Dot(v, v));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<double> Normalize(this in Vector256<double> v)
        {
            var length = Math.Sqrt(Vector256.Dot(v, v));
            return v / length;
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
            var tmp0 = a.Permute4x64(0b11001001);
            var tmp1 = b.Permute4x64(0b11010010);
            var tmp2 = tmp0 * b;
            var tmp3 = tmp0 * tmp1;
            var tmp4 = tmp2.Permute4x64(0b11001001);
            var result = tmp3 - tmp4;

            return result & resultMask;
        }

        public static Vector256<double> CrossProduct3D(this in Vector256<double> a, in Vector256<double> b)
        {
            var mask = Vector256.Create(0xFFFFFFFFFFFFFFFFUL, 0xFFFFFFFFFFFFFFFFUL, 0xFFFFFFFFFFFFFFFFUL, 0).AsDouble();
            return (a & mask).CrossProduct(b & mask);
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

            var low = (en1 & 1, en2 & 1) switch
            {
                (0, 0) => Sse2.Shuffle(lo1, lo2, 0b00),
                (1, 0) => Sse2.Shuffle(lo1, lo2, 0b01),
                (0, 1) => Sse2.Shuffle(lo1, lo2, 0b10),
                (1, 1) => Sse2.Shuffle(lo1, lo2, 0b11),
                _ => throw new InvalidOperationException()
            };
            var high = (en3 & 1, en4 & 1) switch
            {
                (0, 0) => Sse2.Shuffle(hi1, hi2, 0b00),
                (1, 0) => Sse2.Shuffle(hi1, hi2, 0b01),
                (0, 1) => Sse2.Shuffle(hi1, hi2, 0b10),
                (1, 1) => Sse2.Shuffle(hi1, hi2, 0b11),
                _ => throw new InvalidOperationException()
            };

            return Vector256.Create(low, high);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<double> Permute4x64(this in Vector256<double> v, [ConstantExpected] byte control)
        {
            if (Avx2.IsSupported)
            {
                return Avx2.Permute4x64(v, control);
            }
            else
            {
                var en1 = (control & 0b00000011);
                var en2 = (control & 0b00001100) >> 2;
                var en3 = (control & 0b00110000) >> 4;
                var en4 = (control & 0b11000000) >> 6;
                return Vector256.Shuffle(v, Vector256.Create(en1, en2, en3, en4));
            }
        }
    }
}
