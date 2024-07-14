using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Image;
using NiVE3.Numerics;
using NiVE3.PresetPlugin.Internal.Drawing.Primitive3D;
using NiVE3.Plugin.Interfaces;
using NiVE3.Shared.Extension;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Buffers;
using NiVE3.Image.Drawing;
using ComputeSharp;
using NiVE3.PresetPlugin.Internal.Extension;
using NiVE3.PresetPlugin.Internal.Drawing.ComputeShader.Render3D;
using NiVE3.PresetPlugin.Internal.Drawing.ComputeShader;

namespace NiVE3.PresetPlugin.Internal.Drawing
{
    class CPURenderer3D : Renderer3DBase
    {
        NManagedImage RenderImage { get; }

        public CPURenderer3D(NManagedImage renderImage, int width, int height, List<PointLight> pointLights, List<SpotLight> spotLights, List<ParallelLight> parallelLights, List<AmbientLight> ambientLights)
            : base(width, height, pointLights, spotLights, parallelLights, ambientLights)
        {
            RenderImage = renderImage;
        }

        public void Render(bool enableAntiAlias, bool enableShadowAntiAlias)
        {
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
            Matrix4x4d.Invert(ViewMatrix, out var invertedViewMatrix);
            Matrix4x4d.Invert(projectionMatrix, out var invertedProjectionMatrix);
            var invertedProjectionViewMatrix = (Matrix4x4)(invertedProjectionMatrix * Matrix4x4d.CreateTranslate(-offsetX, -offsetY, 0.0) * invertedViewMatrix);
            var convertedTexture = new Dictionary<NImage, NManagedImage>();
            var convertedTrackMatte = new Dictionary<RasterizedMaskImage, ManagedRasterizedMaskImage>();
            var hasLight = PointLights.Count > 0 || SpotLights.Count > 0 || ParallelLights.Count > 0 || AmbientLights.Count > 0;

            var shadowBuffer = new ShadowBuffer();
            var shadowSize = Size == Width ? (int)(Size / scaleRateX) : (int)(Size / scaleRateY);
            var pointLightShadows = PointLights.Select(l => l.IsEnableShadow ? RenderPointLightShadow(l, shadowBuffer, shadowSize, (float)offsetX, (float)offsetY) : null).ToArray();
            var spotLightShadows = SpotLights.Select(l => l.IsEnableShadow && LightTriangles[l].Count > 0 ? RenderSpotLightShadow(l, shadowBuffer, shadowSize, (float)offsetX, (float)offsetY) : null).ToArray();
            var parallelLightShadows = ParallelLights.Select(l => l.IsEnableShadow && LightTriangles[l].Count > 0 ? RenderParallelLightShadow(l, shadowBuffer, shadowSize, (float)offsetX, (float)offsetY) : null).ToArray();
            var hasShadow = pointLightShadows.Any(ss => ss != null && ss.Any(s => s != null)) || spotLightShadows.Any(s => s != null) || parallelLightShadows.Any(s => s != null);

            var renderImageOffsetX = (int)(OffsetX / scaleRateX);
            var renderImageOffsetY = (int)(OffsetY / scaleRateY);

            var preProcessedTriangle = new PreProcessedTriangle[triangles.Length];
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

                preProcessedTriangle[i] = new PreProcessedTriangle(
                    triangle.Id,
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
                    svvX,
                    svvY,
                    svvZ,
                    denom,
                    isFrontFace,
                    triangle.FloatNormal,
                    managedTexture,
                    triangle.InterpolationQuality,
                    managedTrackMatte,
                    triangle.Opacity,
                    triangle.LightTransmission,
                    triangle.BlendMode,
                    triangle.IsAcceptShadow,
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
                using var interpolate = (NManagedImage)RenderImage.Copy();
                Rasterize(
                    RenderImage,
                    preProcessedTriangle,
                    renderImageOffsetX,
                    renderImageOffsetY,
                    scaleRateX,
                    scaleRateY,
                    invertedProjectionViewMatrix,
                    hasLight,
                    PointLights,
                    SpotLights,
                    ParallelLights,
                    AmbientLights,
                    hasShadow,
                    pointLightShadows,
                    spotLightShadows,
                    parallelLightShadows,
                    enableShadowAntiAlias,
                    0.0F,
                    0.0F
                );
                Rasterize(
                    interpolate,
                    preProcessedTriangle,
                    renderImageOffsetX,
                    renderImageOffsetY,
                    scaleRateX,
                    scaleRateY,
                    invertedProjectionViewMatrix,
                    hasLight,
                    PointLights,
                    SpotLights,
                    ParallelLights,
                    AmbientLights,
                    hasShadow,
                    pointLightShadows,
                    spotLightShadows,
                    parallelLightShadows,
                    enableShadowAntiAlias,
                    0.5F,
                    0.5F
                );

                var renderImageData = RenderImage.Data;
                var interpolateData = interpolate.Data;
                var firstTa = renderImageData[0].W * 0.875F + interpolateData[0].W * 0.125F;
                if (firstTa > 0.0F)
                {
                    var firstPixel = (renderImageData[0] * renderImageData[0].W * 0.875F + interpolateData[0] * interpolateData[0].W * 0.125F) / firstTa;
                    firstPixel.W = firstTa;
                    renderImageData[0] = firstPixel;
                }

                Parallel.For(1, renderImageWidth, x =>
                {
                    var i1 = interpolateData[x - 1];
                    var i2 = interpolateData[x];
                    var targetPixel = renderImageData[x];
                    var ta = i1.W * 0.125F + i2.W * 0.125F + targetPixel.W * 0.75F;
                    if (ta > 0.0F)
                    {
                        targetPixel = (targetPixel * targetPixel.W * 0.75F + i1 * i1.W * 0.125F + i2 * i2.W * 0.125F) / ta;
                        targetPixel.W = ta;
                        renderImageData[x] = targetPixel;
                    }
                });
                Parallel.For(1, renderImageHeight, y =>
                {
                    var p = y * renderImageWidth;
                    var i1 = interpolateData[p - renderImageWidth];
                    var i2 = interpolateData[p];
                    var targetPixel = renderImageData[p];
                    var ta = i1.W * 0.125F + i2.W * 0.125F + targetPixel.W * 0.75F;
                    if (ta > 0.0F)
                    {
                        targetPixel = (targetPixel * targetPixel.W * 0.75F + i1 * i1.W * 0.125F + i2 * i2.W * 0.125F) / ta;
                        targetPixel.W = ta;
                        renderImageData[p] = targetPixel;
                    }
                });
                Parallel.For(1, renderImageHeight, y =>
                {
                    var renderImageDataSpan = renderImageData.AsSpan(y * renderImageWidth, renderImageWidth);
                    var prevLineInterpolateDataSpan = interpolateData.AsSpan((y - 1) * renderImageWidth, renderImageWidth);
                    var interpolateDataSpan = interpolateData.AsSpan(y * renderImageWidth, renderImageWidth);
                    for (var x = 1; x < renderImageWidth; x++)
                    {
                        var i1 = prevLineInterpolateDataSpan[x - 1];
                        var i2 = prevLineInterpolateDataSpan[x];
                        var i3 = interpolateDataSpan[x - 1];
                        var i4 = interpolateDataSpan[x];
                        var targetPixel = renderImageDataSpan[x];
                        var ta = targetPixel.W * 0.5F + i1.W * 0.125F + i2.W * 0.125F + i3.W * 0.125F + i4.W * 0.125F;
                        if (ta > 0.0F)
                        {
                            targetPixel = (targetPixel * targetPixel.W * 0.5F + i1 * i1.W * 0.125F + i2 * i2.W * 0.125F + i3 * i3.W * 0.125F + i4 * i4.W * 0.125F) / ta;
                            targetPixel.W = ta;
                            renderImageDataSpan[x] = targetPixel;
                        }
                    }
                });
            }
            else
            {
                Rasterize(
                    RenderImage,
                    preProcessedTriangle,
                    renderImageOffsetX,
                    renderImageOffsetY,
                    scaleRateX,
                    scaleRateY,
                    invertedProjectionViewMatrix,
                    hasLight,
                    PointLights,
                    SpotLights,
                    ParallelLights,
                    AmbientLights,
                    hasShadow,
                    pointLightShadows,
                    spotLightShadows,
                    parallelLightShadows,
                    enableShadowAntiAlias,
                    0.0F,
                    0.0F
                );
            }

            foreach (var (_, i) in convertedTexture)
            {
                i.Dispose();
            }
            foreach (var (_, i) in convertedTrackMatte)
            {
                i.Dispose();
            }

            foreach (var ss in pointLightShadows)
            {
                if (ss != null)
                {
                    foreach (var s in ss)
                    {
                        s?.Dispose();
                    }
                }
            }
            foreach (var s in spotLightShadows)
            {
                s?.Dispose();
            }
            foreach (var s in parallelLightShadows)
            {
                s?.Dispose();
            }
        }

