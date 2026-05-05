using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ComputeSharp;
using NiVE3.Image;
using NiVE3.Image.Drawing;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Property.Properties;
using NiVE3.Plugin.Resource;
using NiVE3.Plugin.ValueObject;
using NiVE3.PresetPlugin.Effect.Util;
using NiVE3.PresetPlugin.Extension;
using NiVE3.PresetPlugin.Internal.ComputeShader;
using NiVE3.PresetPlugin.Resource;

namespace NiVE3.PresetPlugin.Effect.Channel
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.Channel_SolidComposite_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_Channel, LanguageResourceDictionary.Channel_SolidComposite_Description, ID, IsSupportGpu = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class SolidComposite : IEffect
    {
        const string ID = "94CF8891-60FB-4ED0-AE28-3E5577576766";

        const string PropertySourceOpacityId = nameof(PropertySourceOpacityId);

        const string PropertyColorId = nameof(PropertyColorId);

        const string PropertyOpacityId = nameof(PropertyOpacityId);

        const string PropertyOrderId = nameof(PropertyOrderId);

        const string PropertyBlendModeId = nameof(PropertyBlendModeId);

        const string PropertyIsKeepAlphaId = nameof(PropertyIsKeepAlphaId);

        IAcceleratorObject? AcceleratorObject { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            AcceleratorObject = accelerator;
        }

        public PropertyBase[] GetProperties(Int32Size sourceSize)
        {
            return
            [
                new DoubleProperty(PropertySourceOpacityId, LanguageResourceDictionary.ResourceKeys.Channel_SolidComposite_SourceOpacity, 100.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent),
                new ColorProperty(PropertyColorId, LanguageResourceDictionary.ResourceKeys.Channel_SolidComposite_Color, LanguageResourceDictionary.ResourceKeys.Dialog_ColorDialog_Title_Color, LanguageResourceDictionary.ResourceKeys.Dialog_OK, LanguageResourceDictionary.ResourceKeys.Dialog_Cancel, Vector4.One),
                new DoubleProperty(PropertyOpacityId, LanguageResourceDictionary.ResourceKeys.Channel_SolidComposite_Opacity, 100.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent),
                new EnumProperty(PropertyOrderId, LanguageResourceDictionary.ResourceKeys.Channel_SolidComposite_Order, typeof(CompositeOrder), typeof(LanguageResourceDictionary), CompositeOrder.Front, selectBoxWidth: 90.0),
                new EnumProperty(PropertyBlendModeId, LanguageResourceDictionary.ResourceKeys.Channel_SolidComposite_BlendMode, typeof(BlendMode), typeof(LanguageResourceDictionary), BlendMode.Normal, selectBoxWidth: 90.0),
                new CheckBoxProperty(PropertyIsKeepAlphaId, LanguageResourceDictionary.ResourceKeys.Channel_SolidComposite_IsKeepAlpha, false)
            ];
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, Time layerTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer, bool useGpu)
        {
            var sourceOpacity = (float)properties.GetValue(PropertySourceOpacityId, layerTime, 100.0) * 0.01F;
            var color = properties.GetValue(PropertyColorId, layerTime, Vector4.Zero);
            var opacity = (float)properties.GetValue(PropertyOpacityId, layerTime, 100.0) * 0.01F;
            var order = properties.GetValue(PropertyOrderId, layerTime, CompositeOrder.Front);
            var blendMode = properties.GetValue(PropertyBlendModeId, layerTime, BlendMode.Normal);
            var isKeepAlpha = properties.GetValue(PropertyIsKeepAlphaId, layerTime, false);
            color.W = opacity;

            if (useGpu && AcceleratorObject != null)
            {
                return ProcessGpu(AcceleratorObject.CurrentDevice, image, roi, sourceOpacity, color, order, blendMode, isKeepAlpha);
            }
            else
            {
                return ProcessCpu(image, roi, sourceOpacity, color, order, blendMode, isKeepAlpha);
            }
        }

        public float[] Process(float[] audio, Time startTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }

        static NManagedImage ProcessCpu(NImage image, ROI roi, float sourceOpacity, Vector4 color, CompositeOrder order, BlendMode blendMode, bool isKeepAlpha)
        {
            var managedImage = image.ToManaged();

            var imageData = managedImage.Data;
            if (isKeepAlpha)
            {
                if (order == CompositeOrder.Front)
                {
                    Parallel.For(roi.Top, roi.Bottom, y =>
                    {
                        var imageDataSpan = imageData.AsSpan(y * managedImage.Width, managedImage.Width);
                        for (var x = roi.Left; x < roi.Right; x++)
                        {
                            var c = imageDataSpan[x];
                            var alpha = c.W;
                            c.W *= sourceOpacity;
                            var newColor = Blend.Process(blendMode, color, c);
                            newColor.W = alpha;
                            imageDataSpan[x] = newColor;
                        }
                    });
                }
                else
                {
                    Parallel.For(roi.Top, roi.Bottom, y =>
                    {
                        var imageDataSpan = imageData.AsSpan(y * managedImage.Width, managedImage.Width);
                        for (var x = roi.Left; x < roi.Right; x++)
                        {
                            var c = imageDataSpan[x];
                            var alpha = c.W;
                            c.W *= sourceOpacity;
                            var newColor = Blend.Process(blendMode, c, color);
                            newColor.W = alpha;
                            imageDataSpan[x] = newColor;
                        }
                    });
                }
            }
            else
            {
                if (order == CompositeOrder.Front)
                {
                    Parallel.For(roi.Top, roi.Bottom, y =>
                    {
                        var imageDataSpan = imageData.AsSpan(y * managedImage.Width, managedImage.Width);
                        for (var x = roi.Left; x < roi.Right; x++)
                        {
                            var c = imageDataSpan[x];
                            c.W *= sourceOpacity;
                            imageDataSpan[x] = Blend.Process(blendMode, color, c);
                        }
                    });
                }
                else
                {
                    Parallel.For(roi.Top, roi.Bottom, y =>
                    {
                        var imageDataSpan = imageData.AsSpan(y * managedImage.Width, managedImage.Width);
                        for (var x = roi.Left; x < roi.Right; x++)
                        {
                            var c = imageDataSpan[x];
                            c.W *= sourceOpacity;
                            imageDataSpan[x] = Blend.Process(blendMode, c, color);
                        }
                    });
                }
            }

            return managedImage;
        }

        static NGPUImage ProcessGpu(GraphicsDevice device, NImage image, ROI roi, float sourceOpacity, Vector4 color, CompositeOrder order, BlendMode blendMode, bool isKeepAlpha)
        {
            var gpuImage = image.ToGpu(device);

            using var context = device.CreateComputeContext();
            context.For(roi.Width, roi.Height, new SolidCompositeProcess(gpuImage.Data, gpuImage.Width, sourceOpacity, color, (int)order, (int)blendMode, isKeepAlpha, roi.Left, roi.Top));

            return gpuImage;
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct SolidCompositeProcess(ReadWriteBuffer<Float4> image, int width, float sourceOpacity, Float4 color, int order, int blendMode, bool isKeepAlpha, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            var pos = (ThreadIds.Y + startY) * width + ThreadIds.X + startX;
            var c = image[pos];
            var alpha = c.W;
            c.W *= sourceOpacity;

            if (isKeepAlpha)
            {
                var newColor = Float4.Zero;
                if (order == 0)
                {
                    newColor = BlendMethods.Process(blendMode, color, c);
                }
                else
                {
                    newColor = BlendMethods.Process(blendMode, c, color);
                }
                newColor.W = alpha;
                image[pos] = newColor;
            }
            else
            {
                if (order == 0)
                {
                    image[pos] = BlendMethods.Process(blendMode, color, c);
                }
                else
                {
                    image[pos] = BlendMethods.Process(blendMode, c, color);
                }
            }
        }
    }
}
