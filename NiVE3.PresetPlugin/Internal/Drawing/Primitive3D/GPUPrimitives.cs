using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;
using ComputeSharp;
using NiVE3.Image;
using NiVE3.Image.Drawing;
using NiVE3.Plugin.Interfaces;

namespace NiVE3.PresetPlugin.Internal.Drawing.Primitive3D
{
    readonly record struct GPUTriangle(
        int Id,
        int MinX,
        int MaxX,
        int MinY,
        int MaxY,
        Float4 EdgeX,
        Float4 EdgeY,
        Float4 VVEX,
        Float4 VVEY,
        Float4 U,
        Float4 V,
        Float4 W,
        Float4 VVX,
        Float4 VVY,
        Float4 VVZ,
        Float4 SVVX,
        Float4 SVVY,
        Float4 SVVZ,
        Float4 Denominator,
        Bool IsFrontFace,
        Float3 FloatNormal,
        int InterpolationQuality,
        float Opacity,
        float LightTransmission,
        int BlendMode,
        Bool IsAcceptShadow,
        Bool IsAcceptLight,
        float Ambient,
        float Diffuse,
        float SpecularIntensity,
        float SpecularShininess,
        float Metal
    )
    {
        public readonly int Id = Id;
        public readonly int MinX = MinX;
        public readonly int MaxX = MaxX;
        public readonly int MinY = MinY;
        public readonly int MaxY = MaxY;
        public readonly int TrueMinX = Math.Min(MinX, MaxX);
        public readonly int TrueMaxX = Math.Max(MinX, MaxX);
        public readonly int TrueMinY = Math.Min(MinY, MaxY);
        public readonly int TrueMaxY = Math.Max(MinY, MaxY);
        public readonly Float4 EdgeX = EdgeX;
        public readonly Float4 EdgeY = EdgeY;
        public readonly Float4 VVEX = VVEX;
        public readonly Float4 VVEY = VVEY;
        public readonly Float4 U = U;
        public readonly Float4 V = V;
        public readonly Float4 W = W;
        public readonly Float4 VVX = VVX;
        public readonly Float4 VVY = VVY;
        public readonly Float4 VVZ = VVZ;
        public readonly Float4 SVVX = SVVX;
        public readonly Float4 SVVY = SVVY;
        public readonly Float4 SVVZ = SVVZ;
        public readonly Float4 Denominator = Denominator;
        public readonly Bool IsFrontFace = IsFrontFace;
        public readonly Float3 FloatNormal = FloatNormal;
        public readonly int InterpolationQuality = InterpolationQuality;
        public readonly float Opacity = Opacity;
        public readonly float LightTransmission = LightTransmission;
        public readonly int BlendMode = BlendMode;
        public readonly Bool IsAcceptShadow = IsAcceptShadow;
        public readonly Bool IsAcceptLight = IsAcceptLight;
        public readonly float Ambient = Ambient;
        public readonly float Diffuse = Diffuse;
        public readonly float SpecularIntensity = SpecularIntensity;
        public readonly float SpecularShininess = SpecularShininess;
        public readonly float Metal = Metal;
    }

    readonly record struct GPUMaskTriangle(
        int MinX,
        int MaxX,
        int MinY,
        int MaxY,
        Float4 EdgeX,
        Float4 EdgeY,
        Float4 VVEX,
        Float4 VVEY,
        Float4 U,
        Float4 V,
        Float4 W,
        Float4 VVX,
        Float4 VVY,
        Float4 VVZ,
        Float4 Denominator,
        Bool IsFrontFace,
        Float3 FloatNormal,
        int InterpolationQuality,
        float Opacity,
        float LightTransmission,
        Bool IsAcceptLight,
        float Ambient,
        float Diffuse,
        float SpecularIntensity,
        float SpecularShininess,
        float Metal
    )
    {
        public readonly int MinX = MinX;
        public readonly int MaxX = MaxX;
        public readonly int MinY = MinY;
        public readonly int MaxY = MaxY;
        public readonly int TrueMinX = Math.Min(MinX, MaxX);
        public readonly int TrueMaxX = Math.Max(MinX, MaxX);
        public readonly int TrueMinY = Math.Min(MinY, MaxY);
        public readonly int TrueMaxY = Math.Max(MinY, MaxY);
        public readonly Float4 EdgeX = EdgeX;
        public readonly Float4 EdgeY = EdgeY;
        public readonly Float4 VVEX = VVEX;
        public readonly Float4 VVEY = VVEY;
        public readonly Float4 U = U;
        public readonly Float4 V = V;
        public readonly Float4 W = W;
        public readonly Float4 VVX = VVX;
        public readonly Float4 VVY = VVY;
        public readonly Float4 VVZ = VVZ;
        public readonly Float4 Denominator = Denominator;
        public readonly Bool IsFrontFace = IsFrontFace;
        public readonly Float3 FloatNormal = FloatNormal;
        public readonly int InterpolationQuality = InterpolationQuality;
        public readonly float Opacity = Opacity;
        public readonly float LightTransmission = LightTransmission;
        public readonly Bool IsAcceptLight = IsAcceptLight;
        public readonly float Ambient = Ambient;
        public readonly float Diffuse = Diffuse;
        public readonly float SpecularIntensity = SpecularIntensity;
        public readonly float SpecularShininess = SpecularShininess;
        public readonly float Metal = Metal;
    }

