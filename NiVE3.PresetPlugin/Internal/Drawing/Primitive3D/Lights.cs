using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Interfaces.RendererParams;

namespace NiVE3.PresetPlugin.Internal.Drawing.Primitive3D
{
    class PointLight
    {
        public readonly Vector256<double> Position;

        public readonly Vector128<float> FloatPosition;

        public readonly Vector4 Color;

        public readonly LightFalloffType FalloffType;

        public readonly float FalloffStart;

        public readonly float FalloffLength;

        public readonly bool IsEnableShadow;

        public readonly double ShadowStrength;

        public readonly double ShadowScatterSize;

        public PointLight(in Vector256<double> position, in Vector4 color, double intensity, LightFalloffType falloffType, double falloffStart, double falloffLength, bool isEnableShadow, double shadowStrength, double shadowScatterSize)
        {
            Position = position;
            FloatPosition = Avx.ConvertToVector128Single(position);
            Color = color * (float)intensity;
            FalloffType = falloffType;
            FalloffStart = (float)falloffStart;
            FalloffLength = (float)falloffLength;
            IsEnableShadow = isEnableShadow;
            ShadowStrength = shadowStrength;
            ShadowScatterSize = shadowScatterSize;
        }
    }

    class SpotLight
    {
        public readonly Vector256<double> Position;

        public readonly Vector128<float> FloatPosition;

        public readonly Vector256<double> Target;

        public readonly Vector128<float> FloatTarget;

        public readonly double ConeAttenuationRate;

        public readonly double InnerCone;

        public readonly double OuterCone;

        public readonly Vector4 Color;

        public readonly LightFalloffType FalloffType;

        public readonly float FalloffStart;

        public readonly float FalloffLength;

        public readonly bool IsEnableShadow;

        public readonly bool IsParallel;

        public readonly double ShadowStrength;

        public readonly double ShadowScatterSize;

        public SpotLight(Vector256<double> position, Vector256<double> target, double coneRadians, double coneAttenuation, Vector4 color, double intensity, LightFalloffType falloffType, double falloffStart, double falloffLength, bool isEnableShadow, double shadowStrength, double shadowScatterSize)
        {
            Position = position;
            Target = target;
            FloatPosition = Avx.ConvertToVector128Single(position);
            FloatTarget = Avx.ConvertToVector128Single(target);
            if (coneRadians >= Math.PI)
            {
                InnerCone = 0.0;
                OuterCone = Math.PI;
                ConeAttenuationRate = 0.0;
                IsParallel = true;
            }
            else
            {
                InnerCone = coneRadians * (1.0 - coneAttenuation) * 0.5;
                OuterCone = coneRadians * 0.5;
                ConeAttenuationRate = OuterCone * coneAttenuation;
                IsParallel = false;
            }
            Color = color * (float)intensity;
            FalloffType = falloffType;
            FalloffStart = (float)falloffStart;
            FalloffLength = (float)falloffLength;
            IsEnableShadow = isEnableShadow;
            ShadowStrength = shadowStrength;
            ShadowScatterSize = shadowScatterSize;
        }
    }

    class AmbientLight
    {
        public readonly Vector4 Color;

        public AmbientLight(Vector4 color, double intensity)
        {
            Color = color * (float)intensity;
        }
    }
}
