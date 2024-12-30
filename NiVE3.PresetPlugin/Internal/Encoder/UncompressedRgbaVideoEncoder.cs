using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using SharpAvi;
using SharpAvi.Codecs;

namespace NiVE3.PresetPlugin.Internal.Encoder
{
    class UncompressedRgbaVideoEncoder : IVideoEncoder
    {
        public FourCC Codec => new FourCC(0);

        public BitsPerPixel BitsPerPixel => BitsPerPixel.Bpp32;

        public int MaxEncodedSize { get; }

        int Height { get; }

        int Stride { get; }

        public UncompressedRgbaVideoEncoder(int width, int height)
        {
            Height = height;
            Stride = width * 4;
            MaxEncodedSize = width * height * 4;
        }

        public int EncodeFrame(byte[] source, int srcOffset, byte[] destination, int destOffset, out bool isKeyFrame)
        {
            if (source.Length > destination.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(destination));
            }

            Parallel.For(0, Height, i =>
            {
                var srcSpan = source.AsSpan(i * Stride + srcOffset, Stride);
                var dstSpan = destination.AsSpan((Height - i - 1) * Stride + destOffset, Stride);

                srcSpan.CopyTo(dstSpan);
            });

            isKeyFrame = true;
            return MaxEncodedSize;
        }

        public int EncodeFrame(ReadOnlySpan<byte> source, Span<byte> destination, out bool isKeyFrame)
        {
            if (source.Length > destination.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(destination));
            }

            for (var i = 0; i < Height; i++)
            {
                source.Slice(i * Stride, Stride).CopyTo(destination.Slice((Height - i - 1) * Stride, Stride));
            }

            isKeyFrame = true;
            return MaxEncodedSize;
        }
    }
}
