using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Shared.Extension;

namespace NiVE3.Image.Color
{
    [StructLayout(LayoutKind.Sequential)]
    public record struct Xyz(float X, float Y, float Z)
    {
        public float X = X;

        public float Y = Y;

        public float Z = Z;

#pragma warning disable IDE0040 // for cast to Vector4
        readonly float Spacer;
#pragma warning restore IDE0040

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Vector4 ToRgb()
        {
            // from: http://www.brucelindbloom.com/index.html?Eqn_RGB_XYZ_Matrix.html
            var rgbRow1 = new Vector4( 3.2404542F, -1.5371385F, -0.4985314F, 0.0F);
            var rgbRow2 = new Vector4(-0.9692660F,  1.8760108F,  0.0415560F, 0.0F);
            var rgbRow3 = new Vector4( 0.0556434F, -0.2040259F,  1.0572252F, 0.0F);

            var xyz = Unsafe.BitCast<Xyz, Vector4>(this);
            return new Vector4(
                Vector4.Dot(xyz, rgbRow3),
                Vector4.Dot(xyz, rgbRow2),
                Vector4.Dot(xyz, rgbRow1),
                1.0F
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Xyz FromRgb(Vector4 color)
        {
            // from: http://www.brucelindbloom.com/index.html?Eqn_RGB_XYZ_Matrix.html
            var xyzRow1 = new Vector4(0.1804375F, 0.3575761F, 0.4124564F, 0.0F);
            var xyzRow2 = new Vector4(0.0721750F, 0.7151522F, 0.2126729F, 0.0F);
            var xyzRow3 = new Vector4(0.9503041F, 0.1191920F, 0.0193339F, 0.0F);

            return new Xyz(
                Vector4.Dot(color, xyzRow1),
                Vector4.Dot(color, xyzRow2),
                Vector4.Dot(color, xyzRow3)
            );
        }
    }
}