        static void Rasterize(
            NManagedImage renderTarget,
            PreProcessedTriangle[] triangles,
            int renderImageOffsetX,
            int renderImageOffsetY,
            float scaleRateX,
            float scaleRateY,
            Matrix4x4 invertedProjectionViewMatrix,
            bool hasLight,
            List<PointLight> pointLightList,
            List<SpotLight> spotLightList,
            List<ParallelLight> parallelLightList,
            List<AmbientLight> ambientLightList,
            bool hasShadow,
            ShadowMap?[]?[] pointLightShadows,
            ShadowMap?[] spotLightShadows,
            ShadowMap?[] parallelLightShadows,
            bool enableShadowAntiAlias,
            float offsetX,
            float offsetY
        )
        {
            var renderImageWidth = renderTarget.Width;

            foreach (var triangle in triangles)
            {
                var useLight = hasLight && (triangle.IsAcceptLight || triangle.IsAcceptShadow);

                Parallel.For(triangle.MinY, triangle.MaxY, y =>
                {
                    var renderImageSpan = renderTarget.GetDataSpan();
                    var trackMatteSpan = (triangle.TrackMatte?.Data ?? EmptyTrackMatte).AsSpan();
                    var texture = triangle.Texture.GetDataSpan();
                    var eY = (triangle.EdgeX * (Vector128.Create(y + offsetY) * scaleRateY - triangle.VVEY)) & Const.WithoutWMask128;

                    var pointLights = CollectionsMarshal.AsSpan(pointLightList);
                    var spotLights = CollectionsMarshal.AsSpan(spotLightList);
                    var parallelLights = CollectionsMarshal.AsSpan(parallelLightList);
                    var ambientLights = CollectionsMarshal.AsSpan(ambientLightList);

                    var offset = (y - renderImageOffsetY) * renderImageWidth;
                    var p = offset + (triangle.MinX - renderImageOffsetX);

                    var id = triangle.Id;
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
                    var svvX = triangle.SVVX;
                    var svvY = triangle.SVVY;
                    var svvZ = triangle.SVVZ;
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
                        var tx = Vector128.Sum(u * e / tw) * textureWidth;
                        var ty = Vector128.Sum(v * e / tw) * textureHeight;

                        var color = triangle.InterpolationQuality == ImageInterpolationQuality.Level1 ? ImageInterpolation.NearestNeighbor(texture, textureWidth, textureHeight, tx, ty) : ImageInterpolation.Bilinear(texture, textureWidth, textureHeight, tx, ty);
                        color.W *= triangle.Opacity * trackMatteSpan[p % trackMatteSpan.Length];
                        if (color.W <= 0.0F)
                        {
                            continue;
                        }

                        if (useLight)
                        {
                            var alpha = color.W;
                            var position = CalcBarycentricCoord(vvX, vvY, vvZ, e);
                            var n = isFrontFace ? -triangle.FloatNormal : triangle.FloatNormal;
                            var shadowProjectionPos = Vector4.Zero;
                            if (hasShadow)
                            {
                                shadowProjectionPos = CalcBarycentricCoord(svvX, svvY, svvZ, e).AsVector4();
                            }

                            if (hasShadow && !triangle.IsAcceptLight && triangle.IsAcceptShadow)
                            {
                                for (var i = 0; i < pointLights.Length; i++)
                                {
                                    var l = pointLights[i];
                                    var shadows = pointLightShadows[i];
                                    if (shadows == null)
                                    {
                                        continue;
                                    }

                                    var face = PointLightShadowDirection.Front;
                                    var faceDir = Vector4.Transform(Vector4.Transform(shadowProjectionPos, invertedProjectionViewMatrix), l.FaceDetectionMatrix);
                                    var absDir = Vector4.Abs(faceDir);
                                    if (absDir.Z >= absDir.X && absDir.Z >= absDir.Y)
                                    {
                                        face = faceDir.Z < 0.0F ? PointLightShadowDirection.Back : PointLightShadowDirection.Front;
                                    }
                                    else if (absDir.Y >= absDir.X)
                                    {
                                        face = faceDir.Y < 0.0F ? PointLightShadowDirection.Top : PointLightShadowDirection.Bottom;
                                    }
                                    else
                                    {
                                        face = faceDir.X < 0.0F ? PointLightShadowDirection.Right : PointLightShadowDirection.Left;
                                    }

                                    var shadow = shadows[(int)face];
                                    if (shadow != null)
                                    {
                                        var transmissionColor = GetShadowColor(id, shadow, l.ShadowScatterSize, enableShadowAntiAlias, shadowProjectionPos, invertedProjectionViewMatrix, shadow.LightViewProjectionMatrix);
                                        color *= transmissionColor;
                                    }
                                }

                                for (var i = 0; i < spotLights.Length; i++)
                                {
                                    var l = spotLights[i];
                                    var lightDiff = (position - l.Position).AsVector3();
                                    var light = Vector3.Normalize(lightDiff);
                                    var spotCone = MathF.Acos(Vector3.Dot(l.Direction, light));

                                    if (spotCone <= l.OuterCone)
                                    {
                                        var shadow = spotLightShadows[i];
                                        if (shadow == null)
                                        {
                                            continue;
                                        }

                                        var attenuation = 1.0F;
                                        if (l.ConeAttenuationRate > 0.0)
                                        {
                                            attenuation = MathF.Cos((1.0F - Math.Min((MathF.Cos(spotCone) - l.OuterConeCos) * l.InvertInnerConeCos, 1.0F)) * MathF.PI * 0.5F);
                                        }
                                        var transmissionColor = GetShadowColor(id, shadow, l.ShadowScatterSize, enableShadowAntiAlias, shadowProjectionPos, invertedProjectionViewMatrix, shadow.LightViewProjectionMatrix);
                                        color *= Vector4.Lerp(Vector4.One, transmissionColor, attenuation);
                                    }
                                }

                                for (var i = 0; i < parallelLights.Length; i++)
                                {
                                    var l = parallelLights[i];
                                    var shadow = parallelLightShadows[i];
                                    if (shadow == null)
                                    {
                                        continue;
                                    }

                                    var transmissionColor = GetShadowColor(id, shadow, l.ShadowScatterSize, enableShadowAntiAlias, shadowProjectionPos, invertedProjectionViewMatrix, shadow.LightViewProjectionMatrix);
                                    color *= transmissionColor;
                                }

                                color.W = alpha;
                            }
                            else if (triangle.IsAcceptLight)
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
                                    var shadows = pointLightShadows[i];
                                    if (triangle.IsAcceptShadow && shadows != null)
                                    {
                                        var face = PointLightShadowDirection.Front;
                                        var faceDir = Vector4.Transform(Vector4.Transform(shadowProjectionPos, invertedProjectionViewMatrix), l.FaceDetectionMatrix);
                                        var absDir = Vector4.Abs(faceDir);
                                        if (absDir.Z >= absDir.X && absDir.Z >= absDir.Y)
                                        {
                                            face = faceDir.Z < 0.0F ? PointLightShadowDirection.Back : PointLightShadowDirection.Front;
                                        }
                                        else if (absDir.Y >= absDir.X)
                                        {
                                            face = faceDir.Y < 0.0F ? PointLightShadowDirection.Top : PointLightShadowDirection.Bottom;
                                        }
                                        else
                                        {
                                            face = faceDir.X < 0.0F ? PointLightShadowDirection.Right : PointLightShadowDirection.Left;
                                        }

                                        var shadow = shadows[(int)face];
                                        if (shadow != null)
                                        {
                                            var transmissionColor = GetShadowColor(id, shadow, l.ShadowScatterSize, enableShadowAntiAlias, shadowProjectionPos, invertedProjectionViewMatrix, shadow.LightViewProjectionMatrix);
                                            if (!lightColor.CompareGreaterThanBy3Element(Vector3.Zero))
                                            {
                                                continue;
                                            }
                                            lightColor *= transmissionColor;
                                        }
                                    }

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
                                        specularFactor *= -triangle.LightTransmission;
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
                                        var shadow = spotLightShadows[i];
                                        if (triangle.IsAcceptShadow && shadow != null)
                                        {
                                            var transmissionColor = GetShadowColor(id, shadow, l.ShadowScatterSize, enableShadowAntiAlias, shadowProjectionPos, invertedProjectionViewMatrix, shadow.LightViewProjectionMatrix);
                                            if (!transmissionColor.CompareGreaterThanBy3Element(Vector3.Zero))
                                            {
                                                continue;
                                            }
                                            lightColor *= transmissionColor;
                                        }

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
                                            specularFactor *= -triangle.LightTransmission;
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

                                    var shadow = parallelLightShadows[i];
                                    if (triangle.IsAcceptShadow && shadow != null)
                                    {
                                        var transmissionColor = GetShadowColor(id, shadow, l.ShadowScatterSize, enableShadowAntiAlias, shadowProjectionPos, invertedProjectionViewMatrix, shadow.LightViewProjectionMatrix);
                                        if (!transmissionColor.CompareGreaterThanBy3Element(Vector3.Zero))
                                        {
                                            continue;
                                        }
                                        lightColor *= transmissionColor;
                                    }

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
                                        specularFactor *= -triangle.LightTransmission;
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

                        renderImageSpan[p] = Blend.Process(triangle.BlendMode, renderImageSpan[p], color);
                    }
                });
            }
        }

