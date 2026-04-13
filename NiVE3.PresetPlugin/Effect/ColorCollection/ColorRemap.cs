using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Text;
using ComputeSharp;
using NiVE3.Image;
using NiVE3.Image.Color;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Property.Properties;
using NiVE3.Plugin.Resource;
using NiVE3.Plugin.ValueObject;
using NiVE3.PresetPlugin.Effect.Generate;
using NiVE3.PresetPlugin.Effect.Util;
using NiVE3.PresetPlugin.Extension;
using NiVE3.PresetPlugin.Internal;
using NiVE3.PresetPlugin.Internal.ComputeShader;
using NiVE3.PresetPlugin.Internal.Drawing;
using NiVE3.PresetPlugin.Internal.Util;
using NiVE3.PresetPlugin.Resource;
using NiVE3.Shared.Extension;
using SharpGen.Runtime;

namespace NiVE3.PresetPlugin.Effect.ColorCollection
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.ColorCollection_ColorRemap_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_ColorCollection, LanguageResourceDictionary.ColorCollection_ColorRemap_Description, ID, IsRenderEveryFrame = true, IsSupportGpu = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class ColorRemap : IEffect
    {
        const string ID = "167CC5EC-A8AB-4AAB-95A8-0623D08952E0";

        const string PropertySourceGroupId = nameof(PropertySourceGroupId);

        const string PropertySourceSourceChanneld = nameof(PropertySourceSourceChanneld);

        const string PropertySourceAdditionalSourceLayerId = nameof(PropertySourceAdditionalSourceLayerId);

        const string PropertySourceAdditionalSourceLayerPositionId = nameof(PropertySourceAdditionalSourceLayerPositionId);

        const string PropertySourceAdditionalSourceChannelId = nameof(PropertySourceAdditionalSourceChannelId);

        const string PropertySourceSourcePhaseBlendModeId = nameof(PropertySourceSourcePhaseBlendModeId);

        const string PropertySourceSourcePhaseLoopModeId = nameof(PropertySourceSourcePhaseLoopModeId);

        const string PropertySourcePhaseShiftId = nameof(PropertySourcePhaseShiftId);

        const string PropertyOutputGroupId = nameof(PropertyOutputGroupId);

        const string PropertyOutputColorMapId = nameof(PropertyOutputColorMapId);

        const string PropertyOutputUseOkLabInterpolationId = nameof(PropertyOutputUseOkLabInterpolationId);

        const string PropertyOutputCycleCountId = nameof(PropertyOutputCycleCountId);

        const string PropertyBlendOriginalId = nameof(PropertyBlendOriginalId);

        IAcceleratorObject? AcceleratorObject { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            AcceleratorObject = accelerator;
        }

        public PropertyBase[] GetProperties(Int32Size sourceSize)
        {
            var defaultColorStops = new ColorStop[]
            {
                new ColorStop(new Vector4(0.0F, 0.0F, 1.0F, 1.0F), 0.0F),
                new ColorStop(new Vector4(0.0F, 1.0F, 1.0F, 1.0F), 60.0F / 360.0F),
                new ColorStop(new Vector4(0.0F, 1.0F, 0.0F, 1.0F), 120.0F / 360.0F),
                new ColorStop(new Vector4(1.0F, 1.0F, 0.0F, 1.0F), 0.5F),
                new ColorStop(new Vector4(1.0F, 0.0F, 0.0F, 1.0F), 240.0F / 360.0F),
                new ColorStop(new Vector4(1.0F, 0.0F, 1.0F, 1.0F), 300.0F / 360.0F)
            };
            var defaultGradient = new ColorGradient(defaultColorStops, new OpacityStop[] { new OpacityStop(1.0F, 0.0F), new OpacityStop(1.0F, 1.0F) });
            return
            [
                new PropertyGroup(PropertySourceGroupId, LanguageResourceDictionary.ResourceKeys.ColorCollection_ColorRemap_Source,
                [
                    new EnumProperty(PropertySourceSourceChanneld, LanguageResourceDictionary.ResourceKeys.ColorCollection_ColorRemap_Source_SourceChannel, typeof(WithHSLLOnOffChannelType), typeof(LanguageResourceDictionary), WithHSLLOnOffChannelType.Luminance, selectBoxWidth: 90.0),
                    new UseLayerImageProperty(PropertySourceAdditionalSourceLayerId, LanguageResourceDictionary.ResourceKeys.ColorCollection_ColorRemap_Source_AdditionalSourceLayer, selectBoxWidth: 90.0),
                    new EnumProperty(PropertySourceAdditionalSourceLayerPositionId, LanguageResourceDictionary.ResourceKeys.ColorCollection_ColorRemap_Source_AdditionalSourceLayerPosition, typeof(SourceLayerPositionType), typeof(LanguageResourceDictionary), SourceLayerPositionType.Center, selectBoxWidth: 90.0),
                    new EnumProperty(PropertySourceAdditionalSourceChannelId, LanguageResourceDictionary.ResourceKeys.ColorCollection_ColorRemap_Source_AdditionalSourceChannel, typeof(WithHSLLOnOffChannelType), typeof(LanguageResourceDictionary), WithHSLLOnOffChannelType.Luminance, selectBoxWidth: 90.0),
                    new EnumProperty(PropertySourceSourcePhaseBlendModeId, LanguageResourceDictionary.ResourceKeys.ColorCollection_ColorRemap_Source_SourceBlendMode, typeof(SourcePhaseBlendModeType), typeof(LanguageResourceDictionary), SourcePhaseBlendModeType.Add, selectBoxWidth: 90.0),
                    new EnumProperty(PropertySourceSourcePhaseLoopModeId, LanguageResourceDictionary.ResourceKeys.ColorCollection_ColorRemap_Source_SourcePhaseLoopMode, typeof(SourcePhaseLoopModeType), typeof(LanguageResourceDictionary), SourcePhaseLoopModeType.Repeat, selectBoxWidth: 90.0),
                    new AngleProperty(PropertySourcePhaseShiftId, LanguageResourceDictionary.ResourceKeys.ColorCollection_ColorRemap_Source_SourcePhaseShift, 0.0, digit: 2)
                ]),
                new PropertyGroup(PropertyOutputGroupId, LanguageResourceDictionary.ResourceKeys.ColorCollection_ColorRemap_Output,
                [
                    new ColorGradientProperty(
                        PropertyOutputColorMapId,
                        LanguageResourceDictionary.ResourceKeys.ColorCollection_ColorRemap_Output_ColorMap,
                        LanguageResourceDictionary.ResourceKeys.ColorCollection_ColorRemap_Output_ColorMap_EditText,
                        LanguageResourceDictionary.ResourceKeys.Dialog_OK,
                        LanguageResourceDictionary.ResourceKeys.Dialog_Cancel,
                        defaultGradient,
                        showPreviewOKLabInterpolation: true,
                        enableLoopInterpolation: true
                    ),
                    new CheckBoxProperty(PropertyOutputUseOkLabInterpolationId, LanguageResourceDictionary.ResourceKeys.ColorCollection_ColorRemap_Output_UseOkLabInterpolation, false),
                    new DoubleProperty(PropertyOutputCycleCountId, LanguageResourceDictionary.ResourceKeys.ColorCollection_ColorRemap_Output_CycleCount, 1.0, 0.0, double.MaxValue, slideChangeValue: 0.1, digit: 2)
                ]),
                new DoubleProperty(PropertyBlendOriginalId, LanguageResourceDictionary.ResourceKeys.ColorCollection_ColorRemap_BlendOriginal, 0.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent)
            ];
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, Time layerTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer, bool useGpu)
        {
            var blendOriginal = (float)(properties.GetValue(PropertyBlendOriginalId, layerTime, 0.0) * 0.01);

            if (blendOriginal >= 1.0F)
            {
                return image;
            }

            var sourceGroup = properties.First(p => p.Id == PropertySourceGroupId).GetChildren() ?? [];
            var outputGroup = properties.First(p => p.Id == PropertyOutputGroupId).GetChildren() ?? [];
            var sourceChannel = sourceGroup.GetValue(PropertySourceSourceChanneld, layerTime, WithHSLLOnOffChannelType.Luminance);
            var additionalSourceLayerId = sourceGroup.GetValue(PropertySourceAdditionalSourceLayerId, layerTime, UseLayerImageTarget.Empty);
            var additionalSourceLayerPosition = sourceGroup.GetValue(PropertySourceAdditionalSourceLayerPositionId, layerTime, SourceLayerPositionType.Center);
            var additionalSourceChannel = sourceGroup.GetValue(PropertySourceAdditionalSourceChannelId, layerTime, WithHSLLOnOffChannelType.Luminance);
            var sourcePhaseBlendMode = sourceGroup.GetValue(PropertySourceSourcePhaseBlendModeId, layerTime, SourcePhaseBlendModeType.Add);
            var sourcePhaseLoopMode = sourceGroup.GetValue(PropertySourceSourcePhaseLoopModeId, layerTime, SourcePhaseLoopModeType.Clamp);
            var sourcePhaseShift = (float)sourceGroup.GetValue(PropertySourcePhaseShiftId, layerTime, 0.0);
            var colorMap = outputGroup.GetValue(PropertyOutputColorMapId, layerTime, ColorGradient.WhiteBlackGradient);
            var useOkLabInterpolation = outputGroup.GetValue(PropertyOutputUseOkLabInterpolationId, layerTime, false);
            var cycleCount = (float)outputGroup.GetValue(PropertyOutputCycleCountId, layerTime, 1.0);

            using var additionalSourceImage = additionalSourceLayerId.GetImage(composition, layerTime, downSamplingRateX, useGpu);

            if (useGpu && AcceleratorObject != null)
            {
                return ProcessGpu(AcceleratorObject.CurrentDevice, image, roi, sourceChannel, additionalSourceImage, additionalSourceLayerPosition, additionalSourceChannel, sourcePhaseBlendMode, sourcePhaseLoopMode, sourcePhaseShift, colorMap, useOkLabInterpolation, cycleCount, blendOriginal);
            }
            else
            {
                return ProcessCpu(image, roi, sourceChannel, additionalSourceImage, additionalSourceLayerPosition, additionalSourceChannel, sourcePhaseBlendMode, sourcePhaseLoopMode, sourcePhaseShift, colorMap, useOkLabInterpolation, cycleCount, blendOriginal);
            }
        }

        public float[] Process(float[] audio, Time startTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }

        static NManagedImage ProcessCpu(
            NImage image,
            ROI roi,
            WithHSLLOnOffChannelType sourceChannel,
            NImage? additionalSourceImage,
            SourceLayerPositionType sourceLayerPosition,
            WithHSLLOnOffChannelType additionalSourceChannel,
            SourcePhaseBlendModeType blendMode,
            SourcePhaseLoopModeType loopMode,
            float phaseShift,
            ColorGradient colorMap,
            bool useOkLabInterpolation,
            float cycleCount,
            float blendOriginal
        )
        {
            var managedImage = image.ToManaged();
            var managedAdditionalSourceImage = additionalSourceImage?.ToManaged();

            var phaseMap = ArrayPool<float>.Shared.Rent(managedImage.DataLength);
            var imageWidth = managedImage.Width;
            var imageData = managedImage.Data;
            phaseMap.AsSpan(0, managedImage.DataLength).Clear();
            if (managedAdditionalSourceImage != null)
            {
                var (sourceStartX, sourceStartY) = sourceLayerPosition switch
                {
                    SourceLayerPositionType.Stretch => (0.0F, 0.0F),
                    _ => ((managedAdditionalSourceImage.Width - managedImage.Width) * 0.5F, (managedAdditionalSourceImage.Height - managedImage.Height) * 0.5F)
                };
                var (sourceDiffX, sourceDiffY) = sourceLayerPosition switch
                {
                    SourceLayerPositionType.Stretch => ((managedAdditionalSourceImage.Width - 1) / (float)(managedImage.Width - 1), (managedAdditionalSourceImage.Height - 1) / (float)(managedImage.Height - 1)),
                    _ => (1.0F, 1.0F)
                };
                Parallel.For(roi.Top, roi.Bottom, y =>
                {
                    var phaseMapSpan = phaseMap.AsSpan(y * imageWidth, imageWidth);
                    var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                    var additionalSourceImageDataSpan = managedAdditionalSourceImage.GetDataSpan();

                    for (var x = roi.Left; x < roi.Right; x++)
                    {
                        var sourceX = sourceStartX + sourceDiffX * roi.Left;
                        var sourceY = sourceStartY + sourceDiffY * y;
                        var additionalColor = sourceLayerPosition == SourceLayerPositionType.Loop ? ImageInterpolation.BilinearLoop(additionalSourceImageDataSpan, managedAdditionalSourceImage.Width, managedAdditionalSourceImage.Height, sourceX, sourceY) :
                            ImageInterpolation.BilinearEdgeRepeat(additionalSourceImageDataSpan, managedAdditionalSourceImage.Width, managedAdditionalSourceImage.Height, sourceX, sourceY);

                        var sourcePhase = CalcPhase(imageDataSpan[x], sourceChannel);
                        var additionalPhase = CalcPhase(additionalColor, additionalSourceChannel);
                        var phase = blendMode switch
                        {
                            SourcePhaseBlendModeType.Subtract => sourcePhase - additionalPhase,
                            SourcePhaseBlendModeType.Multiply => sourcePhase * additionalPhase  / 360.0F,
                            SourcePhaseBlendModeType.Average => (sourcePhase + additionalPhase) * 0.5F,
                            _ => sourcePhase + additionalPhase
                        } + phaseShift;
                        phaseMapSpan[x] = PhaseWrap(phase * cycleCount, loopMode);
                    }
                });
            }
            else
            {
                Parallel.For(roi.Top, roi.Bottom, y =>
                {
                    var phaseMapSpan = phaseMap.AsSpan(y * imageWidth, imageWidth);
                    var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);

                    for (var x = roi.Left; x < roi.Right; x++)
                    {
                        var phase = CalcPhase(imageDataSpan[x], sourceChannel) + phaseShift;
                        phaseMapSpan[x] = PhaseWrap(phase * cycleCount, loopMode);
                    }
                });
            }

            if (blendOriginal <= 0.0F)
            {
                Parallel.For(roi.Top, roi.Bottom, y =>
                {
                    var phaseMapSpan = phaseMap.AsSpan(y * imageWidth, imageWidth);
                    var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);

                    for (var x = roi.Left; x < roi.Right; x++)
                    {
                        imageDataSpan[x] = colorMap.GetLoopColor(phaseMapSpan[x], useOkLabInterpolation);
                    }
                });
            }
            else
            {
                Parallel.For(roi.Top, roi.Bottom, y =>
                {
                    var phaseMapSpan = phaseMap.AsSpan(y * imageWidth, imageWidth);
                    var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);

                    for (var x = roi.Left; x < roi.Right; x++)
                    {
                        imageDataSpan[x] = Vector4.Lerp(colorMap.GetLoopColor(phaseMapSpan[x], useOkLabInterpolation), imageDataSpan[x], blendOriginal);
                    }
                });
            }

            if (managedAdditionalSourceImage != additionalSourceImage)
            {
                managedAdditionalSourceImage?.Dispose();
            }
            ArrayPool<float>.Shared.Return(phaseMap);

            return managedImage;
        }

        static NGPUImage ProcessGpu(
            GraphicsDevice device,
            NImage image,
            ROI roi,
            WithHSLLOnOffChannelType sourceChannel,
            NImage? additionalSourceImage,
            SourceLayerPositionType sourceLayerPosition,
            WithHSLLOnOffChannelType additionalSourceChannel,
            SourcePhaseBlendModeType blendMode,
            SourcePhaseLoopModeType loopMode,
            float phaseShift,
            ColorGradient colorMap,
            bool useOkLabInterpolation,
            float cycleCount,
            float blendOriginal
        )
        {
            var gpuImage = image.ToGpu(device);
            var gpuAdditionalSourceImage = additionalSourceImage?.ToGpu(device);

            using var phaseMap = device.AllocateReadWriteBuffer<float>(gpuImage.DataLength);
            var (colorValues, colorPositions, opacityValues, opacityPositions) = GpuGradientColor.CopyToGPUGradientBuffers(device, colorMap, useOkLabInterpolation);

            if (gpuAdditionalSourceImage != null)
            {
                device.For(
                    roi.Width,
                    roi.Height,
                    new ColorRemapCalcPhaseWithAdditionalSourceProcess(
                        gpuImage.Data,
                        gpuImage.Width,
                        gpuImage.Height,
                        gpuAdditionalSourceImage.Data,
                        gpuAdditionalSourceImage.Width,
                        gpuAdditionalSourceImage.Height,
                        (int)sourceLayerPosition,
                        phaseMap,
                        (int)sourceChannel,
                        (int)additionalSourceChannel,
                        (int)blendMode,
                        (int)loopMode,
                        cycleCount,
                        phaseShift,
                        roi.Left,
                        roi.Top
                    )
                );
            }
            else
            {
                device.For(roi.Width, roi.Height, new ColorRemapCalcPhaseProcess(gpuImage.Data, gpuImage.Width, phaseMap, (int)sourceChannel, (int)loopMode, cycleCount, phaseShift, roi.Left, roi.Top));
            }

            device.For(roi.Width, roi.Height, new ColorRemapRemapProcess(gpuImage.Data, gpuImage.Width, phaseMap, colorValues, colorPositions, opacityValues, opacityPositions, useOkLabInterpolation, blendOriginal, roi.Left, roi.Top));

            colorValues.Dispose();
            colorPositions.Dispose();
            opacityValues.Dispose();
            opacityPositions.Dispose();
            if (gpuAdditionalSourceImage != additionalSourceImage)
            {
                gpuAdditionalSourceImage?.Dispose();
            }

            return gpuImage;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float CalcPhase(in Vector4 color, WithHSLLOnOffChannelType channelType)
        {
            switch (channelType)
            {
                case WithHSLLOnOffChannelType.R:
                    return color.Z * 360.0F;
                case WithHSLLOnOffChannelType.G:
                    return color.Y * 360.0F;
                case WithHSLLOnOffChannelType.B:
                    return color.X * 360.0F;
                case WithHSLLOnOffChannelType.A:
                    return color.W * 360.0F;
                case WithHSLLOnOffChannelType.Luminance:
                    return Vector4.Dot(color, Const.ConvertToGrayScale) * 360.0F;
                case WithHSLLOnOffChannelType.Hue:
                    {
                        var min = color.HorizontalMinBy3Element();
                        var max = color.HorizontalMaxBy3Element();
                        var diff = max - min;
                        return diff != 0.0F ? max switch
                        {
                            _ when max == color.X => (color.Z - color.Y) / diff * 60.0F + 240.0F,
                            _ when max == color.Y => (color.X - color.Z) / diff * 60.0F + 120.0F,
                            _ => (color.Y - color.X) / diff * 60.0F
                        } : 0.0F;
                    }
                case WithHSLLOnOffChannelType.Saturation:
                    {
                        var min = color.AsVector128().HorizontalMinBy3Element().GetElement(0);
                        var max = color.AsVector128().HorizontalMaxBy3Element().GetElement(0);
                        if (max > 0.0F)
                        {
                            return (max - min) / max * 360.0F;
                        }
                        else
                        {
                            return 0.0F;
                        }
                    }
                case WithHSLLOnOffChannelType.Lightness:
                    {
                        var min = color.AsVector128().HorizontalMinBy3Element().GetElement(0);
                        var max = color.AsVector128().HorizontalMaxBy3Element().GetElement(0);
                        return (max + min) * 180.0F; // (max + min) * 0.5F * 360.0F;
                    }
                case WithHSLLOnOffChannelType.On:
                    return 360.0F;
                case WithHSLLOnOffChannelType.Off:
                    return 0.0F;
                default:
                    return 180.0F;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float PhaseWrap(float phase, SourcePhaseLoopModeType loopMode)
        {
            return loopMode switch
            {
                SourcePhaseLoopModeType.Repeat => CoordWrap.Repeat(phase, 360.0F),
                SourcePhaseLoopModeType.Mirror => CoordWrap.Mirror(phase, 360.0F),
                _ => Math.Clamp(phase, 0.0F, 360.0F)
            };
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct ColorRemapCalcPhaseProcess(ReadWriteBuffer<Float4> image, int width, ReadWriteBuffer<float> phaseMap, int channelType, int loopMode, float cycleCount, float phaseShift, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            var pos = (ThreadIds.Y + startY) * width + ThreadIds.X + startX;

            var phase = ColorRemapGpuUtil.CalcPhase(image[pos], channelType) + phaseShift;
            phaseMap[pos] = ColorRemapGpuUtil.PhaseWrap(phase * cycleCount, loopMode);
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct ColorRemapCalcPhaseWithAdditionalSourceProcess(
        ReadWriteBuffer<Float4> image,
        int width,
        int height,
        ReadWriteBuffer<Float4> additionalSourceImage,
        int additionalSourceImageWidth,
        int additionalSourceImageHeight,
        int additionalSourcePosition,
        ReadWriteBuffer<float> phaseMap,
        int channelType,
        int additionalSourceChannelType,
        int blendMode,
        int loopMode,
        float cycleCount,
        float phaseShift,
        int startX,
        int startY
    ) : IComputeShader
    {
        public void Execute()
        {


            var sourceStartX = (additionalSourceImageWidth - width) * 0.5F;
            var sourceStartY = (additionalSourceImageHeight - height) * 0.5F;
            var sourceDiffX = 1.0F;
            var sourceDiffY = 1.0F;
            if (additionalSourcePosition == 1)
            {
                sourceStartX = 0.0F;
                sourceStartY = 0.0F;
                sourceDiffX = (additionalSourceImageWidth - 1) / (float)(width - 1);
                sourceDiffY = (additionalSourceImageHeight - 1) / (float)(height - 1);
            }

            var x = ThreadIds.X + startX;
            var y = ThreadIds.Y + startY;
            var sourceX = sourceStartX + sourceDiffX * x;
            var sourceY = sourceStartY + sourceDiffY * y;

            var additionalColor = Float4.Zero;
            if (additionalSourcePosition == 2)
            {
                additionalColor = SourceImageBilinearLoop(sourceX, sourceY);
            }
            else
            {
                additionalColor = SourceImageBilinear(sourceX, sourceY);
            }

            var pos = y * width + x;
            var sourcePhase = ColorRemapGpuUtil.CalcPhase(image[pos], channelType) + phaseShift;
            var additionalPhase = ColorRemapGpuUtil.CalcPhase(additionalColor, additionalSourceChannelType);
            var phase = 0.0F;
            switch (blendMode)
            {
                case 1:
                    phase = sourcePhase - additionalPhase;
                    break;
                case 2:
                    phase = (sourcePhase * additionalPhase) / 360.0F;
                    break;
                case 3:
                    phase = (sourcePhase + additionalPhase) * 0.5F;
                    break;
                default:
                    phase = sourcePhase + additionalPhase;
                    break;
            }
            phase += phaseShift;
            phaseMap[pos] = ColorRemapGpuUtil.PhaseWrap(phase * cycleCount, loopMode);
        }

        Float4 SourceImageBilinear(float x, float y)
        {
            var ix = (int)Hlsl.Floor(x);
            var iy = (int)Hlsl.Floor(y);

            if (ix == x && iy == y)
            {
                if (ix > -1 && iy > -1 && ix < additionalSourceImageWidth && iy < additionalSourceImageHeight)
                {
                    return additionalSourceImage[iy * additionalSourceImageWidth + ix];
                }
                else
                {
                    return 0.5F;
                }
            }
            else if (ix < -1 || iy < -1 || ix >= additionalSourceImageWidth || iy >= additionalSourceImageHeight)
            {
                return 0.5F;
            }

            var pp = x - ix;
            var qq = y - iy;
            var ip = 1.0F - pp;
            var iq = 1.0F - qq;
            var mw = additionalSourceImageWidth - 1;
            var mh = additionalSourceImageHeight - 1;

            Float4 c1;
            Float4 c2;
            Float4 c3;
            Float4 c4;
            var pos = iy * additionalSourceImageWidth + ix;

            if (ix > -1)
            {
                if (ix < mw)
                {
                    if (iy > -1)
                    {
                        c1 = additionalSourceImage[pos];
                        c2 = additionalSourceImage[pos + 1];
                        if (iy < mh)
                        {
                            pos += additionalSourceImageWidth;
                            c3 = additionalSourceImage[pos];
                            c4 = additionalSourceImage[pos + 1];
                        }
                        else
                        {
                            c3 = c1;
                            c4 = c2;
                        }
                    }
                    else
                    {
                        pos += additionalSourceImageWidth;
                        c1 = additionalSourceImage[pos];
                        c2 = additionalSourceImage[pos + 1];
                        c3 = c1;
                        c4 = c2;
                    }
                }
                else
                {
                    if (iy > -1)
                    {
                        c1 = additionalSourceImage[pos];
                        c2 = c1;
                        if (iy < mh)
                        {
                            c3 = additionalSourceImage[pos + additionalSourceImageWidth];
                            c4 = c3;
                        }
                        else
                        {
                            c3 = c1;
                            c4 = c1;
                        }
                    }
                    else
                    {
                        c3 = additionalSourceImage[pos + additionalSourceImageWidth];
                        c1 = c3;
                        c2 = c3;
                        c4 = c3;
                    }
                }
            }
            else
            {
                pos++;
                if (iy > -1)
                {
                    c2 = additionalSourceImage[pos];
                    c1 = c2;
                    if (iy < mh)
                    {
                        c4 = additionalSourceImage[pos + additionalSourceImageWidth];
                        c3 = c4;
                    }
                    else
                    {
                        c3 = c2;
                        c4 = c2;
                    }
                }
                else
                {
                    c4 = additionalSourceImage[pos + additionalSourceImageWidth];
                    c1 = c4;
                    c2 = c4;
                    c3 = c4;
                }
            }

            var ta = Hlsl.Lerp(Hlsl.Lerp(c1, c3, qq), Hlsl.Lerp(c2, c4, qq), pp).W;
            if (ta <= 0.0F)
            {
                return 0.5F;
            }
            else
            {
                var t = Hlsl.Lerp(Hlsl.Lerp(c1 * c1.W, c3 * c3.W, qq), Hlsl.Lerp(c2 * c2.W, c4 * c4.W, qq), pp) / ta;
                t.W = ta;
                return t;
            }
        }

        Float4 SourceImageBilinearLoop(float x, float y)
        {
            var ix = (int)Hlsl.Floor(x);
            var iy = (int)Hlsl.Floor(y);

            if (ix == x && iy == y)
            {
                return additionalSourceImage[CoordWrapGpu.Repeat(iy, additionalSourceImageHeight) * additionalSourceImageWidth + CoordWrapGpu.Repeat(ix, additionalSourceImageWidth)];
            }

            var pp = x - ix;
            var qq = y - iy;

            var c1 = additionalSourceImage[CoordWrapGpu.Repeat(iy, additionalSourceImageHeight) * additionalSourceImageWidth + CoordWrapGpu.Repeat(ix, additionalSourceImageWidth)];
            var c2 = additionalSourceImage[CoordWrapGpu.Repeat(iy, additionalSourceImageHeight) * additionalSourceImageWidth + CoordWrapGpu.Repeat(ix + 1, additionalSourceImageWidth)];
            var c3 = additionalSourceImage[CoordWrapGpu.Repeat(iy + 1, additionalSourceImageHeight) * additionalSourceImageWidth + CoordWrapGpu.Repeat(ix, additionalSourceImageWidth)];
            var c4 = additionalSourceImage[CoordWrapGpu.Repeat(iy + 1, additionalSourceImageHeight) * additionalSourceImageWidth + CoordWrapGpu.Repeat(ix + 1, additionalSourceImageWidth)];

            var ta = Hlsl.Lerp(Hlsl.Lerp(c1, c3, qq), Hlsl.Lerp(c2, c4, qq), pp).W;
            if (ta <= 0.0F)
            {
                return 0.5F;
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
    readonly partial struct ColorRemapRemapProcess(ReadWriteBuffer<Float4> image, int width, ReadWriteBuffer<float> phaseMap, ReadOnlyBuffer<Float3> colorGradientValues, ReadOnlyBuffer<float> colorGradientPositions, ReadOnlyBuffer<float> opacityGradientValues, ReadOnlyBuffer<float> opacityGradientPositions, bool useOKLabInterpolation, float blendOriginal, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            var pos = (ThreadIds.Y + startY) * width + ThreadIds.X + startX;

            var phase = phaseMap[pos] / 360.0F;
            var color = new Float4(CalcColor(phase), CalcOpacity(phase));
            image[pos] = Hlsl.Lerp(color, image[pos], blendOriginal);
        }

        Float3 CalcColor(float gradientPos)
        {
            if (colorGradientValues.Length < 2)
            {
                if (useOKLabInterpolation)
                {
                    return ColorSpaceConversion.OkLabToRgb(colorGradientValues[0]);
                }
                else
                {
                    return colorGradientValues[0];
                }
            }

            var prevColor = colorGradientValues[colorGradientValues.Length - 1];
            var prevPosition = colorGradientPositions[colorGradientPositions.Length - 1] - 1.0F;
            for (var i = colorGradientPositions.Length - 1; i > -1; i--)
            {
                if (colorGradientPositions[i] <= gradientPos)
                {
                    prevColor = colorGradientValues[i];
                    prevPosition = colorGradientPositions[i];
                    break;
                }
            }

            var nextColor = colorGradientValues[0];
            var nextPosition = colorGradientPositions[0] + 1.0F;
            for (var i = 0; i < colorGradientPositions.Length; i++)
            {
                if (colorGradientPositions[i] > gradientPos)
                {
                    nextColor = colorGradientValues[i];
                    nextPosition = colorGradientPositions[i];
                    break;
                }
            }

            if (prevPosition >= nextPosition)
            {
                if (useOKLabInterpolation)
                {
                    return ColorSpaceConversion.OkLabToRgb(prevColor);
                }
                else
                {
                    return prevColor;
                }
            }

            var result = Hlsl.Lerp(prevColor, nextColor, (gradientPos - prevPosition) / (nextPosition - prevPosition));
            if (useOKLabInterpolation)
            {
                return ColorSpaceConversion.OkLabToRgb(result);
            }
            else
            {
                return result;
            }
        }

        float CalcOpacity(float gradientPos)
        {
            if (opacityGradientValues.Length < 2)
            {
                return opacityGradientValues[0];
            }

            var prevOpacity = opacityGradientValues[opacityGradientValues.Length - 1];
            var prevPosition = opacityGradientPositions[opacityGradientPositions.Length - 1] - 1.0F;
            for (var i = opacityGradientPositions.Length - 1; i > -1; i--)
            {
                if (opacityGradientPositions[i] <= gradientPos)
                {
                    prevOpacity = opacityGradientValues[i];
                    prevPosition = opacityGradientPositions[i];
                    break;
                }
            }

            var nextOpacity = opacityGradientValues[0];
            var nextPosition = opacityGradientPositions[0] + 1.0F;
            for (var i = 0; i < opacityGradientPositions.Length; i++)
            {
                if (opacityGradientPositions[i] > gradientPos)
                {
                    nextOpacity = opacityGradientValues[i];
                    nextPosition = opacityGradientPositions[i];
                    break;
                }
            }

            if (prevPosition >= nextPosition)
            {
                return prevOpacity;
            }
            else
            {
                return Hlsl.Lerp(prevOpacity, nextOpacity, (gradientPos - prevPosition) / (nextPosition - prevPosition));
            }
        }
    }

    static class ColorRemapGpuUtil
    {
        public static float CalcPhase(Float4 color, int channelType)
        {
            switch (channelType)
            {
                case 0:
                    return color.Z * 360.0F;
                case 1:
                    return color.Y * 360.0F;
                case 2:
                    return color.X * 360.0F;
                case 3:
                    return color.W * 360.0F;
                case 4:
                    return Hlsl.Dot(color.XYZ, Const.ConvertToGrayScaleFloat3) * 360.0F;
                case 5:
                    {
                        var min = Hlsl.Min(Hlsl.Min(color.X, color.Y), color.Z);
                        var max = Hlsl.Max(Hlsl.Max(color.X, color.Y), color.Z);
                        var diff = max - min;
                        var h = 0.0F;
                        if (diff != 0.0F)
                        {
                            if (max == color.X)
                            {
                                h = (color.Z - color.Y) / diff * 60.0F + 240.0F;
                            }
                            else if (max == color.Y)
                            {
                                h = (color.X - color.Z) / diff * 60.0F + 120.0F;
                            }
                            else
                            {
                                h = (color.Y - color.X) / diff * 60.0F;
                            }
                        }

                        return h;
                    }
                case 6:
                    {
                        var min = Hlsl.Min(Hlsl.Min(color.X, color.Y), color.Z);
                        var max = Hlsl.Max(Hlsl.Max(color.X, color.Y), color.Z);
                        if (max > 0.0F)
                        {
                            return (max - min) / max * 360.0F;
                        }
                        else
                        {
                            return 0.0F;
                        }
                    }
                case 7:
                    {
                        var min = Hlsl.Min(Hlsl.Min(color.X, color.Y), color.Z);
                        var max = Hlsl.Max(Hlsl.Max(color.X, color.Y), color.Z);
                        return (max + min) * 180.0F;
                    }
                case 8:
                    return 360.0F;
                case 10:
                    return 0.0F;
                default:
                    return 180.0F;
            }
        }

        public static float PhaseWrap(float phase, int loopMode)
        {
            switch (loopMode)
            {
                case 1:
                    return CoordWrapGpu.Repeat(phase, 360.0F);
                case 2:
                    return CoordWrapGpu.Mirror(phase, 360.0F);
                default:
                    return Hlsl.Clamp(phase, 0.0F, 360.0F);
            }
        }
    }

    enum SourcePhaseBlendModeType : int
    {
        Add,
        Subtract,
        Multiply,
        Average
    }

    enum SourcePhaseLoopModeType : int
    {
        Clamp,
        Repeat,
        Mirror
    }
}
