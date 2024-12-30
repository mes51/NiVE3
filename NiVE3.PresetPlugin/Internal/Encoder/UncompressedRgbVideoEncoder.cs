using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpAvi;

namespace NiVE3.PresetPlugin.Internal.Encoder
{
    class UncompressedRgbVideoEncoder : ISourceFormatChangeableVideoEncoder
    {
        public FourCC Codec => new FourCC(0);

        public BitsPerPixel BitsPerPixel => BitsPerPixel.Bpp24;

        public int MaxEncodedSize { get; }

        public bool UseFormatConvertedSource { get; set; }

        int Width { get; }

        int Height { get; }

        int Stride { get; }

        int SrcStride { get; }

        int SourceImageDataSize { get; }

        public UncompressedRgbVideoEncoder(int width, int height)
        {
            Width = width;
            Height = height;
            Stride = width * 3;
            SrcStride = width * 4;
            SourceImageDataSize = width * height * 4;
            MaxEncodedSize = width * height * 3;
        }

        public int EncodeFrame(byte[] source, int srcOffset, byte[] destination, int destOffset, out bool isKeyFrame)
        {
            if (UseFormatConvertedSource)
            {
                if (source.Length - srcOffset < MaxEncodedSize)
                {
                    throw new ArgumentOutOfRangeException(nameof(source));
                }
                else if (destination.Length - destOffset < MaxEncodedSize)
                {
                    throw new ArgumentOutOfRangeException(nameof(destination));
                }

                source.AsSpan(srcOffset, MaxEncodedSize).CopyTo(destination.AsSpan(destOffset, MaxEncodedSize));
            }
            else
            {
                if (source.Length - srcOffset < SourceImageDataSize)
                {
                    throw new ArgumentOutOfRangeException(nameof(source));
                }
                else if (destination.Length - destOffset < MaxEncodedSize)
                {
                    throw new ArgumentOutOfRangeException(nameof(destination));
                }

                Parallel.For(0, Height, i =>
                {
                    var srcSpan = source.AsSpan(i * SrcStride + srcOffset, SrcStride);
                    var dstSpan = destination.AsSpan((Height - i - 1) * Stride + destOffset, Stride);

                    for (int w = 0, sp = 0, dp = 0; w < Width; w++, sp += 4, dp += 3)
                    {
                        dstSpan[dp] = srcSpan[sp];
                        dstSpan[dp + 1] = srcSpan[sp + 1];
                        dstSpan[dp + 2] = srcSpan[sp + 2];
                    }
                });
            }

            isKeyFrame = true;
            return MaxEncodedSize;
        }

        public int EncodeFrame(ReadOnlySpan<byte> source, Span<byte> destination, out bool isKeyFrame)
        {
            if (UseFormatConvertedSource)
            {
                if (source.Length < MaxEncodedSize)
                {
                    throw new ArgumentOutOfRangeException(nameof(source));
                }
                else if (destination.Length < MaxEncodedSize)
                {
                    throw new ArgumentOutOfRangeException(nameof(destination));
                }

                source[..MaxEncodedSize].CopyTo(destination[..MaxEncodedSize]);
            }
            else
            {
                if (source.Length < SourceImageDataSize)
                {
                    throw new ArgumentOutOfRangeException(nameof(source));
                }
                else if (destination.Length < MaxEncodedSize)
                {
                    throw new ArgumentOutOfRangeException(nameof(destination));
                }

                for (var i = 0; i < Height; i++)
                {
                    var srcSpan = source.Slice(i * SrcStride, SrcStride);
                    var dstSpan = destination.Slice ((Height - i - 1) * Stride, Stride);

                    for (int w = 0, sp = 0, dp = 0; w < Width; w++, sp += 4, dp += 3)
                    {
                        dstSpan[dp] = srcSpan[sp];
                        dstSpan[dp + 1] = srcSpan[sp + 1];
                        dstSpan[dp + 2] = srcSpan[sp + 2];
                    }
                }
            }

            isKeyFrame = true;
            return MaxEncodedSize;
        }
    }
}
