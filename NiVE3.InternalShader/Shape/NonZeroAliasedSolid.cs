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
            var px = ThreadIds.X + startX;
            var py = ThreadIds.Y + startY;

            var x = px + offsetX;
            var ly = ThreadIds.Y;

            var lineHitIndexBegin = lineHitIndices[ly].X;
            var lineHitIndexEnd = lineHitIndices[ly].Y;
            if (lineHitIndexEnd - lineHitIndexBegin < 1)
            {
                return;
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
            }

            if (depth > 0)
            {
                var pos = py * imageWidth + px;
                image[pos] = BlendMethods.Process(blendMode, image[pos], new Float4(color.XYZ, 1.0F));
            }
        }
    }
}
