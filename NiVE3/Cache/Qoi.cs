using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Image;
using NiVE3.Numerics;

namespace NiVE3.Cache
{
    // original: https://github.com/phoboslab/qoi
    static class Qoi
    {
        const byte QoiOpIndex = 0x00;

        const byte QoiOpDiff = 0x40;

        const byte QoiOpLuma = 0x80;

        const byte QoiOpRun = 0xC0;

        const byte QoiOpRgb = 0xFE;

        const byte QoiOpRgba = 0xFF;

        const byte QoiMask2 = 0xC0;

        public static SlicedQoiImage Encode(NManagedImage image)
        {
            const int BufferSize = 8192;

            var width = image.Width;
            var height = image.Height;
            var result = new SlicedQoiImage(width, height, image.Origin, Math.Max(Environment.ProcessorCount, 4), 4);

            Parallel.For(0, result.SliceCount * result.ChannelCount, i =>
            {
                var channel = i / result.SliceCount;
                var slice = result.Slices[channel][i % result.SliceCount];
                var dataSpan = MemoryMarshal.Cast<Vector4, int>(image.GetDataSpan().Slice(slice.Y * width, slice.Lines * width));

                Span<int> index = stackalloc int[64];
                Span<byte> prevPixel = stackalloc byte[4];
                Span<byte> currentPixel = stackalloc byte[4];
                ref var prevIntPixel = ref MemoryMarshal.Cast<byte, int>(prevPixel)[0];
                ref var currentIntPixel = ref MemoryMarshal.Cast<byte, int>(currentPixel)[0];

                var currentEncodedData = ArrayPool<byte>.Shared.Rent(BufferSize);
                var outputSpan = currentEncodedData.AsSpan();
                var outputLimit = outputSpan.Length - 6;
                var encodedLength = 0;
                var end = dataSpan.Length - 4;
                var run = 0;
                prevIntPixel = 255 << 24;
                for (var p = channel; p < dataSpan.Length; p += 4)
                {
                    currentIntPixel = dataSpan[p];

                    if (currentIntPixel == prevIntPixel)
                    {
                        run++;
                        if (run == 62 || p >= end)
                        {
                            outputSpan[encodedLength++] = (byte)(QoiOpRun | (run - 1));
                            run = 0;
                        }
                    }
                    else
                    {
                        if (run > 0)
                        {
                            outputSpan[encodedLength++] = (byte)(QoiOpRun | (run - 1));
                            run = 0;
                        }

                        var indexPos = ColorHash(currentPixel);
                        if (index[indexPos] == currentIntPixel)
                        {
                            outputSpan[encodedLength++] = (byte)(QoiOpIndex | indexPos);
                        }
                        else
                        {
                            index[indexPos] = currentIntPixel;

                            if (currentPixel[3] == prevPixel[3])
                            {
                                unchecked
                                {
                                    var vb = (sbyte)(currentPixel[0] - prevPixel[0]);
                                    var vg = (sbyte)(currentPixel[1] - prevPixel[1]);
                                    var vr = (sbyte)(currentPixel[2] - prevPixel[2]);

                                    var vgr = (sbyte)(vr - vg);
                                    var vgb = (sbyte)(vb - vg);

                                    if (vr > -3 && vr < 2 && vg > -3 && vg < 2 && vb > -3 && vb < 2)
                                    {
                                        outputSpan[encodedLength++] = (byte)(QoiOpDiff | (vb + 2) << 4 | (vg + 2) << 2 | (vr + 2));
                                    }
                                    else if (vgr > -9 && vgr < 8 && vg > -33 && vg < 32 && vgb > -9 && vgb < 8)
                                    {
                                        outputSpan[encodedLength++] = (byte)(QoiOpLuma | (vg + 32));
                                        outputSpan[encodedLength++] = (byte)((vgb + 8) << 4 | (vgr + 8));
                                    }
                                    else
                                    {
                                        outputSpan[encodedLength++] = QoiOpRgb;
                                        outputSpan[encodedLength++] = currentPixel[0];
                                        outputSpan[encodedLength++] = currentPixel[1];
                                        outputSpan[encodedLength++] = currentPixel[2];
                                    }
                                }
                            }
                            else
                            {
                                outputSpan[encodedLength++] = QoiOpRgba;
                                MemoryMarshal.Write(outputSpan[encodedLength..], currentIntPixel);
                                encodedLength += 4;
                            }
                        }
                    }

                    if (encodedLength > outputLimit)
                    {
                        slice.Buffers.Add((p - channel + 4, currentEncodedData, encodedLength));

                        prevIntPixel = 255 << 24;
                        currentEncodedData = ArrayPool<byte>.Shared.Rent(BufferSize);
                        outputSpan = currentEncodedData.AsSpan();
                        encodedLength = 0;
                        index.Clear();
                    }
                    else
                    {
                        prevIntPixel = currentIntPixel;
                    }
                }

                if (encodedLength > 0)
                {
                    slice.Buffers.Add((dataSpan.Length, currentEncodedData, encodedLength));
                }
            });

            return result;
        }

