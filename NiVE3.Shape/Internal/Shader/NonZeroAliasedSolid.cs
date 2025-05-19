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
    readonly partial struct NonZeroAliasedSolid(
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
            if (!IsHit(ThreadIds.X, ThreadIds.Y))
            {
                return;
            }

            var px = ThreadIds.X + startX;
            var py = ThreadIds.Y + startY;

            var pos = py * imageWidth + px;
            image[pos] = BlendMethods.Process(blendMode, image[pos], new Float4(color.XYZ, 1.0F));
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

            var depth = 0;
            var dir = false;
            for (var li = lineHitIndexBegin; li < lineHitIndexEnd; li++)
            {
                var lineHit = lineHits[li];
                var value = lineHit.Value;
                if (value > x)
                {
                    break;
                }

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
            }

            return depth > 0;
        }
    }
}
