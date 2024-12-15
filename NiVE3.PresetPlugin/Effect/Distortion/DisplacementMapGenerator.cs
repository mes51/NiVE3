using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;
using ComputeSharp;
using NiVE3.Image;
using NiVE3.PresetPlugin.Effect.Util;
using NiVE3.PresetPlugin.Internal;
using NiVE3.PresetPlugin.Internal.Drawing;
using NiVE3.Shared.Extension;

namespace NiVE3.PresetPlugin.Effect.Distortion
{
    static class DisplacementMapGenerator
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float[] Generate(NManagedImage sourceImage, int targetWidth, int targetHeight, DisplacemenMapChannelType channel, SourceLayerPositionType position)
        {
            var map = ArrayPool<float>.Shared.Rent(targetWidth * targetHeight);

            if (sourceImage.Width != targetWidth || sourceImage.Height != targetHeight)
            {
                using var resizedImage = new NManagedImage(targetWidth, targetHeight);
                var resizedImageData = resizedImage.Data;
                var sourceImageData = sourceImage.Data;

                switch (position)
                {
                    case SourceLayerPositionType.Center:
                        {
                            var leftX = (targetWidth - sourceImage.Width) * 0.5F;
                            var topY = (targetHeight - sourceImage.Height) * 0.5F;

                            Parallel.For(0, targetHeight, y =>
                            {
                                var resizedImageDataSpan = resizedImageData.AsSpan(y * targetWidth, targetWidth);
                                var sourceY = y - topY;

                                for (var x = 0; x < targetWidth; x++)
                                {
                                    var sourceX = x - leftX;
                                    resizedImageDataSpan[x] = ImageInterpolation.BilinearLoop(sourceImageData, sourceImage.Width, sourceImage.Height, sourceX, sourceY);
                                }
                            });
                        }
                        break;
                    case SourceLayerPositionType.Loop:
                        {
                            Parallel.For(0, targetHeight, y =>
                            {
                                var resizedImageDataSpan = resizedImageData.AsSpan(y * targetWidth, targetWidth);
                                var sourceImageDataSpan = sourceImageData.AsSpan(CoordWrap.Repeat(y, sourceImage.Height) * sourceImage.Width, sourceImage.Width);
                                for (var x = 0; x < targetWidth; x++)
                                {
                                    resizedImageDataSpan[x] = sourceImageDataSpan[CoordWrap.Repeat(x, sourceImage.Width)];
                                }
                            });
                        }
                        break;
                    default:
                        {
                            var scaleX = sourceImage.Width / (float)targetWidth;
                            var scaleY = sourceImage.Height / (float)targetHeight;
                            Parallel.For(0, targetHeight, y =>
                            {
                                var resizedImageDataSpan = resizedImageData.AsSpan(y * targetWidth, targetWidth);
                                var sourceY = y * scaleY;

                                for (var x = 0; x < targetWidth; x++)
                                {
                                    var sourceX = x * scaleX;
                                    resizedImageDataSpan[x] = ImageInterpolation.BilinearInflateEdge(sourceImageData, sourceImage.Width, sourceImage.Height, sourceX, sourceY);
                                }
                            });
                        }
                        break;
                }

                CalcMapValues(resizedImage, map, channel);
            }
            else
            {
                CalcMapValues(sourceImage, map, channel);
            }

            return map;
        }

