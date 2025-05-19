using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Image.Internal;
using NiVE3.Shared.Extension;

namespace NiVE3.Image.Color
{
    public record struct Hsv(float Hue, float Saturation, float Value)
    {
        static readonly Vector128<float> WithoutWMask128 = Vector128.Create(0xFFFFFFFFU, 0xFFFFFFFFU, 0xFFFFFFFFU, 0).AsSingle();

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

        // SEE: https://en.wikipedia.org/wiki/HSL_and_HSV#HSV_to_RGB_alternative
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Vector128<float> ToRgbVector128()
        {
            var hue = Hue % 360.0F;
            if (hue < 0.0F)
            {
                hue += 360.0F;
            }
            var saturation = Math.Clamp(Saturation, 0.0F, 1.0F);
            var value = Math.Clamp(Value, 0.0F, 1.0F);

            var k = (Vector128.Create(1.0F, 3.0F, 5.0F, 0.0F) + Vector128.Create(hue / 60.0F)).Mod(6.0F);

            var c = Vector128.Create(value) - Vector128.Create(value * saturation) * Vector128.Max(Vector128<float>.Zero, Vector128.Min(Vector128.Min(k, Vector128.Create(4.0F) - k), Vector128<float>.One));
            return (c & WithoutWMask128) + Vector128.Create(0.0F, 0.0F, 0.0F, 1.0F);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Hsv FromRgb(in Vector128<float> color)
        {
            var clamped = Vector128.Min(Vector128.Max(color, Vector128<float>.Zero), Vector128<float>.One);
            var min = clamped.HorizontalMinBy3Element().GetElement(0);
            var max = clamped.HorizontalMaxBy3Element().GetElement(0);
            var diff = max - min;

            var hue = diff != 0.0F ? max switch
            {
                _ when max == clamped.GetElement(0) => (clamped.GetElement(2) - clamped.GetElement(1)) / diff * 60.0F + 240.0F,
                _ when max == clamped.GetElement(1) => (clamped.GetElement(0) - clamped.GetElement(2)) / diff * 60.0F + 120.0F,
                _ => (clamped.GetElement(1) - clamped.GetElement(0)) / diff * 60.0F
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
