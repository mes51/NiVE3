using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Numerics;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using NiVE3.PresetPlugin.Internal.IO;
using NiVE3.PresetPlugin.Internal.Psd.Structs;
using SharpGen.Runtime;

namespace NiVE3.PresetPlugin.Internal.Psd.Decoder
{
    static class ChannelDataDecoder
    {
        public static IChannelDataStream[] Empty(int channelDepth, int channelCount)
        {
            return [..Enumerable.Range(0, channelCount).Select(_ => new EmptyChannelDataStream(channelDepth))];
        }

        public static IChannelDataStream[] Raw(RandomAccessFileReader reader, in RectTLBR rect, int channelDepth, int channelCount, long begin)
        {
            var width = rect.Width;
            var result = new IChannelDataStream[channelCount];
            var alignedLineDataLength = channelDepth switch
            {
                1 => (int)MathF.Ceiling(width / 8.0F),
                _ => channelDepth / 8 * width,
            };

            var height = rect.Height;
            var channelLength = height * alignedLineDataLength;
            for (var i = 0; i < result.Length; i++)
            {
                var channelBegin = begin + channelLength * i;
                result[i] = new RawChannelDataStream(reader, width, height, channelDepth, channelBegin);
            }
            return result;
        }

        public static IChannelDataStream[] Rle(RandomAccessFileReader reader, in RectTLBR rect, int channelDepth, int channelCount, long begin, bool isPsb)
        {
            var byteCounts = new int[channelCount][];
            reader.Position = begin;
            var height = rect.Height;
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
                result[i] = new RleChannelDataStream(reader, rect.Width, height, channelDepth, currentBegin, byteCounts[i]);
                currentBegin += byteCounts[i].Sum();
            }

            return result;
        }

