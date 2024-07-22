using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;
using ComputeSharp;
using NiVE3.Image;
using NiVE3.Numerics;
using NiVE3.Plugin.Interfaces;
using NiVE3.PresetPlugin.Extension;
using NiVE3.PresetPlugin.Internal.Drawing.ComputeShader;
using NiVE3.PresetPlugin.Internal.Drawing.ComputeShader.Render3D;
using NiVE3.PresetPlugin.Internal.Drawing.Primitive3D;
using NiVE3.Shared.Extension;

namespace NiVE3.PresetPlugin.Internal.Drawing
{
    class MaskRenderer3D : Renderer3DBase
    {
        ManagedRasterizedMaskImage RenderImage { get; }

        public MaskRenderer3D(ManagedRasterizedMaskImage renderImage, int width, int height, List<PointLight> pointLights, List<SpotLight> spotLights, List<ParallelLight> parallelLights, List<AmbientLight> ambientLights)
            : base(width, height, pointLights, spotLights, parallelLights, ambientLights)
        {
            RenderImage = renderImage;
        }

        public void Render(TrackMatteMode trackMatteMode, bool enableAntiAlias)
        {
            if (trackMatteMode == TrackMatteMode.InvertAlpha || trackMatteMode == TrackMatteMode.InvertLuminance)
            {
                RenderImage.GetDataSpan().Fill(1.0F);
            }

            var renderImageWidth = RenderImage.Width;
            var renderImageHeight = RenderImage.Height;
            var triangles = TriangleDivider.ClipAndDivide(Triangles).ToArray();
            if (triangles.Length < 1)
            {
                return;
            }

            var scaleRateX = Width / (float)renderImageWidth;
            var scaleRateY = Height / (float)renderImageHeight;
            var minZ = triangles.Select(t => Math.Min(Math.Min(t.V1.Vertex.GetElement(2), t.V2.Vertex.GetElement(2)), t.V3.Vertex.GetElement(2))).Min();
            var maxZ = triangles.Select(t => Math.Max(Math.Max(t.V1.Vertex.GetElement(2), t.V2.Vertex.GetElement(2)), t.V3.Vertex.GetElement(2))).Max();
            var projectionMatrix = Matrix4x4d.CreatePerspectiveFieldOfView(FieldOfView, 1.0, minZ, maxZ);

            var offsetX = (Size - Width) * 0.5 / Size;
            var offsetY = (Size - Height) * 0.5 / Size;
            Matrix4x4d.Invert(ViewMatrix, out var invtededViewMatrix);
            Matrix4x4d.Invert(projectionMatrix, out var invertedProjectionMatrix);
            var floatInvtededViewMatrix = (Matrix4x4)(invertedProjectionMatrix * Matrix4x4d.CreateTranslate(-offsetX, -offsetY, 0.0) * invtededViewMatrix);
            var convertedTexture = new Dictionary<NImage, NManagedImage>();
            var convertedTrackMatte = new Dictionary<RasterizedMaskImage, ManagedRasterizedMaskImage>();
            var hasLight = PointLights.Count > 0 || SpotLights.Count > 0 || ParallelLights.Count > 0 || AmbientLights.Count > 0;

            var renderImageOffsetX = (int)(OffsetX / scaleRateX);
            var renderImageOffsetY = (int)(OffsetY / scaleRateY);
            var preProcessedTriangles = new PreProcessedTriangle[triangles.Length];
            for (var i = 0; i < triangles.Length; i++)
            {
                var triangle = triangles[i];
                var uv1 = triangle.V1.Transform(projectionMatrix);
                var uv2 = triangle.V2.Transform(projectionMatrix);
                var uv3 = triangle.V3.Transform(projectionMatrix);
                var textureWidth = triangle.Texture.Width;
                var textureHeight = triangle.Texture.Height;

                var w1 = 1.0 / Math.Abs(uv1.Vertex.GetElement(3));
                var w2 = 1.0 / Math.Abs(uv2.Vertex.GetElement(3));
                var w3 = 1.0 / Math.Abs(uv3.Vertex.GetElement(3));
                uv1 *= w1;
                uv2 *= w2;
                uv3 *= w3;
                var dvv1 = (uv1.Vertex + Vector256.Create(1.0, 1.0, 0.0, 0.0)) * Vector256.Create(Size * 0.5, Size * 0.5, 1.0, 1.0);
                var dvv2 = (uv2.Vertex + Vector256.Create(1.0, 1.0, 0.0, 0.0)) * Vector256.Create(Size * 0.5, Size * 0.5, 1.0, 1.0);
                var dvv3 = (uv3.Vertex + Vector256.Create(1.0, 1.0, 0.0, 0.0)) * Vector256.Create(Size * 0.5, Size * 0.5, 1.0, 1.0);
                var vvX = Vector128.Create((float)triangle.V1.Vertex.GetElement(0), (float)triangle.V2.Vertex.GetElement(0), (float)triangle.V3.Vertex.GetElement(0), 0.0F);
                var vvY = Vector128.Create((float)triangle.V1.Vertex.GetElement(1), (float)triangle.V2.Vertex.GetElement(1), (float)triangle.V3.Vertex.GetElement(1), 0.0F);
                var vvZ = Vector128.Create((float)triangle.V1.Vertex.GetElement(2), (float)triangle.V2.Vertex.GetElement(2), (float)triangle.V3.Vertex.GetElement(2), 0.0F);
                var svvX = Vector128.Create((float)uv1.Vertex.GetElement(0), (float)uv2.Vertex.GetElement(0), (float)uv3.Vertex.GetElement(0), 0.0F);
                var svvY = Vector128.Create((float)uv1.Vertex.GetElement(1), (float)uv2.Vertex.GetElement(1), (float)uv3.Vertex.GetElement(1), 0.0F);
                var svvZ = Vector128.Create((float)uv1.Vertex.GetElement(2), (float)uv2.Vertex.GetElement(2), (float)uv3.Vertex.GetElement(2), 0.0F);
                var minX = (int)(MaxClampedSize((int)(Math.Min(Math.Min(dvv1.GetElement(0), dvv2.GetElement(0)), dvv3.GetElement(0))), OffsetX) / scaleRateX);
                var maxX = (int)(MinClampedSize((int)Math.Ceiling(Math.Max(Math.Max(dvv1.GetElement(0), dvv2.GetElement(0)), dvv3.GetElement(0))), Width + OffsetX) / scaleRateX);
                var minY = (int)(MaxClampedSize((int)(Math.Min(Math.Min(dvv1.GetElement(1), dvv2.GetElement(1)), dvv3.GetElement(1))), OffsetY) / scaleRateY);
                var maxY = (int)(MinClampedSize((int)Math.Ceiling(Math.Max(Math.Max(dvv1.GetElement(1), dvv2.GetElement(1)), dvv3.GetElement(1))), Height + OffsetY) / scaleRateY);
                var u = Vector128.Create((float)uv1.U, (float)uv2.U, (float)uv3.U, 0.0F);
                var v = Vector128.Create((float)uv1.V, (float)uv2.V, (float)uv3.V, 0.0F);
                var w = Vector128.Create((float)w1, (float)w2, (float)w3, 0.0F);

                var denom = Vector128.Create((float)(1.0 / (((dvv2.GetElement(0) - dvv1.GetElement(0)) * (dvv3.GetElement(1) - dvv1.GetElement(1))) - ((dvv2.GetElement(1) - dvv1.GetElement(1)) * (dvv3.GetElement(0) - dvv1.GetElement(0))))));
                var edgeX = Vector128.Create((float)dvv3.GetElement(0), (float)dvv1.GetElement(0), (float)dvv2.GetElement(0), 0.0F) - Vector128.Create((float)dvv2.GetElement(0), (float)dvv3.GetElement(0), (float)dvv1.GetElement(0), 0.0F);
                var edgeY = Vector128.Create((float)dvv3.GetElement(1), (float)dvv1.GetElement(1), (float)dvv2.GetElement(1), 0.0F) - Vector128.Create((float)dvv2.GetElement(1), (float)dvv3.GetElement(1), (float)dvv1.GetElement(1), 0.0F);
                var isFrontFace = Vector256.Dot(triangle.Normal, (triangle.V1.Vertex + triangle.V2.Vertex + triangle.V3.Vertex) / 3.0) <= 0.0;
                var vvEX = Vector128.Create((float)dvv2.GetElement(0), (float)dvv3.GetElement(0), (float)dvv1.GetElement(0), 0.0F);
                var vvEY = Vector128.Create((float)dvv2.GetElement(1), (float)dvv3.GetElement(1), (float)dvv1.GetElement(1), 0.0F);

                NManagedImage managedTexture;
                if (triangle.Texture is NGPUImage gpuImage)
                {
                    if (!convertedTexture.ContainsKey(gpuImage))
                    {
                        convertedTexture.Add(gpuImage, gpuImage.CopyToCpu());
                    }
                    managedTexture = convertedTexture[triangle.Texture];
                }
                else
                {
                    managedTexture = (NManagedImage)triangle.Texture;
                }

                ManagedRasterizedMaskImage? managedTrackMatte;
                if (triangle.TrackMatte is GPURasterizedMaskImage gpuRasterizedMask)
                {
                    if (!convertedTrackMatte.ContainsKey(gpuRasterizedMask))
                    {
                        convertedTrackMatte.Add(gpuRasterizedMask, gpuRasterizedMask.CopyToCpu());
                    }
                    managedTrackMatte = convertedTrackMatte[gpuRasterizedMask];
                }
                else
                {
                    managedTrackMatte = (ManagedRasterizedMaskImage?)triangle.TrackMatte;
                }

                preProcessedTriangles[i] = new PreProcessedTriangle(
                    minX,
                    maxX,
                    minY,
                    maxY,
                    edgeX,
                    edgeY,
                    vvEX,
                    vvEY,
                    u,
                    v,
                    w,
                    vvX,
                    vvY,
                    vvZ,
                    denom,
                    isFrontFace,
                    triangle.FloatNormal,
                    managedTexture,
                    triangle.InterpolationQuality,
                    managedTrackMatte,
                    triangle.Opacity,
                    triangle.LightTransmission,
                    triangle.IsAcceptLight,
                    triangle.Ambient,
                    triangle.Diffuse,
                    triangle.SpecularIntensity,
                    triangle.SpecularShininess,
                    triangle.Metal
                );
            }

            if (enableAntiAlias)
            {
                using var interpolate = (ManagedRasterizedMaskImage)RenderImage.Copy();

                Rasterize(trackMatteMode, RenderImage, preProcessedTriangles, renderImageOffsetX, renderImageOffsetY, scaleRateX, scaleRateY, hasLight, PointLights, SpotLights, ParallelLights, AmbientLights, 0.0F, 0.0F);
                Rasterize(trackMatteMode, interpolate, preProcessedTriangles, renderImageOffsetX, renderImageOffsetY, scaleRateX, scaleRateY, hasLight, PointLights, SpotLights, ParallelLights, AmbientLights, 0.5F, 0.5F);

                var renderImageData = RenderImage.Data;
                var interpolateData = interpolate.Data;
                renderImageData[0] = renderImageData[0] * 0.875F + interpolateData[0] * 0.125F;
                Parallel.For(1, renderImageWidth, x =>
                {
                    renderImageData[x] = renderImageData[x] * 0.75F + interpolateData[x - 1] * 0.125F + interpolateData[x] * 0.125F;
                });
                Parallel.For(1, renderImageHeight, y =>
                {
                    var p = y * renderImageWidth;
                    renderImageData[p] = renderImageData[p] * 0.75F + interpolateData[p - renderImageWidth] * 0.125F + interpolateData[p] * 0.125F;
                });
                Parallel.For(1, renderImageHeight, y =>
                {
                    var renderImageDataSpan = renderImageData.AsSpan(y * renderImageWidth, renderImageWidth);
                    var prevLineInterpolateDataSpan = interpolateData.AsSpan((y - 1) * renderImageWidth, renderImageWidth);
                    var interpolateDataSpan = interpolateData.AsSpan(y * renderImageWidth, renderImageWidth);
                    for (var x = 1; x < renderImageWidth; x++)
                    {
                        renderImageDataSpan[x] = renderImageDataSpan[x] * 0.5F +
                            prevLineInterpolateDataSpan[x - 1] * 0.125F +
                            prevLineInterpolateDataSpan[x] * 0.125F +
                            interpolateDataSpan[x - 1] * 0.125F +
                            interpolateDataSpan[x] * 0.125F;
                    }
                });
            }
            else
            {
                Rasterize(trackMatteMode, RenderImage, preProcessedTriangles, renderImageOffsetX, renderImageOffsetY, scaleRateX, scaleRateY, hasLight, PointLights, SpotLights, ParallelLights, AmbientLights, 0.0F, 0.0F);
            }

            foreach (var (_, i) in convertedTexture)
            {
                i.Dispose();
            }
            foreach (var (_, i) in convertedTrackMatte)
            {
                i.Dispose();
            }
        }

