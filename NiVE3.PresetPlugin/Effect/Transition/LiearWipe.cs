using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.CompilerServices;
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
using NiVE3.PresetPlugin.Effect.Util.General;
using NiVE3.PresetPlugin.Extension;
using NiVE3.PresetPlugin.Resource;

namespace NiVE3.PresetPlugin.Effect.Transition
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.Transition_LinearWipe_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_Transition, LanguageResourceDictionary.Transition_LinearWipe_Description, ID, IsSupportGpu = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class LiearWipe : IEffect
    {
        const string ID = "147496DE-BEC1-4B6A-AB37-011D0CF4AE72";

        const string PropertyTransformId = nameof(PropertyTransformId);

        const string PropertyAngleId = nameof(PropertyAngleId);

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
                new DoubleProperty(PropertyTransformId, LanguageResourceDictionary.ResourceKeys.Transition_LinearWipe_Transform, 0.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent),
                new AngleProperty(PropertyAngleId, LanguageResourceDictionary.ResourceKeys.Transition_LinearWipe_Angle, 90.0, digit: 2),
                new DoubleProperty(PropertyBlurId, LanguageResourceDictionary.ResourceKeys.Transition_LinearWipe_Blur, 0.0, 0.0, double.MaxValue, digit: 2)
            ];
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, Time layerTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer, bool useGpu)
        {
            var transform = (float)(properties.GetValue(PropertyTransformId, layerTime, 0.0) * 0.01);
            if (transform <= 0.0F)
            {
                return image;
            }

            var angle = properties.GetValue(PropertyAngleId, layerTime, 0.0);
            var blur = Math.Max((float)(properties.GetValue(PropertyBlurId, layerTime, 0.0) / downSamplingRateX), 0.01F);

            if (useGpu && AcceleratorObject != null)
            {
                return ProcessGpu(AcceleratorObject.CurrentDevice, image, roi, transform, angle, blur);
            }
            else
            {
                return ProcessCpu(image, roi, transform, angle, blur);
            }
        }

        public float[] Process(float[] audio, Time startTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }

        static NManagedImage ProcessCpu(NImage image, ROI roi, float transform, double angle, float blur)
        {
            var managedImage = image.ToManaged();
            var imageWidth = managedImage.Width;
            var imageData = managedImage.Data;
            
            if (transform >= 1.0F)
            {
                ImageMaskProcessor.FillAlphaZeroCpu(managedImage, roi);
                return managedImage;
            }

            transform = 1.0F - transform;

            var (sin, cos) = Math.SinCos(angle / 180.0 * Math.PI);
            var length = (float)(managedImage.Height * Math.Abs(cos) + managedImage.Width * Math.Abs(sin) + blur);
            var wipeStartY = blur * -0.5F + length * transform;
            var wipeEndY = blur * 0.5F + length * transform;
            var add = 1.0F / blur;
            var matrix = Matrix3x3.CreateTranslate(-length * 0.5F, -length * 0.5F)
                .Rotate((float)angle)
                .Translate(managedImage.Width * 0.5F, managedImage.Height * 0.5F);
            if (Matrix3x3.Invert(matrix, out var inverted))
            {
                Parallel.For(roi.Top, roi.Bottom, y =>
                {
                    var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);

                    for (var x = roi.Left; x < roi.Right; x++)
                    {
                        var (_, ty) = inverted.Transform(x, y);
                        if (wipeEndY < ty)
                        {
                            Unsafe.AsRef(ref imageDataSpan[x]).W = 0.0F;
                        }
                        else if (wipeStartY < ty)
                        {
                            Unsafe.AsRef(ref imageDataSpan[x]).W *= (blur - (ty - wipeStartY)) * add;
                        }
                    }
                });
            }

            return managedImage;
        }

        static NGPUImage ProcessGpu(GraphicsDevice device, NImage image, ROI roi, float transform, double angle, float blur)
        {
            var gpuImage = image.ToGpu(device);

            if (transform >= 1.0F)
            {
                ImageMaskProcessor.FillAlphaZeroGpu(device, gpuImage, roi);
                return gpuImage;
            }

            transform = 1.0F - transform;

            var (sin, cos) = Math.SinCos(angle / 180.0 * Math.PI);
            var length = (float)(gpuImage.Height * Math.Abs(cos) + gpuImage.Width * Math.Abs(sin) + blur);
            var wipeStartY = blur * -0.5F + length * transform;
            var wipeEndY = blur * 0.5F + length * transform;
            var matrix = Matrix3x3.CreateTranslate(-length * 0.5F, -length * 0.5F)
                .Rotate((float)angle)
                .Translate(gpuImage.Width * 0.5F, gpuImage.Height * 0.5F);
            if (Matrix3x3.Invert(matrix, out var inverted))
            {
                device.For(roi.Width, roi.Height, new LinearWipeProcess(gpuImage.Data, gpuImage.Width, inverted.ToFloat3x3(), wipeStartY, wipeEndY, blur, roi.Left, roi.Top));
            }

            return gpuImage;
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct LinearWipeProcess(ReadWriteBuffer<Float4> image, int width, Float3x3 matrix, float wipeStartY, float wipeEndY, float blur, int startX, int startY) : IComputeShader
    {
        readonly float Add = 1.0F / blur;

        public void Execute()
        {
            var x = ThreadIds.X + startX;
            var y = ThreadIds.Y + startY;
            var pos = y * width + x;

            var ty = (matrix * new Float3(x, y, 1.0F)).Y;
            if (wipeEndY < ty)
            {
                image[pos].W = 0.0F;
            }
            else if (wipeStartY < ty)
            {
                image[pos].W *= (blur - (ty - wipeStartY)) * Add;
            }
        }
    }
}
