using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Shared.Extension;

namespace NiVE3.Image.Color
{
    public record struct Hsl(float Hue, float Saturation, float Lightness)
    {
        // SEE: https://en.wikipedia.org/wiki/HSL_and_HSV#HSL_to_RGB_alternative
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Vector4 ToRgb()
        {
            var hue = Hue % 360.0F;
            if (hue < 0.0F)
            {
                hue += 360.0F;
            }
            var saturation = Math.Clamp(Saturation, 0.0F, 1.0F);
            var lightness = Math.Clamp(Lightness, 0.0F, 1.0F);

            var k = (new Vector4(4.0F, 8.0F, 0.0F, 0.0F) + new Vector4(hue / 30.0F)).Mod(12.0F);
            var a = saturation * Math.Min(lightness, 1.0F - lightness);

            var c = new Vector4(lightness) - a * Vector4.Max(-Vector4.One, Vector4.Min(Vector4.Min(k - new Vector4(3.0F), new Vector4(9.0F) - k), Vector4.One));
            return c.AsVector3().AsVector4() + Vector4.UnitW;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Hsl FromRgb(in Vector4 color)
        {
            var min = color.HorizontalMinBy3Element();
            var max = color.HorizontalMaxBy3Element();
            var diff = max - min;

            var hue = diff != 0.0F ? max switch
            {
                _ when max == color.X => (color.Z - color.Y) / diff * 60.0F + 240.0F,
                _ when max == color.Y => (color.X - color.Z) / diff * 60.0F + 120.0F,
                _ => (color.Y - color.X) / diff * 60.0F
            } : 0.0F;
            var lightness = (max + min) * 0.5F;
            var saturation = lightness >= 1.0F || lightness <= 0.0F ? 0.0F : (diff / (1.0F - Math.Abs(lightness * 2.0F - 1.0F)));

            return new Hsl(hue, saturation, lightness);
        }
    }
}