        static void Rasterize(
            TrackMatteMode trackMatteMode,
            ManagedRasterizedMaskImage renderTarget,
            PreProcessedTriangle[] triangles,
            int renderImageOffsetX,
            int renderImageOffsetY,
            float scaleRateX,
            float scaleRateY,
            bool hasLight,
            List<PointLight> pointLightList,
            List<SpotLight> spotLightList,
            List<ParallelLight> parallelLightList,
            List<AmbientLight> ambientLightList,
            float offsetX,
            float offsetY
        )
        {
            var renderImageWidth = renderTarget.Width;

            foreach (var triangle in triangles)
            {
                var useLight = hasLight && triangle.IsAcceptLight && (trackMatteMode == TrackMatteMode.Luminance || trackMatteMode == TrackMatteMode.InvertLuminance);

                Parallel.For(triangle.MinY, triangle.MaxY, y =>
                {
                    var renderImageSpan = renderTarget.GetDataSpan();
                    var trackMatteSpan = (triangle.TrackMatte?.Data ?? EmptyTrackMatte).AsSpan();
                    var texture = triangle.Texture.GetDataSpan();
                    var eY = (triangle.EdgeX * (Vector128.Create(y + offsetY) * scaleRateY - triangle.VVEY)) & Const.WithoutWMask128;

                    var offset = (y - renderImageOffsetY) * renderImageWidth;
                    var p = offset + (triangle.MinX - renderImageOffsetX);

                    var pointLights = CollectionsMarshal.AsSpan(pointLightList);
                    var spotLights = CollectionsMarshal.AsSpan(spotLightList);
                    var parallelLights = CollectionsMarshal.AsSpan(parallelLightList);
                    var ambientLights = CollectionsMarshal.AsSpan(ambientLightList);

                    var maxX = triangle.MaxX;
                    var vvEX = triangle.VVEX;
                    var edgeY = triangle.EdgeY;
                    var denom = triangle.Denominator;
                    var textureWidth = triangle.Texture.Width;
                    var textureHeight = triangle.Texture.Height;
                    var u = triangle.U;
                    var v = triangle.V;
                    var w = triangle.W;
                    var vvX = triangle.VVX;
                    var vvY = triangle.VVY;
                    var vvZ = triangle.VVZ;
                    var isFrontFace = triangle.IsFrontFace;
                    for (var x = triangle.MinX; x < maxX; x++, p++)
                    {
                        var eX = (Vector128.Create(x + offsetX) * scaleRateX - vvEX) & Const.WithoutWMask128;
                        var e = (Fma.IsSupported ? Fma.MultiplyAddNegated(edgeY, eX, eY) : (eY - (edgeY * eX))) * denom;

                        var ae = e & Vector128.GreaterThanOrEqual(Vector128.Abs(e), Vector128.Create(TriangleDivider.Epsilon));
                        if (Vector128.LessThanAny(ae, Vector128<float>.Zero))
                        {
                            continue;
                        }

                        var tw = Vector128.Sum(w * e);
                        var tx = Vector128.Sum((u * e) / tw) * textureWidth;
                        var ty = Vector128.Sum((v * e) / tw) * textureHeight;

                        var color = triangle.InterpolationQuality == ImageInterpolationQuality.Level1 ? ImageInterpolation.NearestNeighbor(texture, textureWidth, textureHeight, tx, ty) : ImageInterpolation.Bilinear(texture, textureWidth, textureHeight, tx, ty);
                        color.W *= trackMatteSpan[p % trackMatteSpan.Length];
                        if (color.W <= 0.0F)
                        {
                            continue;
                        }

                        if (useLight)
                        {
                            var alpha = color.W;
                            var position = CalcBarycentricCoord(vvX, vvY, vvZ, e);
                            var n = isFrontFace ? -triangle.FloatNormal : triangle.FloatNormal;

                            if (triangle.IsAcceptLight)
                            {
                                var diffuse = Vector4.Zero;
                                var specular = Vector4.Zero;
                                var ambient = Vector4.Zero;

                                for (var i = 0; i < pointLights.Length; i++)
                                {
                                    var l = pointLights[i];
                                    var lightColor = l.Color;
                                    var lightDiff = (position - l.Position).AsVector3();
                                    var light = Vector3.Normalize(lightDiff);
                                    var falloff = CalcFalloff(lightDiff, l.FalloffType, l.FalloffStart, l.FalloffLength);

                                    var diffuseFactor = Vector3.Dot(light, n);
                                    var isBack = diffuseFactor < 0.0F;
                                    if (isBack)
                                    {
                                        diffuseFactor *= -triangle.LightTransmission;
                                    }
                                    diffuse += lightColor * color * diffuseFactor * falloff;

                                    var view = -Vector3.Normalize(position.AsVector3());
                                    var halfLE = Vector3.Normalize(view - light);
                                    var specularFactor = Math.Max(Vector3.Dot(-n, halfLE), 0.0F);
                                    if (isBack)
                                    {
                                        specularFactor *= triangle.LightTransmission;
                                    }
                                    specular += Vector4.Lerp(lightColor, color * lightColor, triangle.Metal) * MathF.Pow(specularFactor, ShininessStrength * triangle.SpecularShininess) * triangle.SpecularIntensity * falloff;
                                }

                                for (var i = 0; i < spotLights.Length; i++)
                                {
                                    var l = spotLights[i];
                                    var lightColor = l.Color;
                                    var lightDiff = (position - l.Position).AsVector3();
                                    var light = Vector3.Normalize(lightDiff);
                                    var spotCone = MathF.Acos(Vector3.Dot(l.Direction, light));

                                    if (spotCone <= l.OuterCone)
                                    {
                                        var attenuation = 1.0F;
                                        if (l.ConeAttenuationRate > 0.0)
                                        {
                                            attenuation = MathF.Cos((1.0F - Math.Min((MathF.Cos(spotCone) - l.OuterConeCos) * l.InvertInnerConeCos, 1.0F)) * MathF.PI * 0.5F);
                                        }

                                        var falloff = CalcFalloff(lightDiff, l.FalloffType, l.FalloffStart, l.FalloffLength);
                                        var diffuseFactor = Vector3.Dot(light, n);
                                        var isBack = diffuseFactor < 0.0F;
                                        if (isBack)
                                        {
                                            diffuseFactor *= -triangle.LightTransmission;
                                        }
                                        diffuse += lightColor * color * diffuseFactor * falloff * attenuation;

                                        var view = -Vector3.Normalize(position.AsVector3());
                                        var halfLE = Vector3.Normalize(view - light);
                                        var specularFactor = Math.Max(Vector3.Dot(-n, halfLE), 0.0F);
                                        if (isBack)
                                        {
                                            specularFactor *= triangle.LightTransmission;
                                        }
                                        specular += Vector4.Lerp(lightColor, color * lightColor, triangle.Metal) * MathF.Pow(specularFactor, ShininessStrength * triangle.SpecularShininess) * triangle.SpecularIntensity * falloff * attenuation;
                                    }
                                }

                                for (var i = 0; i < parallelLights.Length; i++)
                                {
                                    var l = parallelLights[i];
                                    var lightColor = l.Color;
                                    var lightDiff = (position - l.Position).AsVector3();
                                    var falloff = CalcFalloff(lightDiff, l.FalloffType, l.FalloffStart, l.FalloffLength);

                                    var diffuseFactor = Vector3.Dot(l.Direction, n);
                                    var isBack = diffuseFactor < 0.0F;
                                    if (isBack)
                                    {
                                        diffuseFactor *= -triangle.LightTransmission;
                                    }
                                    diffuse += lightColor * color * diffuseFactor * falloff;

                                    var view = -Vector3.Normalize(position.AsVector3());
                                    var halfLE = Vector3.Normalize(view - l.Direction);
                                    var specularFactor = Math.Max(Vector3.Dot(-n, halfLE), 0.0F);
                                    if (isBack)
                                    {
                                        specularFactor *= triangle.LightTransmission;
                                    }
                                    specular += Vector4.Lerp(lightColor, color * lightColor, triangle.Metal) * MathF.Pow(specularFactor, ShininessStrength * triangle.SpecularShininess) * triangle.SpecularIntensity * falloff;
                                }

                                for (var i = 0; i < ambientLights.Length; i++)
                                {
                                    ambient += ambientLights[i].Color * color;
                                }

                                color = diffuse * triangle.Diffuse + specular + ambient * triangle.Ambient;
                                color.W = alpha;
                                color = Vector4.Max(Vector4.Min(color, Vector4.One), Vector4.Zero);
                            }
                        }

                        renderImageSpan[p] = trackMatteMode switch
                        {
                            TrackMatteMode.Alpha => color.W,
                            TrackMatteMode.Luminance => (color * Const.ConvertToGrayScale).HorizontalAdd(),
                            TrackMatteMode.InvertAlpha => 1.0F - color.W,
                            TrackMatteMode.InvertLuminance => 1.0F - (color * Const.ConvertToGrayScale).HorizontalAdd(),
                            _ => 0.0F
                        } * triangle.Opacity;
                    }
                });
            }
        }

