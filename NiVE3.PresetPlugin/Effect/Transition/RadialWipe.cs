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
using NiVE3.PresetPlugin.Effect.Util.Blur;
using NiVE3.PresetPlugin.Effect.Util.General;
using NiVE3.PresetPlugin.Extension;
using NiVE3.PresetPlugin.Resource;

namespace NiVE3.PresetPlugin.Effect.Transition
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.Transition_RadialWipe_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_Transition, LanguageResourceDictionary.Transition_RadialWipe_Description, ID, IsSupportGpu = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class RadialWipe : IEffect
    {
        const string ID = "761D3ACC-2EED-4ADF-98F9-2F7FEF728057";

        const string PropertyCenterId = nameof(PropertyCenterId);

        const string PropertyAngleId = nameof(PropertyAngleId);

        const string PropertyTransformId = nameof(PropertyTransformId);

        const string PropertyModeId = nameof(PropertyModeId);

        const string PropertyBlurId = nameof(PropertyBlurId);

        IAcceleratorObject? AcceleratorObject { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            AcceleratorObject = accelerator;
        }

        public PropertyBase[] GetProperties(Int32Size sourceSize)
        {
            return
            [
                new Vector3dProperty(PropertyCenterId, LanguageResourceDictionary.ResourceKeys.Transition_RadialWipe_Center, new Vector3d(sourceSize.Width, sourceSize.Height, 0.0) * 0.5, digit: 2, useInteraction: true),
                new AngleProperty(PropertyAngleId, LanguageResourceDictionary.ResourceKeys.Transition_RadialWipe_Angle, 90.0, digit: 2),
                new DoubleProperty(PropertyTransformId, LanguageResourceDictionary.ResourceKeys.Transition_RadialWipe_Transform, 0.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent),
                new EnumProperty(PropertyModeId, LanguageResourceDictionary.ResourceKeys.Transition_RadialWipe_Mode, typeof(RadialWipeMode), typeof(LanguageResourceDictionary), RadialWipeMode.Clock, selectBoxWidth: 90.0),
                new DoubleProperty(PropertyBlurId, LanguageResourceDictionary.ResourceKeys.Transition_RadialWipe_Blur, 0.0, 0.0, double.MaxValue, digit: 2)
            ];
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, Time layerTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer, bool useGpu)
        {
            var transform = (float)(properties.GetValue(PropertyTransformId, layerTime, 0.0) * 0.01);
            if (transform <= 0.0F)
            {
                return image;
            }

            var center = (Vector2)(properties.GetValue(PropertyCenterId, layerTime, Vector3d.Zero) / new Vector3d(downSamplingRateX, downSamplingRateY, 1.0) + new Vector3d(roi.OriginalImagePosition.X, roi.OriginalImagePosition.Y, 0.0));
            var angle = (float)properties.GetValue(PropertyAngleId, layerTime, 0.0);
            var mode = properties.GetValue(PropertyModeId, layerTime, RadialWipeMode.Clock);
            var blur = (float)(properties.GetValue(PropertyBlurId, layerTime, 0.0) / downSamplingRateX);

            if (useGpu && AcceleratorObject != null)
            {
                return ProcessGpu(AcceleratorObject.CurrentDevice, image, roi, center, angle, transform, mode, blur);
            }
            else
            {
                return ProcessCpu(image, roi, center, angle, transform, mode, blur);
            }
        }

        public float[] Process(float[] audio, Time startTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }

        static NManagedImage ProcessCpu(NImage image, ROI roi, Vector2 center, float angle, float transform, RadialWipeMode mode, float blur)
        {
            var managedImage = image.ToManaged();

            if (transform >= 1.0F)
            {
                ImageMaskProcessor.FillAlphaZeroCpu(managedImage, roi);
                return managedImage;
            }

            using var mask = new ManagedRasterizedMaskImage(managedImage.Width, managedImage.Height, 1.0F);
            var imageWidth = managedImage.Width;
            var maskData = mask.Data;

            angle -= mode switch
            {
                RadialWipeMode.CounterClock => transform * 360.0F,
                RadialWipeMode.Both => transform * 180.0F,
                _ => 0.0F
            };
            angle %= 360.0F;
            if (angle < 0.0F)
            {
                angle += 360.0F;
            }
            var beginRad = (angle % 360.0F) / 180.0F * MathF.PI;
            var transformRad = MathF.PI * 2.0F * transform + beginRad;
            var secondTransformRad = Math.Max(0.0F, transformRad - MathF.PI * 2.0F);

            Parallel.For(0, managedImage.Height, y =>
            {
                var maskDataSpan = maskData.AsSpan(y * imageWidth, imageWidth);
                var py = y - center.Y;
                for (var x = 0; x < imageWidth; x++)
                {
                    var px = x - center.X;
                    var rad = MathF.Atan2(py, px) + MathF.PI;
                    if ((rad > beginRad && rad < transformRad) || rad < secondTransformRad)
                    {
                        maskDataSpan[x] = 0.0F;
                    }
                }
            });

            if (blur > 0.0F)
            {
                MaskBoxBlurProcessor.ProcessCpu(mask, roi, blur, blur, 3, EdgeRepeatMode.Wrap);
            }

            ImageMaskProcessor.SameSizeMaskCpu(managedImage, mask, roi);
            
            return managedImage;
        }

        static NGPUImage ProcessGpu(GraphicsDevice device, NImage image, ROI roi, Vector2 center, float angle, float transform, RadialWipeMode mode, float blur)
        {
            var gpuImage = image.ToGpu(device);

            if (transform >= 1.0F)
            {
                ImageMaskProcessor.FillAlphaZeroGpu(device, gpuImage, roi);
                return gpuImage;
            }

            angle -= mode switch
            {
                RadialWipeMode.CounterClock => transform * 360.0F,
                RadialWipeMode.Both => transform * 180.0F,
                _ => 0.0F
            };
            angle %= 360.0F;
            if (angle < 0.0F)
            {
                angle += 360.0F;
            }
            var beginRad = (angle % 360.0F) / 180.0F * MathF.PI;
            var transformRad = MathF.PI * 2.0F * transform + beginRad;
            var secondTransformRad = Math.Max(0.0F, transformRad - MathF.PI * 2.0F);

            using var mask = new GPURasterizedMaskImage(gpuImage.Width, gpuImage.Height, device);
            device.For(mask.Width, mask.Height, new RadialWipeMaskProcess(mask.Data, mask.Width, center, beginRad, transformRad, secondTransformRad));

            if (blur > 0.0F)
            {
                MaskBoxBlurProcessor.ProcessGpu(device, mask, roi, blur, blur, 3, EdgeRepeatMode.Wrap);
            }

            ImageMaskProcessor.SameSizeMaskGpu(device, gpuImage, mask, roi);

            return gpuImage;
        }
    }

    enum RadialWipeMode
    {
        Clock,
        CounterClock,
        Both
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct RadialWipeMaskProcess(ReadWriteBuffer<float> mask, int width, Float2 center, float beginRad, float transformRad, float secondTransformRad) : IComputeShader
    {
        public void Execute()
        {
            var mp = (Float2)ThreadIds.XY - center;
            var rad = Hlsl.Atan2(mp.Y, mp.X) + MathF.PI;
            var pos = ThreadIds.Y * width + ThreadIds.X;
            if ((rad > beginRad && rad < transformRad) || rad < secondTransformRad)
            {
                mask[pos] = 0.0F;
            }
            else
            {
                mask[pos] = 1.0F;
            }
        }
    }
}
