using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ComputeSharp;

namespace NiVE3.PresetPlugin.Internal.Drawing.ComputeShader
{
    static class ShaderUtil
    {
        public static Float4 Mask(Float4 v, Bool4 mask)
        {
            return new Float4(mask.X ? v.X : 0.0F, mask.Y ? v.Y : 0.0F, mask.Z ? v.Z : 0.0F, mask.W ? v.W : 0.0F);
        }

        public static Float4 NotMask(Float4 v, Bool4 mask)
        {
            return new Float4(mask.X ? 0.0F : v.X, mask.Y ? 0.0F : v.Y, mask.Z ? 0.0F : v.Z, mask.W ? 0.0F : v.W);
        }

        public static float Sum(Float4 v)
        {
            return v.X + v.Y + v.Z + v.W;
        }

        public static Float4 CalcBarycentricCoord(Float4 x, Float4 y, Float4 z, Float4 e)
        {
            return new Float4(
                Sum(x * e),
                Sum(y * e),
                Sum(z * e),
                0.0F
            );
        }

        public static float CalcFalloff(Float3 diff, int type, float falloffStart, float falloffLength)
        {
            var length = Hlsl.Length(diff);
            if (length <= falloffStart)
            {
                return 1.0F;
            }
            length -= falloffStart;

            switch (type)
            {
                case 1:
                    return Hlsl.Max((falloffLength - length) / falloffLength, 0.0F);
                case 2:
                    return Hlsl.Min(1.0F / Hlsl.Pow(1.0F + length, 2.0F), 1.0F);
                default:
                    return 1.0F;
            }
        }
    }
}
