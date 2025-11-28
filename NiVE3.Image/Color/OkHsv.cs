using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Text;
using System.Windows.Shapes;
using NiVE3.Shared.Extension;

namespace NiVE3.Image.Color
{
    // SEE: https://bottosson.github.io/posts/colorpicker/
    // SEE: https://bottosson.github.io/misc/ok_color.h
    public record struct OkHsv(float Hue, float Saturation, float Value)
    {
        static readonly Vector128<float> TransferMask = Vector128.Create(0xFFFFFFFFU, 0xFFFFFFFFU, 0xFFFFFFFFU, 0U).AsSingle();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Vector4 ToRgb()
        {
            return ToRgbVector128().AsVector4();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Vector128<float> ToRgbVector128()
        {
            const float S0 = 0.5F;

            var h = (Hue % 360.0F) / 360.0F;

            var a = MathF.Cos(2.0f * MathF.PI * h);
            var b = MathF.Sin(2.0f * MathF.PI * h);

            var cusp = FindCusp(a, b);
            var (maxS, maxT) = ToST(cusp);
            var k = 1.0F - S0 / maxS;

            var Lv = 1 - Saturation * S0 / (S0 + maxT - maxT * k * Saturation);
            var Cv = Saturation * maxT * S0 / (S0 + maxT - maxT * k * Saturation);

            var L = Value * Lv;
            var C = Value * Cv;

            var Lvt = ToEInv(Lv);
            var Cvt = Cv * Lvt / Lv;

            var newL = ToEInv(L);
            C = C * newL / L;
            L = newL;

            var rgbScale = new OkLab(Lvt, a* Cvt, b *Cvt).ToRgb();
            var scaleL = MathF.Cbrt(1.0F / rgbScale.HorizontalMaxBy3Element());

            L *= scaleL;
            C *= scaleL;

            var rgb = new OkLab(L, C * a, C * b).ToRgbVector128();
            return SrgbTransferFunction(rgb) + Vector128.Create(0.0F, 0.0F, 0.0F, 1.0F);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static OkHsv FromRgb(Vector4 rgb)
        {
            return FromRgb(rgb.AsVector128());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static OkHsv FromRgb(Vector128<float> rgb)
        {
            const float S0 = 0.5F;

            var lab = OkLab.FromRgb(SrgbTransferFunctionInv(rgb));

            var C = MathF.Sqrt(lab.a * lab.a + lab.b * lab.b);
            var a = lab.a / C;
            var b = lab.b / C;

            var L = lab.L;
            var h = 0.5F + 0.5F * MathF.Atan2(-lab.b, -lab.a) / MathF.PI;

            var cusp = FindCusp(a, b);
            var (maxS, maxT) = ToST(cusp);
            var k = 1.0F - S0 / maxS;

            var t = maxT / (C + L * maxT);
            var Lv = t * L;
            var Cv = t * C;

            var Lvt = ToEInv(Lv);
            var Cvt = Cv * Lvt / Lv;

            var rgbScale = new OkLab(Lvt, a * Cvt, b * Cvt).ToRgbVector128();
            var scaleL = MathF.Cbrt(1.0F / rgbScale.HorizontalMaxBy3Element().GetElement(0));

            L /= scaleL;
            C /= scaleL;

            var le = ToE(L);
            C = C * le / L;
            L = le;

            var v = L / Lv;
            var s = (S0 + maxT) * Cv / ((maxT * S0) + maxT * k * Cv);

            return new OkHsv(h * 360.0F, s, v);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float ComputeMaxSaturation(float a, float b)
        {
            var w = Vector128<float>.Zero;
            var S = 0.0F;
            if (-1.88170328F * a - 0.80936493F * b > 1.0F)
            {
                // Red component
                var k0 = 1.19086277F;
                var k1 = 1.76576728F;
                var k2 = 0.59662641F;
                var k3 = 0.75515197f;
                var k4 = 0.56771245f;
                w = Vector128.Create(4.0767416621F, -3.3077115913F, 0.2309699292F, 0.0F);

                S = k0 + k1 * a + k2 * b + k3 * a * a + k4 * a * b;
            }
            else if (1.81444104F * a - 1.19445276F * b > 1.0F)
            {
                // Green component
                var k0 = 0.73956515F;
                var k1 = -0.45954404F;
                var k2 = 0.08285427F;
                var k3 = 0.12541070F;
                var k4 = 0.14503204F;
                w = Vector128.Create(-1.2684380046F, 2.6097574011F, -0.3413193965F, 0.0F);

                S = k0 + k1 * a + k2 * b + k3 * a * a + k4 * a * b;
            }
            else
            {
                // Blue component
                var k0 = 1.35733652F;
                var k1 = -0.00915799F;
                var k2 = -1.15130210F;
                var k3 = -0.50559606F;
                var k4 = 0.00692167F;
                w = Vector128.Create(-0.0041960863F, -0.7034186147F, 1.7076147010F, 0.0F);

                S = k0 + k1 * a + k2 * b + k3 * a * a + k4 * a * b;
            }

            var k = Vector128.Create(0.3963377774F, -0.1055613458F, -0.0894841775F, 0.0F) * a + Vector128.Create(0.2158037573F, -0.0638541728F, -1.2914855480F, 0.0F) * b;

            var lms = k * S + Vector128.Create(1.0F, 1.0F, 1.0F, 0.0F);
            var lms3 = lms * lms * lms;
            var dS = 3.0F * k * lms * lms;
            var dS2 = 6.0F * k * k * lms;

            var f = Vector128.Dot(w, lms3);
            var f1 = Vector128.Dot(w, dS);
            var f2 = Vector128.Dot(w, dS2);

            return S - f * f1 / (f1 * f1 - 0.5F * f * f2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector128<float> SrgbTransferFunction(Vector128<float> a)
        {
            var mask = Vector128.GreaterThanOrEqual(Vector128.Create(0.0031308F), a);

            var gte = 12.92F * a;
            var less = 1.055F * a.Pow(Vector128.Create(0.4166666666666667F)) - Vector128.Create(0.055F);

            return (
                (gte & mask) | (less & mask.Not())
            ) & TransferMask;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector128<float> SrgbTransferFunctionInv(Vector128<float> a)
        {
            var mask = Vector128.LessThan(Vector128.Create(0.04045F), a);

            var less = (a + Vector128.Create(1.055F)).Pow(Vector128.Create(2.4F));
            var gte = a / 12.92F;

            return (
                (less & mask) | (gte & mask.Not())
            ) & TransferMask;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector2 FindCusp(float a, float b)
        {
            var S = ComputeMaxSaturation(a, b);

            var rgbAtMax = new OkLab(1.0F, S * a, S * b).ToRgbVector128();
            var L = MathF.Cbrt(1.0F / rgbAtMax.HorizontalMaxBy3Element().GetElement(0));
            var C = L * S;

            return new Vector2(L, C);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float ToE(float x)
        {
            const float K1 = 0.206F;
            const float K2 = 0.03F;
            const float K3 = (1.0F + K1) / (1.0F + K2);

            return 0.5F * (K3 * x - K1 + MathF.Sqrt((K3 * x - K1) * (K3 * x - K1) + 4.0F * K2 * K3 * x));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float ToEInv(float x)
        {
            const float K1 = 0.206F;
            const float K2 = 0.03F;
            const float K3 = (1.0F + K1) / (1.0F + K2);

            return (x * x + K1 * x) / (K3 * (x + K2));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector2 ToST(Vector2 cusp)
        {
            var L = cusp.X;
            var C = cusp.Y;
            return new Vector2(C / L, C / (1.0F - L));
        }
    }

    file static class Vector2Extension
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Deconstruct(this Vector2 v, out float x, out float y)
        {
            x = v.X;
            y = v.Y;
        }
    }
}