        ShadowMap? RenderSpotLightShadow(SpotLight spotLight, ShadowBuffer shadowBuffer, int size, float offsetX, float offsetY)
        {
            var triangles = TriangleDivider.ClipAndDivide(LightTriangles[spotLight]).ToArray();
            if (triangles.Length < 1)
            {
                return null;
            }

            var minZ = triangles.Select(t => Math.Min(Math.Min(t.V1.Vertex.GetElement(2), t.V2.Vertex.GetElement(2)), t.V3.Vertex.GetElement(2))).Min();
            var maxZ = triangles.Select(t => Math.Max(Math.Max(t.V1.Vertex.GetElement(2), t.V2.Vertex.GetElement(2)), t.V3.Vertex.GetElement(2))).Max();
            var lightProjectionMatrix = Matrix4x4d.CreatePerspectiveFieldOfView(spotLight.ConeRadian, 1.0, minZ, maxZ);

            return RenderShadow(shadowBuffer, size, offsetX, offsetY, triangles, spotLight.ShadowStrength, spotLight.FloatLightViewMatrix, lightProjectionMatrix);
        }

        ShadowMap? RenderParallelLightShadow(ParallelLight parallelLight, ShadowBuffer shadowBuffer, int size, float offsetX, float offsetY)
        {
            var triangles = TriangleDivider.ClipAndDivide(LightTriangles[parallelLight]).ToArray();
            if (triangles.Length < 1)
            {
                return null;
            }

            var min = triangles.Select(t => Vector256.Min(Vector256.Min(t.V1.Vertex, t.V2.Vertex), t.V3.Vertex)).Aggregate(Vector256.Min);
            var max = triangles.Select(t => Vector256.Max(Vector256.Max(t.V1.Vertex, t.V2.Vertex), t.V3.Vertex)).Aggregate(Vector256.Max);
            if (min.GetElement(0) == max.GetElement(0) || min.GetElement(1) == max.GetElement(1))
            {
                return null;
            }

            var lightProjectionMatrix = Matrix4x4d.CreateOrthographic(min.GetElement(0), max.GetElement(0), min.GetElement(1), max.GetElement(1), min.GetElement(2), max.GetElement(2));

            return RenderShadow(shadowBuffer, size, offsetX, offsetY, triangles, parallelLight.ShadowStrength, parallelLight.FloatLightViewMatrix, lightProjectionMatrix);
        }

        ShadowMap?[] RenderPointLightShadow(PointLight pointLight, ShadowBuffer shadowBuffer, int size, float offsetX, float offsetY)
        {
            var result = new ShadowMap?[6];
            var lv = new Matrix4x4[]
            {
                pointLight.FloatFrontLightViewMatrix,
                pointLight.FloatBackLightViewMatrix,
                pointLight.FloatLeftLightViewMatrix,
                pointLight.FloatRightLightViewMatrix,
                pointLight.FloatTopLightViewMatrix,
                pointLight.FloatBottomLightViewMatrix
            };

            foreach (var (i, holder) in PointLightHolder.Directions.Select((d, i) => (i, new PointLightHolder(pointLight, d))))
            {
                var triangles = TriangleDivider.ClipAndDivide(LightTriangles[holder]).ToArray();
                if (triangles.Length < 1)
                {
                    continue;
                }

                var minZ = triangles.Select(t => Math.Min(Math.Min(t.V1.Vertex.GetElement(2), t.V2.Vertex.GetElement(2)), t.V3.Vertex.GetElement(2))).Min();
                var maxZ = triangles.Select(t => Math.Max(Math.Max(t.V1.Vertex.GetElement(2), t.V2.Vertex.GetElement(2)), t.V3.Vertex.GetElement(2))).Max();
                var lightProjectionMatrix = Matrix4x4d.CreatePerspectiveFieldOfView(Math.PI * 0.5, 1.0, minZ, maxZ);

                result[i] = RenderShadow(shadowBuffer, size, offsetX, offsetY, triangles, pointLight.ShadowStrength, lv[i], lightProjectionMatrix);
            }

            return result;
        }

