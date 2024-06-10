using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.Image.Color
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly record struct Lab(float L, float a, float b)
    {
        const double D65X = 95.0489F;

        const double D65Y = 100.0F;

        const double D65Z = 108.884F;

        public readonly float L = L;

        public readonly float a = a;

        public readonly float b = b;

#pragma warning disable IDE0040 // for cast to Vector4
        readonly float Spacer;
#pragma warning restore IDE0040

        // http://www.brucelindbloom.com/index.html?Eqn_RGB_XYZ_Matrix.html
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector4 ToRgb()
        {
            const double Delta = 216.0 / 24389.0;
            const double Kappa = 24389 / 27.0 / 116.0;

            static double f(double v) => v > Delta ? Math.Pow(v, 3.0) : (v - 16.0 / 116.0) / Kappa;

            var y = (L - 16.0) / 116.0;
            var x = a / 500.0 + y;
            var z = y - b / 200.0;

            return new Xyz((float)(f(x) * D65X), (float)(f(y) * D65Y), (float)(f(z) * D65Z)).ToRgb();
        }

        // http://www.brucelindbloom.com/index.html?Eqn_RGB_XYZ_Matrix.html
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Lab FromRgb(Vector4 color)
        {
            const double Kappa = 24389.0 / 27.0 / 116.0;

            static double f(double v) => v > (216.0 / 24389.0) ? Math.Pow(v, 1.0 / 3.0) : Kappa * v + 16.0 / 116.0;

            var xyz = Xyz.FromRgb(color);

            var x = f(xyz.X / D65X);
            var y = f(xyz.Y / D65Y);
            var z = f(xyz.Z / D65Z);

            return new Lab((float)(116.0 * y - 16.0), (float)(500.0 * (x - y)), (float)(200.0 * (y - z)));
        }
    }
}
