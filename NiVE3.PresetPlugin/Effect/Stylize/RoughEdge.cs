using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Numerics;
using System.Text;
using ComputeSharp;
using NiVE3.Image;
using NiVE3.Numerics;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Property.Properties;
using NiVE3.Plugin.Resource;
using NiVE3.Plugin.ValueObject;
using NiVE3.PresetPlugin.Effect.Util.Blur;
using NiVE3.PresetPlugin.Effect.Util.Noise;
using NiVE3.PresetPlugin.Extension;
using NiVE3.PresetPlugin.Resource;

namespace NiVE3.PresetPlugin.Effect.Stylize
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.Stylize_RoughEdge_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_Stylize, LanguageResourceDictionary.Stylize_RoughEdge_Description, ID, IsSupportGpu = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class RoughEdge : IEffect
    {
        const string ID = "737B0180-1110-4827-9375-05241B08EE0E";

        const string PropertyIsColorizeEdge = nameof(PropertyIsColorizeEdge);

        const string PropertyEdgeColor = nameof(PropertyEdgeColor);

        const string PropertyEdgeWidth = nameof(PropertyEdgeWidth);

        const string PropertyEdgeSharpness = nameof(PropertyEdgeSharpness);

        const string PropertyFractalStrength = nameof(PropertyFractalStrength);

        const string PropertyFractalOptionGroup = nameof(PropertyFractalOptionGroup);

        const string PropertyFractalOptionContrast = nameof(PropertyFractalOptionContrast);

        const string PropertyFractalOptionLuminance = nameof(PropertyFractalOptionLuminance);

        const string PropertyFractalOptionScale = nameof(PropertyFractalOptionScale);

        const string PropertyFractalOptionOffset = nameof(PropertyFractalOptionOffset);

        const string PropertyFractalOptionOctarve = nameof(PropertyFractalOptionOctarve);

        const string PropertyFractalOptionEvolution = nameof(PropertyFractalOptionEvolution);

        const string PropertyFractalOptionRandomSeed = nameof(PropertyFractalOptionRandomSeed);

        const int BlurLoopCount = 3;

        IAcceleratorObject? AcceleratorObject { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            AcceleratorObject = accelerator;
        }

        public ROI CalcRoi(ROI baseRoi, double downSamplingRateX, double downSamplingRateY, Time layerTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer)
        {
            var edgeWidth = (float)properties.GetValue(PropertyEdgeWidth, layerTime, 0.0);

            if (edgeWidth > 0.0)
            {
                var expandX = (int)Math.Ceiling(edgeWidth / downSamplingRateX);
                var expandY = (int)Math.Ceiling(edgeWidth / downSamplingRateY);
                return baseRoi.Expand(-expandX, -expandY, expandX, expandY);
            }
            else
            {
                return baseRoi;
            }
        }

        public PropertyBase[] GetProperties(Int32Size sourceSize)
        {
            return
            [
                new CheckBoxProperty(PropertyIsColorizeEdge, LanguageResourceDictionary.ResourceKeys.Stylize_RoughEdge_IsColorizeEdge, false),
                new ColorProperty(PropertyEdgeColor, LanguageResourceDictionary.ResourceKeys.Stylize_RoughEdge_EdgeColor, LanguageResourceDictionary.ResourceKeys.Dialog_ColorDialog_Title_Color, LanguageResourceDictionary.ResourceKeys.Dialog_OK, LanguageResourceDictionary.ResourceKeys.Dialog_Cancel, new Vector4(0.3F, 0.4F, 1.0F, 1.0F)),
                new DoubleProperty(PropertyEdgeWidth, LanguageResourceDictionary.ResourceKeys.Stylize_RoughEdge_EdgeWidth, 50.0, 0.0, 1000.0, digit: 2),
                new DoubleProperty(PropertyEdgeSharpness, LanguageResourceDictionary.ResourceKeys.Stylize_RoughEdge_EdgeSharpness, 5.0, 0.0, 100.0, slideChangeValue: 0.1, digit: 2),
                new DoubleProperty(PropertyFractalStrength, LanguageResourceDictionary.ResourceKeys.Stylize_RoughEdge_FractalStrength, 100.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent),
                new PropertyGroup(PropertyFractalOptionGroup, LanguageResourceDictionary.ResourceKeys.Stylize_RoughEdge_FractalOption,
                [
                    new DoubleProperty(PropertyFractalOptionContrast, LanguageResourceDictionary.ResourceKeys.Stylize_RoughEdge_FractalOption_Contrast, 100.0, 0.0, 10000.0, digit: 2),
                    new DoubleProperty(PropertyFractalOptionLuminance, LanguageResourceDictionary.ResourceKeys.Stylize_RoughEdge_FractalOption_Luminance, 0.0, -10000.0, 10000.0, digit: 2),
                    new Vector3dProperty(PropertyFractalOptionScale, LanguageResourceDictionary.ResourceKeys.Stylize_RoughEdge_FractalOption_Scale, new Vector3d(200.0), new Vector3d(0.1), new Vector3d(double.MaxValue), digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent, separator: ",", useLinkRatio: true),
                    new Vector3dProperty(PropertyFractalOptionOffset, LanguageResourceDictionary.ResourceKeys.Stylize_RoughEdge_FractalOption_Offset, Vector3d.Zero, digit: 2),
                    new DoubleProperty(PropertyFractalOptionOctarve, LanguageResourceDictionary.ResourceKeys.Stylize_RoughEdge_FractalOption_Octarve, 2.0, 1.0, 20.0, slideChangeValue: 0.1, digit: 3),
                    new AngleProperty(PropertyFractalOptionEvolution, LanguageResourceDictionary.ResourceKeys.Stylize_RoughEdge_FractalOption_Evolution, 0.0, digit: 2, isOnlyPositiveDirection: true),
                    new DoubleProperty(PropertyFractalOptionRandomSeed, LanguageResourceDictionary.ResourceKeys.Stylize_RoughEdge_FractalOption_RandomSeed, 0.0, 0.0, int.MaxValue, digit: 2)
                ])
            ];
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, Time layerTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer, bool useGpu)
        {
            var isColorizeEdge = properties.GetValue(PropertyIsColorizeEdge, layerTime, false);
            var edgeColor = properties.GetValue(PropertyEdgeColor, layerTime, Vector4.Zero);
            var edgeWidth = (float)properties.GetValue(PropertyEdgeWidth, layerTime, 0.0);
            var edgeSharpness = (float)properties.GetValue(PropertyEdgeSharpness, layerTime, 0.0) + 1.0F;
            var fractalStrength = (float)(properties.GetValue(PropertyFractalStrength, layerTime, 0.0) * 0.01);
            var fractalOption = properties.First(p => p.Id == PropertyFractalOptionGroup).GetChildren() ?? [];
            var fractalContrast = (float)(fractalOption.GetValue(PropertyFractalOptionContrast, layerTime, 100.0) * 0.01);
            var fractalLuminance = (float)(fractalOption.GetValue(PropertyFractalOptionLuminance, layerTime, 0.0) * 0.01);
            var fractalScale = (Vector3)(fractalOption.GetValue(PropertyFractalOptionScale, layerTime, Vector3d.One) * 0.1);
            var fractalOffset = (Vector3)(fractalOption.GetValue(PropertyFractalOptionOffset, layerTime, Vector3d.Zero) / new Vector3d(downSamplingRateX, downSamplingRateY, 1.0) + new Vector3d(roi.OriginalImagePosition.X, roi.OriginalImagePosition.Y, 0.0));
            var fractalOctarve = (float)fractalOption.GetValue(PropertyFractalOptionOctarve, layerTime, 1.0);
            var fractalEvolution = (float)fractalOption.GetValue(PropertyFractalOptionEvolution, layerTime, 0.0);
            var fractalRandomSeed = (uint)fractalOption.GetValue(PropertyFractalOptionRandomSeed, layerTime, 0.0);

            edgeColor.W = 1.0F;
            fractalScale.Z = 1.0F;

            if (useGpu && AcceleratorObject != null)
            {
                return ProcessGpu(AcceleratorObject.CurrentDevice, image, roi, isColorizeEdge, edgeColor, edgeWidth, edgeSharpness, fractalStrength, fractalContrast, fractalLuminance, fractalScale, fractalOffset, fractalOctarve, fractalEvolution, fractalRandomSeed, downSamplingRateX, downSamplingRateY);
            }
            else
            {
                return ProcessCpu(image, roi, isColorizeEdge, edgeColor, edgeWidth, edgeSharpness, fractalStrength, fractalContrast, fractalLuminance, fractalScale, fractalOffset, fractalOctarve, fractalEvolution, fractalRandomSeed, downSamplingRateX, downSamplingRateY);
            }
        }

        public float[] Process(float[] audio, Time startTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }

        static NManagedImage ProcessCpu(NImage image, ROI roi, bool isColorizeEdge, Vector4 edgeColor, float edgeWidth, float edgeSharpness, float fractalStrength, float fractalContrast, float fractalLuminance, Vector3 fractalScale, Vector3 fractalOffset, float fractalOctarve, float fractalEvolution, uint fractalRandomSeed, double downSamplingRateX, double downSamplingRateY)
        {
            var managedImage = image.ToManaged();

            var mapRoi = new ROI(roi.OriginalImagePosition, roi.OriginalImageSize, 0, 0, managedImage.Width, managedImage.Height);

            using var alphaMap = (NManagedImage)managedImage.Copy();
            if (edgeWidth > 0.0F)
            {
                edgeWidth /= 3.0F;
                BoxBlurProcessor.ProcessCpu(alphaMap, mapRoi, edgeWidth, edgeWidth, BlurLoopCount, EdgeRepeatMode.None);
            }

            var imageWidth = managedImage.Width;
            var alphaMapData = alphaMap.Data;
            if (fractalStrength > 0.0F)
            {
                using var fractalMap = new NManagedImage(managedImage.Width, managedImage.Height);
                FractalNoiseProcessor.GenerateCpu(
                    fractalMap,
                    mapRoi,
                    (float)downSamplingRateX,
                    (float)downSamplingRateY,
                    FractalType.Normal,
                    NoiseType.Parlin,
                    false,
                    fractalContrast,
                    fractalLuminance,
                    fractalOffset,
                    fractalScale,
                    0.0F,
                    fractalOctarve,
                    0.7F,
                    Vector3.Zero,
                    new Vector3(0.56F),
                    0.0F,
                    false,
                    fractalEvolution,
                    fractalRandomSeed,
                    1.0F
                );

                var fractalMapData = fractalMap.Data;
                Parallel.For(roi.Top, roi.Bottom, y =>
                {
                    var alphaMapDataSpan = alphaMapData.AsSpan(y * imageWidth, imageWidth);
                    var fractalMapDataSpan = fractalMapData.AsSpan(y * imageWidth, imageWidth);

                    for (var x = roi.Left; x < roi.Right; x++)
                    {
                        var color = alphaMapDataSpan[x];
                        var rate = (1.0F - Math.Abs(1.0F - color.W * 2.0F)) * 0.5F;
                        color.W = Math.Clamp(color.W - (fractalMapDataSpan[x].X - 0.5F) * fractalStrength * rate, 0.0F, 1.0F);
                        alphaMapDataSpan[x] = color;
                    }
                });
            }

            var imageData = managedImage.Data;
            if (isColorizeEdge)
            {
                Parallel.For(roi.Top, roi.Bottom, y =>
                {
                    var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                    var alphaMapDataSpan = alphaMapData.AsSpan(y * imageWidth, imageWidth);

                    for (var x = roi.Left; x < roi.Right; x++)
                    {
                        var baseAlpha = alphaMapDataSpan[x].W;
                        if (baseAlpha >= 1.0F)
                        {
                            continue;
                        }
                        var alpha = Math.Clamp((baseAlpha * 2.0F - 1.0F) * edgeSharpness + 1.0F, 0.0F, 2.0F);
                        var color = Vector4.Lerp(edgeColor, imageDataSpan[x], Math.Clamp(alpha - 1.0F, 0.0F, 1.0F));
                        color.W = Math.Clamp(alpha, 0.0F, 1.0F);
                        imageDataSpan[x] = color;
                    }
                });
            }
            else
            {
                Parallel.For(roi.Top, roi.Bottom, y =>
                {
                    var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                    var alphaMapDataSpan = alphaMapData.AsSpan(y * imageWidth, imageWidth);

                    for (var x = roi.Left; x < roi.Right; x++)
                    {
                        var baseAlpha = alphaMapDataSpan[x].W;
                        if (baseAlpha >= 1.0F)
                        {
                            continue;
                        }
                        var alpha = Math.Clamp((baseAlpha - 0.5F) * edgeSharpness + 0.5F, 0.0F, 1.0F);
                        var color = imageDataSpan[x];
                        color.W = alpha;
                        imageDataSpan[x] = color;
                    }
                });
            }

            return managedImage;
        }

        static NGPUImage ProcessGpu(GraphicsDevice device, NImage image, ROI roi, bool isColorizeEdge, Vector4 edgeColor, float edgeWidth, float edgeSharpness, float fractalStrength, float fractalContrast, float fractalLuminance, Vector3 fractalScale, Vector3 fractalOffset, float fractalOctarve, float fractalEvolution, uint fractalRandomSeed, double downSamplingRateX, double downSamplingRateY)
        {
            var gpuImage = image.ToGpu(device);

            var mapRoi = new ROI(roi.OriginalImagePosition, roi.OriginalImageSize, 0, 0, gpuImage.Width, gpuImage.Height);

            using var alphaMap = new NGPUImage(gpuImage.Width, gpuImage.Height, device);
            gpuImage.CopyTo(alphaMap);
            if (edgeWidth > 0.0F)
            {
                edgeWidth /= 3.0F;
                BoxBlurProcessor.ProcessGpu(device, alphaMap, mapRoi, edgeWidth, edgeWidth, BlurLoopCount, EdgeRepeatMode.None);
            }

            if (fractalStrength > 0.0F)
            {
                using var fractalMap = new NGPUImage(gpuImage.Width, gpuImage.Height, device);
                FractalNoiseProcessor.GenerateGpu(
                    device,
                    fractalMap,
                    mapRoi,
                    (float)downSamplingRateX,
                    (float)downSamplingRateY,
                    FractalType.Normal,
                    NoiseType.Parlin,
                    false,
                    fractalContrast,
                    fractalLuminance,
                    fractalOffset,
                    fractalScale,
                    0.0F,
                    fractalOctarve,
                    0.7F,
                    Vector3.Zero,
                    new Vector3(0.56F),
                    0.0F,
                    false,
                    fractalEvolution,
                    fractalRandomSeed,
                    1.0F
                );

                device.For(roi.Width, roi.Height, new RoughEdgeFractalMapProcess(alphaMap.Data, gpuImage.Width, fractalMap.Data, fractalStrength, roi.Left, roi.Top));
            }

            if (isColorizeEdge)
            {
                device.For(roi.Width, roi.Height, new RoughEdgeApplyWithEdgeColorProcess(gpuImage.Data, gpuImage.Width, alphaMap.Data, edgeSharpness, edgeColor, roi.Left, roi.Top));
            }
            else
            {
                device.For(roi.Width, roi.Height, new RoughEdgeApplyProcess(gpuImage.Data, gpuImage.Width, alphaMap.Data, edgeSharpness, roi.Left, roi.Top));
            }

            return gpuImage;
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct RoughEdgeFractalMapProcess(ReadWriteBuffer<Float4> alphaMap, int width, ReadWriteBuffer<Float4> fractalMap, float fractalStrength, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            var pos = (ThreadIds.Y + startY) * width + ThreadIds.X + startX;

            var color = alphaMap[pos];
            var rate = (1.0F - Hlsl.Abs(1.0F - color.W * 2.0F)) * 0.5F;
            color.W = Hlsl.Clamp(color.W - (fractalMap[pos].X - 0.5F) * fractalStrength * rate, 0.0F, 1.0F);
            alphaMap[pos] = color;
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct RoughEdgeApplyProcess(ReadWriteBuffer<Float4> image, int width, ReadWriteBuffer<Float4> alphaMap, float edgeSharpness, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            var pos = (ThreadIds.Y + startY) * width + ThreadIds.X + startX;

            var baseAlpha = alphaMap[pos].W;
            if (baseAlpha < 1.0F)
            {
                var alpha = Hlsl.Clamp((baseAlpha - 0.5F) * edgeSharpness + 0.5F, 0.0F, 1.0F);
                var color = image[pos];
                color.W = alpha;
                image[pos] = color;
            }
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct RoughEdgeApplyWithEdgeColorProcess(ReadWriteBuffer<Float4> image, int width, ReadWriteBuffer<Float4> alphaMap, float edgeSharpness, Float4 edgeColor, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            var pos = (ThreadIds.Y + startY) * width + ThreadIds.X + startX;

            var baseAlpha = alphaMap[pos].W;
            if (baseAlpha < 1.0F)
            {
                var alpha = Hlsl.Clamp((baseAlpha * 2.0F - 1.0F) * edgeSharpness + 1.0F, 0.0F, 2.0F);
                var color = Hlsl.Lerp(edgeColor, image[pos], Hlsl.Clamp(alpha - 1.0F, 0.0F, 1.0F));
                color.W = Hlsl.Clamp(alpha, 0.0F, 1.0F);
                image[pos] = color;
            }
        }
    }
}