        static ShadowMap RenderShadow(ShadowBuffer shadowBuffer, int size, float offsetX, float offsetY, LightTriangle[] dividedLightTriangles, float shadowStrength, in Matrix4x4 lightViewMatrix, in Matrix4x4d lightProjectionMatrix)
        {
            var convertedTexture = new Dictionary<NImage, NManagedImage>();

            var shadowMap = new ShadowMap(shadowBuffer, size, lightViewMatrix * Matrix4x4.CreateTranslation(offsetX, offsetY, 0.0F) * (Matrix4x4)lightProjectionMatrix);

            foreach (var triangle in dividedLightTriangles)
            {
                var uv1 = triangle.V1.Transform(lightProjectionMatrix);
                var uv2 = triangle.V2.Transform(lightProjectionMatrix);
                var uv3 = triangle.V3.Transform(lightProjectionMatrix);
                var textureWidth = triangle.Texture.Width;
                var textureHeight = triangle.Texture.Height;

                var w1 = 1.0 / Math.Abs(uv1.Vertex.GetElement(3));
                var w2 = 1.0 / Math.Abs(uv2.Vertex.GetElement(3));
                var w3 = 1.0 / Math.Abs(uv3.Vertex.GetElement(3));
                uv1 *= w1;
                uv2 *= w2;
                uv3 *= w3;
                var dvv1 = (uv1.Vertex + Vector256.Create(1.0, 1.0, 0.0, 0.0)) * Vector256.Create(size * 0.5, size * 0.5, 1.0, 1.0);
                var dvv2 = (uv2.Vertex + Vector256.Create(1.0, 1.0, 0.0, 0.0)) * Vector256.Create(size * 0.5, size * 0.5, 1.0, 1.0);
                var dvv3 = (uv3.Vertex + Vector256.Create(1.0, 1.0, 0.0, 0.0)) * Vector256.Create(size * 0.5, size * 0.5, 1.0, 1.0);
                var vvX = Vector128.Create((float)uv1.Vertex.GetElement(0), (float)uv2.Vertex.GetElement(0), (float)uv3.Vertex.GetElement(0), 0.0F);
                var vvY = Vector128.Create((float)uv1.Vertex.GetElement(1), (float)uv2.Vertex.GetElement(1), (float)uv3.Vertex.GetElement(1), 0.0F);
                var vvZ = Vector128.Create((float)uv1.Vertex.GetElement(2), (float)uv2.Vertex.GetElement(2), (float)uv3.Vertex.GetElement(2), 0.0F);
                var minX = MaxClampedSize((int)(Math.Min(Math.Min(dvv1.GetElement(0), dvv2.GetElement(0)), dvv3.GetElement(0))), 0);
                var maxX = MinClampedSize((int)Math.Ceiling(Math.Max(Math.Max(dvv1.GetElement(0), dvv2.GetElement(0)), dvv3.GetElement(0))), size);
                var minY = MaxClampedSize((int)(Math.Min(Math.Min(dvv1.GetElement(1), dvv2.GetElement(1)), dvv3.GetElement(1))), 0);
                var maxY = MinClampedSize((int)Math.Ceiling(Math.Max(Math.Max(dvv1.GetElement(1), dvv2.GetElement(1)), dvv3.GetElement(1))), size);
                var u = Vector128.Create((float)uv1.U, (float)uv2.U, (float)uv3.U, 0.0F);
                var v = Vector128.Create((float)uv1.V, (float)uv2.V, (float)uv3.V, 0.0F);
                var w = Vector128.Create((float)w1, (float)w2, (float)w3, 0.0F);

                var denom = Vector128.Create((float)(1.0 / (((dvv2.GetElement(0) - dvv1.GetElement(0)) * (dvv3.GetElement(1) - dvv1.GetElement(1))) - ((dvv2.GetElement(1) - dvv1.GetElement(1)) * (dvv3.GetElement(0) - dvv1.GetElement(0))))));
                var edgeX = Vector128.Create((float)dvv3.GetElement(0), (float)dvv1.GetElement(0), (float)dvv2.GetElement(0), 0.0F) - Vector128.Create((float)dvv2.GetElement(0), (float)dvv3.GetElement(0), (float)dvv1.GetElement(0), 0.0F);
                var edgeY = Vector128.Create((float)dvv3.GetElement(1), (float)dvv1.GetElement(1), (float)dvv2.GetElement(1), 0.0F) - Vector128.Create((float)dvv2.GetElement(1), (float)dvv3.GetElement(1), (float)dvv1.GetElement(1), 0.0F);
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

                shadowMap.AllocBuffer();
                Parallel.For(minY, maxY, y =>
                {
                    var texture = managedTexture.GetDataSpan();
                    var eY = edgeX * (Vector128.Create(y, y, y, 0.0F) - vvEY);
                    var offset = y * size;
                    var indicesSpan = shadowMap.Indices.AsSpan(offset, size);
                    var bufferIndicesSpan = shadowMap.BufferIndices.AsSpan(offset, size);
                    for (var x = minX; x < maxX; x++)
                    {
                        var eX = Vector128.Create(x, x, x, 0.0F) - vvEX;
                        var e = (Fma.IsSupported ? Fma.MultiplyAddNegated(edgeY, eX, eY) : (eY - (edgeY * eX))) * denom;
                        var ae = e & Vector128.GreaterThanOrEqual(Vector128.Abs(e), Vector128.Create(TriangleDivider.Epsilon));
                        if (Vector128.LessThanAny(ae, Vector128<float>.Zero))
                        {
                            continue;
                        }

                        var tw = Vector128.Sum(w * e);
                        var tx = Vector128.Sum(u * e / tw) * textureWidth;
                        var ty = Vector128.Sum(v * e / tw) * textureHeight;

                        var color = triangle.InterpolationQuality == ImageInterpolationQuality.Level1 ? ImageInterpolation.NearestNeighbor(texture, textureWidth, textureHeight, tx, ty) : ImageInterpolation.Bilinear(texture, textureWidth, textureHeight, tx, ty);

                        // α == 0 もしくはライト透過100%の白
                        if (color.W <= 0.0F || (triangle.LightTransmission >= 1.0F && color.X >= 1.0F && color.Y >= 1.0F && color.Z >= 1.0F))
                        {
                            continue;
                        }

                        var d = CalcBarycentricCoord(vvX, vvY, vvZ, e).AsVector4();

                        var shadowColor = Vector4.One - Vector4.Lerp(Vector4.One, Vector4.Lerp(Vector4.UnitW, Vector4.Clamp(color, Vector4.Zero, Vector4.One), triangle.LightTransmission), Math.Min(color.W, 1.0F) * triangle.Opacity);
                        shadowColor = Vector4.One - Vector4.Clamp(shadowColor * shadowStrength, Vector4.Zero, Vector4.One);
                        shadowColor.W = 1.0F;
                        var (bufferIndex, index) = shadowBuffer.GetEmptyIndex();
                        shadowBuffer.Buffers[bufferIndex][index] = new ShadowPixel(shadowColor, Math.Clamp(MathF.Round(d.Z, DepthRoundingDigit), 0.0F, 1.0F), triangle.Id, indicesSpan[x], bufferIndicesSpan[x]);
                        indicesSpan[x] = index;
                        bufferIndicesSpan[x] = bufferIndex;
                    }
                });
            }

            foreach (var (_, i) in convertedTexture)
            {
                i.Dispose();
            }

            return shadowMap;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector4 GetShadowColor(int triangleId, ShadowMap shadowMap, float shadowScatterSize, bool isEnableAntiAlias, in Vector4 shadowProjectionPos, in Matrix4x4 invertedProjectionViewMatrix, in Matrix4x4 lightViewProjectionMatrix)
        {
            var shadowPos = Vector4.Transform(Vector4.Transform(shadowProjectionPos, invertedProjectionViewMatrix), lightViewProjectionMatrix);
            shadowPos /= shadowPos.W;
            var shadowTexPos = shadowPos * 0.5F + new Vector4(0.5F, 0.5F, 0.0F, 0.0F);
            var depth = MathF.Round(shadowPos.Z, DepthRoundingDigit);
            var size = shadowMap.ShadowMapSize;
            var shadowBuffer = shadowMap.ShadowBuffer;

            var shadowTextureX = shadowTexPos.X * size;
            var shadowTextureY = shadowTexPos.Y * size;
            var intShadowTextureX = (int)shadowTextureX;
            var intShadowTextureY = (int)shadowTextureY;

            if (isEnableAntiAlias)
            {
                var s1 = SamplingShadowColor(triangleId, shadowMap, shadowScatterSize, intShadowTextureX, intShadowTextureY, depth);
                var s2 = SamplingShadowColor(triangleId, shadowMap, shadowScatterSize, intShadowTextureX + 1, intShadowTextureY, depth);
                var s3 = SamplingShadowColor(triangleId, shadowMap, shadowScatterSize, intShadowTextureX, intShadowTextureY + 1, depth);
                var s4 = SamplingShadowColor(triangleId, shadowMap, shadowScatterSize, intShadowTextureX +1, intShadowTextureY + 1, depth);

                return Vector4.Lerp(
                    Vector4.Lerp(s1, s2, shadowTextureX - intShadowTextureX),
                    Vector4.Lerp(s3, s4, shadowTextureX - intShadowTextureX),
                    shadowTextureY - intShadowTextureY
                );
            }
            else
            {
                return SamplingShadowColor(triangleId, shadowMap, shadowScatterSize, intShadowTextureX, intShadowTextureY, depth);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector4 SamplingShadowColor(int triangleId, ShadowMap shadowMap, float shadowScatterSize, int shadowTextureX, int shadowTextureY, float depth)
        {
            var size = shadowMap.ShadowMapSize;
            var shadowBuffer = shadowMap.ShadowBuffer;

            // TODO: 重くならないならfor文の方とまとめる
            if (shadowScatterSize <= 0.0F)
            {
                if (shadowTextureX < 0 || shadowTextureX >= size || shadowTextureY < 0 || shadowTextureY >= size)
                {
                    return Vector4.One;
                }
                else
                {
                    var tc = Vector4.One;
                    var si = shadowTextureY * size + shadowTextureX;
                    var index = shadowMap.Indices[si];
                    var bufferIndex = shadowMap.BufferIndices[si];
                    while (index >= 0 && tc.CompareGreaterThanBy3Element(Vector3.Zero))
                    {
                        var sp = shadowBuffer.Buffers[bufferIndex][index];
                        if (sp.TriangleId == triangleId || depth < sp.Depth)
                        {
                            break;
                        }

                        tc *= sp.Color;
                        index = sp.NextIndex;
                        bufferIndex = sp.NextBuffer;
                    }
                    return tc;
                }
            }
            else
            {
                var transmissionColor = Vector4.Zero;
                // TODO: ちゃんと距離に応じてぼけるようにする
                //        Deep Shadow Mapsと相性が悪いのであれば他のShadow Mappingアルゴリズムに切り替えることも検討する
                var samplingRange = (int)MathF.Ceiling(shadowScatterSize) * 2 + 1;
                var edgeRate = shadowScatterSize % 1.0F;
                if (edgeRate <= 0.0F)
                {
                    edgeRate = 1.0F;
                }
                for (int stsy = shadowTextureY - samplingRange / 2, cy = 0; cy < samplingRange; stsy++, cy++)
                {
                    var yRate = (cy == 0 || cy == samplingRange - 1 ? edgeRate : 1.0F);
                    if (stsy < 0 || stsy >= size)
                    {
                        transmissionColor += Vector4.One * ((samplingRange - 2) + edgeRate * 2.0F) * yRate;
                        continue;
                    }
                    for (int stsx = shadowTextureX - samplingRange / 2, cx = 0; cx < samplingRange; stsx++, cx++)
                    {
                        var rate = (cx == 0 || cx == samplingRange - 1 ? edgeRate : 1.0F) * yRate;
                        if (stsx < 0 || stsx >= size)
                        {
                            transmissionColor += Vector4.One * rate;
                            continue;
                        }

                        var tc = Vector4.One;
                        var si = stsy * size + stsx;
                        var index = shadowMap.Indices[si];
                        var bufferIndex = shadowMap.BufferIndices[si];
                        while (index >= 0 && tc.CompareGreaterThanBy3Element(Vector3.Zero))
                        {
                            var sp = shadowBuffer.Buffers[bufferIndex][index];
                            if (sp.TriangleId == triangleId || depth < sp.Depth)
                            {
                                break;
                            }

                            tc *= sp.Color;
                            index = sp.NextIndex;
                            bufferIndex = sp.NextBuffer;
                        }
                        transmissionColor += tc * rate;
                    }
                }

                return transmissionColor / ((shadowScatterSize * 2.0F + 1.0F) * (shadowScatterSize * 2.0F + 1.0F));
            }
        }

        #region Debug functions
#if DEBUG
#pragma warning disable IDE0051 // 使用されていないプライベート メンバーを削除する
        void DisplayShadowMapForDebug(ShadowMap? shadowMap, ShadowBuffer shadowBuffer)
#pragma warning restore IDE0051 // 使用されていないプライベート メンバーを削除する
        {
            if (shadowMap == null)
            {
                return;
            }

            var size = shadowMap.ShadowMapSize / 4;
            var image = RenderImage.GetDataSpan();
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var color = Vector4.Zero;
                    for (int sy = y * 4, cy = 0; cy < 4; sy++, cy++)
                    {
                        for (int sx = x * 4, cx = 0; cx < 4; sx++, cx++)
                        {
                            var spi = sy * shadowMap.ShadowMapSize + sx;
                            if (shadowMap.Indices[spi] < 0)
                            {
                                color += Vector4.One;
                                continue;
                            }

                            var sp = shadowBuffer.Buffers[shadowMap.BufferIndices[spi]][shadowMap.Indices[spi]];
                            color += sp.Color;
                        }
                    }
                    image[y * RenderImage.Width + x] = color / 16;
                }
            }
        }
#endif
        #endregion

        private record PreProcessedTriangle(
            int Id,
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
            Vector128<float> SVVX,
            Vector128<float> SVVY,
            Vector128<float> SVVZ,
            Vector128<float> Denominator,
            bool IsFrontFace,
            Vector3 FloatNormal,
            NManagedImage Texture,
            ImageInterpolationQuality InterpolationQuality,
            ManagedRasterizedMaskImage? TrackMatte,
            float Opacity,
            float LightTransmission,
            BlendMode BlendMode,
            bool IsAcceptShadow,
            bool IsAcceptLight,
            float Ambient,
            float Diffuse,
            float SpecularIntensity,
            float SpecularShininess,
            float Metal
        );
    }

