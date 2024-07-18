using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;
using ComputeSharp;
using NiVE3.Image;
using NiVE3.Numerics;
using NiVE3.PresetPlugin.Extension;
using NiVE3.PresetPlugin.Internal.Drawing.ComputeShader;
using NiVE3.PresetPlugin.Internal.Drawing.ComputeShader.Render3D;
using NiVE3.PresetPlugin.Internal.Drawing.Primitive3D;
using NiVE3.Shared.Extension;
using static Vanara.PInvoke.Kernel32;

namespace NiVE3.PresetPlugin.Internal.Drawing
{
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
            var textureList = new List<NGPUImage>(triangles.Length);
            var trackMatteList = new List<GPURasterizedMaskImage?>(triangles.Length);
            var hasLight = PointLights.Count > 0 || SpotLights.Count > 0 || ParallelLights.Count > 0 || AmbientLights.Count > 0;

            var shadowSize = Size == Width ? (int)(Size / scaleRateX) : (int)(Size / scaleRateY);
            var pointLightShadows = PointLights.Select(l => l.IsEnableShadow ? RenderPointLightShadow(l, shadowSize, (float)offsetX, (float)offsetY, convertedTextures) : null).ToArray();
            var spotLightShadows = SpotLights.Select(l => l.IsEnableShadow && LightTriangles[l].Count > 0 ? RenderSpotLightShadow(l, shadowSize, (float)offsetX, (float)offsetY, convertedTextures) : null).ToArray();
            var parallelLightShadows = ParallelLights.Select(l => l.IsEnableShadow && LightTriangles[l].Count > 0 ? RenderParallelLightShadow(l, shadowSize, (float)offsetX, (float)offsetY, convertedTextures) : null).ToArray();

            var renderImageOffsetX = (int)(OffsetX / scaleRateX);
            var renderImageOffsetY = (int)(OffsetY / scaleRateY);
            var preProcessedTriangles = new List<GPUTriangle>(triangles.Length);
            var preProcessedTriangleTexturing = new List<GPUTriangleTexturing>(triangles.Length);
            var preProcessedTriangleLighting = new List<GPUTriangleLighting>(triangles.Length);
            var triangleMaterials = new List<(int id, int minX, int maxX, int minY, int maxY, bool useLight, bool isAcceptLight, float ambient, int blendMode)>();
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
                var minX = (int)(MaxClampedSize((int)(Math.Min(Math.Min(dvv1.GetElement(0), dvv2.GetElement(0)), dvv3.GetElement(0))), OffsetX) / scaleRateX);
                var maxX = (int)(MinClampedSize((int)Math.Ceiling(Math.Max(Math.Max(dvv1.GetElement(0), dvv2.GetElement(0)), dvv3.GetElement(0))), Width + OffsetX) / scaleRateX);
                var minY = (int)(MaxClampedSize((int)(Math.Min(Math.Min(dvv1.GetElement(1), dvv2.GetElement(1)), dvv3.GetElement(1))), OffsetY) / scaleRateY);
                var maxY = (int)(MinClampedSize((int)Math.Ceiling(Math.Max(Math.Max(dvv1.GetElement(1), dvv2.GetElement(1)), dvv3.GetElement(1))), Height + OffsetY) / scaleRateY);

                if (minX - renderImageOffsetX >= renderImageWidth || maxX - renderImageOffsetX <= 0 || minY - renderImageOffsetY >= renderImageHeight || maxY - renderImageOffsetY <= 0)
                {
                    continue;
                }

                var vvX = Vector128.Create((float)triangle.V1.Vertex.GetElement(0), (float)triangle.V2.Vertex.GetElement(0), (float)triangle.V3.Vertex.GetElement(0), 0.0F);
                var vvY = Vector128.Create((float)triangle.V1.Vertex.GetElement(1), (float)triangle.V2.Vertex.GetElement(1), (float)triangle.V3.Vertex.GetElement(1), 0.0F);
                var vvZ = Vector128.Create((float)triangle.V1.Vertex.GetElement(2), (float)triangle.V2.Vertex.GetElement(2), (float)triangle.V3.Vertex.GetElement(2), 0.0F);
                var svvX = Vector128.Create((float)uv1.Vertex.GetElement(0), (float)uv2.Vertex.GetElement(0), (float)uv3.Vertex.GetElement(0), 0.0F);
                var svvY = Vector128.Create((float)uv1.Vertex.GetElement(1), (float)uv2.Vertex.GetElement(1), (float)uv3.Vertex.GetElement(1), 0.0F);
                var svvZ = Vector128.Create((float)uv1.Vertex.GetElement(2), (float)uv2.Vertex.GetElement(2), (float)uv3.Vertex.GetElement(2), 0.0F);
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
                textureList.Add(gpuTexture);

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
                trackMatteList.Add(gpuTrackMatte);

