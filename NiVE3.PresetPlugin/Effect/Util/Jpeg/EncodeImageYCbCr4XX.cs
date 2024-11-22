using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Shared.Extension;

namespace NiVE3.PresetPlugin.Effect.Util.Jpeg
{
    class EncodeImageYCbCr4XX : EncodeImageBase
    {
        const int GroupSize = 2;

        const int ChannelCount = 3;

        protected override int[][] InterleaveGroupMap { get; }

        protected override HuffmanTable[] DCHuffmanTables => [HuffmanTable.LuminanceDCTable, HuffmanTable.ChrominanceDCTable];

        protected override HuffmanTable[] ACHuffmanTables => [HuffmanTable.LuminanceACTable, HuffmanTable.ChrominanceACTable];

        int HorizontalSubSampling { get; }

        int VerticalSubSampling { get; }

        public EncodeImageYCbCr4XX(int width, int height, float quality, int horizontalSubSampling, int verticalSubSampling) : base(width, height)
        {
            HorizontalSubSampling = horizontalSubSampling;
            VerticalSubSampling = verticalSubSampling;

            QuantizeTables = [
                new QuantizeTable(QuantizeTable.LuminanceBaseQuantizeTable, quality),
                new QuantizeTable(QuantizeTable.ChrominanceBaseQuantizeTable, quality),
                new QuantizeTable(QuantizeTable.ChrominanceBaseQuantizeTable, quality),
            ];

            InterleaveGroupMap = [
                [0, 1, 1, ..Enumerable.Repeat(0, horizontalSubSampling - 1)],
                ..Enumerable.Range(0, verticalSubSampling - 1).Select(_ => new int[1])
            ];
        }