    class GPURenderer3D : Renderer3DBase
    {
        NGPUImage RenderImage { get; }

        GraphicsDevice Device { get; }

        public GPURenderer3D(NGPUImage renderImage, GraphicsDevice device, int width, int height, List<PointLight> pointLights, List<SpotLight> spotLights, List<ParallelLight> parallelLights, List<AmbientLight> ambientLights)
            : base(width, height, pointLights, spotLights, parallelLights, ambientLights)
        {
            RenderImage = renderImage;
            Device = device;
        }

        public void Render(bool enableAntiAlias, bool enableShadowAntiAlias)
        {
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
            Matrix4x4d.Invert(ViewMatrix, out var invertedViewMatrix);
            Matrix4x4d.Invert(projectionMatrix, out var invertedProjectionMatrix);
            var invertedProjectionViewMatrix = ((Matrix4x4)(invertedProjectionMatrix * Matrix4x4d.CreateTranslate(-offsetX, -offsetY, 0.0) * invertedViewMatrix)).ToFloat4x4();
            var convertedTextures = new Dictionary<NImage, NGPUImage>();
            var convertedTrackMattes = new Dictionary<RasterizedMaskImage, GPURasterizedMaskImage>();
            var textures = new NGPUImage[triangles.Length];
            var trackMattes = new GPURasterizedMaskImage?[triangles.Length];
            var hasLight = PointLights.Count > 0 || SpotLights.Count > 0 || ParallelLights.Count > 0 || AmbientLights.Count > 0;

            var shadowSize = Size == Width ? (int)(Size / scaleRateX) : (int)(Size / scaleRateY);
            //var pointLightShadows = PointLights.Select(l => l.IsEnableShadow ? RenderPointLightShadow(l, shadowSize, (float)offsetX, (float)offsetY) : null).ToArray();
            var spotLightShadows = SpotLights.Select(l => l.IsEnableShadow && LightTriangles[l].Count > 0 ? RenderSpotLightShadow(l, shadowSize, (float)offsetX, (float)offsetY, convertedTextures) : null).ToArray();
            //var parallelLightShadows = ParallelLights.Select(l => l.IsEnableShadow && LightTriangles[l].Count > 0 ? RenderParallelLightShadow(l, shadowSize, (float)offsetX, (float)offsetY) : null).ToArray();
            //var hasShadow = pointLightShadows.Any(ss => ss != null && ss.Any(s => s != null)) || spotLightShadows.Any(s => s != null) || parallelLightShadows.Any(s => s != null);

            var renderImageOffsetX = (int)(OffsetX / scaleRateX);
            var renderImageOffsetY = (int)(OffsetY / scaleRateY);
            var preProcessedTriangles = new GPUTriangle[triangles.Length];
            var triangleStates = new (int, int, int, int, int, bool, bool, int)[triangles.Length];
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
                    if (!convertedTextures.ContainsKey(managedTexture))
                    {
                        convertedTextures.Add(managedTexture, managedTexture.CopyToGpu(Device));
                    }
                    gpuTexture = convertedTextures[triangle.Texture];
                }
                else
                {
                    gpuTexture = (NGPUImage)triangle.Texture;
                }
                textures[i] = gpuTexture;

