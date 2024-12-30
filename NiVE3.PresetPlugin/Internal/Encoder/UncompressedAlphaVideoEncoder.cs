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

        public UncompressedAlphaVideoEncoder(int width, int height)
        {
            Width = width;
            Height = height;
            SrcStride = width * 4;
            MaxEncodedSize = width * height;
        }

        public int EncodeFrame(byte[] source, int srcOffset, byte[] destination, int destOffset, out bool isKeyFrame)
        {
            return EncodeFrame(source.AsSpan(srcOffset), destination.AsSpan(destOffset), out isKeyFrame);
        }

        public int EncodeFrame(ReadOnlySpan<byte> source, Span<byte> destination, out bool isKeyFrame)
        {
            if (source.Length / 4 > destination.Length)
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
