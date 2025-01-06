using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;
using SharpAvi;
using SharpAvi.Codecs;

namespace NiVE3.PresetPlugin.Internal.Encoder
{
    class UncompressedAlphaVideoEncoder : ISourceFormatChangeableVideoEncoder
    {
        public FourCC Codec => new FourCC(0);

        public BitsPerPixel BitsPerPixel => BitsPerPixel.Bpp8;

        public int MaxEncodedSize { get; }

        public bool UseFormatConvertedSource { get; set; }

        int Width { get; }

        int Height { get; }

        int SrcStride { get; }

        int DstStride { get; }

        int SourceImageDataSize { get; }

        public UncompressedAlphaVideoEncoder(int width, int height)
        {
            Width = width;
            Height = height;
            SrcStride = width * 4;
            DstStride = (int)Math.Ceiling(width / 4.0) * 4;
            SourceImageDataSize = width * height * 4;
            MaxEncodedSize = DstStride * height;
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

                Parallel.For(0, Height, h =>
                {
                    var srcSpan = source.AsSpan(h * SrcStride + srcOffset, SrcStride);
                    var dstSpan = destination.AsSpan((Height - h - 1) * DstStride + destOffset, DstStride);

                    for (var w = 0; w < Width; w++)
                    {
                        dstSpan[w] = srcSpan[w * 4 + 3];
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

                for (var h = 0; h < Height; h++)
                {
                    var srcSpan = source.Slice(h * SrcStride, SrcStride);
                    var dstSpan = destination.Slice((Height - h - 1) * DstStride, DstStride);

                    for (var w = 0; w < Width; w++)
                    {
                        dstSpan[w] = srcSpan[w * 4 + 3];
                    }
                }
            }

            isKeyFrame = true;
            return MaxEncodedSize;
        }
    }
}
