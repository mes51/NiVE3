using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ComputeSharp;
using NiVE3.Image;
using NiVE3.Numerics;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Property.Properties;
using NiVE3.Plugin.Resource;
using NiVE3.Plugin.ValueObject;
using NiVE3.PresetPlugin.Effect.Util;
using NiVE3.PresetPlugin.Effect.Util.Distortion;
using NiVE3.PresetPlugin.Extension;
using NiVE3.PresetPlugin.Internal;
using NiVE3.PresetPlugin.Internal.Drawing;
using NiVE3.PresetPlugin.Resource;
using NiVE3.Shared.Extension;

namespace NiVE3.PresetPlugin.Effect.Distortion
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.Distortion_PolarDistortion_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_Distortion, LanguageResourceDictionary.Distortion_PolarDistortion_Description, ID, IsSupportGpu = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class PolarDistortion : IEffect
    {
        const string ID = "5703D03F-B411-42C2-ABA7-0835D8CA0D6B";

        const string PropertyTransformId = nameof(PropertyTransformId);

        const string PropertyModeId = nameof(PropertyModeId);

        const string PropertyImageOffsetId = nameof(PropertyImageOffsetId);

        const string PropertyDisplayAreaOffsetId = nameof(PropertyDisplayAreaOffsetId);

        const string PropertyForPreOrPostProcessId = nameof(PropertyForPreOrPostProcessId);

        const float SqrtHalf = 0.7071067811865476F; // Math.Sqrt(0.5);

        IAcceleratorObject? AcceleratorObject { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            AcceleratorObject = accelerator;
        }

        public PropertyBase[] GetProperties(Int32Size sourceSize)
        {
            return
            [
                new DoubleProperty(PropertyTransformId, LanguageResourceDictionary.ResourceKeys.Distortion_PolarDistortion_Transform, 0.0, 0.0, 100.0, digit: 2),
                new EnumProperty(PropertyModeId, LanguageResourceDictionary.ResourceKeys.Distortion_PolarDistortion_Mode, typeof(PolarDistortionMode), typeof(LanguageResourceDictionary), PolarDistortionMode.ToPolar, selectBoxWidth: 90.0),
                new Vector3dProperty(PropertyImageOffsetId, LanguageResourceDictionary.ResourceKeys.Distortion_PolarDistortion_ImageOffset, Vector3d.Zero, digit: 2),
                new Vector3dProperty(PropertyDisplayAreaOffsetId, LanguageResourceDictionary.ResourceKeys.Distortion_PolarDistortion_DisplayAreaOffset, Vector3d.Zero, digit: 2),
                new CheckBoxProperty(PropertyForPreOrPostProcessId, LanguageResourceDictionary.ResourceKeys.Distortion_PolarDistortion_ForPreOrPostProcess, false)
            ];
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, Time layerTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer, bool useGpu)
        {
            var transformAmount = (float)properties.GetValue(PropertyTransformId, layerTime, 0.0) * 0.01F;
            var mode = properties.GetValue(PropertyModeId, layerTime, PolarDistortionMode.ToPolar);
            var imageOffset = ((Vector3)properties.GetValue(PropertyImageOffsetId, layerTime, Vector3d.Zero)).AsVector2();
            var displayAreaOffset = ((Vector3)properties.GetValue(PropertyDisplayAreaOffsetId, layerTime, Vector3d.Zero)).AsVector2();
            var forPreOrPostProcess = properties.GetValue(PropertyForPreOrPostProcessId, layerTime, false);

            if (transformAmount <= 0.0F && imageOffset == Vector2.Zero && displayAreaOffset == Vector2.Zero)
            {
                return image;
            }

            if (useGpu && AcceleratorObject != null)
            {
                return PolarDistortionProcessor.ProcessGpu(AcceleratorObject.CurrentDevice, image, roi, transformAmount, mode, imageOffset, displayAreaOffset, forPreOrPostProcess);
            }
            else
            {
                return PolarDistortionProcessor.ProcessCpu(image, roi, transformAmount, mode, imageOffset, displayAreaOffset, forPreOrPostProcess);
            }
        }

        public float[] Process(float[] audio, Time startTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }
    }
}
