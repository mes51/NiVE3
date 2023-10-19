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
}
