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
    public record struct Hsv(float Hue, float Saturation, float Value)
    {
        // SEE: https://en.wikipedia.org/wiki/HSL_and_HSV#HSV_to_RGB_alternative
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Vector4 ToRgb()
        {
            var hue = Hue % 360.0F;
            if (hue < 0.0F)
            {
                hue += 360.0F;
            }
            var saturation = Math.Clamp(Saturation, 0.0F, 1.0F);
            var value = Math.Clamp(Value, 0.0F, 1.0F);

            var k = (new Vector4(1.0F, 3.0F, 5.0F, 0.0F) + new Vector4(hue / 60.0F)).Mod(6.0F);

            var c = new Vector4(value) - new Vector4(value * saturation) * Vector4.Max(Vector4.Zero, Vector4.Min(Vector4.Min(k, new Vector4(4.0F) - k), Vector4.One));
            return c.AsVector3().AsVector4() + Vector4.UnitW;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Hsv FromRgb(in Vector4 color)
        {
            var clamped = Vector4.Clamp(color, Vector4.Zero, Vector4.One);
            var min = clamped.HorizontalMinBy3Element();
            var max = clamped.HorizontalMaxBy3Element();
            var diff = max - min;

            var hue = diff != 0.0F ? max switch
            {
                _ when max == clamped.X => (clamped.Z - clamped.Y) / diff * 60.0F + 240.0F,
                _ when max == clamped.Y => (clamped.X - clamped.Z) / diff * 60.0F + 120.0F,
                _ => (clamped.Y - clamped.X) / diff * 60.0F
            } : 0.0F;
            if (hue < 0.0F)
            {
                hue += 360.0F;
            }
            var saturation = max <= 0.0F ? 0.0F : (diff / max);

            return new Hsv(hue, saturation, max);
        }
    }
}
