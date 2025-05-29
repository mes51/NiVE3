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
    readonly partial struct EvenOddAliasedSolid(
        ReadWriteBuffer<Float4> image,
        int imageWidth,
        ReadOnlyBuffer<GPULineHit> lineHits,
        ReadOnlyBuffer<Int2> lineHitIndices,
        float offsetX,
        Float4 color,
        int blendMode,
        int startX,
        int startY
    ) : IComputeShader
    {
        public void Execute()
        {
            var px = ThreadIds.X + startX;
            var py = ThreadIds.Y + startY;

            if (IsHit(ThreadIds.X, ThreadIds.Y))
            {
                var pos = py * imageWidth + px;
                image[pos] = BlendMethods.Process(blendMode, image[pos], color);
            }
        }

        bool IsHit(int tx, int ty)
        {
            var x = tx + startX + offsetX;

            var lineHitIndexBegin = lineHitIndices[ty].X;
            var lineHitIndexEnd = lineHitIndices[ty].Y;
            if (lineHitIndexEnd - lineHitIndexBegin < 1)
            {
                return false;
            }

            var inout = false;
            for (var li = lineHitIndexBegin; li < lineHitIndexEnd; li++)
            {
                var lineHit = lineHits[li];
                var value = lineHit.Value;
                if (value > x)
                {
                    break;
                }

                inout = !inout;
            }

            return inout;
        }
    }
}
