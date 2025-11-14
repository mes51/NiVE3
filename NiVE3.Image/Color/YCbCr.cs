using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.Image.Color
{
    public record struct YCbCr(float Y, float Cb, float Cr)
    {
        public float Y = Y;

        public float Cb = Cb;

        public float Cr = Cr;

#pragma warning disable 0169 // for cast to Vector4
        readonly float Spacer;
#pragma warning restore 0169

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Vector4 ToRgb()
        {
            var ycbcr = Unsafe.BitCast<YCbCr, Vector4>(this) - new Vector4(0.0F, 0.5F, 0.5F, 0.0F);
            return new Vector4(
                Vector4.Dot(ycbcr, new Vector4(1.0F, 1.772F, 0.0F, 0.0F)),
                Vector4.Dot(ycbcr, new Vector4(1.0F, -0.344136F, -0.714136F, 0.0F)),
                Vector4.Dot(ycbcr, new Vector4(1.0F, 0.0F, 1.402F, 0.0F)),
                1.0F
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Vector128<float> ToRgbVector128()
        {
            var ycbcr = Unsafe.BitCast<YCbCr, Vector128<float>>(this) - Vector128.Create(0.0F, 0.5F, 0.5F, 0.0F);
            return Vector128.Create(
                Vector128.Dot(ycbcr, Vector128.Create(1.0F, 1.772F, 0.0F, 0.0F)),
                Vector128.Dot(ycbcr, Vector128.Create(1.0F, -0.344136F, -0.714136F, 0.0F)),
                Vector128.Dot(ycbcr, Vector128.Create(1.0F, 0.0F, 1.402F, 0.0F)),
                1.0F
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static YCbCr FromRgb(in Vector4 color)
        {
            return new YCbCr(
                Vector4.Dot(color, new Vector4(0.114F, 0.587F, 0.299F, 0.0F)),
                0.5F + Vector4.Dot(color, new Vector4(0.5F, -0.331264F, -0.168736F, 0.0F)),
                0.5F + Vector4.Dot(color, new Vector4(-0.081312F, -0.418688F, 0.5F, 0.0F))
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static YCbCr FromRgb(in Vector128<float> color)
        {
            return new YCbCr(
                Vector128.Dot(color, Vector128.Create(0.114F, 0.587F, 0.299F, 0.0F)),
                0.5F + Vector128.Dot(color, Vector128.Create(0.5F, -0.331264F, -0.168736F, 0.0F)),
                0.5F + Vector128.Dot(color, Vector128.Create(-0.081312F, -0.418688F, 0.5F, 0.0F))
            );
        }
    }
}
