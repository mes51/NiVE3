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
using NiVE3.PresetPlugin.Internal.Util;

namespace NiVE3.PresetPlugin.Renderer
{
    [Export(typeof(IRenderer))]
    [RendererMetadata(typeof(DefaultRenderer), typeof(DefaultTransformer), LanguageResourceDictionary.Renderer_DefaultRenderer_Name, LanguageResourceDictionary.Renderer_DefaultRenderer_Description, "mes51", "0D30B1E6-3DF3-4A8E-85BB-DCD93BEC7BE0", IsSupportGpu = true, HasSettingView = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
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
                    CurrentGpuFrame = new NGPUImage((int)Math.Floor(Width / downSamplingRate), (int)Math.Floor(Height / downSamplingRate), Accelerator.CurrentDevice);
                    CurrentDownScaleRateX = Width / (float)CurrentGpuFrame.Width;
                    CurrentDownScaleRateY = Height / (float)CurrentGpuFrame.Height;
                }
                else
                {
                    CurrentGpuFrame = new NGPUImage(Width, Height, Accelerator.CurrentDevice);
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
            ViewMatrix = Transform3D.Calc3DViewMatrix(cameraSetting, Width, Height);
            FieldOfView = Math.Atan((Width / cameraSetting.Zoom) * 0.5) * 2.0;
        }

        public void AddLight(LightSetting lightSetting)
        {
            var size = Math.Max(Width, Height);
            var mv = Transform3D.CalcLightMatrix(lightSetting, Width, Height) * ViewMatrix * Matrix4x4d.CreateTranslate((size - Width) * 0.5 / size, (size - Height) * 0.5 / size, 0.0);
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
                            Transform3D.CalcLightViewMatrixWithoutOffset(lightSetting, Width, Height),
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
                            Transform3D.CalcLightViewMatrixWithoutOffset(lightSetting, Width, Height).Translate(-(size - Width) * 0.5 / size, -(size - Height) * 0.5 / size, 0.0)
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
                            Transform3D.CalcLightViewMatrixWithoutOffset(lightSetting, Width, Height).Translate(-(size - Width) * 0.5 / size, -(size - Height) * 0.5 / size, 0.0)
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
            var matrix = Matrix3x3.Identity.Translate((frameWidth - roi.OriginalImageSize.Width) * 0.5F, (frameHeight - roi.OriginalImageSize.Height) * 0.5F);
            renderer?.DrawSingleImage(roi.OriginalImagePosition, image, 1.0F, matrix, interpolationQuality, blendMode, null);
        }

        public NImage GetCurrentRenderedImage()
        {
            if (UseGpu && CurrentGpuFrame != null && Accelerator != null)
            {
                var result = new NGPUImage(CurrentGpuFrame.Width, CurrentGpuFrame.Height, Accelerator.CurrentDevice);
                CurrentGpuFrame.CopyTo(result);
                return result;
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
            if (UseGpu)
            {
                return RenderAdjustmentMaskGpu(image);
            }
            else
            {
                return RenderAdjustmentMaskCpu(image);
            }
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
                            Vector4.One,
                            (float)opacity,
                            trackMatteImage.BlendMode,
                            Matrix4x4d.CreateScale(trackMatteImage.DownSampleRateX, trackMatteImage.DownSampleRateY, 1.0) * Transform3D.Calc3DModelMatrix(trackMatteImage.Transform, trackMatteImage.ParentTransforms, Width, Height),
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
                        var matrix = Matrix3x3.CreateScale(i.DownSampleRateX, i.DownSampleRateY) * Transform2D.CalcTransform2D(trackMatteImage.Transform, trackMatteImage.ParentTransforms) * downScale;
                        renderer.Draw(i.ROI.OriginalImagePosition, trackMatteImage.Image, (float)opacity, matrix, trackMatteImage.InterpolationQuality, null, i.TrackMatteMode.Value);
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
                            Vector4.One,
                            (float)opacity,
                            i.BlendMode,
                            Matrix4x4d.CreateScale(i.DownSampleRateX, i.DownSampleRateY, 1.0) * Transform3D.Calc3DModelMatrix(i.Transform, i.ParentTransforms, Width, Height),
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
                        var matrix = Matrix3x3.CreateScale(i.DownSampleRateX, i.DownSampleRateY) * Transform2D.CalcTransform2D(i.Transform, i.ParentTransforms) * downScale;

                        renderer.AddImage(i.ROI.OriginalImagePosition, i.Image, (float)opacity, matrix, i.InterpolationQuality, i.BlendMode, trackMattes[i]);
                    }

                    renderer.Draw();
                }
            }

