using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using NiVE3.PresetPlugin.Internal.IO;
using NiVE3.PresetPlugin.Internal.Psd.Enums;
using NiVE3.PresetPlugin.Internal.Psd.Structs;
using SharpGen.Runtime;

namespace NiVE3.PresetPlugin.Internal.Psd.Decoder
{
    static class ImageDecoder
    {
        const float InvertedGamma = 1.0F / 2.2F;

        public static Vector4[]? DecodeImage(RandomAccessFileReader reader, in PsdFileHeader header, in RectTLBR rect, Vector4[] indexedColorTable, short transparencyIndex, int needColorChannelPerItem, int[] compressionMethods, long[] imageDataBegins)
        {
            switch (header.ColorMode)
            {
                case ColorMode.GrayScale:
                    return DecodeGrayScale(reader, header, rect, needColorChannelPerItem, compressionMethods, imageDataBegins);
                case ColorMode.Indexed:
                    return DecodeIndexed(reader, header, rect, indexedColorTable, transparencyIndex, compressionMethods, imageDataBegins);
                case ColorMode.RGB:
                    return DecodeRGBA(reader, header, rect, needColorChannelPerItem, compressionMethods, imageDataBegins);
                default: // unsupported color mode
                    return null;
            }
        }

        static Vector4[]? DecodeGrayScale(RandomAccessFileReader reader, PsdFileHeader header, RectTLBR rect, int needColorChannels, int[] compressionMethods, long[] imageDataBegins)
        {
            var streams = compressionMethods.Zip(imageDataBegins, (c, b) => CreateStream(reader, header, rect, needColorChannels, c, b)).SelectMany(_ => _).ToArray();
            if (streams.Length < 1)
            {
                return null;
            }

            var width = header.ImageWidth;
            var result = new Vector4[width * header.ImageHeight];
            if (header.ColorDepth >= 32)
            {
                switch (streams.Length)
                {
                    case 1:
                        for (var y = rect.Top; y < rect.Bottom; y++)
                        {
                            var pos = y * width + rect.Left;
                            for (var x = rect.Left; x < rect.Right; x++, pos++)
                            {
                                var g = MathF.Pow(streams[0].ReadChannel(), InvertedGamma);

                                result[pos] = new Vector4(g, g, g, 1.0F);
                            }
                        }
                        break;
                    default:
                        for (var y = rect.Top; y < rect.Bottom; y++)
                        {
                            var pos = y * width + rect.Left;
                            for (var x = rect.Left; x < rect.Right; x++, pos++)
                            {
                                var g = MathF.Pow(streams[0].ReadChannel(), InvertedGamma);
                                var a = streams[1].ReadChannel();

                                result[pos] = new Vector4(g, g, g, a);
                            }
                        }
                        break;
                }
            }
            else
            {
                switch (streams.Length)
                {
                    case 1:
                        for (var y = rect.Top; y < rect.Bottom; y++)
                        {
                            var pos = y * width + rect.Left;
                            for (var x = rect.Left; x < rect.Right; x++, pos++)
                            {
                                var g = streams[0].ReadChannel();

                                result[pos] = new Vector4(g, g, g, 1.0F);
                            }
                        }
                        break;
                    default:
                        for (var y = rect.Top; y < rect.Bottom; y++)
                        {
                            var pos = y * width + rect.Left;
                            for (var x = rect.Left; x < rect.Right; x++, pos++)
                            {
                                var g = streams[0].ReadChannel();
                                var a = streams[1].ReadChannel();

                                result[pos] = new Vector4(g, g, g, a);
                            }
                        }
                        break;
                }
            }

            return result;
        }

        static Vector4[]? DecodeIndexed(RandomAccessFileReader reader, PsdFileHeader header, RectTLBR rect, Vector4[] indexedColorTable, short transparencyIndex, int[] compressionMethods, long[] imageDataBegins)
        {
            var stream = compressionMethods.Zip(imageDataBegins, (c, b) => CreateStream(reader, header, rect, 1, c, b)).SelectMany(_ => _).ToArray().FirstOrDefault();
            if (stream == null)
            {
                return null;
            }

            var width = header.ImageWidth;
            var result = new Vector4[width * header.ImageHeight];

            for (var y = rect.Top; y < rect.Bottom; y++)
            {
                var pos = y * width + rect.Left;
                for (var x = rect.Left; x < rect.Right; x++, pos++)
                {
                    var index = stream.ReadBytes()[0];
                    if (index == transparencyIndex)
                    {
                        result[pos] = Const.EmptyPixel;
                    }
                    else
                    {
                        result[pos] = indexedColorTable[index];
                    }
                }
            }

            return result;
        }