        public static ReadWriteBuffer<float> Generate(GraphicsDevice device, NGPUImage gpuImage, int targetWidth, int targetHeight, DisplacemenMapChannelType channel, SourceLayerPositionType position)
        {
            var map = device.AllocateReadWriteBuffer<float>(targetWidth * targetHeight);

            using var context = device.CreateComputeContext();
            if (gpuImage.Width == targetWidth && gpuImage.Height == targetHeight)
            {
                context.For(targetWidth, targetHeight, new GenerateDisplacementMap(gpuImage.Data, map, gpuImage.Width, gpuImage.Height, targetWidth, targetHeight, (int)channel, -1));
            }
            else
            {
                context.For(targetWidth, targetHeight, new GenerateDisplacementMap(gpuImage.Data, map, gpuImage.Width, gpuImage.Height, targetWidth, targetHeight, (int)channel, (int)position));
            }

            return map;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void CalcMapValues(NManagedImage sourceImage, float[] map, DisplacemenMapChannelType type)
        {
            var sourceImageData = sourceImage.Data;
            var width = sourceImage.Width;
            switch (type)
            {
                case DisplacemenMapChannelType.R:
                    Parallel.For(0, sourceImage.Height, y =>
                    {
                        var sourceImageDataSpan = sourceImageData.AsSpan(y * width, width);
                        var mapSpan = map.AsSpan(y * width, width);

                        for (var x = 0; x < sourceImageDataSpan.Length; x++)
                        {
                            mapSpan[x] = (sourceImageDataSpan[x].Z - 0.5F) * 2.0F;
                        }
                    });
                    break;
                case DisplacemenMapChannelType.G:
                    Parallel.For(0, sourceImage.Height, y =>
                    {
                        var sourceImageDataSpan = sourceImageData.AsSpan(y * width, width);
                        var mapSpan = map.AsSpan(y * width, width);

                        for (var x = 0; x < sourceImageDataSpan.Length; x++)
                        {
                            mapSpan[x] = (sourceImageDataSpan[x].Y - 0.5F) * 2.0F;
                        }
                    });
                    break;
                case DisplacemenMapChannelType.B:
                    Parallel.For(0, sourceImage.Height, y =>
                    {
                        var sourceImageDataSpan = sourceImageData.AsSpan(y * width, width);
                        var mapSpan = map.AsSpan(y * width, width);

                        for (var x = 0; x < sourceImageDataSpan.Length; x++)
                        {
                            mapSpan[x] = (sourceImageDataSpan[x].X - 0.5F) * 2.0F;
                        }
                    });
                    break;
                case DisplacemenMapChannelType.A:
                    Parallel.For(0, sourceImage.Height, y =>
                    {
                        var sourceImageDataSpan = sourceImageData.AsSpan(y * width, width);
                        var mapSpan = map.AsSpan(y * width, width);

                        for (var x = 0; x < sourceImageDataSpan.Length; x++)
                        {
                            mapSpan[x] = (sourceImageDataSpan[x].W - 0.5F) * 2.0F;
                        }
                    });
                    break;
                case DisplacemenMapChannelType.Luminance:
                    Parallel.For(0, sourceImage.Height, y =>
                    {
                        var sourceImageDataSpan = sourceImageData.AsSpan(y * width, width);
                        var mapSpan = map.AsSpan(y * width, width);

                        for (var x = 0; x < sourceImageDataSpan.Length; x++)
                        {
                            mapSpan[x] = (Vector4.Dot(sourceImageDataSpan[x], Const.ConvertToGrayScale) - 0.5F) * 2.0F;
                        }
                    });
                    break;
                case DisplacemenMapChannelType.Hue:
                    Parallel.For(0, sourceImage.Height, y =>
                    {
                        var sourceImageDataSpan = sourceImageData.AsSpan(y * width, width);
                        var mapSpan = map.AsSpan(y * width, width);

                        for (var x = 0; x < sourceImageDataSpan.Length; x++)
                        {
                            var color = sourceImageDataSpan[x];
                            var min = color.HorizontalMinBy3Element();
                            var max = color.HorizontalMaxBy3Element();
                            var diff = max - min;
                            var h = diff != 0.0F ? max switch
                            {
                                _ when max == color.X => (color.Z - color.Y) / diff * 60.0F + 240.0F,
                                _ when max == color.Y => (color.X - color.Z) / diff * 60.0F + 120.0F,
                                _ => (color.Y - color.X) / diff * 60.0F
                            } : 180.0F;

                            mapSpan[x] = (h - 180.0F) / 180.0F;
                        }
                    });
                    break;
                case DisplacemenMapChannelType.Saturation:
                    Parallel.For(0, sourceImage.Height, y =>
                    {
                        var sourceImageDataSpan = sourceImageData.AsSpan(y * width, width);
                        var mapSpan = map.AsSpan(y * width, width);

                        for (var x = 0; x < sourceImageDataSpan.Length; x++)
                        {
                            var color = sourceImageDataSpan[x];
                            var min = color.AsVector128().HorizontalMinBy3Element().GetElement(0);
                            var max = color.AsVector128().HorizontalMaxBy3Element().GetElement(0);
                            if (max > 0.0F)
                            {
                                mapSpan[x] = ((max - min) / max - 0.5F) * 2.0F;
                            }
                            else
                            {
                                mapSpan[x] = 0.5F;
                            }
                        }
                    });
                    break;
                case DisplacemenMapChannelType.Lightness:
                    Parallel.For(0, sourceImage.Height, y =>
                    {
                        var sourceImageDataSpan = sourceImageData.AsSpan(y * width, width);
                        var mapSpan = map.AsSpan(y * width, width);

                        for (var x = 0; x < sourceImageDataSpan.Length; x++)
                        {
                            var color = sourceImageDataSpan[x];
                            var min = color.AsVector128().HorizontalMinBy3Element().GetElement(0);
                            var max = color.AsVector128().HorizontalMaxBy3Element().GetElement(0);
                            mapSpan[x] = max + min - 1.0F;
                        }
                    });
                    break;
                case DisplacemenMapChannelType.On:
                    map.AsSpan(sourceImage.Width * sourceImage.Height).Fill(1.0F);
                    break;
                case DisplacemenMapChannelType.Off:
                    map.AsSpan(sourceImage.Width * sourceImage.Height).Fill(-1.0F);
                    break;
                default:
                    map.AsSpan(sourceImage.Width * sourceImage.Height).Fill(0.0F);
                    break;
            }
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct GenerateDisplacementMap(ReadWriteBuffer<Float4> sourceImage, ReadWriteBuffer<float> displacementMap, int sourceImageWidth, int sourceImageHeight, int mapWidth, int mapHeight, int channel, int position) : IComputeShader
    {
        public void Execute()
        {
            var mapPos = ThreadIds.Y * mapWidth + ThreadIds.X;
            var color = Float4.Zero;
            switch (position)
            {
                case -1: // No resize
                    color = displacementMap[mapPos];
                    break;
                case 0: // Center
                    {
                        var leftX = (mapWidth - sourceImageWidth) * 0.5F;
                        var topY = (mapHeight - sourceImageHeight) * 0.5F;

                        color = SourceImageBilinear(ThreadIds.X - leftX, ThreadIds.Y - topY);
                    }
                    break;
                case 1: // Loop
                    color = sourceImage[CoordWrapGpu.Repeat(ThreadIds.Y, sourceImageHeight) * sourceImageWidth + CoordWrapGpu.Repeat(ThreadIds.X, sourceImageWidth)];
                    break;
                default: // Stretch
                    {
                        var scaleX = sourceImageWidth / (float)mapWidth;
                        var scaleY = sourceImageHeight / (float)mapHeight;

                        color = SourceImageBilinear(ThreadIds.X * scaleX, ThreadIds.Y * scaleY);
                    }
                    break;
            }

            displacementMap[mapPos] = CalcMoveRate(color, channel);
        }


        static float CalcMoveRate(Float4 color, int channelType)
        {
            switch (channelType)
            {
                case 0:
                    return (color.Z - 0.5F) * 2.0F;
                case 1:
                    return (color.Y - 0.5F) * 2.0F;
                case 2:
                    return (color.X - 0.5F) * 2.0F;
                case 3:
                    return (color.W - 0.5F) * 2.0F;
                case 4:
                    return (Hlsl.Dot(color.XYZ, Const.ConvertToGrayScaleFloat3) - 0.5F) * 2.0F;
                case 5:
                    {
                        var min = Hlsl.Min(Hlsl.Min(color.X, color.Y), color.Z);
                        var max = Hlsl.Max(Hlsl.Max(color.X, color.Y), color.Z);
                        var diff = max - min;
                        var h = 180.0F;
                        if (diff != 0.0F)
                        {
                            if (max == color.X)
                            {
                                h = (color.Z - color.Y) / diff * 60.0F + 240.0F;
                            }
                            else if (max == color.Y)
                            {
                                h = (color.X - color.Z) / diff * 60.0F + 120.0F;
                            }
                            else
                            {
                                h = (color.Y - color.X) / diff * 60.0F;
                            }
                        }

                        return (h - 180.0F) / 180.0F;
                    }
                case 6:
                    {
                        var min = Hlsl.Min(Hlsl.Min(color.X, color.Y), color.Z);
                        var max = Hlsl.Max(Hlsl.Max(color.X, color.Y), color.Z);
                        if (max > 0.0F)
                        {
                            return ((max - min) / max - 0.5F) * 2.0F;
                        }
                        else
                        {
                            return 0.5F;
                        }
                    }
                case 7:
                    {
                        var min = Hlsl.Min(Hlsl.Min(color.X, color.Y), color.Z);
                        var max = Hlsl.Max(Hlsl.Max(color.X, color.Y), color.Z);
                        return max + min - 1.0F;
                    }
                case 8:
                    return 1.0F;
                case 10:
                    return -1.0F;
                default:
                    return 0.0F;
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
                    return 0.5F;
                }
            }
            else if (ix < -1 || iy < -1 || ix >= sourceImageWidth || iy >= sourceImageHeight)
            {
                return 0.5F;
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
