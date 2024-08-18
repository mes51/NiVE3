using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.PresetPlugin.Internal.Effect.Jpeg
{
    class EncodeImageRgb : EncodeImageBase
    {
        const int ChannelCount = 3;

        protected override int[][] InterleaveGroupMap => [[0]];

        protected override HuffmanTable[] DCHuffmanTables => [HuffmanTable.LuminanceDCTable];

        protected override HuffmanTable[] ACHuffmanTables => [HuffmanTable.LuminanceACTable];

        public EncodeImageRgb(int width, int height, float quality) : base(width, height)
        {
            QuantizeTables = [
                new QuantizeTable(QuantizeTable.LuminanceBaseQuantizeTable, quality),
                new QuantizeTable(QuantizeTable.LuminanceBaseQuantizeTable, quality),
                new QuantizeTable(QuantizeTable.LuminanceBaseQuantizeTable, quality),
            ];
        }

        public override void Compress(Vector4[] image)
        {
            var mcuData = ArrayPool<Mcu>.Shared.Rent(McuBlockRows * McuBlockCols * ChannelCount);
            Parallel.For(0, McuBlockRows, row =>
            {
                Span<float> imageData = stackalloc float[Mcu.BlockSize * 4];
                Span<float> channelData = stackalloc float[Mcu.BlockSize];
                Span<float> dct = stackalloc float[Mcu.BlockSize];
                var imageDataVector = MemoryMarshal.Cast<float, Vector4>(imageData);
                var imageDataVector256 = MemoryMarshal.Cast<float, Vector256<float>>(imageData);

                for (var col = 0; col < McuBlockCols; col++)
                {
                    imageData.Clear();
                    channelData.Clear();

                    for (int y = 0, imageY = row * Mcu.LineSize; y < Mcu.LineSize && imageY < Height; y++, imageY++)
                    {
                        var imageX = col * Mcu.LineSize;
                        image.AsSpan(imageY * Width + imageX, Math.Min(Mcu.LineSize, Width - imageX)).CopyTo(imageDataVector[(y * Mcu.LineSize)..]);
                    }
                    FillOutSideBlock(imageDataVector, Width, Height, row, col);

                    for (var i = 0; i < imageDataVector256.Length; i++)
                    {
                        imageDataVector256[i] = imageDataVector256[i] * 255.0F - Vector256.Create(127.0F);
                    }

                    for (int channel = 0, mpos = (row * McuBlockCols + col) * ChannelCount; channel < ChannelCount; channel++, mpos++)
                    {
                        for (int n = 0, ipos = channel; n < Mcu.BlockSize; n++, ipos += 4)
                        {
                            channelData[n] = imageData[ipos];
                        }

                        Dct(channelData, dct);
                        Quantize(dct, QuantizeTables[channel].GetVectorTable(), ref mcuData[mpos]);
                        Mcu.ReOrderZigZag(ref mcuData[mpos]);
                    }
                }
            });

            (EncodedDataLength, EncodedData) = Encode(mcuData, McuBlockRows, [McuBlockCols * ChannelCount], InterleaveGroupMap, DCHuffmanTables, ACHuffmanTables);

            ArrayPool<Mcu>.Shared.Return(mcuData, true);
        }

        public override void Decompress(Vector4[] image)
        {
            var mcuData = Decode(EncodedData, McuBlockRows, [McuBlockCols * ChannelCount], InterleaveGroupMap, DCHuffmanTables, ACHuffmanTables);

            Parallel.For(0, McuBlockRows, row =>
            {
                Span<float> imageData = stackalloc float[Mcu.BlockSize * 4];
                Span<float> channelData = stackalloc float[Mcu.BlockSize];
                Span<float> dct = stackalloc float[Mcu.BlockSize];
                var imageDataVector = MemoryMarshal.Cast<float, Vector4>(imageData);
                var imageDataVector256 = MemoryMarshal.Cast<float, Vector256<float>>(imageData);

                for (var col = 0; col < McuBlockCols; col++)
                {
                    imageData.Clear();
                    channelData.Clear();

                    for (int channel = 0, mpos = (row * McuBlockCols + col) * ChannelCount; channel < ChannelCount; channel++, mpos++)
                    {
                        Dequantize(ref mcuData[mpos], QuantizeTables[channel].GetVectorTable(), dct);
                        IDct(dct, channelData);

                        for (int n = 0, ipos = channel; n < Mcu.BlockSize; n++, ipos += 4)
                        {
                            imageData[ipos] = channelData[n];
                        }
                    }

                    for (var i = 0; i < imageDataVector256.Length; i++)
                    {
                        imageDataVector256[i] = Vector256.Min(Vector256.Max(imageDataVector256[i] + Vector256.Create(127.0F), Vector256<float>.Zero), Vector256.Create(255.0F)) / 255.0F;
                    }
                    for (var i = 3; i < imageData.Length; i += 4)
                    {
                        imageData[i] = 1.0F;
                    }

                    for (int y = 0, imageY = row * Mcu.LineSize; y < Mcu.LineSize && imageY < Height; y++, imageY++)
                    {
                        var imageX = col * Mcu.LineSize;
                        imageDataVector.Slice(y * Mcu.LineSize, Math.Min(Mcu.LineSize, Width - imageX)).CopyTo(image.AsSpan(imageY * Width + imageX));
                    }
                }
            });

            ArrayPool<Mcu>.Shared.Return(mcuData, true);
        }
    }
}