                GPURasterizedMaskImage? gpuTrackMatte;
                if (triangle.TrackMatte is ManagedRasterizedMaskImage managedTrackMatte)
                {
                    if (!convertedTrackMattes.ContainsKey(managedTrackMatte))
                    {
                        convertedTrackMattes.Add(managedTrackMatte, managedTrackMatte.CopyToGpu(Device));
                    }
                    gpuTrackMatte = convertedTrackMattes[managedTrackMatte];
                }
                else
                {
                    gpuTrackMatte = (GPURasterizedMaskImage?)triangle.TrackMatte;
                }
                trackMattes[i] = gpuTrackMatte;

                preProcessedTriangles[i] = new GPUTriangle(
                    triangle.Id,
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
                    svvX.AsFloat4(),
                    svvY.AsFloat4(),
                    svvZ.AsFloat4(),
                    denom.AsFloat4(),
                    isFrontFace,
                    triangle.FloatNormal,
                    (int)triangle.InterpolationQuality,
                    triangle.Opacity,
                    triangle.LightTransmission,
                    (int)triangle.BlendMode,
                    triangle.IsAcceptShadow,
                    triangle.IsAcceptLight,
                    triangle.Ambient,
                    triangle.Diffuse,
                    triangle.SpecularIntensity,
                    triangle.SpecularShininess,
                    triangle.Metal
                );
                var useLight = hasLight && (triangle.IsAcceptLight || triangle.IsAcceptShadow);
                triangleStates[i] = (triangle.Id, preProcessedTriangles[i].TrueMinX, preProcessedTriangles[i].TrueMaxX, preProcessedTriangles[i].TrueMinY, preProcessedTriangles[i].TrueMaxY, useLight, triangle.IsAcceptLight, preProcessedTriangles[i].BlendMode);
            }

            using (var triangleBuffer = Device.AllocateReadOnlyBuffer(preProcessedTriangles))
            using (var pointLightBuffer = PointLights.Count > 0 ? Device.AllocateReadOnlyBuffer([.. PointLights.Select(p => p.ToGpu())]) : Device.AllocateReadOnlyBuffer<GPUPointLight>(1))
            using (var spotLightBuffer = SpotLights.Count > 0 ? Device.AllocateReadOnlyBuffer([.. SpotLights.Select(s => s.ToGpu())]) : Device.AllocateReadOnlyBuffer<GPUSpotLight>(1))
            using (var parallelLightBuffer = ParallelLights.Count > 0 ? Device.AllocateReadOnlyBuffer([.. ParallelLights.Select(p => p.ToGpu())]) : Device.AllocateReadOnlyBuffer<GPUParallelLight>(1))
            using (var ambientLightBuffer = AmbientLights.Count > 0 ? Device.AllocateReadOnlyBuffer([.. AmbientLights.Select(a => a.ToGpu())]) : Device.AllocateReadOnlyBuffer<GPUAmbientLight>(1))
            {
                if (enableAntiAlias)
                {
                    using var interpolate = new NGPUImage(RenderImage.Width, RenderImage.Height, Device);
                    RenderImage.CopyTo(interpolate);

                    Rasterize(
                        Device,
                        RenderImage,
                        triangleBuffer,
                        triangleStates,
                        textures,
                        trackMattes,
                        renderImageOffsetX,
                        renderImageOffsetY,
                        scaleRateX,
                        scaleRateY,
                        invertedProjectionViewMatrix,
                        PointLights.Count,
                        pointLightBuffer,
                        SpotLights.Count,
                        spotLightBuffer,
                        spotLightShadows,
                        ParallelLights.Count,
                        parallelLightBuffer,
                        AmbientLights.Count,
                        ambientLightBuffer,
                        shadowSize,
                        enableShadowAntiAlias,
                        0.0F,
                        0.0F
                    );
                    Rasterize(
                        Device,
                        interpolate,
                        triangleBuffer,
                        triangleStates,
                        textures,
                        trackMattes,
                        renderImageOffsetX,
                        renderImageOffsetY,
                        scaleRateX,
                        scaleRateY,
                        invertedProjectionViewMatrix,
                        PointLights.Count,
                        pointLightBuffer,
                        SpotLights.Count,
                        spotLightBuffer,
                        spotLightShadows,
                        ParallelLights.Count,
                        parallelLightBuffer,
                        AmbientLights.Count,
                        ambientLightBuffer,
                        shadowSize,
                        enableShadowAntiAlias,
                        0.5F,
                        0.5F
                    );

                    using var context = Device.CreateComputeContext();
                    context.For(RenderImage.Width, RenderImage.Height, new AntiAlias(RenderImage.Data, interpolate.Data, RenderImage.Width));
                }
                else
                {
                    Rasterize(
                        Device,
                        RenderImage,
                        triangleBuffer,
                        triangleStates,
                        textures,
                        trackMattes,
                        renderImageOffsetX,
                        renderImageOffsetY,
                        scaleRateX,
                        scaleRateY,
                        invertedProjectionViewMatrix,
                        PointLights.Count,
                        pointLightBuffer,
                        SpotLights.Count,
                        spotLightBuffer,
                        spotLightShadows,
                        ParallelLights.Count,
                        parallelLightBuffer,
                        AmbientLights.Count,
                        ambientLightBuffer,
                        shadowSize,
                        enableShadowAntiAlias,
                        0.0F,
                        0.0F
                    );
                }
            }

            foreach (var (_, i) in convertedTextures)
            {
                i.Dispose();
            }
            foreach (var (_, i) in convertedTrackMattes)
            {
                i?.Dispose();
            }

            foreach (var (shadowMap, shadowBuffer, _) in spotLightShadows.NonNull())
            {
                shadowMap.Dispose();
                shadowBuffer.Dispose();
            }
        }

