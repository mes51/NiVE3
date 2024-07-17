using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ComputeSharp;
using NiVE3.PresetPlugin.Internal.Drawing.Primitive3D;

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
                Hlsl.Dot(x.XYZ, e.XYZ),
                Hlsl.Dot(y.XYZ, e.XYZ),
                Hlsl.Dot(z.XYZ, e.XYZ),
                1.0F
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

        public static float DepthRound(float value)
        {
            const float DepthRoundingDigit = 10000.0F; // TODO: 要調整
            return Hlsl.Round(value * DepthRoundingDigit) / DepthRoundingDigit;
        }

        public static Float4 CalcE(int x, int y, GPUTriangle triangle, float scaleRateX, float scaleRateY, float offsetX, float offsetY)
        {
            const float Epsilon = 1E-7F;

            if (x < triangle.MinX || x >= triangle.MaxX || y < triangle.MinY || y >= triangle.MaxY)
            {
                return -float.NaN;
            }

            var eY = new Float4((triangle.EdgeX * ((y + offsetY) * scaleRateY - triangle.VVEY)).XYZ, 0.0F);
            var eX = new Float4(((x + offsetX) * scaleRateX - triangle.VVEX).XYZ, 0.0F);
            var e = (eY - (triangle.EdgeY * eX)) * triangle.Denominator;

            var ae = Mask(e, Hlsl.Abs(e) >= Epsilon);
            if (Hlsl.Any(ae < 0.0F))
            {
                return -float.NaN;
            }

            return e;
        }
    }
}
