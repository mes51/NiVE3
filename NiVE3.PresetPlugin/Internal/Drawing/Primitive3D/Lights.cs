using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Interfaces.RendererParams;
using NiVE3.Shared.Extension;
using NiVE3.Plugin.Numerics;
using System.Windows.Media.Media3D;

namespace NiVE3.PresetPlugin.Internal.Drawing.Primitive3D
{
    class PointLight
    {
        public readonly Vector128<float> Position;

        public readonly Vector4 Color;

        public readonly LightFalloffType FalloffType;

        public readonly float FalloffStart;

        public readonly float FalloffLength;

        public readonly bool IsEnableShadow;

        public readonly float ShadowStrength;

        public readonly float ShadowScatterSize;

        public PointLight(in Vector256<double> position, in Vector3 color, double intensity, LightFalloffType falloffType, double falloffStart, double falloffLength, bool isEnableShadow, double shadowStrength, double shadowScatterSize)
        {
            Position = Avx.ConvertToVector128Single(position);
            Color = new Vector4(color * (float)intensity, 1.0F);
            FalloffType = falloffType;
            FalloffStart = (float)falloffStart;
            FalloffLength = (float)falloffLength;
            IsEnableShadow = isEnableShadow;
            ShadowStrength = (float)shadowStrength;
            ShadowScatterSize = (float)shadowScatterSize;
        }
    }

    class SpotLight
    {
        public readonly Vector128<float> Position;

        public readonly Vector3 Direction;

        public readonly double ConeRadian;

        public readonly double ConeAttenuationRate;

        public readonly float InnerCone;

        public readonly float OuterCone;

        public readonly float OuterConeCos;

        public readonly float InvertInnerConeCos;

        public readonly Vector4 Color;

        public readonly LightFalloffType FalloffType;

        public readonly float FalloffStart;

        public readonly float FalloffLength;

        public readonly bool IsEnableShadow;

        public readonly float ShadowStrength;

        public readonly float ShadowScatterSize;

        public readonly Matrix4x4d LightViewMatrix;

        public readonly Matrix4x4 FloatLightViewMatrix;

        public SpotLight(Vector256<double> position, Vector256<double> target, double coneRadian, double coneAttenuation, Vector3 color, double intensity, LightFalloffType falloffType, double falloffStart, double falloffLength, bool isEnableShadow, double shadowStrength, double shadowScatterSize, in Matrix4x4d lightViewMartrix)
        {
            Position = Avx.ConvertToVector128Single(position);
            Direction = Vector3.Normalize(Avx.Subtract(target, position).AsVector3());
            ConeRadian = coneRadian;
            var innerCone = coneRadian * (1.0 - coneAttenuation) * 0.5;
            var outerCone = coneRadian * 0.5;
            InnerCone = (float)innerCone;
            OuterCone = (float)outerCone;
            OuterConeCos = (float)Math.Cos(outerCone);
            ConeAttenuationRate = outerCone * coneAttenuation;
            InvertInnerConeCos = (float)(1.0 / (Math.Cos(innerCone) - Math.Cos(outerCone)));
            Color = new Vector4(color * (float)intensity, 1.0F);
            FalloffType = falloffType;
            FalloffStart = (float)falloffStart;
            FalloffLength = (float)falloffLength;
            IsEnableShadow = isEnableShadow;
            ShadowStrength = (float)shadowStrength;
            ShadowScatterSize = (float)shadowScatterSize;
            LightViewMatrix = lightViewMartrix;
            FloatLightViewMatrix = (Matrix4x4)lightViewMartrix;
        }
    }

    class ParallelLight
    {
        public readonly Vector128<float> Position;

        public readonly Vector3 Direction;

        public readonly Vector4 Color;

        public readonly LightFalloffType FalloffType;

        public readonly float FalloffStart;

        public readonly float FalloffLength;

        public readonly bool IsEnableShadow;

        public readonly bool IsParallel;

        public readonly float ShadowStrength;

        public readonly float ShadowScatterSize;

        public readonly Matrix4x4d LightViewMatrix;

        public readonly Matrix4x4 FloatLightViewMatrix;

        public ParallelLight(Vector256<double> position, Vector256<double> target, Vector3 color, double intensity, LightFalloffType falloffType, double falloffStart, double falloffLength, bool isEnableShadow, double shadowStrength, double shadowScatterSize, in Matrix4x4d lightViewMartrix)
        {
            Position = Avx.ConvertToVector128Single(position);
            Direction = Vector3.Normalize(Avx.Subtract(target, position).AsVector3());
            Color = new Vector4(color * (float)intensity, 1.0F);
            FalloffType = falloffType;
            FalloffStart = (float)falloffStart;
            FalloffLength = (float)falloffLength;
            IsEnableShadow = isEnableShadow;
            ShadowStrength = (float)shadowStrength;
            ShadowScatterSize = (float)shadowScatterSize;
            LightViewMatrix = lightViewMartrix;
            FloatLightViewMatrix = (Matrix4x4)lightViewMartrix;
        }
    }

    class AmbientLight
    {
        public readonly Vector4 Color;

        public AmbientLight(Vector3 color, double intensity)
        {
            Color = new Vector4(color * (float)intensity, 1.0F);
        }
    }
}
