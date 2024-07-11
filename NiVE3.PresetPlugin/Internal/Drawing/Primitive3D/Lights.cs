using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Numerics;
using NiVE3.Plugin.Interfaces.RendererParams;
using NiVE3.Shared.Extension;
using NiVE3.PresetPlugin.Internal.Extension;

namespace NiVE3.PresetPlugin.Internal.Drawing.Primitive3D
{
    class PointLight
    {
        static readonly Vector256<double> UnitX = Vector256.Create(1.0, 0.0, 0.0, 0.0);
        static readonly Vector256<double> UnitY = Vector256.Create(0.0, 1.0, 0.0, 0.0);
        static readonly Vector256<double> UnitZ = Vector256.Create(0.0, 0.0, 1.0, 0.0);
        static readonly Vector256<double> UnitNegX = Vector256.Create(-1.0, 0.0, 0.0, 0.0);
        static readonly Vector256<double> UnitNegY = Vector256.Create(0.0, -1.0, 0.0, 0.0);
        static readonly Vector256<double> UnitNegZ = Vector256.Create(0.0, 0.0, -1.0, 0.0);

        static readonly Matrix4x4d LookAtFront = Matrix4x4d.CreateLookAt(Vector256<double>.Zero, UnitZ, UnitY);
        static readonly Matrix4x4d LookAtBack = Matrix4x4d.CreateLookAt(Vector256<double>.Zero, UnitNegZ, UnitY);
        static readonly Matrix4x4d LookAtLeft = Matrix4x4d.CreateLookAt(Vector256<double>.Zero, UnitNegX, UnitY);
        static readonly Matrix4x4d LookAtRight = Matrix4x4d.CreateLookAt(Vector256<double>.Zero, UnitX, UnitY);
        static readonly Matrix4x4d LookAtTop = Matrix4x4d.CreateLookAt(Vector256<double>.Zero, UnitY, UnitZ);
        static readonly Matrix4x4d LookAtBottom = Matrix4x4d.CreateLookAt(Vector256<double>.Zero, UnitNegY, UnitNegZ);

        public readonly Vector128<float> Position;

        public readonly Vector4 Color;

        public readonly LightFalloffType FalloffType;

        public readonly float FalloffStart;

        public readonly float FalloffLength;

        public readonly bool IsEnableShadow;

        public readonly float ShadowStrength;

        public readonly float ShadowScatterSize;

        public readonly Matrix4x4d FrontLightViewMatrix;

        public readonly Matrix4x4d BackLightViewMatrix;

        public readonly Matrix4x4d LeftLightViewMatrix;

        public readonly Matrix4x4d RightLightViewMatrix;

        public readonly Matrix4x4d TopLightViewMatrix;

        public readonly Matrix4x4d BottomLightViewMatrix;

        public readonly Matrix4x4 FloatFrontLightViewMatrix;

        public readonly Matrix4x4 FloatBackLightViewMatrix;

        public readonly Matrix4x4 FloatLeftLightViewMatrix;

        public readonly Matrix4x4 FloatRightLightViewMatrix;

        public readonly Matrix4x4 FloatTopLightViewMatrix;

        public readonly Matrix4x4 FloatBottomLightViewMatrix;

        public readonly Matrix4x4 FaceDetectionMatrix;

        public PointLight(in Vector256<double> position, in Vector3 color, double intensity, LightFalloffType falloffType, double falloffStart, double falloffLength, bool isEnableShadow, double shadowStrength, double shadowScatterSize, in Matrix4x4d baseLightViewMartrix, in Matrix4x4d viewOffsetMatrix)
        {
            Position = Avx.ConvertToVector128Single(position);
            Color = new Vector4(color * (float)intensity, 1.0F);
            FalloffType = falloffType;
            FalloffStart = (float)falloffStart;
            FalloffLength = (float)falloffLength;
            IsEnableShadow = isEnableShadow;
            ShadowStrength = (float)shadowStrength;
            ShadowScatterSize = (float)shadowScatterSize;

            FrontLightViewMatrix = baseLightViewMartrix * LookAtFront * viewOffsetMatrix;
            BackLightViewMatrix = baseLightViewMartrix * LookAtBack * viewOffsetMatrix;
            LeftLightViewMatrix = baseLightViewMartrix * LookAtLeft * viewOffsetMatrix;
            RightLightViewMatrix = baseLightViewMartrix * LookAtRight * viewOffsetMatrix;
            TopLightViewMatrix = baseLightViewMartrix * LookAtTop * viewOffsetMatrix;
            BottomLightViewMatrix = baseLightViewMartrix * LookAtBottom * viewOffsetMatrix;
            FloatFrontLightViewMatrix = (Matrix4x4)FrontLightViewMatrix;
            FloatBackLightViewMatrix = (Matrix4x4)BackLightViewMatrix;
            FloatLeftLightViewMatrix = (Matrix4x4)LeftLightViewMatrix;
            FloatRightLightViewMatrix = (Matrix4x4)RightLightViewMatrix;
            FloatTopLightViewMatrix = (Matrix4x4)TopLightViewMatrix;
            FloatBottomLightViewMatrix = (Matrix4x4)BottomLightViewMatrix;
            FaceDetectionMatrix = (Matrix4x4)(baseLightViewMartrix * LookAtFront);
        }

        public GPUPointLight ToGpu()
        {
            return new GPUPointLight(
                Position.AsFloat4(),
                Color.AsFloat4(),
                (int)FalloffType,
                FalloffStart,
                FalloffLength,
                IsEnableShadow,
                ShadowStrength,
                ShadowScatterSize,
                FloatFrontLightViewMatrix.ToFloat4x4(),
                FloatBackLightViewMatrix.ToFloat4x4(),
                FloatLeftLightViewMatrix.ToFloat4x4(),
                FloatRightLightViewMatrix.ToFloat4x4(),
                FloatTopLightViewMatrix.ToFloat4x4(),
                FloatBottomLightViewMatrix.ToFloat4x4(),
                FaceDetectionMatrix.ToFloat4x4()
            );
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

        public GPUSpotLight ToGpu()
        {
            return new GPUSpotLight(
                Position.AsFloat4(),
                Direction.AsFloat3(),
                (float)ConeAttenuationRate,
                InnerCone,
                OuterCone,
                OuterConeCos,
                InvertInnerConeCos,
                Color.AsFloat4(),
                (int)FalloffType,
                FalloffStart,
                FalloffLength,
                IsEnableShadow,
                ShadowStrength,
                ShadowScatterSize,
                FloatLightViewMatrix.ToFloat4x4()
            );
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

        public GPUParallelLight ToGpu()
        {
            return new GPUParallelLight(
                Position.AsFloat4(),
                Direction.AsFloat3(),
                Color.AsFloat4(),
                (int)FalloffType,
                FalloffStart,
                FalloffLength,
                IsEnableShadow,
                ShadowStrength,
                ShadowScatterSize,
                FloatLightViewMatrix.ToFloat4x4()
            );
        }
    }

    class AmbientLight
    {
        public readonly Vector4 Color;

        public AmbientLight(Vector3 color, double intensity)
        {
            Color = new Vector4(color * (float)intensity, 1.0F);
        }

        public GPUAmbientLight ToGpu()
        {
            return new GPUAmbientLight(Color.AsFloat4());
        }
    }
}
