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
using NiVE3.PresetPlugin.Internal.Drawing;
using NiVE3.PresetPlugin.Resource;

namespace NiVE3.PresetPlugin.Effect.Distortion
{
    // SEE: https://www.shadertoy.com/view/MtcXDH

    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.Distortion_FishEye_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_Distortion, LanguageResourceDictionary.Distortion_FishEye_Description, ID, IsSupportGpu = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class FishEye : IEffect
    {
        const string ID = "B0CDC76D-B619-4816-A50D-5914E66F7BBB";

        const string PropertyAmountId = nameof(PropertyAmountId);

        const float PowerRate = 0.2F;

        IAcceleratorObject? AcceleratorObject { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            AcceleratorObject = accelerator;
        }

        public PropertyBase[] GetProperties(Int32Size sourceSize)
        {
            return
            [
                new DoubleProperty(PropertyAmountId, LanguageResourceDictionary.ResourceKeys.Distortion_FishEye_Amount, 0.0, double.MinValue, double.MaxValue, slideChangeValue: 0.1, digit: 2)
            ];
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, double layerTime, IPropertyObject[] properties, ICompositionObject composition, bool useGpu)
        {
            var amount = (float)properties.GetValue(PropertyAmountId, layerTime, 0.0);

            if (amount == 0.0F)
            {
                return image;
            }

            if (useGpu && AcceleratorObject != null)
            {
                return ProcessGpu(AcceleratorObject.CurrentDevice, image, roi, amount);
            }
            else
            {
                return ProcessCpu(image, roi, amount * PowerRate);
            }
        }

        public float[] Process(float[] audio, double startTime, IPropertyObject[] properties, ICompositionObject composition)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }

        static NManagedImage ProcessCpu(NImage image, ROI roi, float amount)
        {
            var managedImage = image.ToManaged();

            using var sourceImage = (NManagedImage)managedImage.Copy();

            var imageWidth = managedImage.Width;
            var imageHeight = managedImage.Height;
            var imageData = managedImage.Data;
            var sourceImageData = sourceImage.Data;

            var center = new Vector2(0.5F);
            var imageSize = new Vector2(imageWidth, imageHeight);
            Parallel.For(roi.Top, roi.Bottom, y =>
            {
                var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                for (var x = roi.Left; x < roi.Right; x++)
                {
                    var normalizedPos = new Vector2(x, y) / imageSize;
                    var m = normalizedPos - center;
                    var sourcePos = (center + m / MathF.Sqrt(1.0F - amount * Vector2.Dot(m, m))) * imageSize;

                    imageDataSpan[x] = ImageInterpolation.Bilinear(sourceImageData, imageWidth, imageHeight, sourcePos.X, sourcePos.Y);
                }
            });

            return managedImage;
        }

        static NGPUImage ProcessGpu(GraphicsDevice device, NImage image, ROI roi, float amount)
        {
            var gpuImage = image.ToGpu(device);

            using var sourceImage = new NGPUImage(gpuImage.Width, gpuImage.Height, device);
            gpuImage.CopyTo(sourceImage);

            using var context = device.CreateComputeContext();
            context.For(roi.Width, roi.Height, new FishEyeProcess(gpuImage.Data, sourceImage.Data, gpuImage.Width, gpuImage.Height, amount, roi.Left, roi.Top));

            return gpuImage;
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct FishEyeProcess(ReadWriteBuffer<Float4> image, ReadWriteBuffer<Float4> originalImage, int width, int height, float amount, int startX, int startY) : IComputeShader
    {
        readonly Float2 ImageSize = new Float2(width, height);

        public void Execute()
        {
            var pos = (ThreadIds.Y + startY) * width + ThreadIds.X + startX;

            var normalizedPos = new Float2(ThreadIds.X + startX, ThreadIds.Y + startY) / ImageSize;
            var m = normalizedPos - 0.5F;
            var sourcePos = (0.5F + m / Hlsl.Sqrt(1.0F - amount * Hlsl.Dot(m, m))) * ImageSize;

            image[pos] = OriginalImageBilinear(sourcePos.X, sourcePos.Y);
        }

        Float4 OriginalImageBilinear(float x, float y)
        {
            var ix = (int)x;
            var iy = (int)y;

            if (ix == x && iy == y)
            {
                if (ix > -1 && iy > -1 && ix < width && iy < height)
                {
                    return originalImage[iy * width + ix];
                }
                else
                {
                    return new Float4(1.0F, 1.0F, 1.0F, 0.0F);
                }
            }
            else if (ix < -1 || iy < -1 || ix >= width || iy >= height)
            {
                return new Float4(1.0F, 1.0F, 1.0F, 0.0F);
            }

            var pp = x - ix;
            var qq = y - iy;
            var ip = 1.0F - pp;
            var iq = 1.0F - qq;
            var mw = width - 1;
            var mh = height - 1;

            var c1 = new Float4(1.0F, 1.0F, 1.0F, 0.0F);
            var c2 = new Float4(1.0F, 1.0F, 1.0F, 0.0F);
            var c3 = new Float4(1.0F, 1.0F, 1.0F, 0.0F);
            var c4 = new Float4(1.0F, 1.0F, 1.0F, 0.0F);
            var pos = iy * width + ix;

            if (ix > -1)
            {
                if (ix < mw)
                {
                    if (iy > -1)
                    {
                        c1 = originalImage[pos];
                        c2 = originalImage[pos + 1];
                        if (iy < mh)
                        {
                            pos += width;
                            c3 = originalImage[pos];
                            c4 = originalImage[pos + 1];
                        }
                    }
                    else
                    {
                        pos += width;
                        c3 = originalImage[pos];
                        c4 = originalImage[pos + 1];
                    }
                }
                else
                {
                    if (iy > -1)
                    {
                        c1 = originalImage[pos];
                        if (iy < mh)
                        {
                            c3 = originalImage[pos + width];
                        }
                    }
                    else
                    {
                        c3 = originalImage[pos + width];
                    }
                }
            }
            else
            {
                pos++;
                if (iy > -1)
                {
                    c2 = originalImage[pos];
                    if (iy < mh)
                    {
                        c4 = originalImage[pos + width];
                    }
                }
                else
                {
                    c4 = originalImage[pos + width];
                }
            }

            var ta = Hlsl.Lerp(Hlsl.Lerp(c1, c3, qq), Hlsl.Lerp(c2, c4, qq), pp).W;
            if (ta <= 0.0F)
            {
                return new Float4(1.0F, 1.0F, 1.0F, 0.0F);
            }
            else
            {
                var t = Hlsl.Lerp(Hlsl.Lerp(c1 * c1.W, c3 * c3.W, qq), Hlsl.Lerp(c2 * c2.W, c4 * c4.W, qq), pp) / ta;
                t.W = ta;
                return t;
            }
        }
    }
}
