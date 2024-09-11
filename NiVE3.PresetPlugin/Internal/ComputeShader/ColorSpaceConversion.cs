using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ComputeSharp;

namespace NiVE3.PresetPlugin.Internal.ComputeShader
{
    static class ColorSpaceConversion
    {
        public static Float4 RgbToHsl(Float4 color)
        {
            var min = Hlsl.Min(Hlsl.Min(color.X, color.Y), color.Z);
            var max = Hlsl.Max(Hlsl.Max(color.X, color.Y), color.Z);
            var diff = max - min;
            var hue = 0.0F;
            if (diff != 0.0F)
            {
                if (max == color.X)
                {
                    hue = (color.Z - color.Y) / diff * 60.0F + 240.0F;
                }
                else if (max == color.Y)
                {
                    hue = (color.X - color.Z) / diff * 60.0F + 120.0F;
                }
                else
                {
                    hue = (color.Y - color.X) / diff * 60.0F;
                }
            }

            var lightness = (max + min) * 0.5F;
            var saturation = lightness >= 1.0F || lightness <= 0.0F ? 0.0F : (diff / (1.0F - Hlsl.Abs(lightness * 2.0F - 1.0F)));

            return new Float4(hue, saturation, lightness, color.W);
        }

        public static Float4 RgbToYCbCr(Float4 color)
        {
            return new Float4(
                Hlsl.Dot(color, new Float4(0.114F, 0.587F, 0.299F, 0.0F)),
                0.5F + Hlsl.Dot(color, new Float4(0.5F, -0.331264F, -0.168736F, 0.0F)),
                0.5F + Hlsl.Dot(color, new Float4(-0.081312F, -0.418688F, 0.5F, 0.0F)),
                color.W
            );
        }

        // SEE: https://en.wikipedia.org/wiki/HSL_and_HSV#HSL_to_RGB_alternative
        public static Float4 HslToRgb(Float4 hsl)
        {
            var hue = hsl.X % 360.0F;
            if (hue < 0.0F)
            {
                hue += 360.0F;
            }
            var saturation = Hlsl.Clamp(hsl.Y, 0.0F, 1.0F);
            var lightness = Hlsl.Clamp(hsl.Z, 0.0F, 1.0F);

            var k = (new Float4(4.0F, 8.0F, 0.0F, 0.0F) + (hue / 30.0F)) % 12.0F;
            var a = saturation * Hlsl.Min(lightness, 1.0F - lightness);

            var c = lightness - a * Hlsl.Max(-1.0F, Hlsl.Min(Hlsl.Min(k - 3.0F, 9.0F - k), 1.0F));
            return new Float4(c.XYZ, hsl.W);
        }

        public static Float4 YCbCrToRgb(Float4 ycbcr)
        {
            ycbcr -= new Float4(0.0F, 0.5F, 0.5F, 0.0F);
            return new Float4(
                Hlsl.Dot(ycbcr, new Float4(1.0F, 1.772F, 0.0F, 0.0F)),
                Hlsl.Dot(ycbcr, new Float4(1.0F, -0.344136F, -0.714136F, 0.0F)),
                Hlsl.Dot(ycbcr, new Float4(1.0F, 0.0F, 1.402F, 0.0F)),
                ycbcr.W
            );
        }
    }
}
