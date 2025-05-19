using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ComputeSharp;

namespace NiVE3.Shape.Internal.Shader
{
    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct MaskFillAntiAliased(
        ReadWriteBuffer<float> image,
        int imageWidth,
        ReadOnlyBuffer<GPULineHit> lineHits,
        ReadOnlyBuffer<Int2> lineHitIndices,
        int superSamplingCount,
        float offsetX,
        float opacity,
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

            var area = CalcArea(ThreadIds.X, ThreadIds.Y);
            if (area > 0.0F)
            {
                var pos = py * imageWidth + px;
                image[pos] = MaskBlendMethods.Process(blendMode, image[pos], opacity * area);
            }
        }

        float CalcArea(int tx, int ty)
        {
            var x = tx + startX + offsetX;
            var ly = ty * superSamplingCount;
            var totalArea = 0.0F;
            for (var s = 0; s < superSamplingCount; s++)
            {
                var lineHitIndexBegin = lineHitIndices[ly + s].X;
                var lineHitIndexEnd = lineHitIndices[ly + s].Y;
                if (lineHitIndexEnd - lineHitIndexBegin < 1)
                {
                    continue;
                }

                var depth = 0;
                var dir = false;
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
                    if (depth > 0)
                    {
                        if (dir != lineHit.IsDown)
                        {
                            depth--;
                        }
                        else
                        {
                            depth++;
                        }
                    }
                    else
                    {
                        dir = lineHit.IsDown;
                        depth++;
                    }

                    if (prev != depth > 0)
                    {
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
                        prev = depth > 0;
                    }
                }

                totalArea += area;
            }

            return totalArea * SamplingRate;
        }
    }
}