            foreach (var trackMatte in trackMattes.Values.NonNull())
            {
                trackMatte.Dispose();
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
                            Vector4.One,
                            (float)opacity,
                            trackMatteImage.BlendMode,
                            Matrix4x4d.CreateScale(trackMatteImage.DownSampleRateX, trackMatteImage.DownSampleRateY, 1.0) * Transform3D.Calc3DModelMatrix(trackMatteImage.Transform, trackMatteImage.ParentTransforms, Width, Height),
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
                        var matrix = Matrix3x3.CreateScale(i.DownSampleRateX, i.DownSampleRateY) * Transform2D.CalcTransform2D(trackMatteImage.Transform, trackMatteImage.ParentTransforms) * downScale;
                        renderer.Draw(trackMatteImage.ROI.OriginalImagePosition, trackMatteImage.Image, (float)opacity, matrix, trackMatteImage.InterpolationQuality, null, i.TrackMatteMode.Value);
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
                            Vector4.One,
                            (float)opacity,
                            i.BlendMode,
                            Matrix4x4d.CreateScale(i.DownSampleRateX, i.DownSampleRateY, 1.0) * Transform3D.Calc3DModelMatrix(i.Transform, i.ParentTransforms, Width, Height),
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
                        var matrix = Matrix3x3.CreateScale(i.DownSampleRateX, i.DownSampleRateY) * Transform2D.CalcTransform2D(i.Transform, i.ParentTransforms) * downScale;

                        renderer.AddImage(i.ROI.OriginalImagePosition, i.Image, (float)opacity, matrix, i.InterpolationQuality, i.BlendMode, trackMattes[i]);
                    }

