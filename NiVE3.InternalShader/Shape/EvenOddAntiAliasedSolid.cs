using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ComputeSharp;

namespace NiVE3.InternalShader.Shape
{
    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct EvenOddAntiAliasedSolid(
        ReadWriteBuffer<Float4> image,
        int imageWidth,
        ReadOnlyBuffer<GPULineHit> lineHits,
        ReadOnlyBuffer<Int2> lineHitIndices,
        int superSamplingCount,
        float offsetX,
        Float4 color,
        int blendMode,
        int startX,
        int startY
    ) : IComputeShader
    {
        readonly float SamplingRate = 1.0F / superSamplingCount;

        public void Execute()
        {
            var px = ThreadIds.X + startX;
            var py = ThreadIds.Y + startY;

            var x = px + offsetX;
            var ly = ThreadIds.Y * superSamplingCount;
            var totalArea = 0.0F;
            for (var s = 0; s < superSamplingCount; s++)
            {
                var lineHitIndexBegin = lineHitIndices[ly + s].X;
                var lineHitIndexEnd = lineHitIndices[ly + s].Y;
                if (lineHitIndexEnd - lineHitIndexBegin < 1)
                {
                    continue;
                }

                var prev = false;
                var area = 0.0F;
                for (var li = lineHitIndexBegin; li < lineHitIndexEnd; li++)
                {
                    var lineHit = lineHits[li];
                    var value = lineHit.Value;
                    if (value > x)
                    {
                        break;
                    }

                    var d = x - value;
                    if (d < 1.0F && d > 0.0F)
                    {
                        var diff = (1.0F - (value - (int)Hlsl.Floor(value))) * (prev ? -1.0F : 1.0F);
                        area += diff;
                    }
                    else
                    {
                        if (prev && value < x)
                        {
                            area = 0.0F;
                        }
                        else
                        {
                            area = 1.0F;
                        }
                    }
                    prev = !prev;
                }

                totalArea += area;
            }

            var pos = py * imageWidth + px;
            image[pos] = BlendMethods.Process(blendMode, image[pos], new Float4(color.XYZ, totalArea * SamplingRate));
        }
    }
}
