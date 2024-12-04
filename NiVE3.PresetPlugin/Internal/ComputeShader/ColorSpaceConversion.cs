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

        // https://bottosson.github.io/posts/oklab/
        public static Float4 RgbToOkLab(Float4 rgb)
        {
            var mask = rgb.XYZ >= 0.04045F;
            var linear = FloatNUtil.Mask(Hlsl.Pow((rgb.XYZ + 0.055F) / 1.055F, 2.4F), mask) + FloatNUtil.NotMask(rgb.XYZ / 12.92F, mask);

            var lmsMatrix = new Float3x3(
                0.0514459929F, 0.5363325363F, 0.4122214708F,
                0.1073969566F, 0.6806995451F, 0.2119034982F,
                0.6299787005F, 0.2817188376F, 0.0883024619F
            );
            var lms = Hlsl.Pow(Hlsl.Mul(lmsMatrix, linear), 1.0F / 3.0F);

            var labMatrix = new Float3x3(
                0.2104542553F, 0.7936177850F, -0.0040720468F,
                1.9779984951F, -2.4285922050F, 0.4505937099F,
                0.0259040371F, 0.7827717662F, -0.8086757660F
            );

            return new Float4(Hlsl.Mul(labMatrix, lms), rgb.W);
        }

        // https://bottosson.github.io/posts/oklab/
        public static Float4 OkLabToRgb(Float4 okLab)
        {
            var lmsMatrix = new Float3x3(
                1.0F, 0.3963377774F, 0.2158037573F,
                1.0F, -0.1055613458F, -0.0638541728F,
                1.0F, -0.0894841775F, -1.2914855480F
            );
            var lms = Hlsl.Mul(lmsMatrix, okLab.XYZ);
            lms = lms * lms * lms;

            var rgbMatrix = new Float3x3(
                -0.0041960863F, -0.7034186147F, 1.7076147010F,
                -1.2684380046F, 2.6097574011F, -0.3413193965F,
                4.0767416621F, -3.3077115913F, 0.2309699292F
            );

            var linear = Hlsl.Mul(rgbMatrix, lms);

            var mask = linear >= 0.0031308F;
            var rgb = FloatNUtil.Mask(Hlsl.Sign(linear) * (Hlsl.Pow(linear, 1.0F / 2.4F) * 1.055F - 0.055F), mask) + FloatNUtil.NotMask(linear * 12.92F, mask);

            return new Float4(rgb, okLab.W);
        }
    }
}
