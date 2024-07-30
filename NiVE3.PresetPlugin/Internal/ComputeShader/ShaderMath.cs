using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ComputeSharp;

namespace NiVE3.PresetPlugin.Internal.ComputeShader
{
    static class ShaderMath
    {
        public static float PowRetainSign(float x, float y)
        {
            return Hlsl.Pow(Hlsl.Abs(x), y) * Hlsl.Sign(x);
        }

        public static Float2 PowRetainSign(Float2 x, Float2 y)
        {
            return Hlsl.Pow(Hlsl.Abs(x), y) * Hlsl.Sign(x);
        }

        public static Float3 PowRetainSign(Float3 x, Float3 y)
        {
            return Hlsl.Pow(Hlsl.Abs(x), y) * Hlsl.Sign(x);
        }

        public static Float4 PowRetainSign(Float4 x, Float4 y)
        {
            return Hlsl.Pow(Hlsl.Abs(x), y) * Hlsl.Sign(x);
        }
    }
}