        static Vector4[]? DecodeRGBA(RandomAccessFileReader reader, PsdFileHeader header, RectTLBR rect, int needColorChannels, int[] compressionMethods, long[] imageDataBegins)
        {
            var streams = compressionMethods.Zip(imageDataBegins, (c, b) => CreateStream(reader, header, rect, needColorChannels, c, b)).SelectMany(_ => _).ToArray();
            if (streams.Length < 1)
            {
                return null;
            }

            var width = header.ImageWidth;
            var result = new Vector4[header.ImageWidth * header.ImageHeight];
            if (header.ColorDepth >= 32)
            {
                switch (streams.Length)
                {
                    case 1:
                        for (var y = rect.Top; y < rect.Bottom; y++)
                        {
                            var pos = y * width + rect.Left;
                            for (var x = rect.Left; x < rect.Right; x++, pos++)
                            {
                                var r = MathF.Pow(streams[0].ReadChannel(), InvertedGamma);

                                result[pos] = new Vector4(0.0F, 0.0F, r, 1.0F);
                            }
                        }
                        break;
                    case 2:
                        for (var y = rect.Top; y < rect.Bottom; y++)
                        {
                            var pos = y * width + rect.Left;
                            for (var x = rect.Left; x < rect.Right; x++, pos++)
                            {
                                var r = MathF.Pow(streams[0].ReadChannel(), InvertedGamma);
                                var g = MathF.Pow(streams[1].ReadChannel(), InvertedGamma);

                                result[pos] = new Vector4(0.0F, g, r, 1.0F);
                            }
                        }
                        break;
                    case 3:
                        for (var y = rect.Top; y < rect.Bottom; y++)
                        {
                            var pos = y * width + rect.Left;
                            for (var x = rect.Left; x < rect.Right; x++, pos++)
                            {
                                var r = MathF.Pow(streams[0].ReadChannel(), InvertedGamma);
                                var g = MathF.Pow(streams[1].ReadChannel(), InvertedGamma);
                                var b = MathF.Pow(streams[2].ReadChannel(), InvertedGamma);

                                result[pos] = new Vector4(b, g, r, 1.0F);
                            }
                        }
                        break;
                    default:
                        for (var y = rect.Top; y < rect.Bottom; y++)
                        {
                            var pos = y * width + rect.Left;
                            for (var x = rect.Left; x < rect.Right; x++, pos++)
                            {
                                var r = MathF.Pow(streams[0].ReadChannel(), InvertedGamma);
                                var g = MathF.Pow(streams[1].ReadChannel(), InvertedGamma);
                                var b = MathF.Pow(streams[2].ReadChannel(), InvertedGamma);
                                var a = streams[3].ReadChannel();

                                result[pos] = new Vector4(b, g, r, a);
                            }
                        }
                        break;
                }
            }
            else
            {
                switch (streams.Length)
                {
                    case 1:
                        for (var y = rect.Top; y < rect.Bottom; y++)
                        {
                            var pos = y * width + rect.Left;
                            for (var x = rect.Left; x < rect.Right; x++, pos++)
                            {
                                var r = streams[0].ReadChannel();

                                result[pos] = new Vector4(0.0F, 0.0F, r, 1.0F);
                            }
                        }
                        break;
                    case 2:
                        for (var y = rect.Top; y < rect.Bottom; y++)
                        {
                            var pos = y * width + rect.Left;
                            for (var x = rect.Left; x < rect.Right; x++, pos++)
                            {
                                var r = streams[0].ReadChannel();
                                var g = streams[1].ReadChannel();

                                result[pos] = new Vector4(0.0F, g, r, 1.0F);
                            }
                        }
                        break;
                    case 3:
                        for (var y = rect.Top; y < rect.Bottom; y++)
                        {
                            var pos = y * width + rect.Left;
                            for (var x = rect.Left; x < rect.Right; x++, pos++)
                            {
                                var r = streams[0].ReadChannel();
                                var g = streams[1].ReadChannel();
                                var b = streams[2].ReadChannel();

                                result[pos] = new Vector4(b, g, r, 1.0F);
                            }
                        }
                        break;
                    default:
                        for (var y = rect.Top; y < rect.Bottom; y++)
                        {
                            var pos = y * width + rect.Left;
                            for (var x = rect.Left; x < rect.Right; x++, pos++)
                            {
                                var r = streams[0].ReadChannel();
                                var g = streams[1].ReadChannel();
                                var b = streams[2].ReadChannel();
                                var a = streams[3].ReadChannel();

                                result[pos] = new Vector4(b, g, r, a);
                            }
                        }
                        break;
                }
            }

            return result;
        }

        static IChannelDataStream[] CreateStream(RandomAccessFileReader reader, in PsdFileHeader header, in RectTLBR rect, int needColorChannels, int compressionMethod, long begin)
        {
            switch (compressionMethod)
            {
                case int.MinValue:
                    return ChannelDataDecoder.Empty(header.ColorDepth, needColorChannels);
                case 0:
                    return ChannelDataDecoder.Raw(reader, rect, header.ColorDepth, needColorChannels, begin);
                case 1:
                    return ChannelDataDecoder.Rle(reader, rect, header.ColorDepth, needColorChannels, begin, header.IsPsb);
                default:
                    return [];
            }
        }
    }
}