        public static IChannelDataStream[] Zip(RandomAccessFileReader reader, in RectTLBR rect, int channelDepth, int channelCount, long begin, long length, bool withPrediction)
        {
            var width = rect.Width;
            var result = new IChannelDataStream[channelCount];
            var alignedLineDataLength = channelDepth switch
            {
                1 => (int)MathF.Ceiling(width / 8.0F),
                _ => channelDepth / 8 * width,
            };

            var height = rect.Height;
            var channelLength = height * alignedLineDataLength;
            for (var i = 0; i < result.Length; i++)
            {
                var channelBegin = begin + channelLength * i;
                result[i] = new ZipChannelDataStream(reader, width, height, channelDepth, channelBegin, length, withPrediction);
            }
            return result;
        }
    }

    interface IChannelDataStream
    {
        float ReadChannel();

        byte[] ReadBytes();
    }

    file class EmptyChannelDataStream : IChannelDataStream
    {
        int ChannelDepth { get; }

        public EmptyChannelDataStream(int channelDepth)
        {
            ChannelDepth = channelDepth;
        }

        public byte[] ReadBytes()
        {
            switch (ChannelDepth)
            {
                case 16:
                    return [0, 0];
                case 32:
                    return [0, 0, 0, 0];
                default:
                    return [0];
            }
        }

        public float ReadChannel()
        {
            return 0.0F;
        }
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

            RleDecoder.Decode(Reader, Buffer, ByteCounts[CurrentLineIndex]);

            CurrentLinePixelIndex = 0;
            CurrentLineIndex++;
        }
    }

    file class ZipChannelDataStream : IChannelDataStream
    {
        RandomAccessReaderBase Reader { get; }

        int Width { get; }

        int Height { get; }

        int ChannelDepth { get; }

        long DataLength { get; }

        bool WithPrediction { get; }

        long TotalPixelCount { get; }

        int CurrentLinePixelIndex { get; set; }

        long ReadPixelCount { get; set; }

        byte CurrentBitReadingByte { get; set; }

        bool IsStreamEnded => DecompressedData == null || DecompressedData.Length <= DataPosition || ReadPixelCount >= TotalPixelCount;

        byte[]? DecompressedData { get; set; }

        int DataPosition { get; set; }

        public ZipChannelDataStream(RandomAccessFileReader reader, int width, int height, int channelDepth, long begin, long length, bool withPrediction)
        {
            Reader = reader.CreateSubReader(begin);
            Width = width;
            Height = height;
            ChannelDepth = channelDepth;
            DataLength = length;
            WithPrediction = withPrediction;
            TotalPixelCount = width * height;
        }

        public byte[] ReadBytes()
        {
            DecompressAndReorderData();

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
                            CurrentBitReadingByte = DecompressedData[DataPosition];
                            DataPosition++;
                        }

                        result = [(byte)(((CurrentBitReadingByte & (1 << bitCursor)) >> bitCursor) > 0 ? 255 : 0)];
                    }
                    break;
                case 8:
                    result = [DecompressedData[DataPosition]];
                    DataPosition++;
                    break;
                case 16:
                    {
                        result = [DecompressedData[DataPosition], DecompressedData[DataPosition + 1]];
                        DataPosition += 2;
                    }
                    break;
                case 32:
                    {
                        result = [
                            DecompressedData[DataPosition],
                            DecompressedData[DataPosition + 1],
                            DecompressedData[DataPosition + 2],
                            DecompressedData[DataPosition + 3]
                        ];
                        DataPosition += 4;
                    }
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

        public float ReadChannel()
        {
            DecompressAndReorderData();

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
                            CurrentBitReadingByte = DecompressedData[DataPosition];
                            DataPosition++;
                        }

                        result = ((CurrentBitReadingByte & (1 << bitCursor)) >> bitCursor) > 0 ? 1.0F : 0.0F;
                    }
                    break;
                case 8:
                    result = DecompressedData[DataPosition] / 255.0F;
                    DataPosition++;
                    break;
                case 16:
                    result = BitConverter.ToUInt16(DecompressedData.AsSpan(DataPosition, 2)) / 65535.0F;
                    DataPosition += 2;
                    break;
                case 32:
                    result = BitConverter.ToSingle(DecompressedData.AsSpan(DataPosition, 4));
                    DataPosition += 4;
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

        [MemberNotNull(nameof(DecompressedData))]
        void DecompressAndReorderData()
        {
            if (DecompressedData != null)
            {
                return;
            }

            using var stream = new ZLibStream(new RandomAccessReaderWrapper(Reader, DataLength), CompressionMode.Decompress);
            using var ms = new MemoryStream();
            Span<byte> buffer = new byte[4096]; //stackalloc byte[4096];
            var readCount = 0;
            while ((readCount = stream.Read(buffer)) > 0)
            {
                ms.Write(buffer[..readCount]);
            }

            DecompressedData = ms.ToArray();

            if (WithPrediction)
            {
                switch (ChannelDepth)
                {
                    case 16:
                        DecodeDeltaWithType<ushort>(DecompressedData, Width, Height);
                        break;
                    case 32:
                        DecodeDelta(DecompressedData, Width * sizeof(float), Height);
                        ReorderForSingle(DecompressedData, Width, Height);
                        break;
                    default:
                        DecodeDelta(DecompressedData, Width, Height);
                        break;
                }
            }
        }

        static void DecodeDelta(byte[] data, int rowLength, int height)
        {
            Parallel.For(0, height, y =>
            {
                var dataSpan = data.AsSpan(y * rowLength, rowLength);
                for (var x = 1; x < dataSpan.Length; x++)
                {
                    dataSpan[x] = unchecked((byte)(dataSpan[x - 1] + dataSpan[x]));
                }
            });
        }

        static void DecodeDeltaWithType<T>(byte[] data, int width, int height) where T : unmanaged, INumber<T>
        {
            var dataSize = Marshal.SizeOf<T>();
            var rowLength = width * dataSize;
            Parallel.For(0, height, y =>
            {
                var dataSpan = data.AsSpan(y * rowLength, rowLength);
                var castedSpan = MemoryMarshal.Cast<byte, T>(dataSpan);
                dataSpan[..dataSize].Reverse();
                for (var x = 1; x < castedSpan.Length; x++)
                {
                    dataSpan.Slice(x * dataSize, dataSize).Reverse();
                    castedSpan[x] = unchecked(castedSpan[x - 1] + castedSpan[x]);
                }
            });
        }

        static void ReorderForSingle(byte[] data, int width, int height)
        {
            const int ByteLength = sizeof(float);

            Parallel.For(0, height, y =>
            {
                var dataSpan = data.AsSpan(y * width * ByteLength, width * ByteLength);
                var temp = ArrayPool<byte>.Shared.Rent(dataSpan.Length);
                var tempSpan = temp.AsSpan(0, dataSpan.Length);
                tempSpan.Clear();

                for (var x = 0; x < width; x++)
                {
                    for (var b = 0; b < ByteLength; b++)
                    {
                        tempSpan[x * ByteLength + b] = dataSpan[width * b + x];
                    }
                }

                for (var i = 0; i < tempSpan.Length; i += ByteLength)
                {
                    tempSpan.Slice(i, ByteLength).Reverse();
                }

                tempSpan.CopyTo(dataSpan);

                ArrayPool<byte>.Shared.Return(temp);
            });
        }
    }

    file static class RleDecoder
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

    file class RandomAccessReaderWrapper : Stream
    {
        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

        public override long Length { get; }

        public override long Position
        {
            get => Math.Clamp(Reader.Position - BeginPosition, 0, Length);
            set => Reader.Position = Math.Clamp(value, 0, Length) + BeginPosition;
        }

        RandomAccessReaderBase Reader { get; }

        long BeginPosition { get; }

        public RandomAccessReaderWrapper(RandomAccessReaderBase reader, long length)
        {
            Reader = reader;
            BeginPosition = reader.Position;
            Length = length;
        }

        public override void Flush() { }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var limit = Math.Max(Math.Min(count, (int)(Length - Position)), 0);
            return Reader.Read(buffer.AsSpan(offset, limit));
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    Position = offset;
                    break;
                case SeekOrigin.Current:
                    Position += offset;
                    break;
                case SeekOrigin.End:
                    Position = Length - offset;
                    break;
            }

            return Position;
        }

        public override void SetLength(long value) { }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }
    }
}
