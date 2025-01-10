using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using ComputeSharp;
using NiVE3.Image;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Property.Properties;
using NiVE3.Plugin.Resource;
using NiVE3.Plugin.ValueObject;
using NiVE3.PresetPlugin.Effect.Util;
using NiVE3.PresetPlugin.Extension;
using NiVE3.PresetPlugin.Internal;
using NiVE3.PresetPlugin.Internal.Drawing;
using NiVE3.PresetPlugin.Resource;

namespace NiVE3.PresetPlugin.Effect.Distortion
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.Distortion_GlassDistortion_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_Distortion, LanguageResourceDictionary.Distortion_GlassDistortion_Description, ID, IsSupportGpu = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class GlassDistortion : IEffect
    {
        const string ID = "766B9EEE-982B-4372-8F87-E7DD71597C6C";

        const string PropertySourceLayerId = nameof(PropertySourceLayerId);

        const string PropertyChannelId = nameof(PropertyChannelId);

        const string PropertySourceLayerPositionId = nameof(PropertySourceLayerPositionId);

        const string PropertyRateId = nameof(PropertyRateId);

        const string PropertyDisplacementAmountId = nameof(PropertyDisplacementAmountId);

        IAcceleratorObject? AcceleratorObject { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            AcceleratorObject = accelerator;
        }

        public PropertyBase[] GetProperties(Int32Size sourceSize)
        {
            return
            [
                new UseLayerImageProperty(PropertySourceLayerId, LanguageResourceDictionary.ResourceKeys.Distortion_GlassDistortion_SourceLayer, 90.0),
                new EnumProperty(PropertyChannelId, LanguageResourceDictionary.ResourceKeys.Distortion_GlassDistortion_Channel, typeof(DisplacemenMapChannelType), typeof(LanguageResourceDictionary), DisplacemenMapChannelType.Luminance, selectBoxWidth: 90.0),
                new EnumProperty(PropertySourceLayerPositionId, LanguageResourceDictionary.ResourceKeys.Distortion_GlassDistortion_SourceLayerPosition, typeof(SourceLayerPositionType), typeof(LanguageResourceDictionary), SourceLayerPositionType.Center, selectBoxWidth: 90.0),
                new DoubleProperty(PropertyRateId, LanguageResourceDictionary.ResourceKeys.Distortion_GlassDistortion_Rate, 50.0, -100.0, 100.0, digit: 2),
                new DoubleProperty(PropertyDisplacementAmountId, LanguageResourceDictionary.ResourceKeys.Distortion_GlassDistortion_DisplacementAmount, 100.0, -1000.0, 1000.0, digit: 2)
            ];
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, Time layerTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer, bool useGpu)
        {
            var targetLayerId = properties.GetValue(PropertySourceLayerId, layerTime, UseLayerImageTarget.Empty);
            var channel = properties.GetValue(PropertyChannelId, layerTime, DisplacemenMapChannelType.Luminance);
            var position = properties.GetValue(PropertySourceLayerPositionId, layerTime, SourceLayerPositionType.Center);
            var rate = (float)properties.GetValue(PropertyRateId, layerTime, 0.0) * 0.01F;
            var displacement = (float)properties.GetValue(PropertyDisplacementAmountId, layerTime, 0.0);

            var targetLayer = targetLayerId != UseLayerImageTarget.Empty ? composition.GetLayer(targetLayerId.LayerId) : null;
            if (targetLayer == null || rate == 0.0 || displacement == 0.0)
            {
                return image;
            }

            using var sourceImage = targetLayerId.ImageProcessType switch
            {
                LayerImageProcessType.Effected => targetLayer.GetEffectedImage(layerTime, downSamplingRateX, useGpu),
                _ => targetLayer.GetRawImage(layerTime, downSamplingRateX, useGpu)
            };

            if (useGpu && AcceleratorObject != null)
            {
                return ProcessGpu(AcceleratorObject.CurrentDevice, image, roi, sourceImage, rate, displacement, channel, position);
            }
            else
            {
                return ProcessCpu(image, roi, sourceImage, rate, displacement, channel, position);
            }
        }

        public float[] Process(float[] audio, Time startTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }

        static NManagedImage ProcessCpu(NImage image, ROI roi, NImage sourceImage, float rate, float displacementAmount, DisplacemenMapChannelType channel, SourceLayerPositionType position)
        {
            var managedImage = image.ToManaged();
            var managedSourceImage = sourceImage.ToManaged();

            using var originalImage = (NManagedImage)managedImage.Copy();

            var map = DisplacementMapGenerator.Generate(managedSourceImage, managedImage.Width, managedImage.Height, channel, position);

            Parallel.For(roi.Top, roi.Bottom, y =>
            {
                var width = managedImage.Width;
                var originalImageDataSpan = originalImage.GetDataSpan();
                var imageDataLineSpan = managedImage.Data.AsSpan(y * width, width);
                var centerSourceLineSpan = map.AsSpan(y * width, width);
                var topSourceLineSpan = map.AsSpan((y > 0 ? (y - 1) : y) * width, width);
                var bottomSourceLineSpan = map.AsSpan((y <= managedImage.Height - 2 ? y + 1 : managedImage.Height - 1) * width, width);
                for (var x = roi.Left; x < roi.Right; x++)
                {
                    var displacementVec = Vector2.Zero;
                    var center = centerSourceLineSpan[x];
                    var isLeftEdge = x < 1;
                    var isRightEdge = x >= width - 1;

                    displacementVec += new Vector2(1.0F, 0.0F) * (centerSourceLineSpan[x + (isRightEdge ? 0 : 1)] - center);
                    displacementVec += new Vector2(1.0F, 1.0F) * (bottomSourceLineSpan[x + (isRightEdge ? 0 : 1)] - center);
                    displacementVec += new Vector2(0.0F, 1.0F) * (bottomSourceLineSpan[x] - center);
                    displacementVec += new Vector2(-1.0F, 1.0F) * (bottomSourceLineSpan[x - (isLeftEdge ? 0 : 1)] - center);
                    displacementVec += new Vector2(-1.0F, 0.0F) * (centerSourceLineSpan[x - (isLeftEdge ? 0 : 1)] - center);
                    displacementVec += new Vector2(-1.0F, -1.0F) * (topSourceLineSpan[x - (isLeftEdge ? 0 : 1)] - center);
                    displacementVec += new Vector2(0.0F, -1.0F) * (topSourceLineSpan[x] - center);
                    displacementVec += new Vector2(1.0F, -1.0F) * (topSourceLineSpan[x + (isRightEdge ? 0 : 1)] - center);

                    displacementVec *= rate * displacementAmount;
                    imageDataLineSpan[x] = BlurryDisplacementColor(x + displacementVec.X, y + displacementVec.Y, originalImageDataSpan, managedImage.Width, managedImage.Height, displacementVec.Length());
                }
            });

            if (managedSourceImage != sourceImage)
            {
                managedSourceImage.Dispose();
            }

            ArrayPool<float>.Shared.Return(map);

            return managedImage;
        }

        static NGPUImage ProcessGpu(GraphicsDevice device, NImage image, ROI roi, NImage sourceImage, float rate, float displacementAmount, DisplacemenMapChannelType channel, SourceLayerPositionType position)
        {
            var gpuImage = image.ToGpu(device);
            var gpuSourceImage = sourceImage.ToGpu(device);

            using var originalImage = new NGPUImage(gpuImage.Width, gpuImage.Height, device);
            gpuImage.CopyTo(originalImage);

            using var map = DisplacementMapGenerator.Generate(device, gpuSourceImage, gpuImage.Width, gpuImage.Height, channel, position);

            using (var context = device.CreateComputeContext())
            {
                context.For(roi.Width, roi.Height, new GlassDistortionProcess(gpuImage.Data, originalImage.Data, gpuImage.Width, gpuImage.Height, map, rate, displacementAmount, roi.Left, roi.Top));
            }

            if (gpuSourceImage != sourceImage)
            {
                gpuSourceImage.Dispose();
            }

            return gpuImage;
        }

        static Vector4 BlurryDisplacementColor(float x, float y, ReadOnlySpan<Vector4> originalImageDataSpan, int width, int height, float distance)
        {
            // NOTE: sqrt((1000 ** 2) * 2) で 5x5とする
            var blurAmount = distance / 1414.213562373095F * 2.0F;
            var range = (int)MathF.Ceiling(blurAmount);
            var fullValueRange = (int)MathF.Floor(blurAmount);
            var edgeAmount = blurAmount - fullValueRange;

            var color = Vector4.Zero;
            var a = 0.0F;

            for (var by = -range; by <= range; by++)
            {
                var yr = Math.Abs(by) <= fullValueRange ? 1.0F : edgeAmount;

                for (var bx = -range; bx <= range; bx++)
                {
                    var c = ImageInterpolation.Bilinear(originalImageDataSpan, width, height, CoordWrap.Mirror(x + bx, width), CoordWrap.Mirror(y + by, height));
                    var ta = c.W * yr * (Math.Abs(bx) <= fullValueRange ? 1.0F : edgeAmount);
                    color += c * ta;
                    a += ta;
                }
            }

            if (a > 0.0F)
            {
                var count = blurAmount * 2.0F + 1.0F;
                var result = color / a;
                result.W = a / (count * count);
                return result;
            }
            else
            {
                return new Vector4(1.0F, 1.0F, 1.0F, 0.0F);
            }
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct GlassDistortionProcess(ReadWriteBuffer<Float4> image, ReadWriteBuffer<Float4> originalImage, int width, int height, ReadWriteBuffer<float> displacementMap, float rate, float displacementAmount, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            var x = ThreadIds.X + startX;
            var y = ThreadIds.Y + startY;
            var pos = y * width + x;

            var centerLine = y * width + x;
            var topLine = centerLine - (y > 0 ? width : 0);
            var bottomLine = centerLine + (y < height - 1 ? width : 0);
            var isLeftEdge = x < 1;
            var isRightEdge = x >= width - 1;

            var displacementVec = Float2.Zero;
            var center = displacementMap[pos];

            displacementVec += new Float2(1.0F, 0.0F) * (displacementMap[centerLine + (isRightEdge ? 0 : 1)] - center);
            displacementVec += new Float2(1.0F, 1.0F) * (displacementMap[bottomLine + (isRightEdge ? 0 : 1)] - center);
            displacementVec += new Float2(0.0F, 1.0F) * (displacementMap[bottomLine] - center);
            displacementVec += new Float2(-1.0F, 1.0F) * (displacementMap[bottomLine - (isLeftEdge ? 0 : 1)] - center);
            displacementVec += new Float2(-1.0F, 0.0F) * (displacementMap[centerLine - (isLeftEdge ? 0 : 1)] - center);
            displacementVec += new Float2(-1.0F, -1.0F) * (displacementMap[topLine - (isLeftEdge ? 0 : 1)] - center);
            displacementVec += new Float2(0.0F, -1.0F) * (displacementMap[topLine] - center);
            displacementVec += new Float2(1.0F, -1.0F) * (displacementMap[topLine + (isRightEdge ? 0 : 1)] - center);

            displacementVec *= rate * displacementAmount;
            image[pos] = BlurryDisplacementColor(displacementVec + new Float2(x, y), Hlsl.Length(displacementVec));
        }

        Float4 BlurryDisplacementColor(Float2 pos, float distance)
        {
            // NOTE: sqrt((1000 ** 2) * 2) で 5x5とする
            var blurAmount = distance / 1414.213562373095F * 2.0F;
            var edgeAmount = Hlsl.Frac(blurAmount);
            var range = (int)Hlsl.Ceil(blurAmount);
            var fullValueRange = (int)Hlsl.Floor(blurAmount);

            var color = Float3.Zero;
            var a = 0.0F;

            for (var by = -range; by <= range; by++)
            {
                var yr = Hlsl.Abs(by) <= fullValueRange ? 1.0F : edgeAmount;

                for (var bx = -range; bx <= range; bx++)
                {
                    var c = OriginalImageBilinear(CoordWrapGpu.Mirror(pos.X + bx, width), CoordWrapGpu.Mirror(pos.Y + by, height));
                    var ta = c.W * yr * (Hlsl.Abs(bx) <= fullValueRange ? 1.0F : edgeAmount);
                    color += c.XYZ * ta;
                    a += ta;
                }
            }

            if (a > 0.0F)
            {
                var count = blurAmount * 2.0F + 1.0F;
                return new Float4(color / a, a / (count * count));
            }
            else
            {
                return Const.EmptyPixelFloat4;
            }
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
                    return 0.5F;
                }
            }
            else if (ix < -1 || iy < -1 || ix >= width || iy >= height)
            {
                return 0.5F;
            }

            var pp = x - ix;
            var qq = y - iy;
            var ip = 1.0F - pp;
            var iq = 1.0F - qq;
            var mw = width - 1;
            var mh = height - 1;

            Float4 c1;
            Float4 c2;
            Float4 c3;
            Float4 c4;
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
                        else
                        {
                            c3 = c1;
                            c4 = c2;
                        }
                    }
                    else
                    {
                        pos += width;
                        c1 = originalImage[pos];
                        c2 = originalImage[pos + 1];
                        c3 = c1;
                        c4 = c2;
                    }
                }
                else
                {
                    if (iy > -1)
                    {
                        c1 = originalImage[pos];
                        c2 = c1;
                        if (iy < mh)
                        {
                            c3 = originalImage[pos + width];
                            c4 = c3;
                        }
                        else
                        {
                            c3 = c1;
                            c4 = c1;
                        }
                    }
                    else
                    {
                        c3 = originalImage[pos + width];
                        c1 = c3;
                        c2 = c3;
                        c4 = c3;
                    }
                }
            }
            else
            {
                pos++;
                if (iy > -1)
                {
                    c2 = originalImage[pos];
                    c1 = c2;
                    if (iy < mh)
                    {
                        c4 = originalImage[pos + width];
                        c3 = c4;
                    }
                    else
                    {
                        c3 = c2;
                        c4 = c2;
                    }
                }
                else
                {
                    c4 = originalImage[pos + width];
                    c1 = c4;
                    c2 = c4;
                    c3 = c4;
                }
            }

            var ta = Hlsl.Lerp(Hlsl.Lerp(c1, c3, qq), Hlsl.Lerp(c2, c4, qq), pp).W;
            if (ta <= 0.0F)
            {
                return 0.5F;
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
