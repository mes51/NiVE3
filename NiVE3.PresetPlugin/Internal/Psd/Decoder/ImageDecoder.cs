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

        public static Vector4[]? DecodeImage(RandomAccessFileReader reader, in PsdFileHeader header, Vector4[] indexedColorTable, short transparencyIndex, int[] compressionMethod, long[] imageDataBegin)
        {
            switch (header.ColorMode)
            {
                case ColorMode.GrayScale:
                    return DecodeGrayScale(reader, header, compressionMethod, imageDataBegin);
                case ColorMode.Indexed:
                    return DecodeIndexed(reader, header, indexedColorTable, transparencyIndex, compressionMethod, imageDataBegin);
                case ColorMode.RGB:
                    return DecodeRGBA(reader, header, compressionMethod, imageDataBegin);
                default: // unsupported color mode
                    return null;
            }
        }

        static Vector4[]? DecodeGrayScale(RandomAccessFileReader reader, PsdFileHeader header, int[] compressionMethod, long[] imageDataBegin)
        {
            var streams = compressionMethod.Zip(imageDataBegin, (c, b) => CreateStream(reader, header, c, b)).SelectMany(_ => _).ToArray();
            if (streams.Length < 1)
            {
                return null;
            }

            var result = new Vector4[header.ImageWidth * header.ImageHeight];
            if (header.ColorDepth >= 32)
            {
                switch (streams.Length)
                {
                    case 1:
                        for (var i = 0; i < result.Length; i++)
                        {
                            var g = MathF.Pow(streams[0].ReadChannel(), InvertedGamma);

                            result[i] = new Vector4(g, g, g, 1.0F);
                        }
                        break;
                    default:
                        for (var i = 0; i < result.Length; i++)
                        {
                            var g = MathF.Pow(streams[0].ReadChannel(), InvertedGamma);
                            var a = streams[1].ReadChannel();

                            result[i] = new Vector4(g, g, g, a);
                        }
                        break;
                }
            }
            else
            {
                switch (streams.Length)
                {
                    case 1:
                        for (var i = 0; i < result.Length; i++)
                        {
                            var g = streams[0].ReadChannel();

                            result[i] = new Vector4(g, g, g, 1.0F);
                        }
                        break;
                    default:
                        for (var i = 0; i < result.Length; i++)
                        {
                            var g = streams[0].ReadChannel();
                            var a = streams[1].ReadChannel();

                            result[i] = new Vector4(g, g, g, a);
                        }
                        break;
                }
            }

            return result;
        }

        static Vector4[]? DecodeIndexed(RandomAccessFileReader reader, PsdFileHeader header, Vector4[] indexedColorTable, short transparencyIndex, int[] compressionMethod, long[] imageDataBegin)
        {
            var stream = compressionMethod.Zip(imageDataBegin, (c, b) => CreateStream(reader, header, c, b)).SelectMany(_ => _).ToArray().FirstOrDefault();
            if (stream == null)
            {
                return null;
            }

            var result = new Vector4[header.ImageWidth * header.ImageHeight];

            for (var i = 0; i < result.Length; i++)
            {
                var index = stream.ReadBytes()[0];
                if (index == transparencyIndex)
                {
                    result[i] = Const.EmptyPixel;
                }
                else
                {
                    result[i] = indexedColorTable[index];
                }
            }

            return result;
        }

        static Vector4[]? DecodeRGBA(RandomAccessFileReader reader, PsdFileHeader header, int[] compressionMethod, long[] imageDataBegin)
        {
            var streams = compressionMethod.Zip(imageDataBegin, (c, b) => CreateStream(reader, header, c, b)).SelectMany(_ => _).ToArray();
            if (streams.Length < 1)
            {
                return null;
            }

            var result = new Vector4[header.ImageWidth * header.ImageHeight];
            if (header.ColorDepth >= 32)
            {
                switch (streams.Length)
                {
                    case 1:
                        for (var i = 0; i < result.Length; i++)
                        {
                            var r = MathF.Pow(streams[0].ReadChannel(), InvertedGamma);

                            result[i] = new Vector4(0.0F, 0.0F, r, 1.0F);
                        }
                        break;
                    case 2:
                        for (var i = 0; i < result.Length; i++)
                        {
                            var r = MathF.Pow(streams[0].ReadChannel(), InvertedGamma);
                            var g = MathF.Pow(streams[1].ReadChannel(), InvertedGamma);

                            result[i] = new Vector4(0.0F, g, r, 1.0F);
                        }
                        break;
                    case 3:
                        for (var i = 0; i < result.Length; i++)
                        {
                            var r = MathF.Pow(streams[0].ReadChannel(), InvertedGamma);
                            var g = MathF.Pow(streams[1].ReadChannel(), InvertedGamma);
                            var b = MathF.Pow(streams[2].ReadChannel(), InvertedGamma);

                            result[i] = new Vector4(b, g, r, 1.0F);
                        }
                        break;
                    default:
                        for (var i = 0; i < result.Length; i++)
                        {
                            var r = MathF.Pow(streams[0].ReadChannel(), InvertedGamma);
                            var g = MathF.Pow(streams[1].ReadChannel(), InvertedGamma);
                            var b = MathF.Pow(streams[2].ReadChannel(), InvertedGamma);
                            var a = streams[3].ReadChannel();

                            result[i] = new Vector4(b, g, r, a);
                        }
                        break;
                }
            }
            else
            {
                switch (streams.Length)
                {
                    case 1:
                        for (var i = 0; i < result.Length; i++)
                        {
                            var r = streams[0].ReadChannel();

                            result[i] = new Vector4(0.0F, 0.0F, r, 1.0F);
                        }
                        break;
                    case 2:
                        for (var i = 0; i < result.Length; i++)
                        {
                            var r = streams[0].ReadChannel();
                            var g = streams[1].ReadChannel();

                            result[i] = new Vector4(0.0F, g, r, 1.0F);
                        }
                        break;
                    case 3:
                        for (var i = 0; i < result.Length; i++)
                        {
                            var r = streams[0].ReadChannel();
                            var g = streams[1].ReadChannel();
                            var b = streams[2].ReadChannel();

                            result[i] = new Vector4(b, g, r, 1.0F);
                        }
                        break;
                    default:
                        for (var i = 0; i < result.Length; i++)
                        {
                            var r = streams[0].ReadChannel();
                            var g = streams[1].ReadChannel();
                            var b = streams[2].ReadChannel();
                            var a = streams[3].ReadChannel();

                            result[i] = new Vector4(b, g, r, a);
                        }
                        break;
                }
            }

            return result;
        }

        static IChannelDataStream[] CreateStream(RandomAccessFileReader reader, in PsdFileHeader header, int compressionMethod, long begin)
        {
            switch (compressionMethod)
            {
                case 0:
                    return ChannelDataDecoder.Raw(reader, header.ImageWidth, header.ImageHeight, header.ColorDepth, header.ColorChannels, begin);
                case 1:
                    return ChannelDataDecoder.Rle(reader, header.ImageWidth, header.ImageHeight, header.ColorDepth, header.ColorChannels, begin, header.IsPsb);
                default:
                    return [];
            }
        }
    }
}
