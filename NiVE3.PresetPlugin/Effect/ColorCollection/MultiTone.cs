using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ComputeSharp;
using NiVE3.Image;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Property.Properties;
using NiVE3.Plugin.Resource;
using NiVE3.Plugin.ValueObject;
using NiVE3.PresetPlugin.Extension;
using NiVE3.PresetPlugin.Internal;
using NiVE3.PresetPlugin.Resource;

namespace NiVE3.PresetPlugin.Effect.ColorCollection
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.ColorCollection_MultiTone_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_ColorCollection, LanguageResourceDictionary.ColorCollection_MultiTone_Description, ID, IsSupportGpu = true, IsRenderEveryFrame = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class MultiTone : IEffect
    {
        const string ID = "E542A978-1AE4-4DB2-BA0B-66DFA5A0E362";

        const string PropertyUseMidToneCountId = nameof(PropertyUseMidToneCountId);

        const string PropertyShadowColorId = nameof(PropertyShadowColorId);

        const string PropertyMidToneColor1Id = nameof(PropertyMidToneColor1Id);

        const string PropertyMidToneColor2Id = nameof(PropertyMidToneColor2Id);

        const string PropertyMidToneColor3Id = nameof(PropertyMidToneColor3Id);

        const string PropertyMidToneColor4Id = nameof(PropertyMidToneColor4Id);

        const string PropertyHighlightColorId = nameof(PropertyHighlightColorId);

        IAcceleratorObject? AcceleratorObject { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            AcceleratorObject = accelerator;
        }

        public PropertyBase[] GetProperties()
        {
            var dialogTitle = LanguageResourceDictionary.ResourceKeys.Dialog_ColorDialog_Title_Color;
            var dialogOk = LanguageResourceDictionary.ResourceKeys.Dialog_OK;
            var dialogCancel = LanguageResourceDictionary.ResourceKeys.Dialog_Cancel;
            return
            [
                new DoubleProperty(PropertyUseMidToneCountId, LanguageResourceDictionary.ResourceKeys.ColorCollection_MultiTone_UseMidToneCount, 4, 0, 4, digit: 0),
                new ColorProperty(PropertyShadowColorId, LanguageResourceDictionary.ResourceKeys.ColorCollection_MultiTone_ShadowColor, dialogTitle, dialogOk, dialogCancel, new Vector4(0.043137254901960784F, 0.06274509803921569F, 0.2823529411764706F, 1.0F)),
                new ColorProperty(PropertyMidToneColor1Id, LanguageResourceDictionary.ResourceKeys.ColorCollection_MultiTone_MidToneColor1, dialogTitle, dialogOk, dialogCancel, new Vector4(0.027450980392156862F, 0.12549019607843137F, 0.5764705882352941F, 1.0F)),
                new ColorProperty(PropertyMidToneColor2Id, LanguageResourceDictionary.ResourceKeys.ColorCollection_MultiTone_MidToneColor2, dialogTitle, dialogOk, dialogCancel, new Vector4(0.0F, 0.20392156862745098F, 0.8745098039215686F, 1.0F)),
                new ColorProperty(PropertyMidToneColor3Id, LanguageResourceDictionary.ResourceKeys.ColorCollection_MultiTone_MidToneColor3, dialogTitle, dialogOk, dialogCancel, new Vector4(0.043137254901960784F, 0.403921568627451F, 0.984313725490196F, 1.0F)),
                new ColorProperty(PropertyMidToneColor4Id, LanguageResourceDictionary.ResourceKeys.ColorCollection_MultiTone_MidToneColor4, dialogTitle, dialogOk, dialogCancel, new Vector4(0.058823529411764705F, 0.6F, 0.9333333333333333F, 1.0F)),
                new ColorProperty(PropertyHighlightColorId, LanguageResourceDictionary.ResourceKeys.ColorCollection_MultiTone_HighlightColor, dialogTitle, dialogOk, dialogCancel, new Vector4(0.1450980392156863F, 0.8352941176470589F, 0.9254901960784314F, 1.0F)),
            ];
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, double layerTime, IPropertyObject[] properties, ICompositionObject composition, bool useGpu)
        {
            var useMidToneCount = (int)properties.GetValue(PropertyUseMidToneCountId, layerTime, 0.0);
            var shadowColor = properties.GetValue(PropertyShadowColorId, layerTime, Vector4.UnitW);
            var midToneColors = new Vector4[]
            {
                properties.GetValue(PropertyMidToneColor1Id, layerTime, Vector4.UnitW),
                properties.GetValue(PropertyMidToneColor2Id, layerTime, Vector4.UnitW),
                properties.GetValue(PropertyMidToneColor3Id, layerTime, Vector4.UnitW),
                properties.GetValue(PropertyMidToneColor4Id, layerTime, Vector4.UnitW)
            };
            var highlightColor = properties.GetValue(PropertyHighlightColorId, layerTime, Vector4.UnitW);

            var colorMap = new Vector4[useMidToneCount + 2];
            var colorPositions = new float[useMidToneCount + 2];

            colorMap[0] = shadowColor;
            colorPositions[0] = 0.0F;
            colorMap[^1] = highlightColor;
            colorPositions[^1] = 1.0F;

            for (int i = 1, c = 0; c < useMidToneCount; i++, c++)
            {
                colorMap[i] = midToneColors[c];
                colorPositions[i] = i / (useMidToneCount + 1.0F);
            }

            var overShadowRate = (colorMap[1] - colorMap[0]) / colorMap[1];
            var overHighlightRate = (colorMap[^1] - colorMap[^2]) / (1.0F - colorPositions[^2]);

            if (useGpu && AcceleratorObject != null)
            {
                return ProcessGpu(AcceleratorObject.CurrentDevice, image, roi, colorMap, colorPositions, overShadowRate, overHighlightRate);
            }
            else
            {
                return ProcessCpu(image, roi, colorMap, colorPositions, overShadowRate, overHighlightRate);
            }
        }

        public float[] Process(float[] audio, double startTime, IPropertyObject[] properties, ICompositionObject composition)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }

        static NManagedImage ProcessCpu(NImage image, ROI roi, Vector4[] colorMap, float[] colorPositions, Vector4 overShadowRate, Vector4 overHighlightRate)
        {
            var managedImage = image switch
            {
                NGPUImage gpuImage => gpuImage.CopyToCpu(),
                _ => (NManagedImage)image
            };

            var imageWidth = managedImage.Width;
            var imageData = managedImage.Data;
            Parallel.For(roi.Top, roi.Bottom, y =>
            {
                var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);

                for (var x = roi.Left; x < roi.Right; x++)
                {
                    var oldColor = imageDataSpan[x];
                    var lum = Vector4.Dot(oldColor, Const.ConvertToGrayScale);

                    var newColor = oldColor;
                    if (lum <= 0.0F)
                    {
                        newColor = colorMap[0] + overShadowRate * lum;
                    }
                    else if (lum >= 1.0F)
                    {
                        newColor = colorMap[^1] + overHighlightRate * (lum - 1.0F);
                    }
                    else
                    {
                        for (var i = 1; i < colorPositions.Length; i++)
                        {
                            if (colorPositions[i] > lum)
                            {
                                newColor = colorMap[i - 1] + (colorMap[i] - colorMap[i - 1]) * ((lum - colorPositions[i - 1]) / (colorPositions[i] - colorPositions[i - 1]));
                                break;
                            }
                        }
                    }

                    newColor.W = oldColor.W;
                    imageDataSpan[x] = newColor;
                }
            });

            return managedImage;
        }

        static NGPUImage ProcessGpu(GraphicsDevice device, NImage image, ROI roi, Vector4[] colorMap, float[] colorPositions, Vector4 overShadowRate, Vector4 overHighlightRate)
        {
            var gpuImage = image switch
            {
                NManagedImage managedImage => managedImage.CopyToGpu(device),
                _ => (NGPUImage)image
            };

            using var colorMapBuffer = device.AllocateReadOnlyBuffer<Float4>(MemoryMarshal.Cast<Vector4, Float4>(colorMap));
            using var colorPositionBuffer = device.AllocateReadOnlyBuffer(colorPositions);
            using var context = device.CreateComputeContext();

            context.For(roi.Width, roi.Height, new MultiToneProcess(gpuImage.Data, gpuImage.Width, colorMapBuffer, colorPositionBuffer, overShadowRate, overHighlightRate, roi.Left, roi.Top));

            return gpuImage;
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct MultiToneProcess(ReadWriteBuffer<Float4> image, int width, ReadOnlyBuffer<Float4> colorMap, ReadOnlyBuffer<float> colorPositions, Float4 overShadowRate, Float4 overHighlightRate, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            var pos = (ThreadIds.Y + startY) * width + ThreadIds.X + startX;

            var oldColor = image[pos];
            var lum = Hlsl.Dot(oldColor.XYZ, Const.ConvertToGrayScaleFloat3);

            var newColor = oldColor;
            if (lum <= 0.0F)
            {
                newColor = colorMap[0] + overShadowRate * lum;
            }
            else if (lum >= 1.0F)
            {
                newColor = colorMap[colorMap.Length - 1] + overHighlightRate * (lum - 1.0F);
            }
            else
            {
                for (var i = 1; i < colorPositions.Length; i++)
                {
                    if (colorPositions[i] > lum)
                    {
                        newColor = colorMap[i - 1] + (colorMap[i] - colorMap[i - 1]) * ((lum - colorPositions[i - 1]) / (colorPositions[i] - colorPositions[i - 1]));
                        break;
                    }
                }
            }

            newColor.W = oldColor.W;
            image[pos] = newColor;
        }
    }
}
