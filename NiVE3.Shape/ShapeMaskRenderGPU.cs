using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ComputeSharp;
using NiVE3.Image.Drawing;
using NiVE3.Image;
using NiVE3.Shape.Internal;
using NiVE3.Shape.Internal.Shader;

namespace NiVE3.Shape
{
    // NOTE: マスクはNonZeroのみ
    public static class ShapeMaskRenderGPU
    {
        const int SuperSamplingCount = 8;

        public static void Fill(GraphicsDevice device, Polygon[] polygons, GPURasterizedMaskImage image, float opacity, float offsetX = 0.0F, float offsetY = 0.0F)
        {
            if (polygons.Length < 1)
            {
                return;
            }

            var (startX, width, startY, height, lineHitIndices, lineHits) = GetHitLines(device, polygons, image.Width, image.Height, SuperSamplingCount, offsetX, offsetY);
            if (width < 1 || height < 1 || lineHitIndices == null || lineHits == null)
            {
                lineHitIndices?.Dispose();
                lineHits?.Dispose();
                return;
            }

            device.For(
                width,
                height,
                new MaskFillAntiAliased(
                    image.Data,
                    image.Width,
                    lineHits,
                    lineHitIndices,
                    SuperSamplingCount,
                    offsetX + 1.0F,
                    opacity,
                    startX,
                    startY
                )
            );

            lineHitIndices.Dispose();
            lineHits.Dispose();
        }

        public static void FillAliased(GraphicsDevice device, Polygon[] polygons, GPURasterizedMaskImage image, float opacity, float offsetX = 0.0F, float offsetY = 0.0F)
        {
            if (polygons.Length < 1)
            {
                return;
            }

            var (startX, width, startY, height, lineHitIndices, lineHits) = GetHitLines(device, polygons, image.Width, image.Height, 1, offsetX - 1.0F, offsetY);
            if (width < 1 || height < 1 || lineHitIndices == null || lineHits == null)
            {
                lineHitIndices?.Dispose();
                lineHits?.Dispose();
                return;
            }

            device.For(
                width,
                height,
                new MaskFillAliased(
                    image.Data,
                    image.Width,
                    lineHits,
                    lineHitIndices,
                    offsetX,
                    opacity,
                    startX,
                    startY
                )
            );

            lineHitIndices.Dispose();
            lineHits.Dispose();
        }

        static (int minX, int width, int minY, int height, ReadOnlyBuffer<Int2>? lineHitIndices, ReadOnlyBuffer<GPULineHit>? lineHits) GetHitLines(GraphicsDevice device, Polygon[] polygons, int imageWidth, int imageHeight, int superSamplingCount, float offsetX, float offsetY)
        {
            offsetX++;
            var minY = Math.Max((int)MathF.Floor(polygons.Min(p => p.MinY) - offsetY), 0);
            var maxY = Math.Min((int)MathF.Ceiling(polygons.Max(p => p.MaxY) - offsetY), imageHeight);
            var minX = Math.Max((int)MathF.Floor(polygons.Min(p => p.MinX) - offsetX) - 1, 0);
            var maxX = Math.Min((int)MathF.Ceiling(polygons.Max(p => p.MaxX) - offsetX) + 1, imageWidth);
            if (minY >= maxY)
            {
                return (0, 0, 0, 0, null, null);
            }

            var lineHits = new List<GPULineHit>[(maxY - minY) * superSamplingCount];
            var samplingRate = 1.0F / superSamplingCount;
            Parallel.For(minY, maxY, h =>
            {
                var li = (h - minY) * superSamplingCount;

                for (var s = 0; s < superSamplingCount; s++, li++)
                {
                    var max = float.MinValue;
                    var hitLine = new List<GPULineHit>();
                    for (var i = 0; i < polygons.Length; i++)
                    {
                        var y = h + samplingRate * s + offsetY;
                        if (polygons[i].MinY <= y && polygons[i].MaxY >= y)
                        {
                            var ls = polygons[i].Lines;
                            for (var n = 0; n < ls.Length; n++)
                            {
                                var p = ls[n].GetCrossHorizonalPositionAndDirection(y);
                                if (p.Value > float.MinValue)
                                {
                                    max = Math.Max(max, p.Value);
                                    hitLine.Add(new GPULineHit(p.Value, p.IsDown));
                                }
                            }
                        }
                    }

                    if (max > offsetX - 2.0F)
                    {
                        hitLine.Sort();
                        lineHits[li] = hitLine;
                    }
                    else
                    {
                        lineHits[li] = [];
                    }
                }
            });

            var lineHitCount = lineHits.Sum(l => l.Count);
            if (lineHitCount < 1)
            {
                return (0, 0, 0, 0, null, null);
            }

            using var lineHitIndicesUploadBuffer = device.AllocateUploadBuffer<Int2>(lineHits.Length);
            using var lineHitsUploadBuffer = device.AllocateUploadBuffer<GPULineHit>(lineHits.Sum(l => l.Count));

            var headIndex = 0;
            var lineHitIndicesUploadBufferSpan = lineHitIndicesUploadBuffer.Span;
            var lineHitsUploadBufferSpan = lineHitsUploadBuffer.Span;
            for (var i = 0; i < lineHits.Length; i++)
            {
                var prev = headIndex;
                lineHits[i].CopyTo(lineHitsUploadBufferSpan[prev..]);
                headIndex += lineHits[i].Count;
                lineHitIndicesUploadBufferSpan[i] = new Int2(prev, headIndex);
            }

            var lineHitIndicesBuffer = device.AllocateReadOnlyBuffer<Int2>(lineHitIndicesUploadBuffer.Length);
            var lineHitsBuffer = device.AllocateReadOnlyBuffer<GPULineHit>(lineHitsUploadBuffer.Length);
            lineHitIndicesUploadBuffer.CopyTo(lineHitIndicesBuffer);
            lineHitsUploadBuffer.CopyTo(lineHitsBuffer);

            return (minX, maxX - minX, minY, maxY - minY, lineHitIndicesBuffer, lineHitsBuffer);
        }
    }
}