        private record PreProcessedTriangle(
            int MinX,
            int MaxX,
            int MinY,
            int MaxY,
            Vector128<float> EdgeX,
            Vector128<float> EdgeY,
            Vector128<float> VVEX,
            Vector128<float> VVEY,
            Vector128<float> U,
            Vector128<float> V,
            Vector128<float> W,
            Vector128<float> VVX,
            Vector128<float> VVY,
            Vector128<float> VVZ,
            Vector128<float> Denominator,
            bool IsFrontFace,
            Vector3 FloatNormal,
            NManagedImage Texture,
            ImageInterpolationQuality InterpolationQuality,
            ManagedRasterizedMaskImage? TrackMatte,
            float Opacity,
            float LightTransmission,
            bool IsAcceptLight,
            float Ambient,
            float Diffuse,
            float SpecularIntensity,
            float SpecularShininess,
            float Metal
        );
    }

    class GPUMaskRenderer3D : Renderer3DBase
    {
        GPURasterizedMaskImage RenderImage { get; }

        GraphicsDevice Device { get; }

        public GPUMaskRenderer3D(GPURasterizedMaskImage renderImage, GraphicsDevice device, int width, int height, List<PointLight> pointLights, List<SpotLight> spotLights, List<ParallelLight> parallelLights, List<AmbientLight> ambientLights)
            : base(width, height, pointLights, spotLights, parallelLights, ambientLights)
        {
            RenderImage = renderImage;
            Device = device;
        }

