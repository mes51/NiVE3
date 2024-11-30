using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Image;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Property.Properties;
using NiVE3.Plugin.Resource;
using NiVE3.Plugin.ValueObject;
using NiVE3.PresetPlugin.Effect.Util.Blur;
using NiVE3.PresetPlugin.Extension;
using NiVE3.PresetPlugin.Resource;

namespace NiVE3.PresetPlugin.Effect.Blur
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.Blur_DirectionalBlur_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_Blur, LanguageResourceDictionary.Blur_DirectionalBlur_Description, ID, IsSupportGpu = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class DirectionalBlur : IEffect
    {
        const string ID = "8FC54C8A-45DC-4F54-9340-D102591FBD6E";

        const string PropertyAngleId = nameof(PropertyAngleId);

        const string PropertyAmountId = nameof(PropertyAmountId);

        const string PropertyEdgeRepeatModeId = nameof(PropertyEdgeRepeatModeId);

        const string PropertyFastModeId = nameof(PropertyFastModeId);

        IAcceleratorObject? AcceleratorObject { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            AcceleratorObject = accelerator;
        }

        public PropertyBase[] GetProperties(Int32Size sourceSize)
        {
            return
            [
                new AngleProperty(PropertyAngleId, LanguageResourceDictionary.ResourceKeys.Blur_DirectionalBlur_Angle, 0.0, digit: 2),
                new DoubleProperty(PropertyAmountId, LanguageResourceDictionary.ResourceKeys.Blur_DirectionalBlur_Amount, 0.0, 0.0, double.MaxValue, digit: 2),
                new EnumProperty(PropertyEdgeRepeatModeId, LanguageResourceDictionary.ResourceKeys.Blur_DirectionalBlur_EdgeRepeatMode, typeof(EdgeRepeatMode), typeof(LanguageResourceDictionary), EdgeRepeatMode.None, selectBoxWidth: 90.0),
                new CheckBoxProperty(PropertyFastModeId, LanguageResourceDictionary.ResourceKeys.Blur_DirectionalBlur_FastMode, false)
            ];
        }

        public ROI CalcRoi(ROI baseRoi, double downSamplingRateX, double downSamplingRateY, double layerTime, IPropertyObject[] properties, ICompositionObject composition)
        {
            var edgeRepeatMode = properties.GetValue(PropertyEdgeRepeatModeId, layerTime, EdgeRepeatMode.None);

            if (edgeRepeatMode == EdgeRepeatMode.AddAmount)
            {
                var rad = (properties.GetValue(PropertyAngleId, layerTime, 0.0) + 90.0) / 180.0 * Math.PI;
                var amount = (float)(properties.GetValue(PropertyAmountId, layerTime, 0.0) / downSamplingRateX);
                var maxRange = (int)MathF.Ceiling(amount) * 2;
                var sin = (float)Math.Sin(rad);
                var cos = (float)Math.Cos(rad);

                var expandX = (int)Math.Ceiling(Math.Abs(maxRange * cos));
                var expandY = (int)Math.Ceiling(Math.Abs(maxRange * sin));
                return baseRoi.Expand(-expandX, -expandY, expandX, expandY);
            }
            else
            {
                return baseRoi;
            }
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, double layerTime, IPropertyObject[] properties, ICompositionObject composition, bool useGpu)
        {
            var angle = (float)properties.GetValue(PropertyAngleId, layerTime, 0.0) + 90.0F;
            var amount = (float)(properties.GetValue(PropertyAmountId, layerTime, 0.0) / downSamplingRateX);
            var edgeRepeatMode = properties.GetValue(PropertyEdgeRepeatModeId, layerTime, EdgeRepeatMode.None);
            var fastMode = properties.GetValue(PropertyFastModeId, layerTime, false);

            if (amount <= 0.0F)
            {
                return image;
            }

            var rad = angle / 180.0 * Math.PI;
            if (useGpu && AcceleratorObject != null)
            {
                var device = AcceleratorObject.CurrentDevice;
                var gpuImage = image switch
                {
                    NManagedImage managedImage => managedImage.CopyToGpu(device),
                    _ => (NGPUImage)image
                };
                DirectionalBlurProcess.ProcessGpu(device, gpuImage, roi, rad, amount, edgeRepeatMode);
                return gpuImage;
            }
            else
            {
                var managedImage = image switch
                {
                    NGPUImage gpuImage => gpuImage.CopyToCpu(),
                    _ => (NManagedImage)image
                };
                DirectionalBlurProcess.ProcessCpu(managedImage, roi, rad, amount, edgeRepeatMode, fastMode);
                return managedImage;
            }
        }

        public float[] Process(float[] audio, double startTime, IPropertyObject[] properties, ICompositionObject composition)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }
    }
}