        public static NManagedImage Decode(SlicedQoiImage qoiImage)
        {
            var result = new NManagedImage(qoiImage.Width, qoiImage.Height)
            {
                Origin = qoiImage.Origin
            };

            Parallel.For(0, qoiImage.SliceCount * qoiImage.ChannelCount, i =>
            {
                var channel = i / qoiImage.SliceCount;
                var slice = qoiImage.Slices[channel][i % qoiImage.SliceCount];
                var outputIntSpan = MemoryMarshal.Cast<Vector4, int>(result.GetDataSpan().Slice(slice.Y * qoiImage.Width, slice.Lines * qoiImage.Width));

                Span<int> index = stackalloc int[64];
                Span<byte> currentPixel = stackalloc byte[4];
                ref var currentIntPixel = ref MemoryMarshal.Cast<byte, int>(currentPixel)[0];

                var start = 0;
                foreach (var (processed, data, encodedLength) in slice.Buffers)
                {
                    currentIntPixel = 255 << 24;
                    index.Clear();
                    var decodePos = 0;
                    var run = 0;
                    for (var p = start + channel; p < processed; p += 4)
                    {
                        if (run > 0)
                        {
                            run--;
                        }
                        else if (decodePos < encodedLength)
                        {
                            var b1 = data[decodePos++];

                            if (b1 == QoiOpRgb)
                            {
                                currentPixel[0] = data[decodePos++];
                                currentPixel[1] = data[decodePos++];
                                currentPixel[2] = data[decodePos++];
                            }
                            else if (b1 == QoiOpRgba)
                            {
                                currentIntPixel = MemoryMarshal.Read<int>(data.AsSpan()[decodePos..]);
                                decodePos += 4;
                            }
                            else
                            {
                                var op = b1 & QoiMask2;
                                if (op == QoiOpIndex)
                                {
                                    currentIntPixel = index[b1];
                                }
                                else if (op == QoiOpDiff)
                                {
                                    unchecked
                                    {
                                        currentPixel[0] = (byte)(currentPixel[0] + ((b1 >> 4) & 0x03) - 2);
                                        currentPixel[1] = (byte)(currentPixel[1] + ((b1 >> 2) & 0x03) - 2);
                                        currentPixel[2] = (byte)(currentPixel[2] + (b1 & 0x03) - 2);
                                    }
                                }
                                else if (op == QoiOpLuma)
                                {
                                    var b2 = data[decodePos++];
                                    var vg = (b1 & 0x3F) - 32;

                                    unchecked
                                    {
                                        currentPixel[0] = (byte)(currentPixel[0] + vg - 8 + ((b2 >> 4) & 0x0F));
                                        currentPixel[1] = (byte)(currentPixel[1] + vg);
                                        currentPixel[2] = (byte)(currentPixel[2] + vg - 8 + (b2 & 0x0F));
                                    }
                                }
                                else if (op == QoiOpRun)
                                {
                                    run = b1 & 0x3F;
                                }
                            }

                            index[ColorHash(currentPixel)] = currentIntPixel;
                        }
                        else
                        {
                            break;
                        }

                        outputIntSpan[p] = currentIntPixel;
                    }
                    start = processed;
                }
            });

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static byte ColorHash(ReadOnlySpan<byte> color)
        {
            return unchecked((byte)((color[0] * 7 + color[1] * 5 + color[2] * 3 + color[3] * 11) & 0x3F));
        }
    }

    class SlicedQoiImage : IDisposable
    {
        public int Width { get; }

        public int Height { get; }

        public QoiSlice[][] Slices { get; }

        public int SliceCount { get; }

        public int ChannelCount { get; }

        public Vector2d Origin { get; }

        public SlicedQoiImage(int width, int height, Vector2d origin, int slices, int channels = 1)
        {
            Width = width;
            Height = height;
            Origin = origin;
            SliceCount = slices;
            ChannelCount = channels;
            Slices = new QoiSlice[channels][];

            for (var c = 0; c < channels; c++)
            {
                var channelSlices = new QoiSlice[slices];
                var lines = Height / slices;
                for (var i = 0; i < slices - 1; i++)
                {
                    channelSlices[i] = new QoiSlice(lines * i, lines);
                }
                channelSlices[slices - 1] = new QoiSlice(lines * (slices - 1), Height - (lines * (slices - 1)));

                Slices[c] = channelSlices;
            }
        }

        public int GetAllocatedSize()
        {
            return Slices.SelectMany(_ => _).Sum(s => s.Buffers.Sum(b => b.encodedData.Length));
        }

        public void Dispose()
        {
            foreach (var slice in Slices.SelectMany(_ => _))
            {
                slice.Dispose();
            }
        }
    }

    record QoiSlice(int Y, int Lines) : IDisposable
    {
        public List<(int processedLength, byte[] encodedData, int encodedDataLength)> Buffers { get; } = [];

        public void Dispose()
        {
            foreach (var (_, encodedData, _) in Buffers)
            {
                ArrayPool<byte>.Shared.Return(encodedData);
            }
        }
    }
}
