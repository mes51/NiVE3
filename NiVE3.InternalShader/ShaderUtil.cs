using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ComputeSharp;

namespace NiVE3.InternalShader
{
    internal class ShaderUtil
    {
        // https://bottosson.github.io/posts/oklab/
        public static Float3 OkLabToRgb(Float3 okLab)
        {
            var lmsMatrix = new Float3x3(
                1.0F, 0.3963377774F, 0.2158037573F,
                1.0F, -0.1055613458F, -0.0638541728F,
                1.0F, -0.0894841775F, -1.2914855480F
            );
            var lms = Hlsl.Mul(lmsMatrix, okLab);
            lms = lms * lms * lms;

            var rgbMatrix = new Float3x3(
                -0.0041960863F, -0.7034186147F, 1.7076147010F,
                -1.2684380046F, 2.6097574011F, -0.3413193965F,
                4.0767416621F, -3.3077115913F, 0.2309699292F
            );

            var linear = Hlsl.Mul(rgbMatrix, lms);

            var mask = linear >= 0.0031308F;
            var rgb = FloatNUtil.Mask(Hlsl.Sign(linear) * (Hlsl.Pow(linear, 1.0F / 2.4F) * 1.055F - 0.055F), mask) + FloatNUtil.NotMask(linear * 12.92F, mask);

            return rgb;
        }
    }

    static class FloatNUtil
    {
        public static Float3 Mask(Float3 v, Bool3 mask)
        {
            return new Float3(mask.X ? v.X : 0.0F, mask.Y ? v.Y : 0.0F, mask.Z ? v.Z : 0.0F);
        }

        public static Float3 NotMask(Float3 v, Bool3 mask)
        {
            return new Float3(mask.X ? 0.0F : v.X, mask.Y ? 0.0F : v.Y, mask.Z ? 0.0F : v.Z);
        }
        public static Float4 Mask(Float4 v, Bool4 mask)
        {
            return new Float4(mask.X ? v.X : 0.0F, mask.Y ? v.Y : 0.0F, mask.Z ? v.Z : 0.0F, mask.W ? v.W : 0.0F);
        }

        public static Float4 NotMask(Float4 v, Bool4 mask)
        {
            return new Float4(mask.X ? 0.0F : v.X, mask.Y ? 0.0F : v.Y, mask.Z ? 0.0F : v.Z, mask.W ? 0.0F : v.W);
        }
    }
}
