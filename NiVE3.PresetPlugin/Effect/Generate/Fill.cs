using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Numerics;
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
using NiVE3.PresetPlugin.Resource;

namespace NiVE3.PresetPlugin.Effect.Generate
{
    [EffectMetadata(LanguageResourceDictionary.Generate_Fill_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_Generate, LanguageResourceDictionary.Generate_Fill_Description, ID, IsSupportGpu = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    [Export(typeof(IEffect))]
    public class Fill : IEffect
    {
        const string ID = "6986E6E8-9837-45A2-AFB8-98F42B5E9E78";

        const string PropertyColorId = nameof(PropertyColorId);

        const string PropertyKeepAlphaId = nameof(PropertyKeepAlphaId);

        IAcceleratorObject? AcceleratorObject { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            AcceleratorObject = accelerator;
        }

        public PropertyBase[] GetProperties(Int32Size sourceSize)
        {
            return
            [
                new ColorProperty(PropertyColorId, LanguageResourceDictionary.ResourceKeys.Generate_Fill_Color, LanguageResourceDictionary.ResourceKeys.Dialog_ColorDialog_Title_Color, LanguageResourceDictionary.ResourceKeys.Dialog_OK, LanguageResourceDictionary.ResourceKeys.Dialog_Cancel, new Vector4(0.0F, 0.0F, 1.0F, 1.0F)),
                new CheckBoxProperty(PropertyKeepAlphaId, LanguageResourceDictionary.ResourceKeys.Generate_Fill_Keep_Alpha, true)
            ];
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, Time layerTime, IPropertyObject[] properties, ICompositionObject composition, bool useGpu)
        {
            var color = properties.GetValue(PropertyColorId, layerTime, Vector4.UnitW);
            var keepAlpha = properties.GetValue(PropertyKeepAlphaId, layerTime, false);

            if (useGpu && AcceleratorObject != null)
            {
                return ProcessGpu(AcceleratorObject.CurrentDevice, image, roi, color, keepAlpha);
            }
            else
            {
                return ProcessCpu(image, roi, color, keepAlpha);
            }
        }

        public float[] Process(float[] audio, Time startTime, IPropertyObject[] properties, ICompositionObject composition)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }

        static NManagedImage ProcessCpu(NImage image, ROI roi, Vector4 color, bool keepAlpha)
        {
            var managedImage = image.ToManaged();

            var imageData = managedImage.Data;
            if (keepAlpha)
            {
                Parallel.For(roi.Top, roi.Bottom, y =>
                {
                    var imageDataSpan = imageData.AsSpan(y * managedImage.Width, managedImage.Width);
                    for (var x = roi.Left; x < roi.Right; x++)
                    {
                        var c = color;
                        c.W = imageDataSpan[x].W;
                        imageDataSpan[x] = c;
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
                        imageDataSpan[x] = color;
                    }
                });
            }

            return managedImage;
        }

        static NGPUImage ProcessGpu(GraphicsDevice device, NImage image, ROI roi, Vector4 color, bool keepAlpha)
        {
            var gpuImage = image.ToGpu(device);

            using var context = device.CreateComputeContext();
            context.For(roi.Width, roi.Height, new FillProcess(gpuImage.Data, gpuImage.Width, color, keepAlpha, roi.Left, roi.Top));

            return gpuImage;
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct FillProcess(ReadWriteBuffer<Float4> image, int width, Float4 color, Bool keepAlpha, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            var pos = (ThreadIds.Y + startY) * width + ThreadIds.X + startX;
            if (keepAlpha)
            {
                image[pos].XYZ = color.XYZ;
            }
            else
            {
                image[pos] = color;
            }
        }
    }
}