                preProcessedTriangles.Add(new GPUTriangle(
                    minX,
                    maxX,
                    minY,
                    maxY,
                    edgeX.AsFloat4(),
                    edgeY.AsFloat4(),
                    vvEX.AsFloat4(),
                    vvEY.AsFloat4(),
                    denom.AsFloat4()
                ));
                preProcessedTriangleTexturing.Add(new GPUTriangleTexturing(
                    u.AsFloat4(),
                    v.AsFloat4(),
                    w.AsFloat4(),
                    (int)triangle.InterpolationQuality,
                    triangle.Opacity
                ));
                preProcessedTriangleLighting.Add(new GPUTriangleLighting(
                    triangle.Id,
                    vvX.AsFloat4(),
                    vvY.AsFloat4(),
                    vvZ.AsFloat4(),
                    svvX.AsFloat4(),
                    svvY.AsFloat4(),
                    svvZ.AsFloat4(),
                    isFrontFace,
                    triangle.FloatNormal,
                    triangle.LightTransmission,
                    triangle.IsAcceptShadow,
                    triangle.IsAcceptLight,
                    triangle.Diffuse,
                    triangle.SpecularIntensity,
                    triangle.SpecularShininess,
                    triangle.Metal
                ));
                triangleMaterials.Add((
                    triangle.Id,
                    Math.Min(minX, maxX),
                    Math.Max(minX, maxX),
                    Math.Min(minY, maxY),
                    Math.Max(minY, maxY),
                    hasLight && (triangle.IsAcceptLight || triangle.IsAcceptShadow),
                    triangle.IsAcceptLight,
                    triangle.Ambient,
                    (int)triangle.BlendMode
                ));
            }

            var triangleGroups = new List<TriangleGroup>();
            foreach (var triangleGroup in triangleMaterials.ZipWithIndex().GroupByPrev(t => t.Item1.id).Select(g => g.ToArray()))
            {
                var minX = triangleGroup.Select(t => t.Item1.minX).Min();
                var maxX = triangleGroup.Select(t => t.Item1.maxX).Max();
                var minY = triangleGroup.Select(t => t.Item1.minY).Min();
                var maxY = triangleGroup.Select(t => t.Item1.maxY).Max();

                if (maxX - minX < 1 || maxY - minY < 1)
                {
                    continue;
                }

                triangleGroups.Add(new TriangleGroup(
                    triangleGroup[0].Item1.id,
                    minX,
                    maxX,
                    minY,
                    maxY,
                    triangleGroup[0].Item1.useLight,
                    triangleGroup[0].Item1.isAcceptLight,
                    triangleGroup[0].Item1.ambient,
                    triangleGroup[0].Item1.blendMode,
                    triangleGroup[0].Item2,
                    triangleGroup[^1].Item2
                ));
            }

            if (hasLight)
            {
                var ambientLightColor = AmbientLights.Aggregate(Vector4.Zero, (m, a) => m + a.Color);

                using (var triangleBuffer = Device.AllocateReadOnlyBuffer<GPUTriangle>(CollectionsMarshal.AsSpan(preProcessedTriangles)))
                using (var triangleTexturingBuffer = Device.AllocateReadOnlyBuffer<GPUTriangleTexturing>(CollectionsMarshal.AsSpan(preProcessedTriangleTexturing)))
                using (var triangleLightingBuffer = Device.AllocateReadOnlyBuffer<GPUTriangleLighting>(CollectionsMarshal.AsSpan(preProcessedTriangleLighting)))
                using (var pointLightBuffer = PointLights.Count > 0 ? Device.AllocateReadOnlyBuffer([.. PointLights.Select(p => p.ToGpu())]) : Device.AllocateReadOnlyBuffer<GPUPointLight>(1))
                using (var spotLightBuffer = SpotLights.Count > 0 ? Device.AllocateReadOnlyBuffer([.. SpotLights.Select(s => s.ToGpu())]) : Device.AllocateReadOnlyBuffer<GPUSpotLight>(1))
                using (var parallelLightBuffer = ParallelLights.Count > 0 ? Device.AllocateReadOnlyBuffer([.. ParallelLights.Select(p => p.ToGpu())]) : Device.AllocateReadOnlyBuffer<GPUParallelLight>(1))
                {
                    if (enableAntiAlias)
                    {
                        using var interpolate = new NGPUImage(RenderImage.Width, RenderImage.Height, Device);
                        RenderImage.CopyTo(interpolate);

                        Rasterize(
                            Device,
                            RenderImage,
                            triangleBuffer,
                            triangleTexturingBuffer,
                            triangleLightingBuffer,
                            CollectionsMarshal.AsSpan(triangleGroups),
                            CollectionsMarshal.AsSpan(textureList),
                            CollectionsMarshal.AsSpan(trackMatteList),
                            renderImageOffsetX,
                            renderImageOffsetY,
                            scaleRateX,
                            scaleRateY,
                            invertedProjectionViewMatrix,
                            PointLights.Count,
                            pointLightBuffer,
                            pointLightShadows,
                            SpotLights.Count,
                            spotLightBuffer,
                            spotLightShadows,
                            ParallelLights.Count,
                            parallelLightBuffer,
                            parallelLightShadows,
                            ambientLightColor,
                            shadowSize,
                            enableShadowAntiAlias,
                            0.0F,
                            0.0F
                        );
                        Rasterize(
                            Device,
                            interpolate,
                            triangleBuffer,
                            triangleTexturingBuffer,
                            triangleLightingBuffer,
                            CollectionsMarshal.AsSpan(triangleGroups),
                            CollectionsMarshal.AsSpan(textureList),
                            CollectionsMarshal.AsSpan(trackMatteList),
                            renderImageOffsetX,
                            renderImageOffsetY,
                            scaleRateX,
                            scaleRateY,
                            invertedProjectionViewMatrix,
                            PointLights.Count,
                            pointLightBuffer,
                            pointLightShadows,
                            SpotLights.Count,
                            spotLightBuffer,
                            spotLightShadows,
                            ParallelLights.Count,
                            parallelLightBuffer,
                            parallelLightShadows,
                            ambientLightColor,
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
                            triangleTexturingBuffer,
                            triangleLightingBuffer,
                            CollectionsMarshal.AsSpan(triangleGroups),
                            CollectionsMarshal.AsSpan(textureList),
                            CollectionsMarshal.AsSpan(trackMatteList),
                            renderImageOffsetX,
                            renderImageOffsetY,
                            scaleRateX,
                            scaleRateY,
                            invertedProjectionViewMatrix,
                            PointLights.Count,
                            pointLightBuffer,
                            pointLightShadows,
                            SpotLights.Count,
                            spotLightBuffer,
                            spotLightShadows,
                            ParallelLights.Count,
                            parallelLightBuffer,
                            parallelLightShadows,
                            ambientLightColor,
                            shadowSize,
                            enableShadowAntiAlias,
                            0.0F,
                            0.0F
                        );
                    }
                }
            }
            else
            {
                using (var triangleBuffer = Device.AllocateReadOnlyBuffer<GPUTriangle>(CollectionsMarshal.AsSpan(preProcessedTriangles)))
                using (var triangleTexturingBuffer = Device.AllocateReadOnlyBuffer<GPUTriangleTexturing>(CollectionsMarshal.AsSpan(preProcessedTriangleTexturing)))
                {
                    if (enableAntiAlias)
                    {
                        using var interpolate = new NGPUImage(RenderImage.Width, RenderImage.Height, Device);
                        RenderImage.CopyTo(interpolate);

                        RasterizeDirect(
                            Device,
                            RenderImage,
                            triangleBuffer,
                            triangleTexturingBuffer,
                            CollectionsMarshal.AsSpan(triangleGroups),
                            CollectionsMarshal.AsSpan(textureList),
                            CollectionsMarshal.AsSpan(trackMatteList),
                            renderImageOffsetX,
                            renderImageOffsetY,
                            scaleRateX,
                            scaleRateY,
                            0.0F,
                            0.0F
                        );
                        RasterizeDirect(
                            Device,
                            interpolate,
                            triangleBuffer,
                            triangleTexturingBuffer,
                            CollectionsMarshal.AsSpan(triangleGroups),
                            CollectionsMarshal.AsSpan(textureList),
                            CollectionsMarshal.AsSpan(trackMatteList),
                            renderImageOffsetX,
                            renderImageOffsetY,
                            scaleRateX,
                            scaleRateY,
                            0.5F,
                            0.5F
                        );

                        using var context = Device.CreateComputeContext();
                        context.For(RenderImage.Width, RenderImage.Height, new AntiAlias(RenderImage.Data, interpolate.Data, RenderImage.Width));
                    }
                    else
                    {
                        RasterizeDirect(
                            Device,
                            RenderImage,
                            triangleBuffer,
                            triangleTexturingBuffer,
                            CollectionsMarshal.AsSpan(triangleGroups),
                            CollectionsMarshal.AsSpan(textureList),
                            CollectionsMarshal.AsSpan(trackMatteList),
                            renderImageOffsetX,
                            renderImageOffsetY,
                            scaleRateX,
                            scaleRateY,
                            0.0F,
                            0.0F
                        );
                    }
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

            foreach (var (shadowMaps, lightViewProjectionMatrixes) in pointLightShadows.NonNull())
            {
                foreach (var (shadowMap, shadowBuffer) in shadowMaps.NonNull())
                {
                    shadowMap.Dispose();
                    shadowBuffer.Dispose();
                }
                lightViewProjectionMatrixes.Dispose();
            }
            foreach (var (shadowMap, shadowBuffer, _) in spotLightShadows.Concat(parallelLightShadows).NonNull())
            {
                shadowMap.Dispose();
                shadowBuffer.Dispose();
            }
        }

        static void RasterizeDirect(
            GraphicsDevice device,
            NGPUImage renderTarget,
            ReadOnlyBuffer<GPUTriangle> triangleBuffer,
            ReadOnlyBuffer<GPUTriangleTexturing> triangleTexturingBuffer,
            ReadOnlySpan<TriangleGroup> triangleGroups,
            ReadOnlySpan<NGPUImage> textures,
            ReadOnlySpan<GPURasterizedMaskImage?> trackMattes,
            int renderImageOffsetX,
            int renderImageOffsetY,
            float scaleRateX,
            float scaleRateY,
            float offsetX,
            float offsetY
        )
        {
            using var emptyTrackMatte = device.AllocateReadWriteBuffer(EmptyTrackMatte);
            using var context = device.CreateComputeContext();

            for (var gi = 0; gi < triangleGroups.Length; gi++)
            {
                var group = triangleGroups[gi];
                var totalMinX = group.MinX;
                var totalMaxX = group.MaxX;
                var totalMinY = group.MinY;
                var totalMaxY = group.MaxY;

                var texture = textures[group.BeginTriangleIndex];
                context.For(
                    totalMaxX - totalMinX,
                    totalMaxY - totalMinY,
                    new Rasterize3DDirect(
                        renderTarget.Data,
                        renderTarget.Width,
                        renderImageOffsetX,
                        renderImageOffsetY,
                        scaleRateX,
                        scaleRateY,
                        triangleBuffer,
                        triangleTexturingBuffer,
                        group.BeginTriangleIndex,
                        group.EndTriangleIndex,
                        texture.Data,
                        texture.Width,
                        texture.Height,
                        trackMattes[group.BeginTriangleIndex]?.Data ?? emptyTrackMatte,
                        group.BlendMode,
                        offsetX,
                        offsetY,
                        totalMinX,
                        totalMinY
                    )
                );
                context.Barrier(renderTarget.Data);
            }
        }

        static void Rasterize(
            GraphicsDevice device,
            NGPUImage renderTarget,
            ReadOnlyBuffer<GPUTriangle> triangleBuffer,
            ReadOnlyBuffer<GPUTriangleTexturing> triangleTexturingBuffer,
            ReadOnlyBuffer<GPUTriangleLighting> triangleLightingBuffer,
            ReadOnlySpan<TriangleGroup> triangleGroups,
            ReadOnlySpan<NGPUImage> textures,
            ReadOnlySpan<GPURasterizedMaskImage?> trackMattes,
            int renderImageOffsetX,
            int renderImageOffsetY,
            float scaleRateX,
            float scaleRateY,
            in Float4x4 invertedProjectionViewMatrix,
            int pointLightCount,
            ReadOnlyBuffer<GPUPointLight> pointLightBuffer,
            ((ReadWriteBuffer<int> shadowMap, ReadWriteBuffer<GPUShadowPixel> shadowBuffer)?[], ConstantBuffer<Float4x4> lightViewProjectionMatrixs)?[] pointLightShadows,
            int spotLightCount,
            ReadOnlyBuffer<GPUSpotLight> spotLightBuffer,
            (ReadWriteBuffer<int> shadowMap, ReadWriteBuffer<GPUShadowPixel> shadowBuffer, Float4x4 lightViewProjectionMatrix)?[] spotLightShadows,
            int parallelLightCount,
            ReadOnlyBuffer<GPUParallelLight> parallelLightBuffer,
            (ReadWriteBuffer<int> shadowMap, ReadWriteBuffer<GPUShadowPixel> shadowBuffer, Float4x4 lightViewProjectionMatrix)?[] parallelLightShadows,
            Vector4 ambientLightColor, // NOTE: シェーダーに渡す前にレイヤーのアンビエント適用率を掛けるためFloat4にはしない
            int shadowMapSize,
            bool enableShadowAntiAlias,
            float offsetX,
            float offsetY
        )
        {
            using var emptyTrackMatte = device.AllocateReadWriteBuffer(EmptyTrackMatte);
            using var rasterizedData = device.AllocateReadWriteBuffer<GPURasterizedPixel>(renderTarget.DataLength);
            using var context = device.CreateComputeContext();

            for (var gi = 0; gi < triangleGroups.Length; gi++)
            {
                var group = triangleGroups[gi];
                var totalMinX = group.MinX;
                var totalMaxX = group.MaxX;
                var totalMinY = group.MinY;
                var totalMaxY = group.MaxY;

                context.For(totalMaxX - totalMinX, totalMaxY - totalMinY, new ClearRasterizedImage(rasterizedData, renderTarget.Width, totalMinX - renderImageOffsetX, totalMinY - renderImageOffsetY));
                context.Barrier(rasterizedData);

                var texture = textures[group.BeginTriangleIndex];
                context.For(
                    totalMaxX - totalMinX,
                    totalMaxY - totalMinY,
                    new Rasterize3D(
                        rasterizedData,
                        renderTarget.Width,
                        renderImageOffsetX,
                        renderImageOffsetY,
                        scaleRateX,
                        scaleRateY,
                        triangleBuffer,
                        triangleTexturingBuffer,
                        group.BeginTriangleIndex,
                        group.EndTriangleIndex,
                        texture.Data,
                        texture.Width,
                        texture.Height,
                        trackMattes[group.BeginTriangleIndex]?.Data ?? emptyTrackMatte,
                        offsetX,
                        offsetY,
                        totalMinX,
                        totalMinY
                    )
                );
                context.Barrier(rasterizedData);

                if (group.UseLight)
                {
                    using var emptyShadowMap = device.AllocateReadWriteBuffer([0]);
                    using var emptyShadowBuffer = device.AllocateReadWriteBuffer([new GPUShadowPixel()]);
                    using var emptyLightViewProjectionMatrixes = device.AllocateConstantBuffer<Float4x4>(1);

                    var emptyPointLightShadows = new (ReadWriteBuffer<int>, ReadWriteBuffer<GPUShadowPixel>)?[6];

                    for (var i = 0; i < pointLightCount; i++)
                    {
                        var (shadowMaps, lightViewProjectionMatrixes) = pointLightShadows[i] ?? (emptyPointLightShadows, emptyLightViewProjectionMatrixes);
                        var (frontShadowMap, frontShadowBuffer) = shadowMaps?[0] ?? (emptyShadowMap, emptyShadowBuffer);
                        var (backShadowMap, backShadowBuffer) = shadowMaps?[1] ?? (emptyShadowMap, emptyShadowBuffer);
                        var (leftShadowMap, leftShadowBuffer) = shadowMaps?[2] ?? (emptyShadowMap, emptyShadowBuffer);
                        var (rightShadowMap, rightShadowBuffer) = shadowMaps?[3] ?? (emptyShadowMap, emptyShadowBuffer);
                        var (topShadowMap, topShadowBuffer) = shadowMaps?[4] ?? (emptyShadowMap, emptyShadowBuffer);
                        var (bottomShadowMap, bottomShadowBuffer) = shadowMaps?[5] ?? (emptyShadowMap, emptyShadowBuffer);

                        context.For(
                            totalMaxX - totalMinX,
                            totalMaxY - totalMinY,
                            new LightingByPointLight(
                                rasterizedData,
                                renderTarget.Width,
                                renderImageOffsetX,
                                renderImageOffsetY,
                                triangleLightingBuffer,
                                pointLightBuffer,
                                i,
                                invertedProjectionViewMatrix,
                                shadowMapSize,
                                pointLightShadows[i] != null,
                                frontShadowMap,
                                frontShadowBuffer,
                                backShadowMap,
                                backShadowBuffer,
                                leftShadowMap,
                                leftShadowBuffer,
                                rightShadowMap,
                                rightShadowBuffer,
                                topShadowMap,
                                topShadowBuffer,
                                bottomShadowMap,
                                bottomShadowBuffer,
                                lightViewProjectionMatrixes,
                                enableShadowAntiAlias,
                                totalMinX,
                                totalMinY
                            )
                        );

                        context.Barrier(rasterizedData);
                    }

                    for (var i = 0; i < spotLightCount; i++)
                    {
                        var (shadowMap, shadowBuffer, lightViewProjectionMatrix) = spotLightShadows[i] ?? (emptyShadowMap, emptyShadowBuffer, new Float4x4());

                        context.For(
                            totalMaxX - totalMinX,
                            totalMaxY - totalMinY,
                            new LightingBySpotLight(
                                rasterizedData,
                                renderTarget.Width,
                                renderImageOffsetX,
                                renderImageOffsetY,
                                triangleLightingBuffer,
                                spotLightBuffer,
                                i,
                                invertedProjectionViewMatrix,
                                lightViewProjectionMatrix,
                                shadowMapSize,
                                spotLightShadows[i] != null,
                                shadowMap,
                                shadowBuffer,
                                enableShadowAntiAlias,
                                totalMinX,
                                totalMinY
                            )
                        );

                        context.Barrier(rasterizedData);
                    }

                    for (var i = 0; i < parallelLightCount; i++)
                    {
                        var (shadowMap, shadowBuffer, lightViewProjectionMatrix) = parallelLightShadows[i] ?? (emptyShadowMap, emptyShadowBuffer, new Float4x4());

                        context.For(
                            totalMaxX - totalMinX,
                            totalMaxY - totalMinY,
                            new LightingByParallelLight(
                                rasterizedData,
                                renderTarget.Width,
                                renderImageOffsetX,
                                renderImageOffsetY,
                                triangleLightingBuffer,
                                parallelLightBuffer,
                                i,
                                invertedProjectionViewMatrix,
                                lightViewProjectionMatrix,
                                shadowMapSize,
                                parallelLightShadows[i] != null,
                                shadowMap,
                                shadowBuffer,
                                enableShadowAntiAlias,
                                totalMinX,
                                totalMinY
                            )
                        );

                        context.Barrier(rasterizedData);
                    }
                }

                context.For(totalMaxX - totalMinX, totalMaxY - totalMinY, new BlendRasterized(renderTarget.Data, rasterizedData, group.UseLight, group.IsAcceptLight, ambientLightColor * group.Ambient, renderTarget.Width, totalMinX - renderImageOffsetX, totalMinY - renderImageOffsetY, group.BlendMode));
                context.Barrier(renderTarget.Data);
            }
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

        (ReadWriteBuffer<int> shadowMap, ReadWriteBuffer<GPUShadowPixel> shadowBuffer, Float4x4 lightViewProjectionMatrix)? RenderParallelLightShadow(ParallelLight parallelLight, int shadowMapSize, float offsetX, float offsetY, Dictionary<NImage, NGPUImage> convertedTexture)
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

            return RenderShadow(Device, triangles, parallelLight.FloatLightViewMatrix, lightProjectionMatrix, shadowMapSize, parallelLight.ShadowStrength, offsetX, offsetY, convertedTexture);
        }

        ((ReadWriteBuffer<int> shadowMap, ReadWriteBuffer<GPUShadowPixel> shadowBuffer)?[], ConstantBuffer<Float4x4> lightViewProjectionMatrixes)? RenderPointLightShadow(PointLight pointLight, int shadowMapSize, float offsetX, float offsetY, Dictionary<NImage, NGPUImage> convertedTexture)
        {
            var result = new (ReadWriteBuffer<int> shadowMap, ReadWriteBuffer<GPUShadowPixel> shadowBuffer, Float4x4 lightViewProjectionMatrix)?[6];
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

                result[i] = RenderShadow(Device, triangles, lv[i], lightProjectionMatrix, shadowMapSize, pointLight.ShadowStrength, offsetX, offsetY, convertedTexture);
            }

            if (result.Any(t => t.HasValue))
            {
                var lightViewProjectionMatrixes = Device.AllocateConstantBuffer<Float4x4>([..result.Select(t => t.HasValue ? t.Value.lightViewProjectionMatrix : new Float4x4())]);
                return (result.Select(t => t.HasValue ? (t.Value.shadowMap, t.Value.shadowBuffer) : ((ReadWriteBuffer<int>, ReadWriteBuffer<GPUShadowPixel>)?)null).ToArray(), lightViewProjectionMatrixes);
            }
            else
            {
                return null;
            }
        }

        static (ReadWriteBuffer<int> shadowMap, ReadWriteBuffer<GPUShadowPixel> shadowBuffer, Float4x4 lightViewProjectionMatrix)? RenderShadow(
            GraphicsDevice device,
            ShadowTriangle[] triangles,
            in Matrix4x4 lightViewMatrix,
            in Matrix4x4d lightProjectionMatrix,
            int shadowMapSize,
            float shadowStrength,
            float offsetX,
            float offsetY,
            Dictionary<NImage, NGPUImage> convertedTexture
        )
        {
            var preProcessedTriangle = new List<GPUShadowTriangle>(triangles.Length);
            var textures = new List<NGPUImage>(triangles.Length);
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
                var minX = MaxClampedSize((int)(Math.Min(Math.Min(dvv1.GetElement(0), dvv2.GetElement(0)), dvv3.GetElement(0))), 0);
                var maxX = MinClampedSize((int)Math.Ceiling(Math.Max(Math.Max(dvv1.GetElement(0), dvv2.GetElement(0)), dvv3.GetElement(0))), shadowMapSize);
                var minY = MaxClampedSize((int)(Math.Min(Math.Min(dvv1.GetElement(1), dvv2.GetElement(1)), dvv3.GetElement(1))), 0);
                var maxY = MinClampedSize((int)Math.Ceiling(Math.Max(Math.Max(dvv1.GetElement(1), dvv2.GetElement(1)), dvv3.GetElement(1))), shadowMapSize);

                if (minX >= shadowMapSize || maxX <= 0 || minY >= shadowMapSize || maxY <= 0)
                {
                    continue;
                }

                var vvX = Vector128.Create((float)uv1.Vertex.GetElement(0), (float)uv2.Vertex.GetElement(0), (float)uv3.Vertex.GetElement(0), 0.0F);
                var vvY = Vector128.Create((float)uv1.Vertex.GetElement(1), (float)uv2.Vertex.GetElement(1), (float)uv3.Vertex.GetElement(1), 0.0F);
                var vvZ = Vector128.Create((float)uv1.Vertex.GetElement(2), (float)uv2.Vertex.GetElement(2), (float)uv3.Vertex.GetElement(2), 0.0F);
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
                textures.Add(gpuTexture);

                var preProcessed = new GPUShadowTriangle(
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
                preProcessedTriangle.Add(preProcessed);

                var barycenter = Vector256.GetLower((dvv1 + dvv2 + dvv3) / 3.0);
                var maxSize = Math.Max(Math.Abs(maxX - minX), Math.Abs(maxY - minY));
                var scale = (maxSize + 2.0F) / maxSize;
                var sva = (Vector256.GetLower(dvv1) - barycenter) * scale;
                var svb = (Vector256.GetLower(dvv2) - barycenter) * scale - sva;
                var svc = (Vector256.GetLower(dvv3) - barycenter) * scale - sva;

                if (preProcessed.TrueMinX < shadowMapSize && preProcessed.TrueMaxX > 0 && preProcessed.TrueMinY < shadowMapSize && preProcessed.TrueMaxY > 0)
                {
                    area += Math.Min((int)Math.Ceiling(Math.Abs(svb.GetElement(0) * svc.GetElement(1) - svc.GetElement(0) * svb.GetElement(1)) * 0.5), Math.Abs(maxX - minX) * Math.Abs(maxY - minY));
                }
            }

            if (area < 1)
            {
                return null;
            }

            var lightViewProjectionMatrix = (lightViewMatrix * Matrix4x4.CreateTranslation(offsetX, offsetY, 0.0F) * (Matrix4x4)lightProjectionMatrix).ToFloat4x4();
            var floatLightProjectionMatrix = ((Matrix4x4)lightProjectionMatrix).ToFloat4x4();
            using var triangleBuffer = device.AllocateReadOnlyBuffer<GPUShadowTriangle>(CollectionsMarshal.AsSpan(preProcessedTriangle));
            using var counter = device.AllocateReadWriteBuffer([0]);
            var shadowMap = device.AllocateReadWriteBuffer<int>(shadowMapSize * shadowMapSize);
            var shadowBuffer = device.AllocateReadWriteBuffer<GPUShadowPixel>(area);
            using (var context = device.CreateComputeContext())
            {
                context.For(shadowMapSize, shadowMapSize, new InitShadowMap(shadowMap, shadowMapSize));

                foreach (var groupedTriangle in preProcessedTriangle.ZipWithIndex().GroupByPrev(t => t.Item1.Id).Select(g => g.ToArray()))
                {
                    if (groupedTriangle.Length < 1)
                    {
                        continue;
                    }

                    var totalMinX = int.MaxValue;
                    var totalMaxX = int.MinValue;
                    var totalMinY = int.MaxValue;
                    var totalMaxY = int.MinValue;
                    foreach (var (triangle, _) in groupedTriangle)
                    {
                        totalMinX = Math.Min(totalMinX, triangle.TrueMinX);
                        totalMaxX = Math.Max(totalMaxX, triangle.TrueMaxX);
                        totalMinY = Math.Min(totalMinY, triangle.TrueMinY);
                        totalMaxY = Math.Max(totalMaxY, triangle.TrueMaxY);
                    }

                    var texture = textures[groupedTriangle[0].Item2];
                    context.For(
                        totalMaxX - totalMinX,
                        totalMaxY - totalMinY,
                        new RasterizeShadow(
                            counter,
                            shadowMap,
                            shadowBuffer,
                            shadowMapSize,
                            triangleBuffer,
                            groupedTriangle[0].Item2,
                            groupedTriangle[^1].Item2,
                            texture.Data,
                            texture.Width,
                            texture.Height,
                            shadowStrength,
                            totalMinX,
                            totalMinY
                        )
                    );

                    context.Barrier(shadowBuffer);
                }
            }

            return (shadowMap, shadowBuffer, lightViewProjectionMatrix);
        }

        private record TriangleGroup(
            int Id,
            int MinX,
            int MaxX,
            int MinY,
            int MaxY,
            bool UseLight,
            bool IsAcceptLight,
            float Ambient,
            int BlendMode,
            int BeginTriangleIndex,
            int EndTriangleIndex
        );
    }
}
