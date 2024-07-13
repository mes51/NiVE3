using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ComputeSharp;
using NiVE3.PresetPlugin.Internal.Drawing;
using NiVE3.PresetPlugin.Internal.Drawing.Primitive3D;

namespace NiVE3.PresetPlugin.Internal.Drawing.ComputeShader.Render3D
{
    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct Rasterize3D(
        ReadWriteBuffer<GPURasterizedPixel> renderImage,
        int renderImageWidth,
        int renderImageOffsetX,
        int renderImageOffsetY,
        float scaleRateX,
        float scaleRateY,
        ReadOnlyBuffer<GPUTriangle> triangles,
        int triangleIndex,
        ReadWriteBuffer<Float4> texture,
        int textureWidth,
        int textureHeight,
        ReadWriteBuffer<float> trackMatte,
        float offsetX,
        float offsetY
    ) : IComputeShader
    {
        const float Epsilon = 1E-7F;

        const float ShininessStrength = 120.0F;

        const float PI = MathF.PI;

        public void Execute()
        {
            var triangle = triangles[triangleIndex];
            var x = ThreadIds.X + triangle.TrueMinX;
            var y = ThreadIds.Y + triangle.TrueMinY;
            if (x < triangle.TrueMinX || x >= triangle.TrueMaxX || y < triangle.TrueMinY || y >= triangle.TrueMaxY)
            {
                return;
            }

            var p = (ThreadIds.Y + triangle.TrueMinY - renderImageOffsetY) * renderImageWidth + ThreadIds.X + triangle.TrueMinX - renderImageOffsetX;

            var eY = new Float4((triangle.EdgeX * ((y + offsetY) * scaleRateY - triangle.VVEY)).XYZ, 0.0F);
            var eX = new Float4(((x + offsetX) * scaleRateX - triangle.VVEX).XYZ, 0.0F);
            var e = (eY - (triangle.EdgeY * eX)) * triangle.Denominator;

            var ae = ShaderUtil.Mask(e, Hlsl.Abs(e) >= Epsilon);
            if (Hlsl.Any(ae < 0.0F))
            {
                return;
            }

            var tw = ShaderUtil.Sum(triangle.W * e);
            var tx = ShaderUtil.Sum(triangle.U * e / tw) * textureWidth;
            var ty = ShaderUtil.Sum(triangle.V * e / tw) * textureHeight;

            var color = triangle.InterpolationQuality == 0 ? NearestNeighbor(tx, ty) : Bilinear(tx, ty);
            color.W *= trackMatte[p % trackMatte.Length] * triangle.Opacity;
            if (color.W <= 0.0F)
            {
                return;
            }

#pragma warning disable IDE0017 // NOTE: ComputeSharpのSourceGeneratorでは非対応
            var result = new GPURasterizedPixel();
#pragma warning restore IDE0017 // オブジェクトの初期化を簡略化します
            result.Color = color;
            result.E = e;
            renderImage[p] = result;
        }

        Float4 NearestNeighbor(float x, float y)
        {
            var ix = (int)x;
            var iy = (int)y;

            if (ix > -1 && iy > -1 && ix < textureWidth && iy < textureHeight)
            {
                return texture[iy * textureWidth + ix];
            }
            else
            {
                return new Float4(1.0F, 1.0F, 1.0F, 0.0F);
            }
        }

        Float4 Bilinear(float x, float y)
        {
            var ix = (int)x;
            var iy = (int)y;

            if (ix == x && iy == y)
            {
                if (ix > -1 && iy > -1 && ix < textureWidth && iy < textureHeight)
                {
                    return texture[iy * textureWidth + ix];
                }
                else
                {
                    return new Float4(1.0F, 1.0F, 1.0F, 0.0F);
                }
            }
            else if (ix < -1 || iy < -1 || ix >= textureWidth || iy >= textureHeight)
            {
                return new Float4(1.0F, 1.0F, 1.0F, 0.0F);
            }

            var pp = x - ix;
            var qq = y - iy;
            var ip = 1.0F - pp;
            var iq = 1.0F - qq;
            var mw = textureWidth - 1;
            var mh = textureHeight - 1;

            var c1 = new Float4(1.0F, 1.0F, 1.0F, 0.0F);
            var c2 = new Float4(1.0F, 1.0F, 1.0F, 0.0F);
            var c3 = new Float4(1.0F, 1.0F, 1.0F, 0.0F);
            var c4 = new Float4(1.0F, 1.0F, 1.0F, 0.0F);
            var pos = iy * textureWidth + ix;

            if (ix > -1)
            {
                if (ix < mw)
                {
                    if (iy > -1)
                    {
                        c1 = texture[pos];
                        c2 = texture[pos + 1];
                        if (iy < mh)
                        {
                            pos += textureWidth;
                            c3 = texture[pos];
                            c4 = texture[pos + 1];
                        }
                    }
                    else
                    {
                        pos += textureWidth;
                        c3 = texture[pos];
                        c4 = texture[pos + 1];
                    }
                }
                else
                {
                    if (iy > -1)
                    {
                        c1 = texture[pos];
                        if (iy < mh)
                        {
                            c3 = texture[pos + textureWidth];
                        }
                    }
                    else
                    {
                        c3 = texture[pos + textureWidth];
                    }
                }
            }
            else
            {
                pos++;
                if (iy > -1)
                {
                    c2 = texture[pos];
                    if (iy < mh)
                    {
                        c4 = texture[pos + textureWidth];
                    }
                }
                else
                {
                    c4 = texture[pos + textureWidth];
                }
            }

            var ta = Hlsl.Lerp(Hlsl.Lerp(c1, c3, qq), Hlsl.Lerp(c2, c4, qq), pp).W;
            if (ta <= 0.0F)
            {
                return new Float4(1.0F, 1.0F, 1.0F, 0.0F);
            }
            else
            {
                var t = Hlsl.Lerp(Hlsl.Lerp(c1 * c1.W, c3 * c3.W, qq), Hlsl.Lerp(c2 * c2.W, c4 * c4.W, qq), pp) / ta;
                t.W = ta;
                return t;
            }
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct LightingByPointLight(
        ReadWriteBuffer<GPURasterizedPixel> rasterizedImage,
        ReadOnlyBuffer<GPUTriangle> triangles,
        int triangleIndex,
        ReadOnlyBuffer<GPUPointLight> pointLights,
        int lightIndex,
        Float4x4 invertedProjectionViewMatrix,
        int shadowMapSize,
        Bool hasShadow,
        ReadWriteBuffer<int> frontShadowMap,
        ReadWriteBuffer<GPUShadowPixel> frontShadowBuffer,
        Float4x4 frontLightViewProjectionMatrix,
        ReadWriteBuffer<int> backShadowMap,
        ReadWriteBuffer<GPUShadowPixel> backShadowBuffer,
        Float4x4 backLightViewProjectionMatrix,
        ReadWriteBuffer<int> leftShadowMap,
        ReadWriteBuffer<GPUShadowPixel> leftShadowBuffer,
        Float4x4 leftLightViewProjectionMatrix,
        ReadWriteBuffer<int> rightShadowMap,
        ReadWriteBuffer<GPUShadowPixel> rightShadowBuffer,
        Float4x4 rightLightViewProjectionMatrix,
        ReadWriteBuffer<int> topShadowMap,
        ReadWriteBuffer<GPUShadowPixel> topShadowBuffer,
        Float4x4 topLightViewProjectionMatrix,
        ReadWriteBuffer<int> bottomShadowMap,
        ReadWriteBuffer<GPUShadowPixel> bottomShadowBuffer,
        Float4x4 bottomLightViewProjectionMatrix,
        Bool enableShadowAntiAlias,
        int width,
        int startX,
        int startY
    ) : IComputeShader
    {
        const float ShininessStrength = 120.0F;

        public void Execute()
        {
            var triangle = triangles[triangleIndex];
            var p = (ThreadIds.Y + startY) * width + ThreadIds.X + startX;
            var rasterizedPixel = rasterizedImage[p];

            var color = rasterizedPixel.Color;
            var a = color.W;
            var l = pointLights[lightIndex];
            var position = ShaderUtil.CalcBarycentricCoord(triangle.VVX, triangle.VVY, triangle.VVZ, rasterizedPixel.E);
            var n = triangle.IsFrontFace ? -triangle.FloatNormal : triangle.FloatNormal;
            var shadowProjectionPos = Float4.Zero;
            if (hasShadow)
            {
                shadowProjectionPos = ShaderUtil.CalcBarycentricCoord(triangle.SVVX, triangle.SVVY, triangle.SVVZ, rasterizedPixel.E);
            }

            if (hasShadow & !triangle.IsAcceptLight & triangle.IsAcceptShadow)
            {
                color *= GetShadowColor(triangle.Id, l.ShadowScatterSize, shadowProjectionPos, invertedProjectionViewMatrix, l.FaceDetectionMatrix);
                color.W = a;
                rasterizedPixel.Color = color;
            }
            else if (triangle.IsAcceptLight)
            {
                var lightColor = l.Color;
                var lightDiff = (position - l.Position).XYZ;
                var light = Hlsl.Normalize(lightDiff);
                var falloff = ShaderUtil.CalcFalloff(lightDiff, l.FalloffType, l.FalloffStart, l.FalloffLength);

                if (triangle.IsAcceptShadow)
                {
                    var transmissionColor = GetShadowColor(triangle.Id, l.ShadowScatterSize, shadowProjectionPos, invertedProjectionViewMatrix, l.FaceDetectionMatrix);
                    if (transmissionColor.W < 1.0F)
                    {
                        if (Hlsl.All(lightColor.XYZ < 0.0F))
                        {
                            return;
                        }
                        lightColor *= transmissionColor;
                    }
                }

                var diffuseFactor = Hlsl.Dot(light, n);
                var isBack = diffuseFactor < 0.0F;
                if (isBack)
                {
                    diffuseFactor *= -triangle.LightTransmission;
                }
                rasterizedPixel.Diffuse += lightColor * color * diffuseFactor * falloff * triangle.Diffuse;

                var view = -Hlsl.Normalize(position.XYZ);
                var halfLE = Hlsl.Normalize(view - light);
                var specularFactor = Hlsl.Max(Hlsl.Dot(-n, halfLE), 0.0F);
                if (isBack)
                {
                    specularFactor *= -triangle.LightTransmission;
                }
                rasterizedPixel.Specular += Hlsl.Lerp(lightColor, color * lightColor, triangle.Metal) * Hlsl.Pow(specularFactor, ShininessStrength * triangle.SpecularShininess) * triangle.SpecularIntensity * falloff;
            }

            rasterizedImage[p] = rasterizedPixel;
        }

        Float4 GetShadowColor(int id, float shadowScatterSize, Float4 shadowProjectionPos, Float4x4 invertedProjectionViewMatrix, Float4x4 faceDetectionMatrix)
        {
            var face = 0;
            var faceDir = (faceDetectionMatrix * (invertedProjectionViewMatrix * shadowProjectionPos));
            var absDir = Hlsl.Abs(faceDir);
            if (absDir.Z >= absDir.X && absDir.Z >= absDir.Y)
            {
                face = faceDir.Z < 0.0F ? 1 : 0;
            }
            else if (absDir.Y >= absDir.X)
            {
                face = faceDir.Y < 0.0F ? 4 : 5;
            }
            else
            {
                face = faceDir.X < 0.0F ? 3 : 2;
            }

            var transmissionColor = Float4.One;
            if (face == 0)
            {
                transmissionColor = GetFrontShadowColor(id, shadowScatterSize, shadowProjectionPos);
            }
            else if (face == 1)
            {
                transmissionColor = GetBackShadowColor(id, shadowScatterSize, shadowProjectionPos);
            }
            else if (face == 2)
            {
                transmissionColor = GetLeftShadowColor(id, shadowScatterSize, shadowProjectionPos);
            }
            else if (face == 3)
            {
                transmissionColor = GetRightShadowColor(id, shadowScatterSize, shadowProjectionPos);
            }
            else if (face == 4)
            {
                transmissionColor = GetBackShadowColor(id, shadowScatterSize, shadowProjectionPos);
            }
            else if (face == 5)
            {
                transmissionColor = GetTopShadowColor(id, shadowScatterSize, shadowProjectionPos);
            }
            else
            {
                transmissionColor = GetBottomShadowColor(id, shadowScatterSize, shadowProjectionPos);
            }

            return transmissionColor;
        }

        Float4 GetFrontShadowColor(int id, float shadowScatterSize, Float4 shadowProjectionPos)
        {
            return Float4.One;
        }

        Float4 GetBackShadowColor(int id, float shadowScatterSize, Float4 shadowProjectionPos)
        {
            return Float4.One;
        }

        Float4 GetLeftShadowColor(int id, float shadowScatterSize, Float4 shadowProjectionPos)
        {
            return Float4.One;
        }

        Float4 GetRightShadowColor(int id, float shadowScatterSize, Float4 shadowProjectionPos)
        {
            return Float4.One;
        }

        Float4 GetTopShadowColor(int id, float shadowScatterSize, Float4 shadowProjectionPos)
        {
            return Float4.One;
        }

        Float4 GetBottomShadowColor(int id, float shadowScatterSize, Float4 shadowProjectionPos)
        {
            return Float4.One;
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct LightingBySpotLight(
        ReadWriteBuffer<GPURasterizedPixel> rasterizedImage,
        ReadOnlyBuffer<GPUTriangle> triangles,
        int triangleIndex,
        ReadOnlyBuffer<GPUSpotLight> spotLights,
        int lightIndex,
        Float4x4 invertedProjectionViewMatrix,
        int shadowMapSize,
        Bool hasShadow,
        ReadWriteBuffer<int> shadowMap,
        ReadWriteBuffer<GPUShadowPixel> shadowBuffer,
        Bool enableShadowAntiAlias,
        int width,
        int startX,
        int startY
    ) : IComputeShader
    {
        const float ShininessStrength = 120.0F;

        const float PI = MathF.PI;

        public void Execute()
        {
            var triangle = triangles[triangleIndex];
            var p = (ThreadIds.Y + startY) * width + ThreadIds.X + startX;
            var rasterizedPixel = rasterizedImage[p];

            var color = rasterizedPixel.Color;
            var a = color.W;
            var l = spotLights[lightIndex];
            var position = ShaderUtil.CalcBarycentricCoord(triangle.VVX, triangle.VVY, triangle.VVZ, rasterizedPixel.E);
            var n = triangle.IsFrontFace ? -triangle.FloatNormal : triangle.FloatNormal;
            var shadowProjectionPos = Float4.Zero;
            if (hasShadow)
            {
                shadowProjectionPos = ShaderUtil.CalcBarycentricCoord(triangle.SVVX, triangle.SVVY, triangle.SVVZ, rasterizedPixel.E);
            }

            if (hasShadow & !triangle.IsAcceptLight & triangle.IsAcceptShadow)
            {
                color *= GetShadowColor(triangle.Id, l.ShadowScatterSize, shadowProjectionPos, invertedProjectionViewMatrix);
                color.W = a;
                rasterizedPixel.Color = color;
            }
            else if (triangle.IsAcceptLight)
            {
                var lightColor = l.Color;
                var lightDiff = (position - l.Position).XYZ;
                var light = Hlsl.Normalize(lightDiff);
                var spotCone = Hlsl.Acos(Hlsl.Dot(l.Direction, light));

                if (spotCone <= l.OuterCone)
                {
                    if (triangle.IsAcceptShadow)
                    {
                        var transmissionColor = GetShadowColor(triangle.Id, l.ShadowScatterSize, shadowProjectionPos, invertedProjectionViewMatrix);
                        if (transmissionColor.W < 1.0F)
                        {
                            if (Hlsl.All(lightColor.XYZ < 0.0F))
                            {
                                return;
                            }
                            lightColor *= transmissionColor;
                        }
                    }

                    var attenuation = 1.0F;
                    if (l.ConeAttenuationRate > 0.0)
                    {
                        attenuation = Hlsl.Cos((1.0F - Hlsl.Min((Hlsl.Cos(spotCone) - l.OuterConeCos) * l.InvertInnerConeCos, 1.0F)) * PI * 0.5F);
                    }

                    var falloff = ShaderUtil.CalcFalloff(lightDiff, l.FalloffType, l.FalloffStart, l.FalloffLength);
                    var diffuseFactor = Hlsl.Dot(light, n);
                    var isBack = diffuseFactor < 0.0F;
                    if (isBack)
                    {
                        diffuseFactor *= -triangle.LightTransmission;
                    }
                    rasterizedPixel.Diffuse += lightColor * color * diffuseFactor * falloff * attenuation * triangle.Diffuse;

                    var view = -Hlsl.Normalize(position.XYZ);
                    var halfLE = Hlsl.Normalize(view - light);
                    var specularFactor = Hlsl.Max(Hlsl.Dot(-n, halfLE), 0.0F);
                    if (isBack)
                    {
                        specularFactor *= -triangle.LightTransmission;
                    }
                    rasterizedPixel.Specular += Hlsl.Lerp(lightColor, color * lightColor, triangle.Metal) * Hlsl.Pow(specularFactor, ShininessStrength * triangle.SpecularShininess) * triangle.SpecularIntensity * falloff * attenuation;
                }
            }

            rasterizedImage[p] = rasterizedPixel;
        }

        Float4 GetShadowColor(int id, float shadowScatterSize, Float4 shadowProjectionPos, Float4x4 invertedProjectionViewMatrix)
        {
            return Float4.One;
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct LightingByParallelLight(
        ReadWriteBuffer<GPURasterizedPixel> rasterizedImage,
        ReadOnlyBuffer<GPUTriangle> triangles,
        int triangleIndex,
        ReadOnlyBuffer<GPUParallelLight> parallelLights,
        int lightIndex,
        Float4x4 invertedProjectionViewMatrix,
        int shadowMapSize,
        Bool hasShadow,
        ReadWriteBuffer<int> shadowMap,
        ReadWriteBuffer<GPUShadowPixel> shadowBuffer,
        Bool enableShadowAntiAlias,
        int width,
        int startX,
        int startY
    ) : IComputeShader
    {
        const float ShininessStrength = 120.0F;

        public void Execute()
        {
            var triangle = triangles[triangleIndex];
            var p = (ThreadIds.Y + startY) * width + ThreadIds.X + startX;
            var rasterizedPixel = rasterizedImage[p];

            var color = rasterizedPixel.Color;
            var a = color.W;
            var l = parallelLights[lightIndex];
            var position = ShaderUtil.CalcBarycentricCoord(triangle.VVX, triangle.VVY, triangle.VVZ, rasterizedPixel.E);
            var n = triangle.IsFrontFace ? -triangle.FloatNormal : triangle.FloatNormal;
            var shadowProjectionPos = Float4.Zero;
            if (hasShadow)
            {
                shadowProjectionPos = ShaderUtil.CalcBarycentricCoord(triangle.SVVX, triangle.SVVY, triangle.SVVZ, rasterizedPixel.E);
            }

            if (hasShadow & !triangle.IsAcceptLight & triangle.IsAcceptShadow)
            {
                color *= GetShadowColor(triangle.Id, l.ShadowScatterSize, shadowProjectionPos, invertedProjectionViewMatrix);
                color.W = a;
                rasterizedPixel.Color = color;
            }
            else if (triangle.IsAcceptLight)
            {
                var lightColor = l.Color;
                var lightDiff = (position - l.Position).XYZ;
                var falloff = ShaderUtil.CalcFalloff(lightDiff, l.FalloffType, l.FalloffStart, l.FalloffLength);

                if (triangle.IsAcceptShadow)
                {
                    var transmissionColor = GetShadowColor(triangle.Id, l.ShadowScatterSize, shadowProjectionPos, invertedProjectionViewMatrix);
                    if (transmissionColor.W < 1.0F)
                    {
                        if (Hlsl.All(lightColor.XYZ < 0.0F))
                        {
                            return;
                        }
                        lightColor *= transmissionColor;
                    }
                }

                var diffuseFactor = Hlsl.Dot(l.Direction, n);
                var isBack = diffuseFactor < 0.0F;
                if (isBack)
                {
                    diffuseFactor *= -triangle.LightTransmission;
                }
                rasterizedPixel.Diffuse += lightColor * color * diffuseFactor * falloff * triangle.Diffuse;

                var view = -Hlsl.Normalize(position.XYZ);
                var halfLE = Hlsl.Normalize(view - l.Direction);
                var specularFactor = Hlsl.Max(Hlsl.Dot(-n, halfLE), 0.0F);
                if (isBack)
                {
                    specularFactor *= -triangle.LightTransmission;
                }
                rasterizedPixel.Specular += Hlsl.Lerp(lightColor, color * lightColor, triangle.Metal) * Hlsl.Pow(specularFactor, ShininessStrength * triangle.SpecularShininess) * triangle.SpecularIntensity * falloff;
            }

            rasterizedImage[p] = rasterizedPixel;
        }

        Float4 GetShadowColor(int id, float shadowScatterSize, Float4 shadowProjectionPos, Float4x4 invertedProjectionViewMatrix)
        {
            return Float4.One;
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct LightingByAmbientLight(
        ReadWriteBuffer<GPURasterizedPixel> rasterizedImage,
        ReadOnlyBuffer<GPUTriangle> triangles,
        int triangleIndex,
        ReadOnlyBuffer<GPUAmbientLight> ambientLights,
        int ambientLightCount,
        int width,
        int startX,
        int startY
    ) : IComputeShader
    {
        public void Execute()
        {
            var ambientRate = triangles[triangleIndex].Ambient;
            var p = (ThreadIds.Y + startY) * width + ThreadIds.X + startX;
            var rasterizedPixel = rasterizedImage[p];
            var ambient = Float4.Zero;

            for (var i = 0; i < ambientLightCount; i++)
            {
                ambient += ambientLights[i].Color * ambientRate;
            }

            rasterizedPixel.Ambient += rasterizedPixel.Color * ambient;
            rasterizedImage[p] = rasterizedPixel;
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct BlendRasterized(ReadWriteBuffer<Float4> renderTarget, ReadWriteBuffer<GPURasterizedPixel> rasterizedImage, Bool useLight, int width, int startX, int startY, int blendMode) : IComputeShader
    {
        public void Execute()
        {
            var p = (ThreadIds.Y + startY) * width + ThreadIds.X + startX;
            var rasterizedPixel = rasterizedImage[p];
            var color = rasterizedPixel.Color;
            if (useLight)
            {
                var a = color.W;
                color = rasterizedPixel.Specular + rasterizedPixel.Diffuse + rasterizedPixel.Ambient;
                color.W = a;
            }
            renderTarget[p] = BlendMethods.Process(blendMode, renderTarget[p], color);
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct AntiAlias(ReadWriteBuffer<Float4> target, ReadWriteBuffer<Float4> interpolate, int width) : IComputeShader
    {
        public void Execute()
        {
            var x = ThreadIds.X;
            var y = ThreadIds.Y;

            if (x == 0 && y == 0)
            {
                target[0] = target[0] * 0.875F + interpolate[0] * 0.125F;
            }
            else if (x == 0)
            {
                var p = y * width;
                target[p] = target[p] * 0.75F + interpolate[p - width] * 0.125F + interpolate[p] * 0.125F;
            }
            else if (y == 0)
            {
                target[x] = target[x] * 0.75F + interpolate[x - 1] * 0.125F + interpolate[x] * 0.125F;
            }
            else
            {
                var p = y * width + x;
                target[p] = target[p] * 0.5F +
                    interpolate[p - 1] * 0.125F +
                    interpolate[p] * 0.125F +
                    interpolate[p - width - 1] * 0.125F +
                    interpolate[p - width] * 0.125F;
            }
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct ClearRasterizedImage(ReadWriteBuffer<GPURasterizedPixel> image, int width, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            var p = (ThreadIds.Y + startY) * width + ThreadIds.X + startX;
            image[p] = new GPURasterizedPixel();
        }
    }
}
