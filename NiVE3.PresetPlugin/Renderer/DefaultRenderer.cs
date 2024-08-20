using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Numerics;
using NiVE3.Image;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Image;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.PresetPlugin.Resource;
using NiVE3.Shared.Extension;
using NiVE3.PresetPlugin.Internal.Drawing;
using System.Runtime.Intrinsics;
using NiVE3.Plugin.Interfaces.RendererParams;
using System.Runtime.Intrinsics.X86;
using NiVE3.Plugin.ValueObject;
using NiVE3.PresetPlugin.Internal.Drawing.Primitive3D;
using NiVE3.Image.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using NiVE3.PresetPlugin.Internal.ViewModel;
using NiVE3.PresetPlugin.Internal.View;
using ComputeSharp;
using NiVE3.PresetPlugin.Internal.Drawing.ComputeShader;

namespace NiVE3.PresetPlugin.Renderer
{
    [Export(typeof(IRenderer))]
    [RendererMetadata(typeof(DefaultRenderer), LanguageResourceDictionary.Renderer_DefaultRenderer_Name, LanguageResourceDictionary.Renderer_DefaultRenderer_Description, "mes51", "0D30B1E6-3DF3-4A8E-85BB-DCD93BEC7BE0", IsSupportGpu = true, HasSettingView = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public class DefaultRenderer : IRenderer
    {
        const double DefaultFov = 0.691111986546211; // 39.5978 / 180.0 * Math.PI

        const double EpsilonZDiff = 1E-10;

        int Width { get; set; }

        int Height { get; set; }

        NManagedImage? CurrentManagedFrame { get; set; }

        NGPUImage? CurrentGpuFrame { get; set; }

        float CurrentDownScaleRateX { get; set; }

        float CurrentDownScaleRateY { get; set; }

        bool UseGpu { get; set; }

        double FieldOfView { get; set; }

        Matrix4x4d ViewMatrix { get; set; }

        List<PointLight> PointLights { get; } = [];

        List<SpotLight> SpotLights { get; } = [];

        List<ParallelLight> ParallelLights { get; } = [];

        List<AmbientLight> AmbientLights { get; } = [];

        bool EnableAntiAlias { get; set; }

        bool EnableShadowAntiAlias { get; set; }

        IAcceleratorObject? Accelerator { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            Accelerator = accelerator;
        }

        public void SetSize(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public object? SaveSetting()
        {
            return new DefaultRendererSetting
            {
                EnableAntiAlias = EnableAntiAlias,
                EnableShadowAntiAlias = EnableShadowAntiAlias
            };
        }

        public bool LoadSetting(object? data)
        {
            if (data is DefaultRendererSetting setting)
            {
                EnableAntiAlias = setting.EnableAntiAlias;
                EnableShadowAntiAlias = setting.EnableShadowAntiAlias;
                return true;
            }
            else if (data is IDictionary<string, object> dictionary &&
                dictionary.TryGetValue(nameof(DefaultRendererSetting.EnableAntiAlias), out bool enableAntiAlias) &&
                dictionary.TryGetValue(nameof(DefaultRendererSetting.EnableShadowAntiAlias), out bool enableShadowAntiAlias))
            {
                EnableAntiAlias = enableAntiAlias;
                EnableShadowAntiAlias = enableShadowAntiAlias;
                return true;
            }

            return false;
        }

        public FrameworkElement? GetRendererSetting(Int32Size compositionSize)
        {
            var viewModel = new DefaultRendererSettingViewModel
            {
                EnableAntiAlias = EnableAntiAlias,
                EnableShadowAntiAlias = EnableShadowAntiAlias
            };
            return new DefaultRendererSettingView { DataContext = viewModel };
        }

        public bool ApplySetting(object? setting)
        {
            if (setting is DefaultRendererSettingViewModel viewModel)
            {
                EnableAntiAlias = viewModel.EnableAntiAlias;
                EnableShadowAntiAlias = viewModel.EnableShadowAntiAlias;
                return true;
            }
            else
            {
                return false;
            }
        }

        public void BeginRendering(double downSamplingRate, bool useGpu)
        {
            if (CurrentManagedFrame != null || CurrentGpuFrame != null)
            {
                throw new InvalidOperationException("rendering is already started"); // bug
            }

            if (useGpu && Accelerator != null)
            {
                if (downSamplingRate != 1.0)
                {
                    CurrentGpuFrame = new NGPUImage((int)Math.Floor(Width / downSamplingRate), (int)Math.Floor(Height / downSamplingRate), Accelerator.CurrentDevice, new Vector4(1.0F, 1.0F, 1.0F, 0.0F));
                    CurrentDownScaleRateX = Width / (float)CurrentGpuFrame.Width;
                    CurrentDownScaleRateY = Height / (float)CurrentGpuFrame.Height;
                }
                else
                {
                    CurrentGpuFrame = new NGPUImage(Width, Height, Accelerator.CurrentDevice, new Vector4(1.0F, 1.0F, 1.0F, 0.0F));
                    CurrentDownScaleRateX = 1.0F;
                    CurrentDownScaleRateY = 1.0F;
                }
            }
            else
            {
                if (downSamplingRate != 1.0)
                {
                    CurrentManagedFrame = new NManagedImage((int)Math.Floor(Width / downSamplingRate), (int)Math.Floor(Height / downSamplingRate));
                    CurrentManagedFrame.GetDataSpan().Fill(new Vector4(1.0F, 1.0F, 1.0F, 0.0F));
                    CurrentDownScaleRateX = Width / (float)CurrentManagedFrame.Width;
                    CurrentDownScaleRateY = Height / (float)CurrentManagedFrame.Height;
                }
                else
                {
                    CurrentManagedFrame = new NManagedImage(Width, Height);
                    CurrentManagedFrame.GetDataSpan().Fill(new Vector4(1.0F, 1.0F, 1.0F, 0.0F));
                    CurrentDownScaleRateX = 1.0F;
                    CurrentDownScaleRateY = 1.0F;
                }
            }
            UseGpu = useGpu;

            ViewMatrix = CreateDefaultViewMatrix(Width);
            FieldOfView = DefaultFov;
            PointLights.Clear();
            SpotLights.Clear();
            ParallelLights.Clear();
            AmbientLights.Clear();
        }

        public void SetCamera(CameraSetting cameraSetting)
        {
            ViewMatrix = Calc3DViewMatrix(cameraSetting, Width, Height);
            FieldOfView = Math.Atan((Width / cameraSetting.Zoom) * 0.5) * 2.0;
        }

        public void AddLight(LightSetting lightSetting)
        {
            var size = Math.Max(Width, Height);
            var mv = CalcLightMatrix(lightSetting, Width, Height) * ViewMatrix * Matrix4x4d.CreateTranslate((size - Width) * 0.5 / size, (size - Height) * 0.5 / size, 0.0);
            var pos = mv.Transform(Vector256.Create(0.0, 0.0, 0.0, 1.0));

            switch (lightSetting.LightType)
            {
                case LightType.Point:
                    {
                        var light = new PointLight(
                            pos,
                            lightSetting.Color,
                            lightSetting.Intensity * 0.01,
                            lightSetting.FalloffType,
                            lightSetting.FalloffStart / size,
                            lightSetting.FalloffLength / size,
                            lightSetting.IsEnableShadow,
                            lightSetting.ShadowStrength * 0.01,
                            lightSetting.ShadowScatterSize,
                            CalcLightViewMatrixWithoutOffset(lightSetting, Width, Height),
                            Matrix4x4d.CreateTranslate(-(size - Width) * 0.5 / size, -(size - Height) * 0.5 / size, 0.0)
                        );
                        PointLights.Add(light);
                    }
                    break;
                case LightType.Spot:
                    {
                        var poi = mv.Transform(Vector256.Create(0.0, 0.0, 1.0, 1.0));
                        var coneRadian = lightSetting.ConeAngle / 180.0 * Math.PI;
                        var light = new SpotLight(
                            pos,
                            poi,
                            coneRadian,
                            lightSetting.ConeAttenuation * 0.01,
                            lightSetting.Color,
                            lightSetting.Intensity * 0.01,
                            lightSetting.FalloffType,
                            lightSetting.FalloffStart / size,
                            lightSetting.FalloffLength / size,
                            lightSetting.IsEnableShadow,
                            lightSetting.ShadowStrength * 0.01,
                            lightSetting.ShadowScatterSize,
                            CalcLightViewMatrixWithoutOffset(lightSetting, Width, Height).Translate(-(size - Width) * 0.5 / size, -(size - Height) * 0.5 / size, 0.0)
                        );
                        SpotLights.Add(light);
                    }
                    break;
                case LightType.Parallel:
                    {
                        var poi = mv.Transform(Vector256.Create(0.0, 0.0, 1.0, 1.0));
                        var light = new ParallelLight(
                            pos,
                            poi,
                            lightSetting.Color,
                            lightSetting.Intensity * 0.01,
                            lightSetting.FalloffType,
                            lightSetting.FalloffStart / size,
                            lightSetting.FalloffLength / size,
                            lightSetting.IsEnableShadow,
                            lightSetting.ShadowStrength * 0.01,
                            lightSetting.ShadowScatterSize,
                            CalcLightViewMatrixWithoutOffset(lightSetting, Width, Height).Translate(-(size - Width) * 0.5 / size, -(size - Height) * 0.5 / size, 0.0)
                        );
                        ParallelLights.Add(light);
                    }
                    break;
                case LightType.Ambient:
                    {
                        var light = new AmbientLight(lightSetting.Color, lightSetting.Intensity * 0.01);
                        AmbientLights.Add(light);
                    }
                    break;
            }
        }

        public void Render(RenderableImage[] images)
        {
            if (UseGpu)
            {
                RenderGpu(images);
            }
            else
            {
                RenderCpu(images);
            }
        }

        public void RenderAdjustmentLayer(NImage image, ROI roi, double downSamplingRate, ImageInterpolationQuality interpolationQuality, BlendMode blendMode)
        {
            Renderer2DBase? renderer = null;
            var frameWidth = 0;
            var frameHeight = 0;
            if (UseGpu && CurrentGpuFrame != null && Accelerator != null)
            {
                renderer = new GPURenderer2D(CurrentGpuFrame, Accelerator.CurrentDevice);
                frameWidth = CurrentGpuFrame.Width;
                frameHeight = CurrentGpuFrame.Height;
            }
            else if (CurrentManagedFrame != null)
            {
                renderer = new CPURenderer2D(CurrentManagedFrame);
                frameWidth = CurrentManagedFrame.Width;
                frameHeight = CurrentManagedFrame.Height;
            }
            var matrix = Matrix3x3.Identity.Translate((frameWidth - roi.OriginalImageSize.Width) * 0.5F + roi.OriginalImagePosition.X, (frameHeight - roi.OriginalImageSize.Height) * 0.5F + roi.OriginalImagePosition.Y);
            renderer?.DrawSingleImage(roi.OriginalImagePosition, image, 1.0F, matrix, interpolationQuality, blendMode, null);
        }

        public NImage GetCurrentRenderedImage()
        {
            if (UseGpu && CurrentGpuFrame != null)
            {
                return CurrentGpuFrame.Copy();
            }
            else if (CurrentManagedFrame != null)
            {
                return CurrentManagedFrame.Copy();
            }
            else
            {
                throw new InvalidOperationException("rendering not started"); // bug
            }
        }

        public NImage FinishRendering()
        {
            if (UseGpu && CurrentGpuFrame != null)
            {
                var result = CurrentGpuFrame;
                CurrentGpuFrame = null;
                return result;
            }
            else if (CurrentManagedFrame != null)
            {
                var result = CurrentManagedFrame;
                CurrentManagedFrame = null;
                return result;
            }
            else
            {
                throw new InvalidOperationException("rendering not started"); // bug
            }
        }

        public void AbortRendering()
        {
            if (CurrentGpuFrame == null && CurrentManagedFrame == null)
            {
                throw new InvalidOperationException("rendering not started"); // bug
            }

            try
            {
                CurrentGpuFrame?.Dispose();
            }
            catch { }
            try
            {
                CurrentManagedFrame?.Dispose();
            }
            catch { }
            CurrentGpuFrame = null;
            CurrentManagedFrame = null;
        }

        public RasterizedMaskImage RenderAdjustmentMask(RenderableImage image)
        {
            var result = new ManagedRasterizedMaskImage(Width, Height);
            var opacity = (double)(image.Transform[ILayerObject.TransformPropertyOpacityId] ?? 0.0) * 0.01;
            if (opacity <= 0.0)
            {
                return result;
            }

            ManagedRasterizedMaskImage? trackMatte = null;
            if (image.TrackMatteImage != null && image.TrackMatteMode.HasValue)
            {
                var trackMatteImage = image.TrackMatteImage;
                trackMatte = new ManagedRasterizedMaskImage(Width, Height);
                var trackMatteOpacity = (double)(trackMatteImage.Transform[ILayerObject.TransformPropertyOpacityId] ?? 0.0) * 0.01;
                if (trackMatteOpacity <= 0.0)
                {
                    trackMatte.GetDataSpan().Fill((image.TrackMatteMode == TrackMatteMode.InvertAlpha || image.TrackMatteMode == TrackMatteMode.InvertLuminance) ? 1.0F : 0.0F);
                }

                if (trackMatteImage.IsEnable3D)
                {
                    var renderer = new MaskRenderer3D(trackMatte, Width, Height, PointLights, SpotLights, ParallelLights, AmbientLights)
                    {
                        ViewMatrix = ViewMatrix,
                        FieldOfView = FieldOfView
                    };

                    renderer.AddRect(
                        trackMatteImage.ROI.OriginalImagePosition,
                        trackMatteImage.Image,
                        trackMatteImage.InterpolationQuality,
                        (float)trackMatteOpacity,
                        trackMatteImage.BlendMode,
                        Matrix4x4d.CreateScale(trackMatteImage.DownSampleRateX, trackMatteImage.DownSampleRateY, 1.0) * Calc3DModelMatrix(trackMatteImage.Transform, trackMatteImage.ParentTransforms, Width, Height),
                        (ShadowCastMode)(trackMatteImage.LayerOptions?[ILayerObject.ImageLayerOptionIsCastShadowId] ?? ShadowCastMode.None),
                        (float)((double)(trackMatteImage.LayerOptions?[ILayerObject.ImageLayerOptionLightTransmissionId] ?? 0.0) * 0.01),
                        (bool)(trackMatteImage.LayerOptions?[ILayerObject.ImageLayerOptionIsAcceptShadowId] ?? false),
                        (bool)(trackMatteImage.LayerOptions?[ILayerObject.ImageLayerOptionIsAcceptLightId] ?? false),
                        (float)((double)(trackMatteImage.LayerOptions?[ILayerObject.ImageLayerOptionAmbientId] ?? 0.0) * 0.01),
                        (float)((double)(trackMatteImage.LayerOptions?[ILayerObject.ImageLayerOptionDiffuseId] ?? 0.0) * 0.01),
                        (float)((double)(trackMatteImage.LayerOptions?[ILayerObject.ImageLayerOptionSpecularIntensityId] ?? 0.0) * 0.01),
                        (float)((double)(trackMatteImage.LayerOptions?[ILayerObject.ImageLayerOptionSpecularShininessId] ?? 0.0) * 0.01),
                        (float)((double)(trackMatteImage.LayerOptions?[ILayerObject.ImageLayerOptionMetalId] ?? 0.0) * 0.01),
                        null
                    );

                    renderer.Render(image.TrackMatteMode.Value, EnableAntiAlias);
                }
                else
                {
                    var renderer = new CPUMaskRender2D(trackMatte);
                    var downScale = Matrix3x3.CreateScale(1.0F / CurrentDownScaleRateX, 1.0F / CurrentDownScaleRateY);
                    var matrix = Matrix3x3.CreateScale(trackMatteImage.DownSampleRateX, trackMatteImage.DownSampleRateY) * CalcTransform2D(trackMatteImage.Transform, trackMatteImage.ParentTransforms) * downScale;
                    renderer.Draw(trackMatteImage.Image, (float)trackMatteOpacity, matrix, trackMatteImage.InterpolationQuality, null, image.TrackMatteMode.Value);
                }
            }

            if (image.IsEnable3D)
            {
                var renderer = new MaskRenderer3D(result, Width, Height, [], [], [], [])
                {
                    ViewMatrix = ViewMatrix,
                    FieldOfView = FieldOfView
                };

                renderer.AddRect(
                    image.ROI.OriginalImagePosition,
                    image.Image,
                    image.InterpolationQuality,
                    (float)opacity,
                    image.BlendMode,
                    Matrix4x4d.CreateScale(image.DownSampleRateX, image.DownSampleRateY, 1.0) * Calc3DModelMatrix(image.Transform, image.ParentTransforms, Width, Height),
                    (ShadowCastMode)(image.LayerOptions?[ILayerObject.ImageLayerOptionIsCastShadowId] ?? ShadowCastMode.None),
                    (float)((double)(image.LayerOptions?[ILayerObject.ImageLayerOptionLightTransmissionId] ?? 0.0) * 0.01),
                    (bool)(image.LayerOptions?[ILayerObject.ImageLayerOptionIsAcceptShadowId] ?? false),
                    (bool)(image.LayerOptions?[ILayerObject.ImageLayerOptionIsAcceptLightId] ?? false),
                    (float)((double)(image.LayerOptions?[ILayerObject.ImageLayerOptionAmbientId] ?? 0.0) * 0.01),
                    (float)((double)(image.LayerOptions?[ILayerObject.ImageLayerOptionDiffuseId] ?? 0.0) * 0.01),
                    (float)((double)(image.LayerOptions?[ILayerObject.ImageLayerOptionSpecularIntensityId] ?? 0.0) * 0.01),
                    (float)((double)(image.LayerOptions?[ILayerObject.ImageLayerOptionSpecularShininessId] ?? 0.0) * 0.01),
                    (float)((double)(image.LayerOptions?[ILayerObject.ImageLayerOptionMetalId] ?? 0.0) * 0.01),
                    trackMatte
                );

                renderer.Render(TrackMatteMode.Alpha, EnableAntiAlias);
            }
            else
            {
                var renderer = new CPUMaskRender2D(result);
                var downScale = Matrix3x3.CreateScale(1.0F / CurrentDownScaleRateX, 1.0F / CurrentDownScaleRateY);
                var matrix = Matrix3x3.CreateScale(image.DownSampleRateX, image.DownSampleRateY) * CalcTransform2D(image.Transform, image.ParentTransforms) * downScale;
                renderer.Draw(image.Image, (float)opacity, matrix, image.InterpolationQuality, trackMatte, TrackMatteMode.Alpha);
            }

            return result;
        }

        public PreviewBoundingBox GetBoundingBox2D(Vector2d origin, int width, int height, PropertyValueGroup transform, ParentTransform[] parentTransforms)
        {
            var matrix = CalcTransform2D(transform, parentTransforms);
            var anchorPoint = (Vector3d)(transform[ILayerObject.TransformAnchorPointId] ?? new Vector3d());
            var transformedAnchorPoint = (Vector2d)matrix.Transform((Vector2)anchorPoint.AsVector2d());

            matrix = Matrix3x3.CreateTranslate(-(float)origin.X, -(float)origin.Y) * matrix;
            var leftTop = (Vector2d)matrix.Transform(new Vector2(0.0F, 0.0F));
            var rightTop = (Vector2d)matrix.Transform(new Vector2(width, 0.0F));
            var leftBottom = (Vector2d)matrix.Transform(new Vector2(0.0F, height));
            var rightBottom = (Vector2d)matrix.Transform(new Vector2(width, height));
            return new PreviewBoundingBox(
                transformedAnchorPoint,
                [new BoundingBoxShape([leftTop, rightTop, rightBottom, leftBottom], true, false)],
                (leftTop - rightTop).IsZero && (leftTop - leftBottom).IsZero && (rightTop - rightBottom).IsZero && (leftBottom - rightBottom).IsZero,
                transformedAnchorPoint.IsNaN() || transformedAnchorPoint.IsInfinty()
            );
        }

        public PreviewBoundingBox GetBoundingBox3D(Vector2d origin, int width, int height, PropertyValueGroup transform, ParentTransform[] parentTransforms, CameraSetting cameraSetting)
        {
            var size = Math.Max(Width, Height);
            var fov = Math.Atan((Width / cameraSetting.Zoom) * 0.5) * 2.0;
            var anchorPoint = (Vector3d)(transform[ILayerObject.TransformAnchorPointId] ?? new Vector3d());

            var projectionMatrix = Matrix4x4d.CreatePerspectiveFieldOfView(fov, 1.0, double.Epsilon, double.PositiveInfinity);
            var modelMatrix = Calc3DModelMatrix(transform, parentTransforms, Width, Height);
            var viewMatrix = Calc3DViewMatrix(cameraSetting, Width, Height);
            var offsetX = (size - Width) * 0.5 / size;
            var offsetY = (size - Height) * 0.5 / size;
            var offsetMatrix = Matrix4x4d.CreateTranslate(offsetX, offsetY, 0.0);

            var mv = modelMatrix * viewMatrix;
            var anchorPointMv = mv * offsetMatrix;

            // ヌルオブジェクト
            if (width == 0 && height == 0)
            {
                var nav = projectionMatrix.Transform(anchorPointMv.Transform(Avx.Divide(Avx.Add(anchorPoint.AsVector256(), Vector256.Create(0.0, 0.0, 0.0, size)), Vector256.Create((double)size))));
                nav = Avx.Divide(nav, Vector256.Create(nav.GetElement(3)));

                var nullObjectAnchorPoint = ((Vector2d)nav) * (new Vector2d(size, size) * 0.5) + (new Vector2d(Width, Height) * 0.5);
                return new PreviewBoundingBox(nullObjectAnchorPoint, [], true, nullObjectAnchorPoint.IsNaN() || nullObjectAnchorPoint.IsInfinty());
            }

            mv = Matrix4x4d.CreateTranslate(-origin.X / size, -origin.Y / size, 0.0) * mv;
            var mvt = mv * offsetMatrix;

            var sv1 = Vector256.Create(0.0, 0.0, 0.0, size) / size;
            var sv2 = Vector256.Create(0.0, height, 0.0, size) / size;
            var sv3 = Vector256.Create(width, height, 0.0, size) / size;
            var sv4 = Vector256.Create(width, 0.0, 0.0, size) / size;
            var v1 = mvt.Transform(sv1);
            var v2 = mvt.Transform(sv2);
            var v3 = mvt.Transform(sv3);
            var v4 = mvt.Transform(sv4);

            Matrix4x4d.Invert(mv, out var invertedModelViewMatrix);
            invertedModelViewMatrix = Matrix4x4d.Transpose(invertedModelViewMatrix);

            var farPoint = Avx.And(mv.Transform(Vector256.Create(0.0, 0.0, -10000.0, 1.0)), Vector256.Create(0xFFFFFFFFFFFFFFFFUL, 0xFFFFFFFFFFFFFFFFUL, 0xFFFFFFFFFFFFFFFFUL, 0).AsDouble());
            var triangles = TriangleDivider.ClipAndDivide([new BoundingBoxTriangle(v1, v2, v3, farPoint, invertedModelViewMatrix), new BoundingBoxTriangle(v1, v3, v4, farPoint, invertedModelViewMatrix)]).ToArray();
            var projectionOffset = Vector256.Create(offsetX, offsetY, 0.0, 0.0) * size;

            var av = projectionMatrix.Transform(anchorPointMv.Transform(Avx.Divide(Avx.Add(anchorPoint.AsVector256(), Vector256.Create(0.0, 0.0, 0.0, size)), Vector256.Create((double)size))));
            av = Avx.Divide(av, Vector256.Create(av.GetElement(3)));
            var s = new Vector2d(size, size) * 0.5;
            var bbAnchorPoint = ((Vector2d)av) * s + (new Vector2d(Width, Height) * 0.5);

            var points = new List<Vector128<double>>();
            foreach (var triangle in triangles)
            {
                var uv1 = triangle.V1.Transform(projectionMatrix).Vertex;
                var uv2 = triangle.V2.Transform(projectionMatrix).Vertex;
                var uv3 = triangle.V3.Transform(projectionMatrix).Vertex;

                uv1 /= Math.Abs(uv1.GetElement(3));
                uv2 /= Math.Abs(uv2.GetElement(3));
                uv3 /= Math.Abs(uv3.GetElement(3));
                var dvv1 = Avx.ExtractVector128((uv1 + Vector256.Create(1.0, 1.0, 0.0, 0.0)) * Vector256.Create(size * 0.5, size * 0.5, 1.0, 1.0) - projectionOffset, 0);
                var dvv2 = Avx.ExtractVector128((uv2 + Vector256.Create(1.0, 1.0, 0.0, 0.0)) * Vector256.Create(size * 0.5, size * 0.5, 1.0, 1.0) - projectionOffset, 0);
                var dvv3 = Avx.ExtractVector128((uv3 + Vector256.Create(1.0, 1.0, 0.0, 0.0)) * Vector256.Create(size * 0.5, size * 0.5, 1.0, 1.0) - projectionOffset, 0);

                points.Add(dvv1);
                points.Add(dvv2);
                points.Add(dvv3);
            }

            points = [..points.Distinct()];
            if (points.Count < 3)
            {
                // 空
                return new PreviewBoundingBox(bbAnchorPoint, [], true, bbAnchorPoint.IsNaN() || bbAnchorPoint.IsInfinty());
            }

            var pointSpan = CollectionsMarshal.AsSpan(points);
            var orderedPoints = new Vector128<double>[points.Count];
            var used = 1;
            orderedPoints[0] = points.MinBy(p => p.GetElement(0));
            var prev = orderedPoints[0];
            while (used < orderedPoints.Length)
            {
                var b = pointSpan[0];
                for (var i = 1; i < pointSpan.Length; i++)
                {
                    var c = pointSpan[i];
                    if (b == prev)
                    {
                        b = c;
                    }
                    else
                    {
                        var ab = b - prev;
                        var ac = c - prev;
                        var v = ab.CrossProduct(ac);
                        if (v > 0.0 || (v == 0.0 && ac.LengthSquared() > ab.LengthSquared()))
                        {
                            b = c;
                        }
                    }
                }

                orderedPoints[used] = b;
                prev = b;
                used++;
            }

            var shape = new BoundingBoxShape([..orderedPoints.Select(v => (Vector2d)v)], true, false);
            return new PreviewBoundingBox(bbAnchorPoint, [shape], false, bbAnchorPoint.IsNaN() || bbAnchorPoint.IsInfinty());
        }

        public PreviewBoundingBox GetCameraBoundingBox(CameraSetting targetCameraSetting, CameraSetting cameraSetting)
        {
            var size = Math.Max(Width, Height);
            var fov = Math.Atan((Width / cameraSetting.Zoom) * 0.5) * 2.0;

            var projectionMatrix = Matrix4x4d.CreatePerspectiveFieldOfView(fov, 1.0, double.Epsilon, double.PositiveInfinity);
            var viewMatrix = Calc3DViewMatrix(cameraSetting, Width, Height);
            var modelMatrix = GetInvertedCameraMatrix(targetCameraSetting, Width, Height);
            foreach (var (type, parentTransform) in targetCameraSetting.ParentTransforms)
            {
                switch (type)
                {
                    case ParentType.Camera:
                        modelMatrix *= GetInvertedCameraMatrix(parentTransform, Width, Height);
                        break;
                    case ParentType.SpotOrParallelLight:
                    case ParentType.PointLight:
                        modelMatrix *= GetLightMatrix(type == ParentType.SpotOrParallelLight ? LightType.Spot : LightType.Point, parentTransform, Width, Height);
                        break;
                    case ParentType.AmbientLight:
                        break;
                    default:
                        modelMatrix *= GetTransform3D(parentTransform, size);
                        break;
                }
            }

            var mv = modelMatrix * viewMatrix * Matrix4x4d.CreateTranslate((size - Width) * 0.5 / size, (size - Height) * 0.5 / size, 0.0);

            var offset = new Vector2d(Width, Height) * 0.5;
            var s = new Vector2d(size, size) * 0.5;
            var length = (targetCameraSetting.PointOfInterest - targetCameraSetting.Position).Length() / size;
            var pos = (Vector2d)projectionMatrix.Transform(mv.Transform(Vector256.Create(0.0, 0.0, length, 1.0))) * s + offset;
            var poi = (Vector2d)projectionMatrix.Transform(mv.Transform(Vector256.Create(0.0, 0.0, 0.0, 1.0))) * s + offset;
            var zoomLength = targetCameraSetting.Zoom / size;
            var frustumSize = new Vector2d(Width, Height) / s * zoomLength * Math.Tan(fov * 0.5) * 0.5;

            var frustum = new Vector256<double>[][]
            {
                [Vector256.Create(-frustumSize.X, -frustumSize.Y, length - zoomLength, 1.0), Vector256.Create(frustumSize.X, -frustumSize.Y, length - zoomLength, 1.0)],
                [Vector256.Create(-frustumSize.X, -frustumSize.Y, length - zoomLength, 1.0), Vector256.Create(-frustumSize.X, frustumSize.Y, length - zoomLength, 1.0)],
                [Vector256.Create(frustumSize.X, -frustumSize.Y, length - zoomLength, 1.0), Vector256.Create(frustumSize.X, frustumSize.Y, length - zoomLength, 1.0)],
                [Vector256.Create(-frustumSize.X, frustumSize.Y, length - zoomLength, 1.0), Vector256.Create(frustumSize.X, frustumSize.Y, length - zoomLength, 1.0)]
            }.Select(shape => new BoundingBoxShape(shape.Select(v => projectionMatrix.Transform(mv.Transform(v))).Select(v => (Vector2d)Avx.Divide(v, v.Permute4x64(0b11_11_11_11)) * s + offset).Prepend(pos).ToArray(), true, false))
            .Prepend(new BoundingBoxShape([poi, pos], false, false))
            .ToArray();

            return new PreviewBoundingBox(poi, frustum, false, double.IsNaN(fov) || double.IsInfinity(fov) || fov >= 180.0);
        }

        public PreviewBoundingBox GetLightBoundingBox(LightSetting lightSetting, CameraSetting cameraSetting)
        {
            var size = Math.Max(Width, Height);
            var fov = Math.Atan((Width / cameraSetting.Zoom) * 0.5) * 2.0;

            var projectionMatrix = Matrix4x4d.CreatePerspectiveFieldOfView(fov, 1.0, double.Epsilon, double.PositiveInfinity);
            var modelMatrix = CalcLightMatrix(lightSetting, Width, Height);
            var viewMatrix = Calc3DViewMatrix(cameraSetting, Width, Height);

            var mv = modelMatrix * viewMatrix * Matrix4x4d.CreateTranslate((size - Width) * 0.5 / size, (size - Height) * 0.5 / size, 0.0);

            var offset = new Vector2d(Width, Height) * 0.5;
            var s = new Vector2d(size, size) * 0.5;
            var length = (lightSetting.PointOfInterest - lightSetting.Position).Length() / size;
            var pos = (Vector2d)projectionMatrix.Transform(mv.Transform(Vector256.Create(0.0, 0.0, 0.0, 1.0))) * s + offset;
            var poi = (Vector2d)projectionMatrix.Transform(mv.Transform(Vector256.Create(0.0, 0.0, length, 1.0))) * s + offset;

            const double PositionMarkSize = 5.0;
            const int PositionMarkStepCount = 8;
            const double PositionMarkStep = Math.PI * 2.0 / PositionMarkStepCount;
            var positionMark = Enumerable.Range(0, PositionMarkStepCount).Select(i => new Vector2d(Math.Cos(PositionMarkStep * i), Math.Sin(PositionMarkStep * i)) * PositionMarkSize + pos).ToArray();

            return new PreviewBoundingBox(
                lightSetting.LightType == LightType.Point ? pos : poi,
                [new BoundingBoxShape([pos, poi], false, false), new BoundingBoxShape(positionMark, true, true, pos)],
                lightSetting.LightType == LightType.Point,
                lightSetting.LightType == LightType.Ambient || double.IsNaN(lightSetting.ConeAngle) || double.IsInfinity(lightSetting.ConeAngle)
            );
        }

        public Guid? SelectLayer(CameraSetting cameraSetting, LayerSkeleton[] layers, Vector2d pos)
        {
            var result = (Guid?)null;

            var size = Math.Max(Width, Height);
            var offsetX = (size - Width) * 0.5 / size;
            var offsetY = (size - Height) * 0.5 / size;
            var offsetMatrix = Matrix4x4d.CreateTranslate(offsetX, offsetY, 0.0);
            var projectionOffset = Vector256.Create(offsetX, offsetY, 0.0, 0.0) * size;
            var clickPoint = Vector256.Create(pos.X, pos.Y, 0.0, 0.0);

            var viewMatrix = Calc3DViewMatrix(cameraSetting, Width, Height);
            var fov = Math.Atan((Width / (cameraSetting.Zoom)) * 0.5) * 2.0;
            var projectionMatrix = Matrix4x4d.CreatePerspectiveFieldOfView(fov, 1.0, double.Epsilon, double.PositiveInfinity);

            foreach (var group in layers.GroupByPrev(l => l.IsEnable3D))
            {
                if (group.First().IsEnable3D)
                {
                    var triangles = new List<BoundingBoxTriangle>();
                    var ids = new Dictionary<int, Guid>();
                    foreach (var (layer, i) in group.Reverse().ZipWithIndex())
                    {
                        var (t1, t2) = CreateBoundingBoxTriangles(layer, Width, Height, viewMatrix, offsetMatrix, i);
                        triangles.Add(t1);
                        triangles.Add(t2);
                        ids.Add(i, layer.LayerId);
                    }

                    var hit = new HashSet<int>();
                    foreach (var triangle in TriangleDivider.ClipAndDivide(triangles))
                    {
                        if (hit.Contains(triangle.Id))
                        {
                            continue;
                        }

                        var uv1 = triangle.V1.Transform(projectionMatrix).Vertex;
                        var uv2 = triangle.V2.Transform(projectionMatrix).Vertex;
                        var uv3 = triangle.V3.Transform(projectionMatrix).Vertex;

                        var w1 = 1.0 / Math.Abs(uv1.GetElement(3));
                        var w2 = 1.0 / Math.Abs(uv2.GetElement(3));
                        var w3 = 1.0 / Math.Abs(uv3.GetElement(3));
                        uv1 *= w1;
                        uv2 *= w2;
                        uv3 *= w3;
                        var dvv1 = (uv1 + Vector256.Create(1.0, 1.0, 0.0, 0.0)) * Vector256.Create(size * 0.5, size * 0.5, 1.0, 1.0) - projectionOffset;
                        var dvv2 = (uv2 + Vector256.Create(1.0, 1.0, 0.0, 0.0)) * Vector256.Create(size * 0.5, size * 0.5, 1.0, 1.0) - projectionOffset;
                        var dvv3 = (uv3 + Vector256.Create(1.0, 1.0, 0.0, 0.0)) * Vector256.Create(size * 0.5, size * 0.5, 1.0, 1.0) - projectionOffset;

                        var ab = Avx.ExtractVector128(dvv2 - dvv1, 0);
                        var bc = Avx.ExtractVector128(dvv3 - dvv2, 0);
                        var ca = Avx.ExtractVector128(dvv1 - dvv3, 0);
                        var ap = Avx.ExtractVector128(clickPoint - dvv1, 0);
                        var bp = Avx.ExtractVector128(clickPoint - dvv2, 0);
                        var cp = Avx.ExtractVector128(clickPoint - dvv3, 0);

                        var abp = ab.CrossProduct(bp);
                        var bcp = bc.CrossProduct(cp);
                        var cap = ca.CrossProduct(ap);

                        // TODO: Z座標を比較する
                        if ((abp > 0.0 && bcp > 0.0 && cap > 0.0) || (abp < 0.0 && bcp < 0.0 && cap < 0.0))
                        {
                            result = ids[triangle.Id];
                            hit.Add(triangle.Id);
                        }
                    }
                }
                else
                {
                    foreach (var (layerId, (origin, width, height), isEnable3D, transformProperty, parentTransformProperties) in group.Reverse())
                    {
                        var transform = CalcTransform2D(transformProperty, parentTransformProperties);
                        transform = Matrix3x3.CreateTranslate(-(float)origin.X, -(float)origin.Y) * transform;
                        if (!Matrix3x3.Invert(transform, out var inverted))
                        {
                            continue;
                        }

                        var (imageX, imageY) = inverted.Transform((float)pos.X, (float)pos.Y);
                        if (imageX > -1.0F && imageY > -1.0F && imageX < width && imageY < height)
                        {
                            result = layerId;
                            break;
                        }
                    }
                }
            }

            return result;
        }

        public Vector2d LocalCoordToScreenCoord(CameraSetting cameraSetting, LayerSkeleton baseLayer, Vector3d pos)
        {
            if (baseLayer.IsEnable3D)
            {
                var size = Math.Max(Width, Height);
                var fov = Math.Atan((Width / cameraSetting.Zoom) * 0.5) * 2.0;
                var projectionMatrix = Matrix4x4d.CreatePerspectiveFieldOfView(fov, 1.0, double.Epsilon, double.PositiveInfinity);
                var modelMatrix = Calc3DModelMatrix(baseLayer.Transform, baseLayer.ParentTransform, Width, Height);
                var viewMatrix = Calc3DViewMatrix(cameraSetting, Width, Height);
                var offsetX = (size - Width) * 0.5 / size;
                var offsetY = (size - Height) * 0.5 / size;
                var offsetMatrix = Matrix4x4d.CreateTranslate(offsetX, offsetY, 0.0);
                var mvt = modelMatrix * viewMatrix * offsetMatrix;

                var pp = projectionMatrix.Transform(mvt.Transform((pos.AsVector256() + Vector256.Create(0.0, 0.0, 0.0, size)) / Vector256.Create((double)size)));
                return (Vector2d)(pp / pp.GetElement(3) * size * 0.5) + (new Vector2d(Width, Height) * 0.5);
            }
            else
            {
                var transform = CalcTransform2D(baseLayer.Transform, baseLayer.ParentTransform);
                var (screenX, screenY) = transform.Transform((float)pos.X, (float)pos.Y);
                return new Vector2d(screenX, screenY);
            }
        }

        public Vector2d WorldCoordToScreenCoord(CameraSetting cameraSetting, Vector3d pos)
        {
            var size = Math.Max(Width, Height);
            var fov = Math.Atan((Width / cameraSetting.Zoom) * 0.5) * 2.0;
            var projectionMatrix = Matrix4x4d.CreatePerspectiveFieldOfView(fov, 1.0, double.Epsilon, double.PositiveInfinity);
            var viewMatrix = Calc3DViewMatrix(cameraSetting, Width, Height);
            var offsetX = (size - Width) * 0.5 / size;
            var offsetY = (size - Height) * 0.5 / size;
            var offsetMatrix = Matrix4x4d.CreateTranslate(offsetX, offsetY, 0.0);
            var mvt = viewMatrix * offsetMatrix;

            var pp = projectionMatrix.Transform(mvt.Transform((pos.AsVector256() + Vector256.Create(0.0, 0.0, 0.0, size)) / Vector256.Create((double)size)));
            return (Vector2d)(pp / pp.GetElement(3) * size * 0.5) + (new Vector2d(Width, Height) * 0.5);
        }

        public Vector3d ScreenCoordToLocalCoord(CameraSetting cameraSetting, LayerSkeleton baseLayer, Vector2d pos)
        {
            if (baseLayer.IsEnable3D)
            {
                var size = Math.Max(Width, Height);
                var offset = Vector256.Create(size - Width, size - Height, 0.0, 0.0) * 0.5 / size;
                var viewMatrix = Calc3DViewMatrix(cameraSetting, Width, Height);
                var fov = Math.Atan((Width / (cameraSetting.Zoom)) * 0.5) * 2.0;

                var minZ = (double)TriangleDivider.NearZ;
                var maxZ = cameraSetting.Zoom / size;
                var offsetX = (size - Width) * 0.5 / size;
                var offsetY = (size - Height) * 0.5 / size;
                var offsetMatrix = Matrix4x4d.CreateTranslate(offsetX, offsetY, 0.0);

                var (t1, t2) = CreateBoundingBoxTriangles(baseLayer, Width, Height, viewMatrix, offsetMatrix);
                var triangles = TriangleDivider.ClipAndDivide([t1, t2]).ToArray();

                if (triangles.Length > 0)
                {
                    minZ = triangles.Select(t => Math.Min(Math.Min(t.V1.Vertex.GetElement(2), t.V2.Vertex.GetElement(2)), t.V3.Vertex.GetElement(2))).Min();
                    maxZ = triangles.Select(t => Math.Max(Math.Max(t.V1.Vertex.GetElement(2), t.V2.Vertex.GetElement(2)), t.V3.Vertex.GetElement(2))).Max();
                }
                if (maxZ - minZ < EpsilonZDiff)
                {
                    maxZ += EpsilonZDiff;
                }

                var projectionMatrix = Matrix4x4d.CreatePerspectiveFieldOfView(fov, 1.0, minZ, maxZ);
                Matrix4x4d.Invert(viewMatrix * projectionMatrix, out var invertedViewProjection);

                var p = Vector256.Create(pos.X, pos.Y, size * 0.5, size * 0.5) / (size * 0.5) - Vector256.Create(1.0, 1.0, 0.0, 0.0);
                var result = invertedViewProjection.Transform(p);
                var w = result.GetElement(3);
                return (Vector3d)(result / (w != 0.0 ? w : 1.0) * size);
            }
            else
            {
                var transform = CalcTransform2D(baseLayer.Transform, baseLayer.ParentTransform);
                Matrix3x3.Invert(transform, out var inverted);
                var (worldX, worldY) = inverted.Transform((float)pos.X, (float)pos.Y);
                return new Vector3d(worldX, worldY, 0.0);
            }
        }

        public Vector3d ScreenCoordToWorldCoord(CameraSetting cameraSetting, Vector2d pos)
        {
            var size = Math.Max(Width, Height);
            var offset = Vector256.Create(size - Width, size - Height, 0.0, 0.0) * 0.5 / size;
            var viewMatrix = Calc3DViewMatrix(cameraSetting, Width, Height);
            var fov = Math.Atan((Width / (cameraSetting.Zoom)) * 0.5) * 2.0;

            var offsetX = (size - Width) * 0.5 / size;
            var offsetY = (size - Height) * 0.5 / size;
            var offsetMatrix = Matrix4x4d.CreateTranslate(offsetX, offsetY, 0.0);

            var projectionMatrix = Matrix4x4d.CreatePerspectiveFieldOfView(fov, 1.0, TriangleDivider.NearZ, cameraSetting.Zoom / size);
            Matrix4x4d.Invert(viewMatrix * projectionMatrix, out var invertedViewProjection);

            var p = Vector256.Create(pos.X, pos.Y, size * 0.5, size * 0.5) / (size * 0.5) - Vector256.Create(1.0, 1.0, 0.0, 0.0);
            var result = invertedViewProjection.Transform(p);
            var w = result.GetElement(3);
            return (Vector3d)(result / (w != 0.0 ? w : 1.0) * size);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void RenderCpu(RenderableImage[] images)
        {
            if (CurrentManagedFrame == null)
            {
                return;
            }

            var trackMattes = images.ToDictionary(i => i, i =>
            {
                if (i.TrackMatteImage != null && i.TrackMatteMode.HasValue)
                {
                    var trackMatteImage = i.TrackMatteImage;
                    var result = new ManagedRasterizedMaskImage(CurrentManagedFrame.Width, CurrentManagedFrame.Height);
                    var opacity = (double)(trackMatteImage.Transform[ILayerObject.TransformPropertyOpacityId] ?? 0.0) * 0.01;
                    if (opacity <= 0.0)
                    {
                        result.GetDataSpan().Fill((i.TrackMatteMode == TrackMatteMode.InvertAlpha || i.TrackMatteMode == TrackMatteMode.InvertLuminance) ? 1.0F : 0.0F);
                        return result;
                    }

                    if (trackMatteImage.IsEnable3D)
                    {
                        var renderer = new MaskRenderer3D(result, Width, Height, PointLights, SpotLights, ParallelLights, AmbientLights)
                        {
                            ViewMatrix = ViewMatrix,
                            FieldOfView = FieldOfView
                        };

                        renderer.AddRect(
                            trackMatteImage.ROI.OriginalImagePosition,
                            trackMatteImage.Image,
                            trackMatteImage.InterpolationQuality,
                            (float)opacity,
                            trackMatteImage.BlendMode,
                            Matrix4x4d.CreateScale(trackMatteImage.DownSampleRateX, trackMatteImage.DownSampleRateY, 1.0) * Calc3DModelMatrix(trackMatteImage.Transform, trackMatteImage.ParentTransforms, Width, Height),
                            (ShadowCastMode)(trackMatteImage.LayerOptions?[ILayerObject.ImageLayerOptionIsCastShadowId] ?? ShadowCastMode.None),
                            (float)((double)(trackMatteImage.LayerOptions?[ILayerObject.ImageLayerOptionLightTransmissionId] ?? 0.0) * 0.01),
                            (bool)(trackMatteImage.LayerOptions?[ILayerObject.ImageLayerOptionIsAcceptShadowId] ?? false),
                            (bool)(trackMatteImage.LayerOptions?[ILayerObject.ImageLayerOptionIsAcceptLightId] ?? false),
                            (float)((double)(trackMatteImage.LayerOptions?[ILayerObject.ImageLayerOptionAmbientId] ?? 0.0) * 0.01),
                            (float)((double)(trackMatteImage.LayerOptions?[ILayerObject.ImageLayerOptionDiffuseId] ?? 0.0) * 0.01),
                            (float)((double)(trackMatteImage.LayerOptions?[ILayerObject.ImageLayerOptionSpecularIntensityId] ?? 0.0) * 0.01),
                            (float)((double)(trackMatteImage.LayerOptions?[ILayerObject.ImageLayerOptionSpecularShininessId] ?? 0.0) * 0.01),
                            (float)((double)(trackMatteImage.LayerOptions?[ILayerObject.ImageLayerOptionMetalId] ?? 0.0) * 0.01),
                            null
                        );

                        renderer.Render(i.TrackMatteMode.Value, EnableAntiAlias);
                    }
                    else
                    {
                        var renderer = new CPUMaskRender2D(result);
                        var downScale = Matrix3x3.CreateScale(1.0F / CurrentDownScaleRateX, 1.0F / CurrentDownScaleRateY);
                        var matrix = Matrix3x3.CreateScale(i.DownSampleRateX, i.DownSampleRateY) * CalcTransform2D(trackMatteImage.Transform, trackMatteImage.ParentTransforms) * downScale;
                        renderer.Draw(trackMatteImage.Image, (float)opacity, matrix, trackMatteImage.InterpolationQuality, null, i.TrackMatteMode.Value);
                    }
                    return result;
                }
                else
                {
                    return null;
                }
            });

            foreach (var group in images.GroupByPrev(i => i.IsEnable3D))
            {
                if (group.First().IsEnable3D)
                {
                    var renderer = new CPURenderer3D(CurrentManagedFrame, Width, Height, PointLights, SpotLights, ParallelLights, AmbientLights)
                    {
                        ViewMatrix = ViewMatrix,
                        FieldOfView = FieldOfView
                    };

                    foreach (var i in group.Reverse())
                    {
                        var opacity = (double)(i.Transform[ILayerObject.TransformPropertyOpacityId] ?? 0.0) * 0.01;

                        renderer.AddRect(
                            i.ROI.OriginalImagePosition,
                            i.Image,
                            i.InterpolationQuality,
                            (float)opacity,
                            i.BlendMode,
                            Matrix4x4d.CreateScale(i.DownSampleRateX, i.DownSampleRateY, 1.0) * Calc3DModelMatrix(i.Transform, i.ParentTransforms, Width, Height),
                            (ShadowCastMode)(i.LayerOptions?[ILayerObject.ImageLayerOptionIsCastShadowId] ?? ShadowCastMode.None),
                            (float)((double)(i.LayerOptions?[ILayerObject.ImageLayerOptionLightTransmissionId] ?? 0.0) * 0.01),
                            (bool)(i.LayerOptions?[ILayerObject.ImageLayerOptionIsAcceptShadowId] ?? false),
                            (bool)(i.LayerOptions?[ILayerObject.ImageLayerOptionIsAcceptLightId] ?? false),
                            (float)((double)(i.LayerOptions?[ILayerObject.ImageLayerOptionAmbientId] ?? 0.0) * 0.01),
                            (float)((double)(i.LayerOptions?[ILayerObject.ImageLayerOptionDiffuseId] ?? 0.0) * 0.01),
                            (float)((double)(i.LayerOptions?[ILayerObject.ImageLayerOptionSpecularIntensityId] ?? 0.0) * 0.01),
                            (float)((double)(i.LayerOptions?[ILayerObject.ImageLayerOptionSpecularShininessId] ?? 0.0) * 0.01),
                            (float)((double)(i.LayerOptions?[ILayerObject.ImageLayerOptionMetalId] ?? 0.0) * 0.01),
                            trackMattes[i]
                        );
                    }

                    renderer.Render(EnableAntiAlias, EnableShadowAntiAlias);
                }
                else
                {
                    var renderer = new CPURenderer2D(CurrentManagedFrame);

                    var downScale = Matrix3x3.CreateScale(1.0F / CurrentDownScaleRateX, 1.0F / CurrentDownScaleRateY);
                    foreach (var i in group)
                    {
                        var opacity = (double)(i.Transform[ILayerObject.TransformPropertyOpacityId] ?? 0.0) * 0.01;
                        var matrix = Matrix3x3.CreateScale(i.DownSampleRateX, i.DownSampleRateY) * CalcTransform2D(i.Transform, i.ParentTransforms) * downScale;

                        renderer.AddImage(i.ROI.OriginalImagePosition, i.Image, (float)opacity, matrix, i.InterpolationQuality, i.BlendMode, trackMattes[i]);
                    }

                    renderer.Draw();
                }
            }
        }

        void RenderGpu(RenderableImage[] images)
        {
            if (CurrentGpuFrame == null || Accelerator == null)
            {
                return;
            }
            var device = Accelerator.CurrentDevice;

            var trackMattes = images.ToDictionary(i => i, i =>
            {
                if (i.TrackMatteImage != null && i.TrackMatteMode.HasValue)
                {
                    var trackMatteImage = i.TrackMatteImage;
                    var result = new GPURasterizedMaskImage(CurrentGpuFrame.Width, CurrentGpuFrame.Height, device);
                    var opacity = (double)(trackMatteImage.Transform[ILayerObject.TransformPropertyOpacityId] ?? 0.0) * 0.01;
                    if (opacity <= 0.0)
                    {
                        if (i.TrackMatteMode == TrackMatteMode.InvertAlpha || i.TrackMatteMode == TrackMatteMode.InvertLuminance)
                        {
                            using (var context = device.CreateComputeContext())
                            {
                                context.For(result.Width, result.Height, new FillMask(result.Data, result.Width, 1.0F));
                            }
                        }
                        return result;
                    }

                    if (trackMatteImage.IsEnable3D)
                    {
                        var renderer = new GPUMaskRenderer3D(result, device, Width, Height, PointLights, SpotLights, ParallelLights, AmbientLights)
                        {
                            ViewMatrix = ViewMatrix,
                            FieldOfView = FieldOfView
                        };

                        renderer.AddRect(
                            trackMatteImage.ROI.OriginalImagePosition,
                            trackMatteImage.Image,
                            trackMatteImage.InterpolationQuality,
                            (float)opacity,
                            trackMatteImage.BlendMode,
                            Matrix4x4d.CreateScale(trackMatteImage.DownSampleRateX, trackMatteImage.DownSampleRateY, 1.0) * Calc3DModelMatrix(trackMatteImage.Transform, trackMatteImage.ParentTransforms, Width, Height),
                            (ShadowCastMode)(trackMatteImage.LayerOptions?[ILayerObject.ImageLayerOptionIsCastShadowId] ?? ShadowCastMode.None),
                            (float)((double)(trackMatteImage.LayerOptions?[ILayerObject.ImageLayerOptionLightTransmissionId] ?? 0.0) * 0.01),
                            (bool)(trackMatteImage.LayerOptions?[ILayerObject.ImageLayerOptionIsAcceptShadowId] ?? false),
                            (bool)(trackMatteImage.LayerOptions?[ILayerObject.ImageLayerOptionIsAcceptLightId] ?? false),
                            (float)((double)(trackMatteImage.LayerOptions?[ILayerObject.ImageLayerOptionAmbientId] ?? 0.0) * 0.01),
                            (float)((double)(trackMatteImage.LayerOptions?[ILayerObject.ImageLayerOptionDiffuseId] ?? 0.0) * 0.01),
                            (float)((double)(trackMatteImage.LayerOptions?[ILayerObject.ImageLayerOptionSpecularIntensityId] ?? 0.0) * 0.01),
                            (float)((double)(trackMatteImage.LayerOptions?[ILayerObject.ImageLayerOptionSpecularShininessId] ?? 0.0) * 0.01),
                            (float)((double)(trackMatteImage.LayerOptions?[ILayerObject.ImageLayerOptionMetalId] ?? 0.0) * 0.01),
                            null
                        );

                        renderer.Render(i.TrackMatteMode.Value, EnableAntiAlias);
                    }
                    else
                    {
                        var renderer = new GPUMaskRender2D(result, device);
                        var downScale = Matrix3x3.CreateScale(1.0F / CurrentDownScaleRateX, 1.0F / CurrentDownScaleRateY);
                        var matrix = Matrix3x3.CreateScale(i.DownSampleRateX, i.DownSampleRateY) * CalcTransform2D(trackMatteImage.Transform, trackMatteImage.ParentTransforms) * downScale;
                        renderer.Draw(trackMatteImage.Image, (float)opacity, matrix, trackMatteImage.InterpolationQuality, null, i.TrackMatteMode.Value);
                    }
                    return result;
                }
                else
                {
                    return null;
                }
            });

            foreach (var group in images.GroupByPrev(i => i.IsEnable3D))
            {
                if (group.First().IsEnable3D)
                {
                    var renderer = new GPURenderer3D(CurrentGpuFrame, device, Width, Height, PointLights, SpotLights, ParallelLights, AmbientLights)
                    {
                        ViewMatrix = ViewMatrix,
                        FieldOfView = FieldOfView
                    };

                    foreach (var i in group.Reverse())
                    {
                        var opacity = (double)(i.Transform[ILayerObject.TransformPropertyOpacityId] ?? 0.0) * 0.01;
                    
                        renderer.AddRect(
                            i.ROI.OriginalImagePosition,
                            i.Image,
                            i.InterpolationQuality,
                            (float)opacity,
                            i.BlendMode,
                            Matrix4x4d.CreateScale(i.DownSampleRateX, i.DownSampleRateY, 1.0) * Calc3DModelMatrix(i.Transform, i.ParentTransforms, Width, Height),
                            (ShadowCastMode)(i.LayerOptions?[ILayerObject.ImageLayerOptionIsCastShadowId] ?? ShadowCastMode.None),
                            (float)((double)(i.LayerOptions?[ILayerObject.ImageLayerOptionLightTransmissionId] ?? 0.0) * 0.01),
                            (bool)(i.LayerOptions?[ILayerObject.ImageLayerOptionIsAcceptShadowId] ?? false),
                            (bool)(i.LayerOptions?[ILayerObject.ImageLayerOptionIsAcceptLightId] ?? false),
                            (float)((double)(i.LayerOptions?[ILayerObject.ImageLayerOptionAmbientId] ?? 0.0) * 0.01),
                            (float)((double)(i.LayerOptions?[ILayerObject.ImageLayerOptionDiffuseId] ?? 0.0) * 0.01),
                            (float)((double)(i.LayerOptions?[ILayerObject.ImageLayerOptionSpecularIntensityId] ?? 0.0) * 0.01),
                            (float)((double)(i.LayerOptions?[ILayerObject.ImageLayerOptionSpecularShininessId] ?? 0.0) * 0.01),
                            (float)((double)(i.LayerOptions?[ILayerObject.ImageLayerOptionMetalId] ?? 0.0) * 0.01),
                            trackMattes[i]
                        );
                    }

                    renderer.Render(EnableAntiAlias, EnableShadowAntiAlias);
                }
                else
                {
                    var renderer = new GPURenderer2D(CurrentGpuFrame, device);

                    var downScale = Matrix3x3.CreateScale(1.0F / CurrentDownScaleRateX, 1.0F / CurrentDownScaleRateY);
                    foreach (var i in group)
                    {
                        var opacity = (double)(i.Transform[ILayerObject.TransformPropertyOpacityId] ?? 0.0) * 0.01;
                        var matrix = Matrix3x3.CreateScale(i.DownSampleRateX, i.DownSampleRateY) * CalcTransform2D(i.Transform, i.ParentTransforms) * downScale;

                        renderer.AddImage(i.ROI.OriginalImagePosition, i.Image, (float)opacity, matrix, i.InterpolationQuality, i.BlendMode, trackMattes[i]);
                    }

                    renderer.Draw();
                }
            }
        }

        static Matrix3x3 CalcTransform2D(PropertyValueGroup transform, ParentTransform[] parentTransforms)
        {
            var matrix = GetTransform2D(transform);
            foreach (var (_, parentTransform) in parentTransforms)
            {
                matrix *= GetTransform2D(parentTransform);
            }

            return matrix;
        }

        static Matrix4x4d Calc3DModelMatrix(PropertyValueGroup transform, ParentTransform[] parentTransforms, double renderWidth, double renderHeight)
        {
            var size = Math.Max(renderWidth, renderHeight);
            var matrix = GetTransform3D(transform, size);

            foreach (var (type, parentTransform) in parentTransforms)
            {
                switch (type)
                {
                    case ParentType.Camera:
                        matrix *= GetInvertedCameraMatrix(parentTransform, renderWidth, renderHeight);
                        break;
                    case ParentType.SpotOrParallelLight:
                    case ParentType.PointLight:
                        matrix *= GetLightMatrix(type == ParentType.SpotOrParallelLight ? LightType.Spot : LightType.Point, parentTransform, renderWidth, renderHeight);
                        break;
                    case ParentType.AmbientLight:
                        break;
                    default:
                        matrix *= GetTransform3D(parentTransform, size);
                        break;
                }
            }

            return matrix;
        }

        static Matrix4x4d Calc3DViewMatrix(CameraSetting cameraSetting, double renderWidth, double renderHeight)
        {
            var size = Math.Max(renderWidth, renderHeight);
            var view = GetCameraMatrix(cameraSetting, renderWidth, renderHeight);
            foreach (var (type, parentTransform) in cameraSetting.ParentTransforms)
            {
                switch (type)
                {
                    case ParentType.Camera:
                        view = GetCameraMatrix(parentTransform, renderWidth, renderHeight) * view;
                        break;
                    case ParentType.SpotOrParallelLight:
                    case ParentType.PointLight:
                        view = GetLightMatrix(type == ParentType.SpotOrParallelLight ? LightType.Spot : LightType.Point, parentTransform, renderWidth, renderHeight) * view;
                        break;
                    case ParentType.AmbientLight:
                        break;
                    default:
                        if (Matrix4x4d.Invert(GetTransform3D(parentTransform, size), out var inverted))
                        {
                            view = inverted * view;
                        }
                        break;
                }
            }
            return view.Translate(-(size - renderWidth) * 0.5 / size, -(size - renderHeight) * 0.5 / size, 0.0);
        }

        static Matrix4x4d CalcLightMatrix(LightSetting lightSetting, double renderWidth, double renderHeight)
        {
            var size = Math.Max(renderWidth, renderHeight);
            var lightModelMatrix = GetLightMatrix(lightSetting, renderWidth, renderHeight);
            foreach (var (type, parentTransform) in lightSetting.ParentTransforms)
            {
                switch (type)
                {
                    case ParentType.Camera:
                        lightModelMatrix *= GetInvertedCameraMatrix(parentTransform, renderWidth, renderHeight);
                        break;
                    case ParentType.SpotOrParallelLight:
                    case ParentType.PointLight:
                        lightModelMatrix *= GetLightMatrix(type == ParentType.SpotOrParallelLight ? LightType.Spot : LightType.Point, parentTransform, renderWidth, renderHeight);
                        break;
                    case ParentType.AmbientLight:
                        break;
                    default:
                        lightModelMatrix *= GetTransform3D(parentTransform, size);
                        break;
                }
            }

            return lightModelMatrix;
        }

        static Matrix4x4d CalcLightViewMatrixWithoutOffset(LightSetting lightSetting, double renderWidth, double renderHeight)
        {
            var size = Math.Max(renderWidth, renderHeight);
            var view = GetLightViewMatrix(lightSetting.LightType, lightSetting.Position, lightSetting.PointOfInterest, lightSetting.Orientation, lightSetting.AngleX, lightSetting.AngleY, lightSetting.AngleZ, renderWidth, renderHeight);
            foreach (var (type, parentTransform) in lightSetting.ParentTransforms)
            {
                switch (type)
                {
                    case ParentType.Camera:
                        view = GetCameraMatrix(parentTransform, renderWidth, renderHeight) * view;
                        break;
                    case ParentType.SpotOrParallelLight:
                    case ParentType.PointLight:
                        view = GetLightMatrix(type == ParentType.SpotOrParallelLight ? LightType.Spot : LightType.Point, parentTransform, renderWidth, renderHeight) * view;
                        break;
                    case ParentType.AmbientLight:
                        break;
                    default:
                        if (Matrix4x4d.Invert(GetTransform3D(parentTransform, size), out var inverted))
                        {
                            view = inverted * view;
                        }
                        break;
                }
            }
            return view;
        }

        static Matrix3x3 GetTransform2D(PropertyValueGroup transform)
        {
            var anchorPoint = (Vector3d)(transform[ILayerObject.TransformAnchorPointId] ?? transform[ILayerObject.TransformPointOfInterestId] ?? new Vector3d());
            var scale = (Vector3d)(transform[ILayerObject.TransformScaleId] ?? new Vector3d(100.0, 100.0, 100.0)) * 0.01;
            var angle = (double)(transform[ILayerObject.TransformZAngleId] ?? 0.0);
            var translate = (Vector3d)(transform[ILayerObject.TransformPositionId] ?? new Vector3d());
            return Matrix3x3.AffineTransform((Vector2)anchorPoint.AsVector2d(), (Vector2)scale.AsVector2d(), (float)angle, (Vector2)translate.AsVector2d());
        }

        static Matrix4x4d GetTransform3D(PropertyValueGroup transform, double rendererSize)
        {
            var anchorPoint = (Vector3d)(transform[ILayerObject.TransformAnchorPointId] ?? new Vector3d()) / rendererSize;
            var scale = (Vector3d)(transform[ILayerObject.TransformScaleId] ?? new Vector3d()) * 0.01;
            var direction = (Vector3d)(transform[ILayerObject.TransformDirectionId] ?? new Vector3d());
            var angleX = (double)(transform[ILayerObject.TransformXAngleId] ?? 0.0);
            var angleY = (double)(transform[ILayerObject.TransformYAngleId] ?? 0.0);
            var angleZ = (double)(transform[ILayerObject.TransformZAngleId] ?? 0.0);
            var translate = (Vector3d)(transform[ILayerObject.TransformPositionId] ?? new Vector3d()) / rendererSize;

            return Matrix4x4d.AffineTransform(anchorPoint, scale, direction, angleX, angleY, angleZ, translate);
        }

        static Matrix4x4d GetCameraMatrix(CameraSetting cameraSetting, double renderWidth, double renderHeight)
        {
            return GetCameraMatrix(cameraSetting.Position, cameraSetting.PointOfInterest, cameraSetting.Orientation, cameraSetting.AngleX, cameraSetting.AngleY, cameraSetting.AngleZ, renderWidth, renderHeight);
        }

        static Matrix4x4d GetCameraMatrix(PropertyValueGroup transform, double renderWidth, double renderHeight)
        {
            return GetCameraMatrix(
                (Vector3d)(transform[ILayerObject.TransformPositionId] ?? new Vector3d()),
                (Vector3d)(transform[ILayerObject.TransformPointOfInterestId] ?? new Vector3d()),
                (Vector3d)(transform[ILayerObject.TransformOrientationId] ?? new Vector3d()),
                (double)(transform[ILayerObject.TransformXAngleId] ?? 0.0),
                (double)(transform[ILayerObject.TransformXAngleId] ?? 0.0),
                (double)(transform[ILayerObject.TransformXAngleId] ?? 0.0),
                renderWidth,
                renderHeight
            );
        }

        static Matrix4x4d GetCameraMatrix(in Vector3d pos, in Vector3d poi, in Vector3d orientation, double angleX, double angleY, double angleZ, double renderWidth, double renderHeight)
        {
            var size = Math.Max(renderWidth, renderHeight);
            var pos256 = Avx.Divide(pos.AsVector256(), Vector256.Create(size));
            var poi256 = Avx.Divide(poi.AsVector256(), Vector256.Create(size));

            var diff = Avx.Subtract(poi256, pos256);
            var x = diff.GetElement(0);
            var y = diff.GetElement(1);
            var z = diff.GetElement(2);

            return Matrix4x4d.Identity
                .Translate(-pos256.GetElement(0), -pos256.GetElement(1), -pos256.GetElement(2))
                .RotateY(-Math.Atan2(x, z) / Math.PI * 180.0)
                .RotateX(Math.Atan2(y, Math.Sqrt(x * x + z * z)) / Math.PI * 180.0)
                .RotateX(orientation.X)
                .RotateY(orientation.Y)
                .RotateZ(orientation.Z)
                .RotateX(angleX)
                .RotateY(angleY)
                .RotateZ(angleZ);
        }

        static Matrix4x4d GetInvertedCameraMatrix(CameraSetting cameraSetting, double renderWidth, double renderHeight)
        {
            return GetInvertedCameraMatrix(cameraSetting.PointOfInterest, cameraSetting.Position, cameraSetting.Orientation, cameraSetting.AngleX, cameraSetting.AngleY, cameraSetting.AngleZ, renderWidth, renderHeight);
        }

        static Matrix4x4d GetInvertedCameraMatrix(PropertyValueGroup transform, double renderWidth, double renderHeight)
        {
            return GetInvertedCameraMatrix(
                (Vector3d)(transform[ILayerObject.TransformPositionId] ?? new Vector3d()),
                (Vector3d)(transform[ILayerObject.TransformPointOfInterestId] ?? new Vector3d()),
                (Vector3d)(transform[ILayerObject.TransformOrientationId] ?? new Vector3d()),
                (double)(transform[ILayerObject.TransformXAngleId] ?? 0.0),
                (double)(transform[ILayerObject.TransformYAngleId] ?? 0.0),
                (double)(transform[ILayerObject.TransformZAngleId] ?? 0.0),
                renderWidth,
                renderHeight
            );
        }

        static Matrix4x4d GetInvertedCameraMatrix(in Vector3d pos, in Vector3d poi, in Vector3d orientation, double angleX, double angleY, double angleZ, double renderWidth, double renderHeight)
        {
            var size = Math.Max(renderWidth, renderHeight);
            var pos256 = Avx.Divide(pos.AsVector256(), Vector256.Create(size));
            var poi256 = Avx.Divide(poi.AsVector256(), Vector256.Create(size));

            var diff = Avx.Subtract(poi256, pos256);
            var x = diff.GetElement(0);
            var y = diff.GetElement(1);
            var z = diff.GetElement(2);

            return Matrix4x4d.Identity
                .RotateZ(-angleZ)
                .RotateY(-angleY)
                .RotateX(-angleX)
                .RotateZ(-orientation.Z)
                .RotateY(-orientation.Y)
                .RotateX(-orientation.X)
                .RotateX(-Math.Atan2(y, Math.Sqrt(x * x + z * z)) / Math.PI * 180.0)
                .RotateY(Math.Atan2(x, z) / Math.PI * 180.0)
                .Translate(pos256.GetElement(0), pos256.GetElement(1), pos256.GetElement(2));
        }

        static Matrix4x4d GetLightMatrix(LightSetting lightSetting, double renderWidth, double renderHeight)
        {
            return GetLightMatrix(lightSetting.LightType, lightSetting.Position, lightSetting.PointOfInterest, lightSetting.Orientation, lightSetting.AngleX, lightSetting.AngleY, lightSetting.AngleZ, renderWidth, renderHeight);
        }

        static Matrix4x4d GetLightMatrix(LightType lightType, PropertyValueGroup transform, double renderWidth, double renderHeight)
        {
            return GetLightMatrix(
                lightType,
                (Vector3d)(transform[ILayerObject.TransformPositionId] ?? new Vector3d()),
                (Vector3d)(transform[ILayerObject.TransformPointOfInterestId] ?? new Vector3d()),
                (Vector3d)(transform[ILayerObject.TransformOrientationId] ?? new Vector3d()),
                (double)(transform[ILayerObject.TransformXAngleId] ?? 0.0),
                (double)(transform[ILayerObject.TransformXAngleId] ?? 0.0),
                (double)(transform[ILayerObject.TransformXAngleId] ?? 0.0),
                renderWidth,
                renderHeight
            );
        }

        static Matrix4x4d GetLightMatrix(LightType lightType, in Vector3d pos, in Vector3d poi, in Vector3d orientation, double angleX, double angleY, double angleZ, double renderWidth, double renderHeight)
        {
            var size = Math.Max(renderWidth, renderHeight);
            var pos256 = Avx.Divide(pos.AsVector256(), Vector256.Create(size));
            switch (lightType)
            {
                case LightType.Point:
                    return Matrix4x4d.CreateTranslate(pos256.GetElement(0), pos256.GetElement(1), pos256.GetElement(2));
                case LightType.Spot:
                case LightType.Parallel:
                    {
                        var poi256 = Avx.Divide(poi.AsVector256(), Vector256.Create(size));

                        var diff = Avx.Subtract(poi256, pos256);
                        var x = diff.GetElement(0);
                        var y = diff.GetElement(1);
                        var z = diff.GetElement(2);

                        return Matrix4x4d.Identity
                            .RotateZ(-angleZ)
                            .RotateY(-angleY)
                            .RotateX(-angleX)
                            .RotateZ(-orientation.Z)
                            .RotateY(-orientation.Y)
                            .RotateX(-orientation.X)
                            .RotateX(-Math.Atan2(y, Math.Sqrt(x * x + z * z)) / Math.PI * 180.0)
                            .RotateY(Math.Atan2(x, z) / Math.PI * 180.0)
                            .Translate(pos256.GetElement(0), pos256.GetElement(1), pos256.GetElement(2));
                    }
                default:
                    return Matrix4x4d.Identity;
            }
        }

        static Matrix4x4d GetLightViewMatrix(LightType lightType, in Vector3d pos, in Vector3d poi, in Vector3d orientation, double angleX, double angleY, double angleZ, double renderWidth, double renderHeight)
        {
            var size = Math.Max(renderWidth, renderHeight);
            var pos256 = Avx.Divide(pos.AsVector256(), Vector256.Create(size));
            switch (lightType)
            {
                case LightType.Point:
                    return Matrix4x4d.CreateTranslate(-pos256.GetElement(0), -pos256.GetElement(1), -pos256.GetElement(2));
                case LightType.Spot:
                case LightType.Parallel:
                    {
                        var poi256 = Avx.Divide(poi.AsVector256(), Vector256.Create(size));

                        var diff = Avx.Subtract(poi256, pos256);
                        var x = diff.GetElement(0);
                        var y = diff.GetElement(1);
                        var z = diff.GetElement(2);

                        return Matrix4x4d.Identity
                            .Translate(-pos256.GetElement(0), -pos256.GetElement(1), -pos256.GetElement(2))
                            .RotateY(-Math.Atan2(x, z) / Math.PI * 180.0)
                            .RotateX(Math.Atan2(y, Math.Sqrt(x * x + z * z)) / Math.PI * 180.0)
                            .RotateX(orientation.X)
                            .RotateY(orientation.Y)
                            .RotateZ(orientation.Z)
                            .RotateX(angleX)
                            .RotateY(angleY)
                            .RotateZ(angleZ);
                    }
                default:
                    return Matrix4x4d.Identity;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static (BoundingBoxTriangle, BoundingBoxTriangle) CreateBoundingBoxTriangles(LayerSkeleton layerSkeleton, int compositionWidth, int compositionHeight, in Matrix4x4d viewMatrix, in Matrix4x4d offsetMatrix, int index = 0)
        {
            var size = Math.Max(compositionWidth, compositionHeight);
            var (_, (origin, width, height), isEnable3D, transformProperty, parentTransformProperties) = layerSkeleton;

            var modelMatrix = Matrix4x4d.CreateTranslate(-origin.X / size, -origin.Y / size, 0.0) * Calc3DModelMatrix(transformProperty, parentTransformProperties, compositionWidth, compositionHeight);
            var mv = modelMatrix * viewMatrix;
            var mvt = mv * offsetMatrix;
            var sv1 = Vector256.Create(0.0, 0.0, 0.0, size) / size;
            var sv2 = Vector256.Create(0.0, height, 0.0, size) / size;
            var sv3 = Vector256.Create(width, height, 0.0, size) / size;
            var sv4 = Vector256.Create(width, 0.0, 0.0, size) / size;
            var v1 = mvt.Transform(sv1);
            var v2 = mvt.Transform(sv2);
            var v3 = mvt.Transform(sv3);
            var v4 = mvt.Transform(sv4);

            Matrix4x4d.Invert(mv, out var invertedModelViewMatrix);
            invertedModelViewMatrix = Matrix4x4d.Transpose(invertedModelViewMatrix);

            var farPoint = Avx.And(mv.Transform(Vector256.Create(0.0, 0.0, -10000.0, 1.0)), Vector256.Create(0xFFFFFFFFFFFFFFFFUL, 0xFFFFFFFFFFFFFFFFUL, 0xFFFFFFFFFFFFFFFFUL, 0).AsDouble());

            return (new BoundingBoxTriangle(v1, v2, v3, farPoint, invertedModelViewMatrix, index), new BoundingBoxTriangle(v1, v3, v4, farPoint, invertedModelViewMatrix, index));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Matrix4x4d CreateDefaultViewMatrix(double width)
        {
            var zoom = width / Math.Tan(DefaultFov * 0.5) * 0.5;
            return Matrix4x4d.CreateLookAt(Vector256.Create(0.5, 0.5, -zoom / width, 0.0), Vector256.Create(0.5, 0.5, 0.0, 0.0), Vector256.Create(0.0, 1.0, 0.0, 0.0));
        }

        public void Dispose()
        {
            CurrentManagedFrame?.Dispose();
            GC.SuppressFinalize(this);
        }
    }

    class DefaultRendererSetting
    {
        public bool EnableAntiAlias { get; set; }

        public bool EnableShadowAntiAlias { get; set; }
    }
}
