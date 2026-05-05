using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
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
using NiVE3.PresetPlugin.Internal;
using NiVE3.PresetPlugin.Internal.ComputeShader;
using NiVE3.PresetPlugin.Internal.Drawing;
using NiVE3.PresetPlugin.Resource;

namespace NiVE3.PresetPlugin.Effect.Channel
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.Channel_BlendLayer_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_Channel, LanguageResourceDictionary.Channel_BlendLayer_Description, ID, IsRenderEveryFrame = true, IsSupportGpu = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class BlendLayer : IEffect
    {
        const string ID = "55651CBB-D66C-46F2-9556-D46B90647FF3";

        const string PropertySourceLayerId = nameof(PropertySourceLayerId);

        const string PropertyBlendModeId = nameof(PropertyBlendModeId);

        const string PropertySourceOpacityId = nameof(PropertySourceOpacityId);

        const string PropertyIsKeepAlphaId = nameof(PropertyIsKeepAlphaId);

        const string PropertySourceLayerPositionId = nameof(PropertySourceLayerPositionId);

        IAcceleratorObject? AcceleratorObject { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            AcceleratorObject = accelerator;
        }

        public PropertyBase[] GetProperties(Int32Size sourceSize)
        {
            return
            [
                new UseLayerImageProperty(PropertySourceLayerId, LanguageResourceDictionary.ResourceKeys.Channel_BlendLayer_SourceLayer, selectBoxWidth: 90.0),
                new EnumProperty(PropertyBlendModeId, LanguageResourceDictionary.ResourceKeys.Channel_BlendLayer_BlendMode, typeof(BlendMode), typeof(LanguageResourceDictionary), BlendMode.Normal, selectBoxWidth: 90.0),
                new DoubleProperty(PropertySourceOpacityId, LanguageResourceDictionary.ResourceKeys.Channel_BlendLayer_SourceOpacity, 100.0, 0.0, 100.0, digit: 2),
                new CheckBoxProperty(PropertyIsKeepAlphaId, LanguageResourceDictionary.ResourceKeys.Channel_BlendLayer_IsKeepAlpha, false),
                new EnumProperty(PropertySourceLayerPositionId, LanguageResourceDictionary.ResourceKeys.Channel_BlendLayer_SourceLayerPosition, typeof(SourceLayerPositionType), typeof(LanguageResourceDictionary), SourceLayerPositionType.Center, selectBoxWidth: 90.0)
            ];
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, Time layerTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer, bool useGpu)
        {
            var targetLayerId = properties.GetValue(PropertySourceLayerId, layerTime, UseLayerImageTarget.Empty);
            var blendMode = properties.GetValue(PropertyBlendModeId, layerTime, BlendMode.Normal);
            var sourceOpacity = (float)properties.GetValue(PropertySourceOpacityId, layerTime, 0.0) * 0.01F;
            var isKeepAlpha = properties.GetValue(PropertyIsKeepAlphaId, layerTime, false);
            var sourceLayerPositionType = properties.GetValue(PropertySourceLayerPositionId, layerTime, SourceLayerPositionType.Center);

            var globalTime = layerTime + layer.SourceStartPoint;
            using var sourceImage = targetLayerId.GetImage(composition, globalTime, downSamplingRateX, useGpu);
            if (sourceImage == null)
            {
                return image;
            }

            if (useGpu && AcceleratorObject != null)
            {
                return ProcessGpu(AcceleratorObject.CurrentDevice, image, roi, sourceImage, blendMode, sourceOpacity, isKeepAlpha, sourceLayerPositionType);
            }
            else
            {
                return ProcessCpu(image, roi, sourceImage, blendMode, sourceOpacity, isKeepAlpha, sourceLayerPositionType);
            }
        }

        public float[] Process(float[] audio, Time startTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }

        static NManagedImage ProcessCpu(NImage imagge, ROI roi, NImage sourceImage, BlendMode blendMode, float sourceOpacity, bool isKeepAlpha, SourceLayerPositionType sourceLayerPositionType)
        {
            var managedImage = imagge.ToManaged();
            var managedSourceImage = sourceImage.ToManaged();

            var (sourceStartX, sourceStartY) = sourceLayerPositionType switch
            {
                SourceLayerPositionType.Stretch => (0.0F, 0.0F),
                _ => ((managedSourceImage.Width - managedImage.Width) * 0.5F, (managedSourceImage.Height - managedImage.Height) * 0.5F)
            };
            var (sourceDiffX, sourceDiffY) = sourceLayerPositionType switch
            {
                SourceLayerPositionType.Stretch => ((managedSourceImage.Width - 1) / (float)(managedImage.Width - 1), (managedSourceImage.Height - 1) / (float)(managedImage.Height - 1)),
                _ => (1.0F, 1.0F)
            };

            var interpolateEdgeMode = sourceLayerPositionType == SourceLayerPositionType.Loop ? BilinearEdgeMode.Repeat : BilinearEdgeMode.None;
            var imageData = managedImage.Data;
            var imageWidth = managedImage.Width;
            if (isKeepAlpha)
            {
                Parallel.For(roi.Top, roi.Bottom, y =>
                {
                    var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                    var sourceDataSpan = managedSourceImage.GetDataSpan();
                    var sourceX = sourceStartX + sourceDiffX * roi.Left;
                    var sourceY = sourceStartY + sourceDiffY * y;
                    for (var x = roi.Left; x < roi.Right; x++, sourceX += sourceDiffX)
                    {
                        var sourceColor = ImageInterpolation.Bilinear(sourceDataSpan, managedSourceImage.Width, managedSourceImage.Height, sourceX, sourceY, Const.EmptyPixel, interpolateEdgeMode);
                        sourceColor.W *= sourceOpacity;
                        var color = imageDataSpan[x];
                        var newColor = Blend.Process(blendMode, color, sourceColor);
                        newColor.W = color.W;
                        imageDataSpan[x] = newColor;
                    }
                });
            }
            else
            {
                Parallel.For(roi.Top, roi.Bottom, y =>
                {
                    var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                    var sourceDataSpan = managedSourceImage.GetDataSpan();
                    var sourceX = sourceStartX + sourceDiffX * roi.Left;
                    var sourceY = sourceStartY + sourceDiffY * y;
                    for (var x = roi.Left; x < roi.Right; x++, sourceX += sourceDiffX)
                    {
                        var sourceColor = ImageInterpolation.Bilinear(sourceDataSpan, managedSourceImage.Width, managedSourceImage.Height, sourceX, sourceY, Const.EmptyPixel, interpolateEdgeMode);
                        sourceColor.W *= sourceOpacity;
                        imageDataSpan[x] = Blend.Process(blendMode, imageDataSpan[x], sourceColor);
                    }
                });
            }

            if (managedSourceImage != sourceImage)
            {
                managedSourceImage.Dispose();
            }

            return managedImage;
        }

        static NGPUImage ProcessGpu(GraphicsDevice device, NImage image, ROI roi, NImage sourceImage, BlendMode blendMode, float sourceOpacity, bool isKeepAlpha, SourceLayerPositionType sourceLayerPositionType)
        {
            var gpuImage = image.ToGpu(device);
            var gpuSourceImage = sourceImage.ToGpu(device);

            var (sourceStartX, sourceStartY) = sourceLayerPositionType switch
            {
                SourceLayerPositionType.Stretch => (0.0F, 0.0F),
                _ => ((gpuSourceImage.Width - gpuImage.Width) * 0.5F, (gpuSourceImage.Height - gpuImage.Height) * 0.5F)
            };
            var (sourceDiffX, sourceDiffY) = sourceLayerPositionType switch
            {
                SourceLayerPositionType.Stretch => ((gpuSourceImage.Width - 1) / (float)(gpuImage.Width - 1), (gpuSourceImage.Height - 1) / (float)(gpuImage.Height - 1)),
                _ => (1.0F, 1.0F)
            };

            using (var context = device.CreateComputeContext())
            {
                context.For(
                    roi.Width,
                    roi.Height,
                    new BlendLayerProcess(
                        gpuImage.Data,
                        gpuImage.Width,
                        gpuImage.Height,
                        gpuSourceImage.Data,
                        gpuSourceImage.Width,
                        gpuSourceImage.Height,
                        (int)sourceLayerPositionType,
                        (int)blendMode,
                        sourceOpacity,
                        isKeepAlpha,
                        roi.Left,
                        roi.Top
                    )
                );
            }

            if (gpuSourceImage != sourceImage)
            {
                gpuSourceImage.Dispose();
            }

            return gpuImage;
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct BlendLayerProcess(ReadWriteBuffer<Float4> image, int width, int height, ReadWriteBuffer<Float4> sourceImage, int sourceImageWidth, int sourceImageHeight, int position, int blendMode, float sourceOpacity, bool isKeepAlpha, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            var sourceStartX = (sourceImageWidth - width) * 0.5F;
            var sourceStartY = (sourceImageHeight - height) * 0.5F;
            var sourceDiffX = 1.0F;
            var sourceDiffY = 1.0F;
            if (position == 1)
            {
                sourceStartX = 0.0F;
                sourceStartY = 0.0F;
                sourceDiffX = (sourceImageWidth - 1) / (float)(width - 1);
                sourceDiffY = (sourceImageHeight - 1) / (float)(height - 1);
            }

            var x = ThreadIds.X + startX;
            var y = ThreadIds.Y + startY;
            var sourceX = sourceStartX + sourceDiffX * x;
            var sourceY = sourceStartY + sourceDiffY * y;

            var sourceColor = Float4.Zero;
            if (position == 2)
            {
                sourceColor = SourceImageBilinearLoop(sourceX, sourceY);
            }
            else
            {
                sourceColor = SourceImageBilinear(sourceX, sourceY);
            }
            sourceColor.W *= sourceOpacity;

            var pos = y * width + x;
            if (isKeepAlpha)
            {
                var color = image[pos];
                var newColor = BlendMethods.Process(blendMode, color, sourceColor);
                newColor.W = color.W;
                image[pos] = newColor;
            }
            else
            {
                image[pos] = BlendMethods.Process(blendMode, image[pos], sourceColor);
            }
        }

        Float4 SourceImageBilinear(float x, float y)
        {
            var ix = (int)Hlsl.Floor(x);
            var iy = (int)Hlsl.Floor(y);

            if (ix == x && iy == y)
            {
                if (ix > -1 && iy > -1 && ix < sourceImageWidth && iy < sourceImageHeight)
                {
                    return sourceImage[iy * sourceImageWidth + ix];
                }
                else
                {
                    return 0.0F;
                }
            }
            else if (ix < -1 || iy < -1 || ix >= sourceImageWidth || iy >= sourceImageHeight)
            {
                return 0.0F;
            }

            var pp = x - ix;
            var qq = y - iy;
            var ip = 1.0F - pp;
            var iq = 1.0F - qq;
            var mw = sourceImageWidth - 1;
            var mh = sourceImageHeight - 1;

            Float4 c1;
            Float4 c2;
            Float4 c3;
            Float4 c4;
            var pos = iy * sourceImageWidth + ix;

            if (ix > -1)
            {
                if (ix < mw)
                {
                    if (iy > -1)
                    {
                        c1 = sourceImage[pos];
                        c2 = sourceImage[pos + 1];
                        if (iy < mh)
                        {
                            pos += sourceImageWidth;
                            c3 = sourceImage[pos];
                            c4 = sourceImage[pos + 1];
                        }
                        else
                        {
                            c3 = c1;
                            c4 = c2;
                        }
                    }
                    else
                    {
                        pos += sourceImageWidth;
                        c1 = sourceImage[pos];
                        c2 = sourceImage[pos + 1];
                        c3 = c1;
                        c4 = c2;
                    }
                }
                else
                {
                    if (iy > -1)
                    {
                        c1 = sourceImage[pos];
                        c2 = c1;
                        if (iy < mh)
                        {
                            c3 = sourceImage[pos + sourceImageWidth];
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
                        c3 = sourceImage[pos + sourceImageWidth];
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
                    c2 = sourceImage[pos];
                    c1 = c2;
                    if (iy < mh)
                    {
                        c4 = sourceImage[pos + sourceImageWidth];
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
                    c4 = sourceImage[pos + sourceImageWidth];
                    c1 = c4;
                    c2 = c4;
                    c3 = c4;
                }
            }

            var ta = Hlsl.Lerp(Hlsl.Lerp(c1, c3, qq), Hlsl.Lerp(c2, c4, qq), pp).W;
            if (ta <= 0.0F)
            {
                return 0.0F;
            }
            else
            {
                var t = Hlsl.Lerp(Hlsl.Lerp(c1 * c1.W, c3 * c3.W, qq), Hlsl.Lerp(c2 * c2.W, c4 * c4.W, qq), pp) / ta;
                t.W = ta;
                return t;
            }
        }

        Float4 SourceImageBilinearLoop(float x, float y)
        {
            var ix = (int)Hlsl.Floor(x);
            var iy = (int)Hlsl.Floor(y);

            if (ix == x && iy == y)
            {
                return sourceImage[CoordWrapGpu.Repeat(iy, sourceImageHeight) * sourceImageWidth + CoordWrapGpu.Repeat(ix, sourceImageWidth)];
            }

            var pp = x - ix;
            var qq = y - iy;

            var c1 = sourceImage[CoordWrapGpu.Repeat(iy, sourceImageHeight) * sourceImageWidth + CoordWrapGpu.Repeat(ix, sourceImageWidth)];
            var c2 = sourceImage[CoordWrapGpu.Repeat(iy, sourceImageHeight) * sourceImageWidth + CoordWrapGpu.Repeat(ix + 1, sourceImageWidth)];
            var c3 = sourceImage[CoordWrapGpu.Repeat(iy + 1, sourceImageHeight) * sourceImageWidth + CoordWrapGpu.Repeat(ix, sourceImageWidth)];
            var c4 = sourceImage[CoordWrapGpu.Repeat(iy + 1, sourceImageHeight) * sourceImageWidth + CoordWrapGpu.Repeat(ix + 1, sourceImageWidth)];

            var ta = Hlsl.Lerp(Hlsl.Lerp(c1, c3, qq), Hlsl.Lerp(c2, c4, qq), pp).W;
            if (ta <= 0.0F)
            {
                return 0.0F;
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