        static void Rasterize(
            GraphicsDevice device,
            NGPUImage renderTarget,
            ReadOnlyBuffer<GPUTriangle> triangleBuffer,
            (int, int, int, int, int, bool, bool, int)[] triangleState,
            NGPUImage[] textures,
            GPURasterizedMaskImage?[] trackMattes,
            int renderImageOffsetX,
            int renderImageOffsetY,
            float scaleRateX,
            float scaleRateY,
            in Float4x4 invertedProjectionViewMatrix,
            int pointLightCount,
            ReadOnlyBuffer<GPUPointLight> pointLightBuffer,
            int spotLightCount,
            ReadOnlyBuffer<GPUSpotLight> spotLightBuffer,
            (ReadWriteBuffer<int> shadowMap, ReadWriteBuffer<GPUShadowPixel> shadowBuffer, Float4x4 lightViewProjectionMatrix)?[] spotLightShadows,
            int parallelLightCount,
            ReadOnlyBuffer<GPUParallelLight> parallelLightBuffer,
            int ambientLightCount,
            ReadOnlyBuffer<GPUAmbientLight> ambientLightBuffer,
            int shadowMapSize,
            bool enableShadowAntiAlias,
            float offsetX,
            float offsetY
        )
        {
            using var emptyTrackMatte = device.AllocateReadWriteBuffer(EmptyTrackMatte);
            using var rasterizedData = device.AllocateReadWriteBuffer<GPURasterizedPixel>(renderTarget.DataLength);
            using var context = device.CreateComputeContext();
            using var emptyShadowMap = device.AllocateReadWriteBuffer([0]);
            using var emptyShadowBuffer = device.AllocateReadWriteBuffer([new GPUShadowPixel()]);

            foreach (var groupedTriangleIds in triangleState.ZipWithIndex().GroupByPrev(t => t.Item1).Select(g => g.ToArray()))
            {
                var useLight = false;
                var acceptLight = false;
                var blendMode = 0;
                var totalMinX = int.MaxValue;
                var totalMaxX = -1;
                var totalMinY = int.MaxValue;
                var totalMaxY = -1;
                foreach (var ((_, minX, maxX, minY, maxY, l, a, b), _) in groupedTriangleIds)
                {
                    useLight = l;
                    acceptLight = a;
                    blendMode = b;
                    totalMinX = Math.Min(totalMinX, minX - renderImageOffsetX);
                    totalMaxX = Math.Max(totalMaxX, maxX - renderImageOffsetX);
                    totalMinY = Math.Min(totalMinY, minY - renderImageOffsetY);
                    totalMaxY = Math.Max(totalMaxY, maxY - renderImageOffsetY);
                }

                if (totalMaxX - totalMinX < 1 || totalMaxY - totalMinY < 1)
                {
                    continue;
                }

                context.For(totalMaxX - totalMinX, totalMaxY - totalMinY, new ClearRasterizedImage(rasterizedData, renderTarget.Width, totalMinX, totalMinY));
                context.Barrier(rasterizedData);

                foreach (var ((_, minX, maxX, minY, maxY, _, _, _), i) in groupedTriangleIds)
                {
                    if (minX - renderImageOffsetX >= renderTarget.Width || maxX - renderImageOffsetX <= 0 || minY - renderImageOffsetY >= renderTarget.Height || maxY - renderImageOffsetY <= 0)
                    {
                        continue;
                    }

                    var texture = textures[i];
                    context.For(
                        maxX - minX,
                        maxY - minY,
                        new Rasterize3D(
                            rasterizedData,
                            renderTarget.Width,
                            renderImageOffsetX,
                            renderImageOffsetY,
                            scaleRateX,
                            scaleRateY,
                            triangleBuffer,
                            i,
                            texture.Data,
                            texture.Width,
                            texture.Height,
                            trackMattes[i]?.Data ?? emptyTrackMatte,
                            offsetX,
                            offsetY
                        )
                    );
                }
                context.Barrier(rasterizedData);

                if (useLight)
                {
                    for (var i = 0; i < pointLightCount; i++)
                    {
                        foreach (var ((_, minX, maxX, minY, maxY, _, _, _), ti) in groupedTriangleIds)
                        {
                            context.For(
                                maxX - minX,
                                maxY - minY,
                                new LightingByPointLight(
                                    rasterizedData,
                                        renderTarget.Width,
                                        renderImageOffsetX,
                                        renderImageOffsetY,
                                        scaleRateX,
                                        scaleRateY,
                                        offsetX,
                                        offsetY,
                                    triangleBuffer,
                                    ti,
                                    pointLightBuffer,
                                    i,
                                    invertedProjectionViewMatrix,
                                    shadowMapSize,
                                    false,
                                    emptyShadowMap,
                                    emptyShadowBuffer,
                                    new Float4x4(),
                                    emptyShadowMap,
                                    emptyShadowBuffer,
                                    new Float4x4(),
                                    emptyShadowMap,
                                    emptyShadowBuffer,
                                    new Float4x4(),
                                    emptyShadowMap,
                                    emptyShadowBuffer,
                                    new Float4x4(),
                                    emptyShadowMap,
                                    emptyShadowBuffer,
                                    new Float4x4(),
                                    emptyShadowMap,
                                    emptyShadowBuffer,
                                    new Float4x4(),
                                    enableShadowAntiAlias
                                )
                            );
                        }
                        context.Barrier(rasterizedData);
                    }

                    for (var i = 0; i < spotLightCount; i++)
                    {
                        var (shadowMap, shadowBuffer, lightViewProjectionMatrix) = spotLightShadows[i] ?? (emptyShadowMap, emptyShadowBuffer, new Float4x4());
                        foreach (var ((_, minX, maxX, minY, maxY, _, _, _), ti) in groupedTriangleIds)
                        {
                            context.For(
                                maxX - minX,
                                maxY - minY,
                                new LightingBySpotLight(
                                    rasterizedData,
                                    renderTarget.Width,
                                    renderImageOffsetX,
                                    renderImageOffsetY,
                                    scaleRateX,
                                    scaleRateY,
                                    offsetX,
                                    offsetY,
                                    triangleBuffer,
                                    ti,
                                    spotLightBuffer,
                                    i,
                                    invertedProjectionViewMatrix,
                                    lightViewProjectionMatrix,
                                    shadowMapSize,
                                    spotLightShadows[i] != null,
                                    shadowMap,
                                    shadowBuffer,
                                    enableShadowAntiAlias
                                )
                            );
                        }
                        context.Barrier(rasterizedData);
                    }

                    for (var i = 0; i < parallelLightCount; i++)
                    {
                        foreach (var ((_, minX, maxX, minY, maxY, _, _, _), ti) in groupedTriangleIds)
                        {
                            context.For(
                                maxX - minX,
                                maxY - minY,
                                new LightingByParallelLight(
                                    rasterizedData,
                                        renderTarget.Width,
                                        renderImageOffsetX,
                                        renderImageOffsetY,
                                        scaleRateX,
                                        scaleRateY,
                                        offsetX,
                                        offsetY,
                                    triangleBuffer,
                                    ti,
                                    parallelLightBuffer,
                                    i,
                                    invertedProjectionViewMatrix,
                                    shadowMapSize,
                                    false,
                                    emptyShadowMap,
                                    emptyShadowBuffer,
                                    enableShadowAntiAlias
                                )
                            );
                        }
                        context.Barrier(rasterizedData);
                    }

                    context.For(totalMaxX - totalMinX, totalMaxY - totalMinY, new LightingByAmbientLight(rasterizedData, triangleBuffer, groupedTriangleIds[0].Item2, ambientLightBuffer, ambientLightCount, renderTarget.Width, totalMinX, totalMinY));
                    context.Barrier(rasterizedData);
                }

                context.For(totalMaxX - totalMinX, totalMaxY - totalMinY, new BlendRasterized(renderTarget.Data, rasterizedData, useLight, acceptLight, renderTarget.Width, totalMinX, totalMinY, blendMode));
                context.Barrier(renderTarget.Data);
            }
        }

        (ReadWriteBuffer<int> shadowMap, ReadWriteBuffer<GPUShadowPixel> shadowBuffer, Float4x4 lightViewProjectionMatrix)?[]? RenderPointLightShadow(PointLight pointLight, int shadowSize, float offsetX, float offsetY)
        {
            return null;
        }

        (ReadWriteBuffer<int> shadowMap, ReadWriteBuffer<GPUShadowPixel> shadowBuffer, Float4x4 lightViewProjectionMatrix)? RenderSpotLightShadow(SpotLight spotLight, int shadowMapSize, float offsetX, float offsetY, Dictionary<NImage, NGPUImage> convertedTexture)
        {
            var triangles = TriangleDivider.ClipAndDivide(LightTriangles[spotLight]).ToArray();
            if (triangles.Length < 1)
            {
                return null;
            }

            var minZ = triangles.Select(t => Math.Min(Math.Min(t.V1.Vertex.GetElement(2), t.V2.Vertex.GetElement(2)), t.V3.Vertex.GetElement(2))).Min();
            var maxZ = triangles.Select(t => Math.Max(Math.Max(t.V1.Vertex.GetElement(2), t.V2.Vertex.GetElement(2)), t.V3.Vertex.GetElement(2))).Max();
            var lightProjectionMatrix = Matrix4x4d.CreatePerspectiveFieldOfView(spotLight.ConeRadian, 1.0, minZ, maxZ);

            return RenderShadow(Device, triangles, spotLight.FloatLightViewMatrix, lightProjectionMatrix, shadowMapSize, spotLight.ShadowStrength, offsetX, offsetY, convertedTexture);
        }

        (ReadWriteBuffer<int> shadowMap, ReadWriteBuffer<GPUShadowPixel> shadowBuffer, Float4x4 lightViewProjectionMatrix)? RenderParallelLightShadow(ParallelLight parallelLight, int shadowSize, float offsetX, float offsetY)
        {
            return null;
        }