        public override void Compress(Vector4[] image)
        {
            var colCounts = Enumerable.Repeat(McuBlockCols, VerticalSubSampling - 1).Prepend(McuBlockCols + (int)MathF.Ceiling(McuBlockCols / (float)HorizontalSubSampling) * 2).ToArray();
            var totalBlocks = colCounts.RepeatInfinity().Take(McuBlockRows).Sum();
            var mcuData = ArrayPool<Mcu>.Shared.Rent(totalBlocks);
            var chrominanceWidth = Width / HorizontalSubSampling;
            var luminance = ArrayPool<float>.Shared.Rent(image.Length);
            var chrominanceB = ArrayPool<float>.Shared.Rent((int)MathF.Ceiling(image.Length / (float)(HorizontalSubSampling * VerticalSubSampling)));
            var chrominanceR = ArrayPool<float>.Shared.Rent((int)MathF.Ceiling(image.Length / (float)(HorizontalSubSampling * VerticalSubSampling)));

            Parallel.For(0, Height, y =>
            {
                var chrominanceLine = y / VerticalSubSampling * chrominanceWidth;
                var isChrominanceSamplingLine = y % VerticalSubSampling == 0;
                for (int x = 0, pos = y * Width; x < Width; x++, pos++)
                {
                    var ycbcr = ToYCbCr(image[pos]) * 255.0F - new Vector4(127.0F);
                    luminance[pos] = ycbcr.X;
                    if (isChrominanceSamplingLine && x % HorizontalSubSampling == 0)
                    {
                        var cpos = chrominanceLine + x / HorizontalSubSampling;
                        chrominanceB[cpos] = ycbcr.Y;
                        chrominanceR[cpos] = ycbcr.Z;
                    }
                }
            });

            Parallel.For(0, McuBlockRows, row =>
            {
                Span<float> channelData = stackalloc float[Mcu.BlockSize];
                Span<float> dct = stackalloc float[Mcu.BlockSize];

                var mline = colCounts.RepeatInfinity().Take(row).Sum();
                var isYOnlyRow = row % VerticalSubSampling != 0;
                for (var col = 0; col < McuBlockCols; col++)
                {
                    channelData.Clear();
                    for (int y = 0, imageY = row * Mcu.LineSize; y < Mcu.LineSize && imageY < Height; y++, imageY++)
                    {
                        var imageX = col * Mcu.LineSize;
                        var pos = y * Mcu.LineSize;
                        var ipos = imageY * Width + imageX;
                        for (var x = 0; x < Mcu.LineSize && imageX < Width; x++, pos++, ipos++, imageX++)
                        {
                            channelData[pos] = luminance[ipos];
                        }
                    }
                    FillOutSideBlock(channelData, Width, Height, row, col);

                    var isYOnlyCol = col % HorizontalSubSampling != 0;
                    var chrominanceCol = col / HorizontalSubSampling;
                    var mpos = mline + col + (isYOnlyRow ? 0 : chrominanceCol * 2 + (isYOnlyCol ? 2 : 0));
                    Dct(channelData, dct);
                    Quantize(dct, QuantizeTables[0].GetVectorTable(), ref mcuData[mpos]);
                    Mcu.ReOrderZigZag(ref mcuData[mpos]);

                    if (!isYOnlyRow && !isYOnlyCol)
                    {
                        channelData.Clear();
                        for (int y = 0, imageY = row / VerticalSubSampling * Mcu.LineSize; y < Mcu.LineSize && imageY < Height; y++, imageY++)
                        {
                            var imageX = chrominanceCol * Mcu.LineSize;
                            var pos = y * Mcu.LineSize;
                            var ipos = imageY * chrominanceWidth + imageX;
                            for (var x = 0; x < Mcu.LineSize && imageX < chrominanceWidth; x++, pos++, ipos++, imageX++)
                            {
                                channelData[pos] = chrominanceB[ipos];
                            }
                        }
                        FillOutSideBlock(channelData, chrominanceWidth, Height, row, chrominanceCol);
                        mpos++;
                        Dct(channelData, dct);
                        Quantize(dct, QuantizeTables[1].GetVectorTable(), ref mcuData[mpos]);
                        Mcu.ReOrderZigZag(ref mcuData[mpos]);

                        channelData.Clear();
                        for (int y = 0, imageY = row / VerticalSubSampling * Mcu.LineSize; y < Mcu.LineSize && imageY < Height; y++, imageY++)
                        {
                            var imageX = chrominanceCol * Mcu.LineSize;
                            var pos = y * Mcu.LineSize;
                            var ipos = imageY * chrominanceWidth + imageX;
                            for (var x = 0; x < Mcu.LineSize && imageX < chrominanceWidth; x++, pos++, ipos++, imageX++)
                            {
                                channelData[pos] = chrominanceR[ipos];
                            }
                        }
                        FillOutSideBlock(channelData, chrominanceWidth, Height, row, chrominanceCol);
                        mpos++;
                        Dct(channelData, dct);
                        Quantize(dct, QuantizeTables[2].GetVectorTable(), ref mcuData[mpos]);
                        Mcu.ReOrderZigZag(ref mcuData[mpos]);
                    }
                }
            });

            (EncodedDataLength, EncodedData) = Encode(mcuData, McuBlockRows, colCounts, InterleaveGroupMap, DCHuffmanTables, ACHuffmanTables);

            ArrayPool<Mcu>.Shared.Return(mcuData, true);
            ArrayPool<float>.Shared.Return(luminance);
            ArrayPool<float>.Shared.Return(chrominanceB);
            ArrayPool<float>.Shared.Return(chrominanceR);
        }

