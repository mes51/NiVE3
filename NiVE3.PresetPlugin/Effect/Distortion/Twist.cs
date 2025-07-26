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
using NiVE3.PresetPlugin.Extension;
using NiVE3.PresetPlugin.Internal;
using NiVE3.PresetPlugin.Internal.Drawing;
using NiVE3.PresetPlugin.Resource;

namespace NiVE3.PresetPlugin.Effect.Distortion
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.Distortion_Twist_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_Distortion, LanguageResourceDictionary.Distortion_Twist_Description, ID, IsSupportGpu = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class Twist : IEffect
    {
        const string ID = "8F318115-099E-499B-A3E7-7AB92FA5F552";

        const string PropertyAngleId = nameof(PropertyAngleId);

        const string PropertyCenterId = nameof(PropertyCenterId);

        IAcceleratorObject? AcceleratorObject { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            AcceleratorObject = accelerator;
        }

        public PropertyBase[] GetProperties(Int32Size sourceSize)
        {
            return
            [
                new AngleProperty(PropertyAngleId, LanguageResourceDictionary.ResourceKeys.Distortion_Twist_Angle, 0.0, digit: 2),
                new Vector3dProperty(PropertyCenterId, LanguageResourceDictionary.ResourceKeys.Distortion_Twist_Center, new Vector3d(sourceSize.Width, sourceSize.Height, 0.0) * 0.5, digit: 2, useInteraction: true)
            ];
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, Time layerTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer, bool useGpu)
        {
            var angle = (float)(properties.GetValue(PropertyAngleId, layerTime, 0.0) / 180.0 * Math.PI * 0.001);
            var center = (Vector2)(properties.GetValue(PropertyCenterId, layerTime, Vector3d.Zero) / new Vector3d(downSamplingRateX, downSamplingRateY, 1.0));

            if (angle == 0.0)
            {
                return image;
            }

            center += new Vector2(roi.OriginalImagePosition.X, roi.OriginalImagePosition.Y);

            if (useGpu && AcceleratorObject != null)
            {
                return ProcessGpu(AcceleratorObject.CurrentDevice, image, roi, angle, center);
            }
            else
            {
                return ProcessCpu(image, roi, angle, center);
            }
        }

        public float[] Process(float[] audio, Time startTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }

        static NManagedImage ProcessCpu(NImage image, ROI roi, float radianRate, Vector2 center)
        {
            var managedImage = image.ToManaged();
            using var sourceImage = (NManagedImage)managedImage.Copy();

            var imageWidth = managedImage.Width;
            var imageHeight = managedImage.Height;
            var imageData = managedImage.Data;
            var sourceData = sourceImage.Data;
            Parallel.For(roi.Top, roi.Bottom, y =>
            {
                var py = y - center.Y;
                var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                for (var x = roi.Left; x < roi.Right; x++)
                {
                    var px = x - center.X;
                    var r = MathF.Sqrt(px * px + py * py);
                    var rad = MathF.Atan2(py, px) + radianRate * r;
                    var pos = new Vector2(MathF.Cos(rad), MathF.Sin(rad)) * r + center;
                    imageDataSpan[x] = ImageInterpolation.Bilinear(sourceData, imageWidth, imageHeight, (float)pos.X, (float)pos.Y);
                }
            });

            return managedImage;
        }

        static NGPUImage ProcessGpu(GraphicsDevice device, NImage image, ROI roi, float radianRate, Vector2 center)
        {
            var gpuImage = image.ToGpu(device);
            using var sourceImage = new NGPUImage(gpuImage.Width, gpuImage.Height, device);
            gpuImage.CopyTo(sourceImage);

            using var context = device.CreateComputeContext();
            context.For(roi.Width, roi.Height, new TwistProcess(gpuImage.Data, sourceImage.Data, gpuImage.Width, gpuImage.Height, radianRate, center, roi.Left, roi.Top));

            return gpuImage;
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct TwistProcess(ReadWriteBuffer<Float4> image, ReadWriteBuffer<Float4> originalImage, int width, int height, float radianRate, Float2 center, int startX, int startY) : IComputeShader
    {
        readonly Float2 ImageSize = new Float2(width, height);

        public void Execute()
        {
            var x = ThreadIds.X + startX;
            var y = ThreadIds.Y + startY;
            var pos = y * width + x;

            var pp = new Float2(x, y) - center;
            var r = Hlsl.Length(pp);
            Hlsl.SinCos(Hlsl.Atan2(pp.Y, pp.X) + radianRate * r, out var sin, out var cos);
            var sourcePos = new Float2(cos, sin) * r + center;

            image[pos] = OriginalImageBilinear(sourcePos.X, sourcePos.Y);
        }

        Float4 OriginalImageBilinear(float x, float y)
        {
            var ix = (int)Hlsl.Floor(x);
            var iy = (int)Hlsl.Floor(y);

            if (ix == x && iy == y)
            {
                if (ix > -1 && iy > -1 && ix < width && iy < height)
                {
                    return originalImage[iy * width + ix];
                }
                else
                {
                    return Const.EmptyPixelFloat4;
                }
            }
            else if (ix < -1 || iy < -1 || ix >= width || iy >= height)
            {
                return Const.EmptyPixelFloat4;
            }

            var pp = x - ix;
            var qq = y - iy;
            var ip = 1.0F - pp;
            var iq = 1.0F - qq;
            var mw = width - 1;
            var mh = height - 1;

            var c1 = Const.EmptyPixelFloat4;
            var c2 = Const.EmptyPixelFloat4;
            var c3 = Const.EmptyPixelFloat4;
            var c4 = Const.EmptyPixelFloat4;
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
                return Const.EmptyPixelFloat4;
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
