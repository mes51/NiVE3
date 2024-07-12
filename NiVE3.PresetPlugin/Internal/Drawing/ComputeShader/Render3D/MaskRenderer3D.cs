using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ComputeSharp;
using NiVE3.PresetPlugin.Internal.Drawing.Primitive3D;

namespace NiVE3.PresetPlugin.Internal.Drawing.ComputeShader.Render3D
{
    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct RasterizeMask3D(
        ReadWriteBuffer<float> renderImage,
        int renderImageWidth,
        int trackMatteMode,
        int renderImageOffsetX,
        int renderImageOffsetY,
        float scaleRateX,
        float scaleRateY,
        ReadOnlyBuffer<GPUMaskTriangle> triangles,
        int triangleIndex,
        Bool hasLight,
        ReadOnlyBuffer<GPUPointLight> pointLights,
        ReadOnlyBuffer<GPUSpotLight> spotLights,
        ReadOnlyBuffer<GPUParallelLight> parallelLights,
        ReadOnlyBuffer<GPUAmbientLight> ambientLights,
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

        static readonly Float4 ToGrayScale = new Float4(0.114478F, 0.586611F, 0.298912F, 0.0F);

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
            var useLight = hasLight & triangle.IsAcceptLight & (trackMatteMode == 2 || trackMatteMode == 3);

            var eY = new Float4((triangle.EdgeX * ((y + offsetY) * scaleRateY - triangle.VVEY)).XYZ, 0.0F);
            var eX = new Float4(((x + offsetX) * scaleRateX - triangle.VVEX).XYZ, 0.0F);
            var e = (eY - (triangle.EdgeY * eX)) * triangle.Denominator;

            var ae = ShaderUtil.Mask(e, Hlsl.Abs(e) >= Epsilon);
            if (Hlsl.Any(ae < 0.0F))
            {
                return;
            }

            var tw = ShaderUtil.Sum(triangle.W * e);
            var tx = ShaderUtil.Sum((triangle.U * e) / tw) * textureWidth;
            var ty = ShaderUtil.Sum((triangle.V * e) / tw) * textureHeight;

            var color = triangle.InterpolationQuality == 0 ? NearestNeighbor(tx, ty) : Bilinear(tx, ty);
            color.W *= trackMatte[p % trackMatte.Length];
            if (color.W <= 0.0F)
            {
                return;
            }

            if (useLight)
            {
                var alpha = color.W;
                var position = ShaderUtil.CalcBarycentricCoord(triangle.VVX, triangle.VVY, triangle.VVZ, e);
                var n = triangle.IsFrontFace ? -triangle.FloatNormal : triangle.FloatNormal;

                if (triangle.IsAcceptLight)
                {
                    var diffuse = Float4.Zero;
                    var specular = Float4.Zero;
                    var ambient = Float4.Zero;

                    for (var i = 0; i < pointLights.Length; i++)
                    {
                        var l = pointLights[i];
                        var lightColor = l.Color;
                        var lightDiff = (position - l.Position).XYZ;
                        var light = Hlsl.Normalize(lightDiff);
                        var falloff = CalcFalloff(lightDiff, l.FalloffType, l.FalloffStart, l.FalloffLength);

                        var diffuseFactor = Hlsl.Dot(light, n);
                        var isBack = diffuseFactor < 0.0F;
                        if (isBack)
                        {
                            diffuseFactor *= -triangle.LightTransmission;
                        }
                        diffuse += lightColor * color * diffuseFactor * falloff;

                        var view = -Hlsl.Normalize(position.XYZ);
                        var halfLE = Hlsl.Normalize(view - light);
                        var specularFactor = Hlsl.Max(Hlsl.Dot(-n, halfLE), 0.0F);
                        if (isBack)
                        {
                            specularFactor *= -triangle.LightTransmission;
                        }
                        specular += Hlsl.Lerp(lightColor, color * lightColor, triangle.Metal) * Hlsl.Pow(specularFactor, ShininessStrength * triangle.SpecularShininess) * triangle.SpecularIntensity * falloff;
                    }

                    for (var i = 0; i < spotLights.Length; i++)
                    {
                        var l = spotLights[i];
                        var lightColor = l.Color;
                        var lightDiff = (position - l.Position).XYZ;
                        var light = Hlsl.Normalize(lightDiff);
                        var spotCone = Hlsl.Acos(Hlsl.Dot(l.Direction, light));

                        if (spotCone <= l.OuterCone)
                        {
                            var attenuation = 1.0F;
                            if (l.ConeAttenuationRate > 0.0)
                            {
                                attenuation = Hlsl.Cos((1.0F - Hlsl.Min((Hlsl.Cos(spotCone) - l.OuterConeCos) * l.InvertInnerConeCos, 1.0F)) * PI * 0.5F);
                            }

                            var falloff = CalcFalloff(lightDiff, l.FalloffType, l.FalloffStart, l.FalloffLength);
                            var diffuseFactor = Hlsl.Dot(light, n);
                            var isBack = diffuseFactor < 0.0F;
                            if (isBack)
                            {
                                diffuseFactor *= -triangle.LightTransmission;
                            }
                            diffuse += lightColor * color * diffuseFactor * falloff * attenuation;

                            var view = -Hlsl.Normalize(position.XYZ);
                            var halfLE = Hlsl.Normalize(view - light);
                            var specularFactor = Hlsl.Max(Hlsl.Dot(-n, halfLE), 0.0F);
                            if (isBack)
                            {
                                specularFactor *= -triangle.LightTransmission;
                            }
                            specular += Hlsl.Lerp(lightColor, color * lightColor, triangle.Metal) * Hlsl.Pow(specularFactor, ShininessStrength * triangle.SpecularShininess) * triangle.SpecularIntensity * falloff * attenuation;
                        }
                    }

                    for (var i = 0; i < parallelLights.Length; i++)
                    {
                        var l = parallelLights[i];
                        var lightColor = l.Color;
                        var lightDiff = (position - l.Position).XYZ;
                        var falloff = CalcFalloff(lightDiff, l.FalloffType, l.FalloffStart, l.FalloffLength);

                        var diffuseFactor = Hlsl.Dot(l.Direction, n);
                        var isBack = diffuseFactor < 0.0F;
                        if (isBack)
                        {
                            diffuseFactor *= -triangle.LightTransmission;
                        }
                        diffuse += lightColor * color * diffuseFactor * falloff;

                        var view = -Hlsl.Normalize(position.XYZ);
                        var halfLE = Hlsl.Normalize(view - l.Direction);
                        var specularFactor = Hlsl.Max(Hlsl.Dot(-n, halfLE), 0.0F);
                        if (isBack)
                        {
                            specularFactor *= -triangle.LightTransmission;
                        }
                        specular += Hlsl.Lerp(lightColor, color * lightColor, triangle.Metal) * Hlsl.Pow(specularFactor, ShininessStrength * triangle.SpecularShininess) * triangle.SpecularIntensity * falloff;
                    }

                    for (var i = 0; i < ambientLights.Length; i++)
                    {
                        ambient += ambientLights[i].Color * color;
                    }

                    color = diffuse * triangle.Diffuse + specular + ambient * triangle.Ambient;
                    color.W = alpha;
                    color = Hlsl.Max(Hlsl.Min(color, 1.0F), 0.0F);
                }
            }

            var matte = 0.0F;
            switch (trackMatteMode)
            {
                case 0:
                    matte = color.W;
                    break;
                case 1:
                    matte = 1.0F - color.W;
                    break;
                case 2:
                    matte = ShaderUtil.Sum(color * ToGrayScale) * color.W;
                    break;
                case 3:
                    matte = 1.0F - ShaderUtil.Sum(color * ToGrayScale) * color.W;
                    break;
            }
            renderImage[p] = matte * triangle.Opacity;
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

        static float CalcFalloff(Float3 diff, int type, float falloffStart, float falloffLength)
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

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct MaskAntiAlias(ReadWriteBuffer<float> target, ReadWriteBuffer<float> interpolate, int width) : IComputeShader
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
}