        static (ReadWriteBuffer<int> shadowMap, ReadWriteBuffer<GPUShadowPixel> shadowBuffer, Float4x4 lightViewProjectionMatrix) RenderShadow(
            GraphicsDevice device,
            LightTriangle[] triangles,
            in Matrix4x4 lightViewMatrix,
            in Matrix4x4d lightProjectionMatrix,
            int shadowMapSize,
            float shadowStrength,
            float offsetX,
            float offsetY,
            Dictionary<NImage, NGPUImage> convertedTexture
        )
        {
            var preProcessedTriangle = new GPUShadowTriangle[triangles.Length];
            var textures = new NGPUImage[triangles.Length];
            var area = 0;

            for (var i = 0; i < triangles.Length; i++)
            {
                var triangle = triangles[i];

                var uv1 = triangle.V1.Transform(lightProjectionMatrix);
                var uv2 = triangle.V2.Transform(lightProjectionMatrix);
                var uv3 = triangle.V3.Transform(lightProjectionMatrix);
                var textureWidth = triangle.Texture.Width;
                var textureHeight = triangle.Texture.Height;

                var w1 = 1.0 / Math.Abs(uv1.Vertex.GetElement(3));
                var w2 = 1.0 / Math.Abs(uv2.Vertex.GetElement(3));
                var w3 = 1.0 / Math.Abs(uv3.Vertex.GetElement(3));
                uv1 *= w1;
                uv2 *= w2;
                uv3 *= w3;
                var dvv1 = (uv1.Vertex + Vector256.Create(1.0, 1.0, 0.0, 0.0)) * Vector256.Create(shadowMapSize * 0.5, shadowMapSize * 0.5, 1.0, 1.0);
                var dvv2 = (uv2.Vertex + Vector256.Create(1.0, 1.0, 0.0, 0.0)) * Vector256.Create(shadowMapSize * 0.5, shadowMapSize * 0.5, 1.0, 1.0);
                var dvv3 = (uv3.Vertex + Vector256.Create(1.0, 1.0, 0.0, 0.0)) * Vector256.Create(shadowMapSize * 0.5, shadowMapSize * 0.5, 1.0, 1.0);
                var vvX = Vector128.Create((float)uv1.Vertex.GetElement(0), (float)uv2.Vertex.GetElement(0), (float)uv3.Vertex.GetElement(0), 0.0F);
                var vvY = Vector128.Create((float)uv1.Vertex.GetElement(1), (float)uv2.Vertex.GetElement(1), (float)uv3.Vertex.GetElement(1), 0.0F);
                var vvZ = Vector128.Create((float)uv1.Vertex.GetElement(2), (float)uv2.Vertex.GetElement(2), (float)uv3.Vertex.GetElement(2), 0.0F);
                var minX = MaxClampedSize((int)(Math.Min(Math.Min(dvv1.GetElement(0), dvv2.GetElement(0)), dvv3.GetElement(0))), 0);
                var maxX = MinClampedSize((int)Math.Ceiling(Math.Max(Math.Max(dvv1.GetElement(0), dvv2.GetElement(0)), dvv3.GetElement(0))), shadowMapSize);
                var minY = MaxClampedSize((int)(Math.Min(Math.Min(dvv1.GetElement(1), dvv2.GetElement(1)), dvv3.GetElement(1))), 0);
                var maxY = MinClampedSize((int)Math.Ceiling(Math.Max(Math.Max(dvv1.GetElement(1), dvv2.GetElement(1)), dvv3.GetElement(1))), shadowMapSize);
                var u = Vector128.Create((float)uv1.U, (float)uv2.U, (float)uv3.U, 0.0F);
                var v = Vector128.Create((float)uv1.V, (float)uv2.V, (float)uv3.V, 0.0F);
                var w = Vector128.Create((float)w1, (float)w2, (float)w3, 0.0F);

                var denom = Vector128.Create((float)(1.0 / (((dvv2.GetElement(0) - dvv1.GetElement(0)) * (dvv3.GetElement(1) - dvv1.GetElement(1))) - ((dvv2.GetElement(1) - dvv1.GetElement(1)) * (dvv3.GetElement(0) - dvv1.GetElement(0))))));
                var edgeX = Vector128.Create((float)dvv3.GetElement(0), (float)dvv1.GetElement(0), (float)dvv2.GetElement(0), 0.0F) - Vector128.Create((float)dvv2.GetElement(0), (float)dvv3.GetElement(0), (float)dvv1.GetElement(0), 0.0F);
                var edgeY = Vector128.Create((float)dvv3.GetElement(1), (float)dvv1.GetElement(1), (float)dvv2.GetElement(1), 0.0F) - Vector128.Create((float)dvv2.GetElement(1), (float)dvv3.GetElement(1), (float)dvv1.GetElement(1), 0.0F);
                var vvEX = Vector128.Create((float)dvv2.GetElement(0), (float)dvv3.GetElement(0), (float)dvv1.GetElement(0), 0.0F);
                var vvEY = Vector128.Create((float)dvv2.GetElement(1), (float)dvv3.GetElement(1), (float)dvv1.GetElement(1), 0.0F);

                NGPUImage gpuTexture;
                if (triangle.Texture is NManagedImage managedTexture)
                {
                    if (!convertedTexture.ContainsKey(managedTexture))
                    {
                        convertedTexture.Add(managedTexture, managedTexture.CopyToGpu(device));
                    }
                    gpuTexture = convertedTexture[triangle.Texture];
                }
                else
                {
                    gpuTexture = (NGPUImage)triangle.Texture;
                }
                textures[i] = gpuTexture;

                preProcessedTriangle[i] = new GPUShadowTriangle(
                    triangle.Id,
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
                    (int)triangle.InterpolationQuality,
                    triangle.Opacity,
                    triangle.LightTransmission
                );

                var barycenter = Vector256.GetLower((dvv1 + dvv2 + dvv3) / 3.0);
                var maxSize = Math.Max(Math.Abs(maxX - minX), Math.Abs(maxY - minY));
                var scale = (maxSize + 2.0F) / maxSize;
                var sva = (Vector256.GetLower(dvv1) - barycenter) * scale;
                var svb = (Vector256.GetLower(dvv2) - barycenter) * scale - sva;
                var svc = (Vector256.GetLower(dvv3) - barycenter) * scale - sva;

                area += Math.Min((int)Math.Ceiling(Math.Abs(svb.GetElement(0) * svc.GetElement(1) - svc.GetElement(0) * svb.GetElement(1)) * 0.5), Math.Abs(maxX - minX) * Math.Abs(maxY - minY));
            }

            var lightViewProjectionMatrix = (lightViewMatrix * Matrix4x4.CreateTranslation(offsetX, offsetY, 0.0F) * (Matrix4x4)lightProjectionMatrix).ToFloat4x4();
            var floatLightProjectionMatrix = ((Matrix4x4)lightProjectionMatrix).ToFloat4x4();
            using var triangleBuffer = device.AllocateReadOnlyBuffer(preProcessedTriangle);
            using var counter = device.AllocateReadWriteBuffer([0]);
            var shadowMap = device.AllocateReadWriteBuffer<int>(shadowMapSize * shadowMapSize);
            var shadowBuffer = device.AllocateReadWriteBuffer<GPUShadowPixel>(area);
            using (var context = device.CreateComputeContext())
            {
                context.For(shadowMapSize * shadowMapSize, new InitShadowMap(shadowMap));

                foreach (var groupedTriangle in preProcessedTriangle.ZipWithIndex().GroupByPrev(t => t.Item1.Id))
                {
                    foreach (var (triangle, i) in groupedTriangle)
                    {
                        if (triangle.TrueMinX >= shadowMapSize || triangle.TrueMaxX <= 0 || triangle.TrueMinY >= shadowMapSize || triangle.TrueMaxY <= 0)
                        {
                            continue;
                        }

                        var texture = textures[i];
                        context.For(
                            triangle.TrueMaxX - triangle.TrueMinX,
                            triangle.TrueMaxY - triangle.TrueMinY,
                            new RasterizeShadow(
                                counter,
                                shadowMap,
                                shadowBuffer,
                                shadowMapSize,
                                triangleBuffer,
                                i,
                                texture.Data,
                                texture.Width,
                                texture.Height,
                                shadowStrength
                            )
                        );
                    }

                    context.Barrier(shadowBuffer);
                }
            }

            return (shadowMap, shadowBuffer, lightViewProjectionMatrix);
        }
    }
}
