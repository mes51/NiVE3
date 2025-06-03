using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using NiVE3.PresetPlugin.Internal.IO;
using SharpGen.Runtime;

namespace NiVE3.PresetPlugin.Internal.Psd.Decoder
{
    static class ChannelDataDecoder
    {
        public static IChannelDataStream[] Raw(RandomAccessFileReader reader, int width, int height, int channelDepth, int channelCount, long begin)
        {
            var result = new IChannelDataStream[channelCount];
            var alignedLineDataLength = channelDepth switch
            {
                1 => (int)MathF.Ceiling(width / 8.0F),
                _ => channelDepth / 8 * width,
            };
            var channelLength = height * alignedLineDataLength;
            for (var i = 0; i < result.Length; i++)
            {
                var channelBegin = begin + channelLength * i;
                result[i] = new RawChannelDataStream(reader, width, height, channelDepth, channelBegin);
            }
            return result;
        }

        public static IChannelDataStream[] Rle(RandomAccessFileReader reader, int width, int height, int channelDepth, int channelCount, long begin, bool isPsb)
        {
            var byteCounts = new int[channelCount][];
            reader.Position = begin;
            for (var i = 0; i < byteCounts.Length; i++)
            {
                var byteCount = new int[height];

                if (isPsb)
                {
                    for (var h = 0; h < height; h++)
                    {
                        byteCount[h] = reader.ReadInt32();
                    }
                }
                else
                {
                    for (var h = 0; h < height; h++)
                    {
                        byteCount[h] = reader.ReadInt16();
                    }
                }

                byteCounts[i] = byteCount;
            }

            var result = new IChannelDataStream[channelCount];
            var currentBegin = reader.Position;
            for (var i = 0; i < result.Length; i++)
            {
                result[i] = new RleChannelDataStream(reader, width, height, channelDepth, currentBegin, byteCounts[i]);
                currentBegin += byteCounts[i].Sum();
            }

            return result;
        }
    }

    interface IChannelDataStream
    {
        float ReadChannel();

        byte[] ReadBytes();
    }

    file class RawChannelDataStream : IChannelDataStream
    {
        RandomAccessReaderBase Reader { get; }

        int Width { get; }

        int ChannelDepth { get; }

        long TotalPixelCount { get; }

        int CurrentLinePixelIndex { get; set; }

        long ReadPixelCount { get; set; }

        byte CurrentBitReadingByte { get; set; }

        bool IsStreamEnded => ReadPixelCount >= TotalPixelCount;

        public RawChannelDataStream(RandomAccessFileReader reader, int width, int height, int channelDepth, long begin)
        {
            Reader = reader.CreateSubReader(begin);
            Width = width;
            ChannelDepth = channelDepth;
            TotalPixelCount = width * height;
        }

        public float ReadChannel()
        {
            if (IsStreamEnded)
            {
                return 0.0F;
            }

            var result = 0.0F;
            switch (ChannelDepth)
            {
                case 1:
                    {
                        var bitCursor = CurrentLinePixelIndex % 8;
                        if (bitCursor == 0)
                        {
                            CurrentBitReadingByte = Reader.ReadByte();
                        }

                        result = ((CurrentBitReadingByte & (1 << bitCursor)) >> bitCursor) > 0 ? 1.0F : 0.0F;
                    }
                    break;
                case 8:
                    result = Reader.ReadByte() / 255.0F;
                    break;
                case 16:
                    result = Reader.ReadUInt16() / 65535.0F;
                    break;
                case 32:
                    result = Reader.ReadSingle();
                    break;
            }

            ReadPixelCount++;
            CurrentLinePixelIndex++;
            if (CurrentLinePixelIndex >= Width)
            {
                CurrentLinePixelIndex = 0;
            }

            return result;
        }

        public byte[] ReadBytes()
        {
            if (IsStreamEnded)
            {
                return [0];
            }

            var result = new byte[1];
            switch (ChannelDepth)
            {
                case 1:
                    {
                        var bitCursor = CurrentLinePixelIndex % 8;
                        if (bitCursor == 0)
                        {
                            CurrentBitReadingByte = Reader.ReadByte();
                        }

                        result = [(byte)(((CurrentBitReadingByte & (1 << bitCursor)) >> bitCursor) > 0 ? 255 : 0)];
                    }
                    break;
                case 8:
                    result = [Reader.ReadByte()];
                    break;
                case 16:
                    result = Reader.ReadBytes(2);
                    break;
                case 32:
                    result = Reader.ReadBytes(4);
                    break;
            }

            ReadPixelCount++;
            CurrentLinePixelIndex++;
            if (CurrentLinePixelIndex >= Width)
            {
                CurrentLinePixelIndex = 0;
            }

            return result;
        }
    }

