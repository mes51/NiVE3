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
    class UncompressedRgbaVideoEncoder : ISourceFormatChangeableVideoEncoder
    {
        public FourCC Codec => new FourCC(0);

        public BitsPerPixel BitsPerPixel => BitsPerPixel.Bpp32;

        public int MaxEncodedSize { get; }

        public bool UseFormatConvertedSource { get; set; }

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
            if (source.Length - srcOffset < MaxEncodedSize)
            {
                throw new ArgumentOutOfRangeException(nameof(source));
            }
            else if (destination.Length - destOffset < MaxEncodedSize)
            {
                throw new ArgumentOutOfRangeException(nameof(destination));
            }

            if (UseFormatConvertedSource)
            {
                source.AsSpan(srcOffset, MaxEncodedSize).CopyTo(destination.AsSpan(destOffset, MaxEncodedSize));
            }
            else
            {
                Parallel.For(0, Height, h =>
                {
                    var srcSpan = source.AsSpan(h * Stride + srcOffset, Stride);
                    var dstSpan = destination.AsSpan((Height - h - 1) * Stride + destOffset, Stride);

                    srcSpan.CopyTo(dstSpan);
                });
            }

            isKeyFrame = true;
            return MaxEncodedSize;
        }

        public int EncodeFrame(ReadOnlySpan<byte> source, Span<byte> destination, out bool isKeyFrame)
        {
            if (source.Length < MaxEncodedSize)
            {
                throw new ArgumentOutOfRangeException(nameof(source));
            }
            else if (destination.Length < MaxEncodedSize)
            {
                throw new ArgumentOutOfRangeException(nameof(destination));
            }

            if (UseFormatConvertedSource)
            {
                source[..MaxEncodedSize].CopyTo(destination[..MaxEncodedSize]);
            }
            else
            {
                for (var h = 0; h < Height; h++)
                {
                    var srcSpan = source.Slice(h * Stride, Stride);
                    var dstSpan = destination.Slice((Height - h - 1) * Stride, Stride);

                    srcSpan.CopyTo(dstSpan);
                }
            }

            isKeyFrame = true;
            return MaxEncodedSize;
        }
    }
}