        public void Render(TrackMatteMode trackMatteMode, bool enableAntiAlias)
        {
            if (trackMatteMode == TrackMatteMode.InvertAlpha || trackMatteMode == TrackMatteMode.InvertLuminance)
            {
                using var context = Device.CreateComputeContext();
                context.For(RenderImage.Width, RenderImage.Height, new FillMask(RenderImage.Data, RenderImage.Width, 1.0F));
            }

            var renderImageWidth = RenderImage.Width;
            var renderImageHeight = RenderImage.Height;
            var triangles = TriangleDivider.ClipAndDivide(Triangles).ToArray();
            if (triangles.Length < 1)
            {
                return;
            }

            var scaleRateX = Width / (float)renderImageWidth;
            var scaleRateY = Height / (float)renderImageHeight;
            var minZ = triangles.Select(t => Math.Min(Math.Min(t.V1.Vertex.GetElement(2), t.V2.Vertex.GetElement(2)), t.V3.Vertex.GetElement(2))).Min();
            var maxZ = triangles.Select(t => Math.Max(Math.Max(t.V1.Vertex.GetElement(2), t.V2.Vertex.GetElement(2)), t.V3.Vertex.GetElement(2))).Max();
            var projectionMatrix = Matrix4x4d.CreatePerspectiveFieldOfView(FieldOfView, 1.0, minZ, maxZ);

            var offsetX = (Size - Width) * 0.5 / Size;
            var offsetY = (Size - Height) * 0.5 / Size;
            Matrix4x4d.Invert(ViewMatrix, out var invtededViewMatrix);
            Matrix4x4d.Invert(projectionMatrix, out var invertedProjectionMatrix);
            var floatInvtededViewMatrix = (Matrix4x4)(invertedProjectionMatrix * Matrix4x4d.CreateTranslate(-offsetX, -offsetY, 0.0) * invtededViewMatrix);
            var convertedTexture = new Dictionary<NImage, NGPUImage>();
            var convertedTrackMatte = new Dictionary<RasterizedMaskImage, GPURasterizedMaskImage>();
            var textures = new NGPUImage[triangles.Length];
            var trackMattes = new GPURasterizedMaskImage?[triangles.Length];
            var hasLight = PointLights.Count > 0 || SpotLights.Count > 0 || ParallelLights.Count > 0 || AmbientLights.Count > 0;

            var renderImageOffsetX = (int)(OffsetX / scaleRateX);
            var renderImageOffsetY = (int)(OffsetY / scaleRateY);
            var preProcessedTriangles = new GPUMaskTriangle[triangles.Length];
            var triangleStates = new (int, int, int, int, int)[triangles.Length];
            for (var i = 0; i < triangles.Length; i++)
            {
                var triangle = triangles[i];
                var uv1 = triangle.V1.Transform(projectionMatrix);
                var uv2 = triangle.V2.Transform(projectionMatrix);
                var uv3 = triangle.V3.Transform(projectionMatrix);
                var textureWidth = triangle.Texture.Width;
                var textureHeight = triangle.Texture.Height;

                var w1 = 1.0 / Math.Abs(uv1.Vertex.GetElement(3));
                var w2 = 1.0 / Math.Abs(uv2.Vertex.GetElement(3));
                var w3 = 1.0 / Math.Abs(uv3.Vertex.GetElement(3));
                uv1 *= w1;
                uv2 *= w2;
                uv3 *= w3;
                var dvv1 = (uv1.Vertex + Vector256.Create(1.0, 1.0, 0.0, 0.0)) * Vector256.Create(Size * 0.5, Size * 0.5, 1.0, 1.0);
                var dvv2 = (uv2.Vertex + Vector256.Create(1.0, 1.0, 0.0, 0.0)) * Vector256.Create(Size * 0.5, Size * 0.5, 1.0, 1.0);
                var dvv3 = (uv3.Vertex + Vector256.Create(1.0, 1.0, 0.0, 0.0)) * Vector256.Create(Size * 0.5, Size * 0.5, 1.0, 1.0);
                var vvX = Vector128.Create((float)triangle.V1.Vertex.GetElement(0), (float)triangle.V2.Vertex.GetElement(0), (float)triangle.V3.Vertex.GetElement(0), 0.0F);
                var vvY = Vector128.Create((float)triangle.V1.Vertex.GetElement(1), (float)triangle.V2.Vertex.GetElement(1), (float)triangle.V3.Vertex.GetElement(1), 0.0F);
                var vvZ = Vector128.Create((float)triangle.V1.Vertex.GetElement(2), (float)triangle.V2.Vertex.GetElement(2), (float)triangle.V3.Vertex.GetElement(2), 0.0F);
                var svvX = Vector128.Create((float)uv1.Vertex.GetElement(0), (float)uv2.Vertex.GetElement(0), (float)uv3.Vertex.GetElement(0), 0.0F);
                var svvY = Vector128.Create((float)uv1.Vertex.GetElement(1), (float)uv2.Vertex.GetElement(1), (float)uv3.Vertex.GetElement(1), 0.0F);
                var svvZ = Vector128.Create((float)uv1.Vertex.GetElement(2), (float)uv2.Vertex.GetElement(2), (float)uv3.Vertex.GetElement(2), 0.0F);
                var minX = (int)(MaxClampedSize((int)(Math.Min(Math.Min(dvv1.GetElement(0), dvv2.GetElement(0)), dvv3.GetElement(0))), OffsetX) / scaleRateX);
                var maxX = (int)(MinClampedSize((int)Math.Ceiling(Math.Max(Math.Max(dvv1.GetElement(0), dvv2.GetElement(0)), dvv3.GetElement(0))), Width + OffsetX) / scaleRateX);
                var minY = (int)(MaxClampedSize((int)(Math.Min(Math.Min(dvv1.GetElement(1), dvv2.GetElement(1)), dvv3.GetElement(1))), OffsetY) / scaleRateY);
                var maxY = (int)(MinClampedSize((int)Math.Ceiling(Math.Max(Math.Max(dvv1.GetElement(1), dvv2.GetElement(1)), dvv3.GetElement(1))), Height + OffsetY) / scaleRateY);
                var u = Vector128.Create((float)uv1.U, (float)uv2.U, (float)uv3.U, 0.0F);
                var v = Vector128.Create((float)uv1.V, (float)uv2.V, (float)uv3.V, 0.0F);
                var w = Vector128.Create((float)w1, (float)w2, (float)w3, 0.0F);

                var denom = Vector128.Create((float)(1.0 / (((dvv2.GetElement(0) - dvv1.GetElement(0)) * (dvv3.GetElement(1) - dvv1.GetElement(1))) - ((dvv2.GetElement(1) - dvv1.GetElement(1)) * (dvv3.GetElement(0) - dvv1.GetElement(0))))));
                var edgeX = Vector128.Create((float)dvv3.GetElement(0), (float)dvv1.GetElement(0), (float)dvv2.GetElement(0), 0.0F) - Vector128.Create((float)dvv2.GetElement(0), (float)dvv3.GetElement(0), (float)dvv1.GetElement(0), 0.0F);
                var edgeY = Vector128.Create((float)dvv3.GetElement(1), (float)dvv1.GetElement(1), (float)dvv2.GetElement(1), 0.0F) - Vector128.Create((float)dvv2.GetElement(1), (float)dvv3.GetElement(1), (float)dvv1.GetElement(1), 0.0F);
                var isFrontFace = Vector256.Dot(triangle.Normal, (triangle.V1.Vertex + triangle.V2.Vertex + triangle.V3.Vertex) / 3.0) <= 0.0;
                var vvEX = Vector128.Create((float)dvv2.GetElement(0), (float)dvv3.GetElement(0), (float)dvv1.GetElement(0), 0.0F);
                var vvEY = Vector128.Create((float)dvv2.GetElement(1), (float)dvv3.GetElement(1), (float)dvv1.GetElement(1), 0.0F);

                NGPUImage gpuTexture;
                if (triangle.Texture is NManagedImage managedTexture)
                {
                    if (!convertedTexture.ContainsKey(managedTexture))
                    {
                        convertedTexture.Add(managedTexture, managedTexture.CopyToGpu(Device));
                    }
                    gpuTexture = convertedTexture[managedTexture];
                }
                else
                {
                    gpuTexture = (NGPUImage)triangle.Texture;
                }
                textures[i] = gpuTexture;

                GPURasterizedMaskImage? gpuTrackMatte;
                if (triangle.TrackMatte is ManagedRasterizedMaskImage managedRasterizedMask)
                {
                    if (!convertedTrackMatte.ContainsKey(managedRasterizedMask))
                    {
                        convertedTrackMatte.Add(managedRasterizedMask, managedRasterizedMask.CopyToGpu(Device));
                    }
                    gpuTrackMatte = convertedTrackMatte[managedRasterizedMask];
                }
                else
                {
                    gpuTrackMatte = (GPURasterizedMaskImage?)triangle.TrackMatte;
                }
                trackMattes[i] = gpuTrackMatte;

                preProcessedTriangles[i] = new GPUMaskTriangle(
                    minX,
                    maxX,
                    minY,
                    maxY,
                    edgeX.AsFloat4(),
                    edgeY.AsFloat4(),
                    vvEX.AsFloat4(),
                    vvEY.AsFloat4(),
                    u.AsFloat4(),
                    v.AsFloat4(),
                    w.AsFloat4(),
                    vvX.AsFloat4(),
                    vvY.AsFloat4(),
                    vvZ.AsFloat4(),
                    denom.AsFloat4(),
                    isFrontFace,
                    triangle.FloatNormal,
                    (int)triangle.InterpolationQuality,
                    triangle.Opacity,
                    triangle.LightTransmission,
                    triangle.IsAcceptLight,
                    triangle.Ambient,
                    triangle.Diffuse,
                    triangle.SpecularIntensity,
                    triangle.SpecularShininess,
                    triangle.Metal
                );
                triangleStates[i] = (triangle.Id, preProcessedTriangles[i].TrueMinX, preProcessedTriangles[i].TrueMaxX, preProcessedTriangles[i].TrueMinY, preProcessedTriangles[i].TrueMaxY);
            }

            var ambientLightColor = AmbientLights.Aggregate(Vector4.Zero, (m, a) => m + a.Color);

            using (var triangleBuffer = Device.AllocateReadOnlyBuffer(preProcessedTriangles))
            using (var pointLightBuffer = PointLights.Count > 0 ? Device.AllocateReadOnlyBuffer([..PointLights.Select(p => p.ToGpu())]) : Device.AllocateReadOnlyBuffer<GPUPointLight>(1))
            using (var spotLightBuffer = SpotLights.Count > 0 ? Device.AllocateReadOnlyBuffer([..SpotLights.Select(s => s.ToGpu())]) : Device.AllocateReadOnlyBuffer<GPUSpotLight>(1))
            using (var parallelLightBuffer = ParallelLights.Count > 0 ? Device.AllocateReadOnlyBuffer([..ParallelLights.Select(p => p.ToGpu())]) : Device.AllocateReadOnlyBuffer<GPUParallelLight>(1))
            {
                if (enableAntiAlias)
                {
                    using var interpolate = new GPURasterizedMaskImage(RenderImage.Width, RenderImage.Height, Device);
                    RenderImage.CopyTo(interpolate);

                    Rasterize(
                        Device,
                        trackMatteMode,
                        RenderImage,
                        triangleBuffer,
                        triangleStates,
                        textures,
                        trackMattes,
                        renderImageOffsetX,
                        renderImageOffsetY,
                        scaleRateX,
                        scaleRateY,
                        hasLight,
                        PointLights.Count,
                        pointLightBuffer,
                        SpotLights.Count,
                        spotLightBuffer,
                        ParallelLights.Count,
                        parallelLightBuffer,
                        ambientLightColor,
                        0.0F,
                        0.0F
                    );
                    Rasterize(
                        Device,
                        trackMatteMode,
                        RenderImage,
                        triangleBuffer,
                        triangleStates,
                        textures,
                        trackMattes,
                        renderImageOffsetX,
                        renderImageOffsetY,
                        scaleRateX,
                        scaleRateY,
                        hasLight,
                        PointLights.Count,
                        pointLightBuffer,
                        SpotLights.Count,
                        spotLightBuffer,
                        ParallelLights.Count,
                        parallelLightBuffer,
                        ambientLightColor,
                        0.5F,
                        0.5F
                    );

                    using var context = Device.CreateComputeContext();
                    context.For(RenderImage.Width, RenderImage.Height, new MaskAntiAlias(RenderImage.Data, interpolate.Data, RenderImage.Width));
                }
                else
                {
                    Rasterize(
                        Device,
                        trackMatteMode,
                        RenderImage,
                        triangleBuffer,
                        triangleStates,
                        textures,
                        trackMattes,
                        renderImageOffsetX,
                        renderImageOffsetY,
                        scaleRateX,
                        scaleRateY,
                        hasLight,
                        PointLights.Count,
                        pointLightBuffer,
                        SpotLights.Count,
                        spotLightBuffer,
                        ParallelLights.Count,
                        parallelLightBuffer,
                        ambientLightColor,
                        0.0F,
                        0.0F
                    );
                }
            }

            foreach (var (_, i) in convertedTexture)
            {
                i.Dispose();
            }
            foreach (var (_, i) in convertedTrackMatte)
            {
                i.Dispose();
            }
        }