    file class RleChannelDataStream : IChannelDataStream
    {
        RandomAccessReaderBase Reader { get; }

        int Width { get; }

        int ChannelDepth { get; }

        int[] ByteCounts { get; }

        long TotalPixelCount { get; }

        int CurrentLinePixelIndex { get; set; }

        int CurrentLineIndex { get; set; }

        byte CurrentBitReadingByte { get; set; }

        long ReadPixelCount { get; set; }

        byte[] Buffer { get; }

        bool IsStreamEnded => ReadPixelCount >= TotalPixelCount;

        public RleChannelDataStream(RandomAccessFileReader reader, int width, int height, int channelDepth, long begin, int[] byteCounts)
        {
            Reader = reader.CreateSubReader(begin);
            Width = width;
            ChannelDepth = channelDepth;
            ByteCounts = byteCounts;
            TotalPixelCount = width * height;
            CurrentLinePixelIndex = width;

            var alignedLineDataLength = channelDepth switch
            {
                1 => (int)MathF.Ceiling(width / 8.0F),
                _ => channelDepth / 8 * width,
            };
            Buffer = new byte[alignedLineDataLength];
        }

        public float ReadChannel()
        {
            if (IsStreamEnded)
            {
                return 0.0F;
            }

            var result = 0.0F;
            switch (ChannelDepth)
            {
                case 1:
                    {
                        var bitCursor = CurrentLinePixelIndex % 8;
                        if (bitCursor == 0)
                        {
                            DecodeLine();
                            CurrentBitReadingByte = Buffer[CurrentLinePixelIndex / 8];
                        }

                        result = ((CurrentBitReadingByte & (1 << bitCursor)) >> bitCursor) > 0 ? 1.0F : 0.0F;
                    }
                    break;
                case 8:
                    DecodeLine();
                    result = Buffer[CurrentLinePixelIndex] / 255.0F;
                    break;
                case 16:
                    DecodeLine();
                    result = Unsafe.ReadUnaligned<short>(ref Buffer[CurrentLinePixelIndex * 2]) / 65535.0F;
                    break;
                case 32:
                    DecodeLine();
                    result = Unsafe.ReadUnaligned<float>(ref Buffer[CurrentLinePixelIndex * 4]);
                    break;
            }

            ReadPixelCount++;
            CurrentLinePixelIndex++;

            return result;
        }

        public byte[] ReadBytes()
        {
            if (IsStreamEnded)
            {
                return [0];
            }

            var result = new byte[1];
            switch (ChannelDepth)
            {
                case 1:
                    {
                        var bitCursor = CurrentLinePixelIndex % 8;
                        if (bitCursor == 0)
                        {
                            DecodeLine();
                            CurrentBitReadingByte = Buffer[CurrentLinePixelIndex / 8];
                        }

                        result = [(byte)(((CurrentBitReadingByte & (1 << bitCursor)) >> bitCursor) > 0 ? 255 : 0)];
                    }
                    break;
                case 8:
                    DecodeLine();
                    result = [Buffer[CurrentLinePixelIndex]];
                    break;
                case 16:
                    DecodeLine();
                    result = new byte[2];
                    Buffer.AsSpan(CurrentLinePixelIndex * 2).CopyTo(result);
                    break;
                case 32:
                    DecodeLine();
                    result = new byte[4];
                    Buffer.AsSpan(CurrentLinePixelIndex * 4, 4).CopyTo(result);
                    break;
            }

            ReadPixelCount++;
            CurrentLinePixelIndex++;

            return result;
        }

        void DecodeLine()
        {
            if (CurrentLinePixelIndex < Width)
            {
                return;
            }

            RleDecodeer.Decode(Reader, Buffer, ByteCounts[CurrentLineIndex]);

            CurrentLinePixelIndex = 0;
            CurrentLineIndex++;
        }
    }

    file static class RleDecodeer
    {
        public static void Decode(RandomAccessReaderBase reader, byte[] buffer, int byteCount)
        {
            var bufferSpan = buffer.AsSpan();
            bufferSpan.Clear();

            var data = ArrayPool<byte>.Shared.Rent(byteCount);
            var dataSpan = data.AsSpan(0, byteCount);

            reader.Read(dataSpan);

            var index = 0;
            for (var i = 0; i < dataSpan.Length;)
            {
                var count = (int)dataSpan[i];
                i++;
                if (count < 128)
                {
                    for (var n = 0; n <= count && index < bufferSpan.Length; n++, index++, i++)
                    {
                        bufferSpan[index] = dataSpan[i];
                    }
                }
                else if (count > 128)
                {
                    count = -(count - 256) + 1;
                    bufferSpan.Slice(index, count).Fill(dataSpan[i]);
                    i++;
                    index += count;
                }
            }

            ArrayPool<byte>.Shared.Return(data);
        }
    }
}