                    renderer.Draw();
                }
            }

            foreach (var trackMatte in trackMattes.Values.NonNull())
            {
                trackMatte.Dispose();
            }
        }

        ManagedRasterizedMaskImage RenderAdjustmentMaskCpu(RenderableImage image)
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
                        Vector4.One,
                        (float)trackMatteOpacity,
                        trackMatteImage.BlendMode,
                        Matrix4x4d.CreateScale(trackMatteImage.DownSampleRateX, trackMatteImage.DownSampleRateY, 1.0) * Transform3D.Calc3DModelMatrix(trackMatteImage.Transform, trackMatteImage.ParentTransforms, Width, Height),
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
                    var matrix = Matrix3x3.CreateScale(trackMatteImage.DownSampleRateX, trackMatteImage.DownSampleRateY) * Transform2D.CalcTransform2D(trackMatteImage.Transform, trackMatteImage.ParentTransforms) * downScale;
                    renderer.Draw(trackMatteImage.ROI.OriginalImagePosition, trackMatteImage.Image, (float)trackMatteOpacity, matrix, trackMatteImage.InterpolationQuality, null, image.TrackMatteMode.Value);
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
                    Vector4.One,
                    (float)opacity,
                    image.BlendMode,
                    Matrix4x4d.CreateScale(image.DownSampleRateX, image.DownSampleRateY, 1.0) * Transform3D.Calc3DModelMatrix(image.Transform, image.ParentTransforms, Width, Height),
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
                var matrix = Matrix3x3.CreateScale(image.DownSampleRateX, image.DownSampleRateY) * Transform2D.CalcTransform2D(image.Transform, image.ParentTransforms) * downScale;
                renderer.Draw(image.ROI.OriginalImagePosition, image.Image, (float)opacity, matrix, image.InterpolationQuality, trackMatte, TrackMatteMode.Alpha);
            }

            return result;
        }

        GPURasterizedMaskImage RenderAdjustmentMaskGpu(RenderableImage image)
        {
            if (Accelerator == null)
            {
                throw new InvalidOperationException(); // bug
            }
            var device = Accelerator.CurrentDevice;

            var result = new GPURasterizedMaskImage(Width, Height, device);
            var opacity = (double)(image.Transform[ILayerObject.TransformPropertyOpacityId] ?? 0.0) * 0.01;
            if (opacity <= 0.0)
            {
                return result;
            }

            GPURasterizedMaskImage? trackMatte = null;
            if (image.TrackMatteImage != null && image.TrackMatteMode.HasValue)
            {
                var trackMatteImage = image.TrackMatteImage;
                var trackMatteOpacity = (double)(trackMatteImage.Transform[ILayerObject.TransformPropertyOpacityId] ?? 0.0) * 0.01;
                trackMatte = new GPURasterizedMaskImage(Width, Height, device, (image.TrackMatteMode == TrackMatteMode.InvertAlpha || image.TrackMatteMode == TrackMatteMode.InvertLuminance) ? 1.0F : 0.0F);

                if (trackMatteImage.IsEnable3D)
                {
                    var renderer = new GPUMaskRenderer3D(trackMatte, device, Width, Height, PointLights, SpotLights, ParallelLights, AmbientLights)
                    {
                        ViewMatrix = ViewMatrix,
                        FieldOfView = FieldOfView
                    };

                    renderer.AddRect(
                        trackMatteImage.ROI.OriginalImagePosition,
                        trackMatteImage.Image,
                        trackMatteImage.InterpolationQuality,
                        Vector4.One,
                        (float)trackMatteOpacity,
                        trackMatteImage.BlendMode,
                        Matrix4x4d.CreateScale(trackMatteImage.DownSampleRateX, trackMatteImage.DownSampleRateY, 1.0) * Transform3D.Calc3DModelMatrix(trackMatteImage.Transform, trackMatteImage.ParentTransforms, Width, Height),
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
                    var renderer = new GPUMaskRender2D(trackMatte, device);
                    var downScale = Matrix3x3.CreateScale(1.0F / CurrentDownScaleRateX, 1.0F / CurrentDownScaleRateY);
                    var matrix = Matrix3x3.CreateScale(trackMatteImage.DownSampleRateX, trackMatteImage.DownSampleRateY) * Transform2D.CalcTransform2D(trackMatteImage.Transform, trackMatteImage.ParentTransforms) * downScale;
                    renderer.Draw(trackMatteImage.ROI.OriginalImagePosition, trackMatteImage.Image, (float)trackMatteOpacity, matrix, trackMatteImage.InterpolationQuality, null, image.TrackMatteMode.Value);
                }
            }

            if (image.IsEnable3D)
            {
                var renderer = new GPUMaskRenderer3D(result, device, Width, Height, [], [], [], [])
                {
                    ViewMatrix = ViewMatrix,
                    FieldOfView = FieldOfView
                };

                renderer.AddRect(
                    image.ROI.OriginalImagePosition,
                    image.Image,
                    image.InterpolationQuality,
                    Vector4.One,
                    (float)opacity,
                    image.BlendMode,
                    Matrix4x4d.CreateScale(image.DownSampleRateX, image.DownSampleRateY, 1.0) * Transform3D.Calc3DModelMatrix(image.Transform, image.ParentTransforms, Width, Height),
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
                var renderer = new GPUMaskRender2D(result, device);
                var downScale = Matrix3x3.CreateScale(1.0F / CurrentDownScaleRateX, 1.0F / CurrentDownScaleRateY);
                var matrix = Matrix3x3.CreateScale(image.DownSampleRateX, image.DownSampleRateY) * Transform2D.CalcTransform2D(image.Transform, image.ParentTransforms) * downScale;
                renderer.Draw(image.ROI.OriginalImagePosition, image.Image, (float)opacity, matrix, image.InterpolationQuality, trackMatte, TrackMatteMode.Alpha);
            }

            return result;
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
