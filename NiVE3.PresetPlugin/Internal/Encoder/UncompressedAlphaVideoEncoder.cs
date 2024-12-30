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
    class UncompressedAlphaVideoEncoder : IVideoEncoder
    {
        public FourCC Codec => new FourCC(0);

        public BitsPerPixel BitsPerPixel => BitsPerPixel.Bpp8;

        public int MaxEncodedSize { get; }

        int Width { get; }

        int Height { get; }

        int SrcStride { get; }

        int ImageDataSize { get; }

        public UncompressedAlphaVideoEncoder(int width, int height)
        {
            Width = width;
            Height = height;
            SrcStride = width * 4;
            ImageDataSize = width * height;
            MaxEncodedSize = width * height;
        }

        public int EncodeFrame(byte[] source, int srcOffset, byte[] destination, int destOffset, out bool isKeyFrame)
        {
            if ((source.Length - srcOffset) / 4 < ImageDataSize || destination.Length - destOffset < ImageDataSize)
            {
                throw new ArgumentOutOfRangeException(nameof(destination));
            }

            Parallel.For(0, Height, h =>
            {
                var srcLine = source.AsSpan(h * SrcStride + srcOffset, SrcStride);
                var dstLine = destination.AsSpan((Height - h - 1) * Width + destOffset, Width);

                for (var w = 0; w < Width; w++)
                {
                    dstLine[w] = srcLine[w * 4 + 3];
                }
            });

            isKeyFrame = true;
            return MaxEncodedSize;
        }

        public int EncodeFrame(ReadOnlySpan<byte> source, Span<byte> destination, out bool isKeyFrame)
        {
            if (source.Length / 4 < ImageDataSize || destination.Length < ImageDataSize)
            {
                throw new ArgumentOutOfRangeException(nameof(destination));
            }

            for (var h = 0; h < Height; h++)
            {
                var srcLine = source.Slice(h * SrcStride, SrcStride);
                var dstLine = destination.Slice((Height - h - 1) * Width, Width);

                for (var w = 0; w < Width; w++)
                {
                    dstLine[w] = srcLine[w * 4 + 3];
                }
            }

            isKeyFrame = true;
            return MaxEncodedSize;
        }
    }
}