    readonly record struct GPUPointLight(
        Float4 Position,
        Float4 Color,
        int FalloffType,
        float FalloffStart,
        float FalloffLength,
        Bool IsEnableShadow,
        float ShadowStrength,
        float ShadowScatterSize,
        Float4x4 FrontLightViewMatrix,
        Float4x4 BackLightViewMatrix,
        Float4x4 LeftLightViewMatrix,
        Float4x4 RightLightViewMatrix,
        Float4x4 TopLightViewMatrix,
        Float4x4 BottomLightViewMatrix,
        Float4x4 FaceDetectionMatrix
    )
    {
        public readonly Float4 Position = Position;
        public readonly Float4 Color = Color;
        public readonly int FalloffType = FalloffType;
        public readonly float FalloffStart = FalloffStart;
        public readonly float FalloffLength = FalloffLength;
        public readonly Bool IsEnableShadow = IsEnableShadow;
        public readonly float ShadowStrength = ShadowStrength;
        public readonly float ShadowScatterSize = ShadowScatterSize;
        public readonly Float4x4 FrontLightViewMatrix = FrontLightViewMatrix;
        public readonly Float4x4 BackLightViewMatrix = BackLightViewMatrix;
        public readonly Float4x4 LeftLightViewMatrix = LeftLightViewMatrix;
        public readonly Float4x4 RightLightViewMatrix = RightLightViewMatrix;
        public readonly Float4x4 TopLightViewMatrix = TopLightViewMatrix;
        public readonly Float4x4 BottomLightViewMatrix = BottomLightViewMatrix;
        public readonly Float4x4 FaceDetectionMatrix = FaceDetectionMatrix;
    }

    readonly record struct GPUSpotLight(
        Float4 Position,
        Float3 Direction,
        float ConeAttenuationRate,
        float InnerCone,
        float OuterCone,
        float OuterConeCos,
        float InvertInnerConeCos,
        Float4 Color,
        int FalloffType,
        float FalloffStart,
        float FalloffLength,
        Bool IsEnableShadow,
        float ShadowStrength,
        float ShadowScatterSize,
        Float4x4 LightViewMatrix
    )
    {
        public readonly Float4 Position = Position;
        public readonly Float3 Direction = Direction;
        public readonly float ConeAttenuationRate = ConeAttenuationRate;
        public readonly float InnerCone = InnerCone;
        public readonly float OuterCone = OuterCone;
        public readonly float OuterConeCos = OuterConeCos;
        public readonly float InvertInnerConeCos = InvertInnerConeCos;
        public readonly Float4 Color = Color;
        public readonly int FalloffType = FalloffType;
        public readonly float FalloffStart = FalloffStart;
        public readonly float FalloffLength = FalloffLength;
        public readonly Bool IsEnableShadow = IsEnableShadow;
        public readonly float ShadowStrength = ShadowStrength;
        public readonly float ShadowScatterSize = ShadowScatterSize;
        public readonly Float4x4 LightViewMatrix = LightViewMatrix;
    }

    readonly record struct GPUParallelLight(
        Float4 Position,
        Float3 Direction,
        Float4 Color,
        int FalloffType,
        float FalloffStart,
        float FalloffLength,
        Bool IsEnableShadow,
        float ShadowStrength,
        float ShadowScatterSize,
        Float4x4 LightViewMatrix
    )
    {
        public readonly Float4 Position = Position;
        public readonly Float3 Direction = Direction;
        public readonly Float4 Color = Color;
        public readonly int FalloffType = FalloffType;
        public readonly float FalloffStart = FalloffStart;
        public readonly float FalloffLength = FalloffLength;
        public readonly Bool IsEnableShadow = IsEnableShadow;
        public readonly float ShadowStrength = ShadowStrength;
        public readonly float ShadowScatterSize = ShadowScatterSize;
        public readonly Float4x4 LightViewMatrix = LightViewMatrix;
    }

    readonly record struct GPUAmbientLight(Float4 Color)
    {
        public readonly Float4 Color = Color;
    }
}