        static void Rasterize(
            GraphicsDevice device,
            TrackMatteMode trackMatteMode,
            GPURasterizedMaskImage renderTarget,
            ReadOnlyBuffer<GPUMaskTriangle> triangleBuffer,
            (int, int, int, int, int)[] triangleState,
            NGPUImage[] textures,
            GPURasterizedMaskImage?[] trackMattes,
            int renderImageOffsetX,
            int renderImageOffsetY,
            float scaleRateX,
            float scaleRateY,
            bool hasLight,
            int pointLightCount,
            ReadOnlyBuffer<GPUPointLight> pointLightBuffer,
            int spotLightCount,
            ReadOnlyBuffer<GPUSpotLight> spotLightBuffer,
            int parallelLightCount,
            ReadOnlyBuffer<GPUParallelLight> parallelLightBuffer,
            Float4 ambientLightColor,
            float offsetX,
            float offsetY
        )
        {
            using var emptyTrackMatte = device.AllocateReadWriteBuffer(EmptyTrackMatte);

            foreach (var groupedTriangleIds in triangleState.ZipWithIndex().GroupByPrev(t => t.Item1))
            {
                using var context = device.CreateComputeContext();
                foreach (var ((_, minX, maxX, minY, maxY), i) in groupedTriangleIds)
                {
                    if (minX - renderImageOffsetX >= renderTarget.Width || maxX - renderImageOffsetX <= 0 || minY - renderImageOffsetY >= renderTarget.Height || maxY - renderImageOffsetY <= 0)
                    {
                        continue;
                    }
                    var texture = textures[i];
                    context.For(
                        maxX - minX,
                        maxY - minY,
                        new RasterizeMask3D(
                            renderTarget.Data,
                            renderTarget.Width,
                            (int)trackMatteMode,
                            renderImageOffsetX,
                            renderImageOffsetY,
                            scaleRateX,
                            scaleRateY,
                            triangleBuffer,
                            i,
                            hasLight,
                            pointLightCount,
                            pointLightBuffer,
                            spotLightCount,
                            spotLightBuffer,
                            parallelLightCount,
                            parallelLightBuffer,
                            ambientLightColor,
                            texture.Data,
                            texture.Width,
                            texture.Height,
                            trackMattes[i]?.Data ?? emptyTrackMatte,
                            offsetX,
                            offsetY
                        )
                    );
                }
            }
        }
    }
}