        public override void Decompress(Vector4[] image)
        {
            var colCounts = Enumerable.Repeat(McuBlockCols, VerticalSubSampling - 1).Prepend(McuBlockCols + (int)MathF.Ceiling(McuBlockCols / (float)HorizontalSubSampling) * 2).ToArray();
            var totalBlocks = colCounts.RepeatInfinity().Take(McuBlockRows).Sum();
            var mcuData = Decode(EncodedData, McuBlockRows, colCounts, InterleaveGroupMap, DCHuffmanTables, ACHuffmanTables);
            var chrominanceWidth = Width / HorizontalSubSampling;
            var luminance = ArrayPool<float>.Shared.Rent(image.Length);
            var chrominanceB = ArrayPool<float>.Shared.Rent((int)MathF.Ceiling(image.Length / (float)HorizontalSubSampling));
            var chrominanceR = ArrayPool<float>.Shared.Rent((int)MathF.Ceiling(image.Length / (float)HorizontalSubSampling));

            Parallel.For(0, McuBlockRows, row =>
            {
                Span<float> channelData = stackalloc float[Mcu.BlockSize];
                Span<float> dct = stackalloc float[Mcu.BlockSize];

                var mline = colCounts.RepeatInfinity().Take(row).Sum();
                var isYOnlyRow = row % VerticalSubSampling != 0;
                for (var col = 0; col < McuBlockCols; col++)
                {
                    channelData.Clear();

                    var isYOnlyCol = col % HorizontalSubSampling != 0;
                    var chrominanceCol = col / HorizontalSubSampling;
                    var mpos = mline + col + (isYOnlyRow ? 0 : chrominanceCol * 2 + (isYOnlyCol ? 2 : 0));

                    Dequantize(ref mcuData[mpos], QuantizeTables[0].GetVectorTable(), dct);
                    IDct(dct, channelData);
                    for (int y = 0, imageY = row * Mcu.LineSize; y < Mcu.LineSize && imageY < Height; y++, imageY++)
                    {
                        var imageX = col * Mcu.LineSize;
                        var ipos = y * Mcu.LineSize;
                        var pos = imageY * Width + imageX;
                        for (var x = 0; x < Mcu.LineSize && imageX < Width; x++, imageX++, pos++, ipos++)
                        {
                            luminance[pos] = channelData[ipos];
                        }
                    }

                    if (!isYOnlyRow && !isYOnlyCol)
                    {
                        channelData.Clear();
                        mpos++;
                        Dequantize(ref mcuData[mpos], QuantizeTables[1].GetVectorTable(), dct);
                        IDct(dct, channelData);
                        for (int y = 0, imageY = row / VerticalSubSampling * Mcu.LineSize; y < Mcu.LineSize && imageY < Height; y++, imageY++)
                        {
                            var imageX = chrominanceCol * Mcu.LineSize;
                            var ipos = y * Mcu.LineSize;
                            var pos = imageY * chrominanceWidth + imageX;
                            for (var x = 0; x < Mcu.LineSize && imageX < chrominanceWidth; x++, imageX++, pos++, ipos++)
                            {
                                chrominanceB[pos] = channelData[ipos];
                            }
                        }

                        channelData.Clear();
                        mpos++;
                        Dequantize(ref mcuData[mpos], QuantizeTables[2].GetVectorTable(), dct);
                        IDct(dct, channelData);
                        for (int y = 0, imageY = row / VerticalSubSampling * Mcu.LineSize; y < Mcu.LineSize && imageY < Height; y++, imageY++)
                        {
                            var imageX = chrominanceCol * Mcu.LineSize;
                            var ipos = y * Mcu.LineSize;
                            var pos = imageY * chrominanceWidth + imageX;
                            for (var x = 0; x < Mcu.LineSize && imageX < chrominanceWidth; x++, imageX++, pos++, ipos++)
                            {
                                chrominanceR[pos] = channelData[ipos];
                            }
                        }
                    }
                }
            });

            Parallel.For(0, Height, y =>
            {
                var chrominanceLine = y / VerticalSubSampling * chrominanceWidth;
                for (int x = 0, pos = y * Width; x < Width; x++, pos++)
                {
                    var cpos = chrominanceLine + x / HorizontalSubSampling;
                    var ycbcr = Vector4.Min(Vector4.Max(new Vector4(luminance[pos], chrominanceB[cpos], chrominanceR[cpos], 0.0F) + new Vector4(127.0F), Vector4.Zero), new Vector4(255.0F)) / 255.0F;
                    image[pos] = ToRgb(ycbcr);
                }
            });

            ArrayPool<Mcu>.Shared.Return(mcuData, true);
            ArrayPool<float>.Shared.Return(luminance);
            ArrayPool<float>.Shared.Return(chrominanceB);
            ArrayPool<float>.Shared.Return(chrominanceR);
        }
    }
}
