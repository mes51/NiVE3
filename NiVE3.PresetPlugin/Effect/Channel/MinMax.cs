using System;
using System.Buffers;
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
using NiVE3.PresetPlugin.Effect.Util;
using NiVE3.PresetPlugin.Extension;
using NiVE3.PresetPlugin.Resource;

namespace NiVE3.PresetPlugin.Effect.Channel
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.Channel_MinMax_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_Channel, LanguageResourceDictionary.Channel_MinMax_Description, ID, IsSupportGpu = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class MinMax : IEffect
    {
        const string ID = "46270A31-FD0B-4513-95E4-3809C439B6F3";

        const string PropertyModeId = nameof(PropertyModeId);

        const string PropertyChannelId = nameof(PropertyChannelId);

        const string PropertyRadiusId = nameof(PropertyRadiusId);

        IAcceleratorObject? AcceleratorObject { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            AcceleratorObject = accelerator;
        }

        public PropertyBase[] GetProperties(Int32Size sourceSize)
        {
            return
            [
                new EnumProperty(PropertyModeId, LanguageResourceDictionary.ResourceKeys.Channel_MinMax_Mode, typeof(MinMaxMode), typeof(LanguageResourceDictionary), MinMaxMode.Min, selectBoxWidth: 90.0),
                new EnumProperty(PropertyChannelId, LanguageResourceDictionary.ResourceKeys.Channel_MinMax_Channel, typeof(ChannelType), typeof(LanguageResourceDictionary), ChannelType.RGB, selectBoxWidth: 90.0),
                new DoubleProperty(PropertyRadiusId, LanguageResourceDictionary.ResourceKeys.Channel_MinMax_Radius, 5.0, 0.0, 1000.0, digit: 0)
            ];
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, Time layerTime, IPropertyObject[] properties, ICompositionObject composition, bool useGpu)
        {
            var mode = properties.GetValue(PropertyModeId, layerTime, MinMaxMode.Min);
            var channel = properties.GetValue(PropertyChannelId, layerTime, ChannelType.RGB);
            var radius = (int)properties.GetValue(PropertyRadiusId, layerTime, 0.0);

            if (radius < 1)
            {
                return image;
            }

            if (useGpu && AcceleratorObject != null)
            {
                switch (mode)
                {
                    case MinMaxMode.Max:
                        return ProcessMaxGpu(AcceleratorObject.CurrentDevice, image, roi, radius, channel);
                    default:
                        return ProcessMinGpu(AcceleratorObject.CurrentDevice, image, roi, radius, channel);
                }
            }
            else
            {
                switch (mode)
                {
                    case MinMaxMode.Max:
                        return ProcessMaxCpu(image, roi, radius, channel);
                    default:
                        return ProcessMinCpu(image, roi, radius, channel);
                }
            }
        }

        public float[] Process(float[] audio, Time startTime, IPropertyObject[] properties, ICompositionObject composition)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }

        static NManagedImage ProcessMinCpu(NImage image, ROI roi, int radius, ChannelType channel)
        {
            var managedImage = image.ToManaged();

            var imageWidth = managedImage.Width;
            var imageHeight = managedImage.Height;
            var imageData = managedImage.Data;
            var temp = ArrayPool<Vector4>.Shared.Rent(managedImage.DataLength);

            var width = Math.Min(roi.Right + radius, imageWidth);
            var x = Math.Max(roi.Left - radius, 0);

            switch (channel)
            {
                case ChannelType.RGB:
                    Parallel.For(Math.Max(roi.Top - radius, 0), Math.Min(roi.Bottom + radius, imageHeight), h =>
                    {
                        var imageDataSpan = imageData.AsSpan(h * imageWidth, imageWidth);

                        for (var w = x; w < width; w++)
                        {
                            var color = imageDataSpan[w];
                            var a = color.W;

                            for (int l = Math.Max(w - radius, 0), limit = Math.Min(w + radius, imageWidth); l < limit; l++)
                            {
                                color = Vector4.Min(color, imageDataSpan[l]);
                            }

                            color.W = a;
                            temp[w * imageHeight + h] = color;
                        }
                    });
                    Parallel.For(roi.Left, roi.Right, w =>
                    {
                        var tempSpan = temp.AsSpan(w * imageHeight, imageHeight);

                        for (var h = roi.Top; h < roi.Bottom; h++)
                        {
                            var color = tempSpan[h];
                            var a = color.W;

                            for (int t = Math.Max(h - radius, 0), limit = Math.Min(h + radius, imageHeight); t < limit; t++)
                            {
                                color = Vector4.Min(color, tempSpan[t]);
                            }

                            color.W = a;
                            imageData[h * imageWidth + w] = color;
                        }
                    });
                    break;
                case ChannelType.R:
                    Parallel.For(Math.Max(roi.Top - radius, 0), Math.Min(roi.Bottom + radius, imageHeight), h =>
                    {
                        var imageDataSpan = imageData.AsSpan(h * imageWidth, imageWidth);

                        for (var w = x; w < width; w++)
                        {
                            var color = imageDataSpan[w];
                            var r = color.Z;

                            for (int l = Math.Max(w - radius, 0), limit = Math.Min(w + radius, imageWidth); l < limit; l++)
                            {
                                r = Math.Min(r, imageDataSpan[l].Z);
                            }

                            color.Z = r;
                            temp[w * imageHeight + h] = color;
                        }
                    });
                    Parallel.For(roi.Left, roi.Right, w =>
                    {
                        var tempSpan = temp.AsSpan(w * imageHeight, imageHeight);

                        for (var h = roi.Top; h < roi.Bottom; h++)
                        {
                            var color = tempSpan[h];
                            var r = color.Z;

                            for (int t = Math.Max(h - radius, 0), limit = Math.Min(h + radius, imageHeight); t < limit; t++)
                            {
                                r = Math.Min(r, tempSpan[t].Z);
                            }

                            color.Z = r;
                            imageData[h * imageWidth + w] = color;
                        }
                    });
                    break;
                case ChannelType.G:
                    Parallel.For(Math.Max(roi.Top - radius, 0), Math.Min(roi.Bottom + radius, imageHeight), h =>
                    {
                        var imageDataSpan = imageData.AsSpan(h * imageWidth, imageWidth);

                        for (var w = x; w < width; w++)
                        {
                            var color = imageDataSpan[w];
                            var g = color.Y;

                            for (int l = Math.Max(w - radius, 0), limit = Math.Min(w + radius, imageWidth); l < limit; l++)
                            {
                                g = Math.Min(g, imageDataSpan[l].Y);
                            }

                            color.Y = g;
                            temp[w * imageHeight + h] = color;
                        }
                    });
                    Parallel.For(roi.Left, roi.Right, w =>
                    {
                        var tempSpan = temp.AsSpan(w * imageHeight, imageHeight);

                        for (var h = roi.Top; h < roi.Bottom; h++)
                        {
                            var color = tempSpan[h];
                            var g = color.Y;

                            for (int t = Math.Max(h - radius, 0), limit = Math.Min(h + radius, imageHeight); t < limit; t++)
                            {
                                g = Math.Min(g, tempSpan[t].Y);
                            }

                            color.Y = g;
                            imageData[h * imageWidth + w] = color;
                        }
                    });
                    break;
                case ChannelType.B:
                    Parallel.For(Math.Max(roi.Top - radius, 0), Math.Min(roi.Bottom + radius, imageHeight), h =>
                    {
                        var imageDataSpan = imageData.AsSpan(h * imageWidth, imageWidth);

                        for (var w = x; w < width; w++)
                        {
                            var color = imageDataSpan[w];
                            var b = color.X;

                            for (int l = Math.Max(w - radius, 0), limit = Math.Min(w + radius, imageWidth); l < limit; l++)
                            {
                                b = Math.Min(b, imageDataSpan[l].X);
                            }

                            color.X = b;
                            temp[w * imageHeight + h] = color;
                        }
                    });
                    Parallel.For(roi.Left, roi.Right, w =>
                    {
                        var tempSpan = temp.AsSpan(w * imageHeight, imageHeight);

                        for (var h = roi.Top; h < roi.Bottom; h++)
                        {
                            var color = tempSpan[h];
                            var b = color.X;

                            for (int t = Math.Max(h - radius, 0), limit = Math.Min(h + radius, imageHeight); t < limit; t++)
                            {
                                b = Math.Min(b, tempSpan[t].X);
                            }

                            color.X = b;
                            imageData[h * imageWidth + w] = color;
                        }
                    });
                    break;
                case ChannelType.A:
                    Parallel.For(Math.Max(roi.Top - radius, 0), Math.Min(roi.Bottom + radius, imageHeight), h =>
                    {
                        var imageDataSpan = imageData.AsSpan(h * imageWidth, imageWidth);

                        for (var w = x; w < width; w++)
                        {
                            var color = imageDataSpan[w];
                            var a = color.W;

                            for (int l = Math.Max(w - radius, 0), limit = Math.Min(w + radius, imageWidth); l < limit; l++)
                            {
                                a = Math.Min(a, imageDataSpan[l].W);
                            }

                            color.W = a;
                            temp[w * imageHeight + h] = color;
                        }
                    });
                    Parallel.For(roi.Left, roi.Right, w =>
                    {
                        var tempSpan = temp.AsSpan(w * imageHeight, imageHeight);

                        for (var h = roi.Top; h < roi.Bottom; h++)
                        {
                            var color = tempSpan[h];
                            var a = color.W;

                            for (int t = Math.Max(h - radius, 0), limit = Math.Min(h + radius, imageHeight); t < limit; t++)
                            {
                                a = Math.Min(a, tempSpan[t].W);
                            }

                            color.W = a;
                            imageData[h * imageWidth + w] = color;
                        }
                    });
                    break;
            }

            ArrayPool<Vector4>.Shared.Return(temp);

            return managedImage;
        }

        static NManagedImage ProcessMaxCpu(NImage image, ROI roi, int radius, ChannelType channel)
        {
            var managedImage = image.ToManaged();

            var imageWidth = managedImage.Width;
            var imageHeight = managedImage.Height;
            var imageData = managedImage.Data;
            var temp = ArrayPool<Vector4>.Shared.Rent(managedImage.DataLength);

            var width = Math.Min(roi.Right + radius, imageWidth);
            var x = Math.Max(roi.Left - radius, 0);

            switch (channel)
            {
                case ChannelType.RGB:
                    Parallel.For(Math.Max(roi.Top - radius, 0), Math.Min(roi.Bottom + radius, imageHeight), h =>
                    {
                        var imageDataSpan = imageData.AsSpan(h * imageWidth, imageWidth);

                        for (var w = x; w < width; w++)
                        {
                            var color = imageDataSpan[w];
                            var a = color.W;

                            for (int l = Math.Max(w - radius, 0), limit = Math.Min(w + radius, imageWidth); l < limit; l++)
                            {
                                color = Vector4.Max(color, imageDataSpan[l]);
                            }

                            color.W = a;
                            temp[w * imageHeight + h] = color;
                        }
                    });
                    Parallel.For(roi.Left, roi.Right, w =>
                    {
                        var tempSpan = temp.AsSpan(w * imageHeight, imageHeight);

                        for (var h = roi.Top; h < roi.Bottom; h++)
                        {
                            var color = tempSpan[h];
                            var a = color.W;

                            for (int t = Math.Max(h - radius, 0), limit = Math.Min(h + radius, imageHeight); t < limit; t++)
                            {
                                color = Vector4.Max(color, tempSpan[t]);
                            }

                            color.W = a;
                            imageData[h * imageWidth + w] = color;
                        }
                    });
                    break;
                case ChannelType.R:
                    Parallel.For(Math.Max(roi.Top - radius, 0), Math.Min(roi.Bottom + radius, imageHeight), h =>
                    {
                        var imageDataSpan = imageData.AsSpan(h * imageWidth, imageWidth);

                        for (var w = x; w < width; w++)
                        {
                            var color = imageDataSpan[w];
                            var r = color.Z;

                            for (int l = Math.Max(w - radius, 0), limit = Math.Min(w + radius, imageWidth); l < limit; l++)
                            {
                                r = Math.Max(r, imageDataSpan[l].Z);
                            }

                            color.Z = r;
                            temp[w * imageHeight + h] = color;
                        }
                    });
                    Parallel.For(roi.Left, roi.Right, w =>
                    {
                        var tempSpan = temp.AsSpan(w * imageHeight, imageHeight);

                        for (var h = roi.Top; h < roi.Bottom; h++)
                        {
                            var color = tempSpan[h];
                            var r = color.Z;

                            for (int t = Math.Max(h - radius, 0), limit = Math.Min(h + radius, imageHeight); t < limit; t++)
                            {
                                r = Math.Max(r, tempSpan[t].Z);
                            }

                            color.Z = r;
                            imageData[h * imageWidth + w] = color;
                        }
                    });
                    break;
                case ChannelType.G:
                    Parallel.For(Math.Max(roi.Top - radius, 0), Math.Min(roi.Bottom + radius, imageHeight), h =>
                    {
                        var imageDataSpan = imageData.AsSpan(h * imageWidth, imageWidth);

                        for (var w = x; w < width; w++)
                        {
                            var color = imageDataSpan[w];
                            var g = color.Y;

                            for (int l = Math.Max(w - radius, 0), limit = Math.Min(w + radius, imageWidth); l < limit; l++)
                            {
                                g = Math.Max(g, imageDataSpan[l].Y);
                            }

                            color.Y = g;
                            temp[w * imageHeight + h] = color;
                        }
                    });
                    Parallel.For(roi.Left, roi.Right, w =>
                    {
                        var tempSpan = temp.AsSpan(w * imageHeight, imageHeight);

                        for (var h = roi.Top; h < roi.Bottom; h++)
                        {
                            var color = tempSpan[h];
                            var g = color.Y;

                            for (int t = Math.Max(h - radius, 0), limit = Math.Min(h + radius, imageHeight); t < limit; t++)
                            {
                                g = Math.Max(g, tempSpan[t].Y);
                            }

                            color.Y = g;
                            imageData[h * imageWidth + w] = color;
                        }
                    });
                    break;
                case ChannelType.B:
                    Parallel.For(Math.Max(roi.Top - radius, 0), Math.Min(roi.Bottom + radius, imageHeight), h =>
                    {
                        var imageDataSpan = imageData.AsSpan(h * imageWidth, imageWidth);

                        for (var w = x; w < width; w++)
                        {
                            var color = imageDataSpan[w];
                            var b = color.X;

                            for (int l = Math.Max(w - radius, 0), limit = Math.Min(w + radius, imageWidth); l < limit; l++)
                            {
                                b = Math.Max(b, imageDataSpan[l].X);
                            }

                            color.X = b;
                            temp[w * imageHeight + h] = color;
                        }
                    });
                    Parallel.For(roi.Left, roi.Right, w =>
                    {
                        var tempSpan = temp.AsSpan(w * imageHeight, imageHeight);

                        for (var h = roi.Top; h < roi.Bottom; h++)
                        {
                            var color = tempSpan[h];
                            var b = color.X;

                            for (int t = Math.Max(h - radius, 0), limit = Math.Min(h + radius, imageHeight); t < limit; t++)
                            {
                                b = Math.Max(b, tempSpan[t].X);
                            }

                            color.X = b;
                            imageData[h * imageWidth + w] = color;
                        }
                    });
                    break;
                case ChannelType.A:
                    Parallel.For(Math.Max(roi.Top - radius, 0), Math.Min(roi.Bottom + radius, imageHeight), h =>
                    {
                        var imageDataSpan = imageData.AsSpan(h * imageWidth, imageWidth);

                        for (var w = x; w < width; w++)
                        {
                            var color = imageDataSpan[w];
                            var a = color.W;

                            for (int l = Math.Max(w - radius, 0), limit = Math.Min(w + radius, imageWidth); l < limit; l++)
                            {
                                a = Math.Max(a, imageDataSpan[l].W);
                            }

                            color.W = a;
                            temp[w * imageHeight + h] = color;
                        }
                    });
                    Parallel.For(roi.Left, roi.Right, w =>
                    {
                        var tempSpan = temp.AsSpan(w * imageHeight, imageHeight);

                        for (var h = roi.Top; h < roi.Bottom; h++)
                        {
                            var color = tempSpan[h];
                            var a = color.W;

                            for (int t = Math.Max(h - radius, 0), limit = Math.Min(h + radius, imageHeight); t < limit; t++)
                            {
                                a = Math.Max(a, tempSpan[t].W);
                            }

                            color.W = a;
                            imageData[h * imageWidth + w] = color;
                        }
                    });
                    break;
            }

            ArrayPool<Vector4>.Shared.Return(temp);

            return managedImage;
        }

        static NGPUImage ProcessMinGpu(GraphicsDevice device, NImage image, ROI roi, int radius, ChannelType channel)
        {
            var gpuImage = image.ToGpu(device);

            var top = Math.Max(roi.Top - radius, 0);
            using var temp = device.AllocateReadWriteBuffer<Float4>(gpuImage.DataLength);
            using var context = device.CreateComputeContext();
            context.For(roi.Width, Math.Min(roi.Bottom + radius, gpuImage.Height) - top, new MinMaxHorizontalMinProcess(temp, gpuImage.Data, gpuImage.Width, radius, (int)channel, roi.Left, top));
            context.Barrier(temp);
            context.For(roi.Width, roi.Height, new MinMaxVerticalMinProcess(gpuImage.Data, temp, gpuImage.Width, gpuImage.Height, radius, (int)channel, roi.Left, roi.Top));

            return gpuImage;
        }

        static NGPUImage ProcessMaxGpu(GraphicsDevice device, NImage image, ROI roi, int radius, ChannelType channel)
        {
            var gpuImage = image.ToGpu(device);

            var top = Math.Max(roi.Top - radius, 0);
            using var temp = device.AllocateReadWriteBuffer<Float4>(gpuImage.DataLength);
            using var context = device.CreateComputeContext();
            context.For(roi.Width, Math.Min(roi.Bottom + radius, gpuImage.Height) - top, new MinMaxHorizontalMaxProcess(temp, gpuImage.Data, gpuImage.Width, radius, (int)channel, roi.Left, top));
            context.Barrier(temp);
            context.For(roi.Width, roi.Height, new MinMaxVerticalMaxProcess(gpuImage.Data, temp, gpuImage.Width, gpuImage.Height, radius, (int)channel, roi.Left, roi.Top));

            return gpuImage;
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct MinMaxHorizontalMinProcess(ReadWriteBuffer<Float4> result, ReadWriteBuffer<Float4> image, int width, int radius, int channel, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            var x = ThreadIds.X + startX;
            var y = ThreadIds.Y + startY;
            var pos = y * width + x;
            var line = y * width;
            var color = image[pos];

            switch (channel)
            {
                case 0: // RGB
                    for (var w = -radius; w < radius; w++)
                    {
                        color.XYZ = Hlsl.Min(color.XYZ, image[line + CoordWrapGpu.Wrap(x + w, width)].XYZ);
                    }
                    break;
                case 1: // R
                    for (var w = -radius; w < radius; w++)
                    {
                        color.Z = Hlsl.Min(color.Z, image[line + CoordWrapGpu.Wrap(x + w, width)].Z);
                    }
                    break;
                case 2: // G
                    for (var w = -radius; w < radius; w++)
                    {
                        color.Y = Hlsl.Min(color.Y, image[line + CoordWrapGpu.Wrap(x + w, width)].Y);
                    }
                    break;
                case 3: // B
                    for (var w = -radius; w < radius; w++)
                    {
                        color.X = Hlsl.Min(color.X, image[line + CoordWrapGpu.Wrap(x + w, width)].X);
                    }
                    break;
                case 4: // A
                    for (var w = -radius; w < radius; w++)
                    {
                        color.W = Hlsl.Min(color.W, image[line + CoordWrapGpu.Wrap(x + w, width)].W);
                    }
                    break;
            }

            result[pos] = color;
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct MinMaxVerticalMinProcess(ReadWriteBuffer<Float4> result, ReadWriteBuffer<Float4> image, int width, int height, int radius, int channel, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            var x = ThreadIds.X + startX;
            var y = ThreadIds.Y + startY;
            var pos = y * width + x;
            var color = image[pos];

            switch (channel)
            {
                case 0: // RGB
                    for (var h = -radius; h < radius; h++)
                    {
                        color.XYZ = Hlsl.Min(color.XYZ, image[CoordWrapGpu.Wrap(y + h, height) * width + x].XYZ);
                    }
                    break;
                case 1: // R
                    for (var h = -radius; h < radius; h++)
                    {
                        color.Z = Hlsl.Min(color.Z, image[CoordWrapGpu.Wrap(y + h, height) * width + x].Z);
                    }
                    break;
                case 2: // G
                    for (var h = -radius; h < radius; h++)
                    {
                        color.Y = Hlsl.Min(color.Y, image[CoordWrapGpu.Wrap(y + h, height) * width + x].Y);
                    }
                    break;
                case 3: // B
                    for (var h = -radius; h < radius; h++)
                    {
                        color.X = Hlsl.Min(color.X, image[CoordWrapGpu.Wrap(y + h, height) * width + x].X);
                    }
                    break;
                case 4: // A
                    for (var h = -radius; h < radius; h++)
                    {
                        color.W = Hlsl.Min(color.W, image[CoordWrapGpu.Wrap(y + h, height) * width + x].W);
                    }
                    break;
            }

            result[pos] = color;
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct MinMaxHorizontalMaxProcess(ReadWriteBuffer<Float4> result, ReadWriteBuffer<Float4> image, int width, int radius, int channel, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            var x = ThreadIds.X + startX;
            var y = ThreadIds.Y + startY;
            var pos = y * width + x;
            var line = y * width;
            var color = image[pos];

            switch (channel)
            {
                case 0: // RGB
                    for (var w = -radius; w < radius; w++)
                    {
                        color.XYZ = Hlsl.Max(color.XYZ, image[line + CoordWrapGpu.Wrap(x + w, width)].XYZ);
                    }
                    break;
                case 1: // R
                    for (var w = -radius; w < radius; w++)
                    {
                        color.Z = Hlsl.Max(color.Z, image[line + CoordWrapGpu.Wrap(x + w, width)].Z);
                    }
                    break;
                case 2: // G
                    for (var w = -radius; w < radius; w++)
                    {
                        color.Y = Hlsl.Max(color.Y, image[line + CoordWrapGpu.Wrap(x + w, width)].Y);
                    }
                    break;
                case 3: // B
                    for (var w = -radius; w < radius; w++)
                    {
                        color.X = Hlsl.Max(color.X, image[line + CoordWrapGpu.Wrap(x + w, width)].X);
                    }
                    break;
                case 4: // A
                    for (var w = -radius; w < radius; w++)
                    {
                        color.W = Hlsl.Max(color.W, image[line + CoordWrapGpu.Wrap(x + w, width)].W);
                    }
                    break;
            }

            result[pos] = color;
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct MinMaxVerticalMaxProcess(ReadWriteBuffer<Float4> result, ReadWriteBuffer<Float4> image, int width, int height, int radius, int channel, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            var x = ThreadIds.X + startX;
            var y = ThreadIds.Y + startY;
            var pos = y * width + x;
            var color = image[pos];

            switch (channel)
            {
                case 0: // RGB
                    for (var h = -radius; h < radius; h++)
                    {
                        color.XYZ = Hlsl.Max(color.XYZ, image[CoordWrapGpu.Wrap(y + h, height) * width + x].XYZ);
                    }
                    break;
                case 1: // R
                    for (var h = -radius; h < radius; h++)
                    {
                        color.Z = Hlsl.Max(color.Z, image[CoordWrapGpu.Wrap(y + h, height) * width + x].Z);
                    }
                    break;
                case 2: // G
                    for (var h = -radius; h < radius; h++)
                    {
                        color.Y = Hlsl.Max(color.Y, image[CoordWrapGpu.Wrap(y + h, height) * width + x].Y);
                    }
                    break;
                case 3: // B
                    for (var h = -radius; h < radius; h++)
                    {
                        color.X = Hlsl.Max(color.X, image[CoordWrapGpu.Wrap(y + h, height) * width + x].X);
                    }
                    break;
                case 4: // A
                    for (var h = -radius; h < radius; h++)
                    {
                        color.W = Hlsl.Max(color.W, image[CoordWrapGpu.Wrap(y + h, height) * width + x].W);
                    }
                    break;
            }

            result[pos] = color;
        }
    }

    enum MinMaxMode
    {
        Min,
        Max
    }
}
