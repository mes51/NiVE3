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
            var x = px + offsetX;

            var lineHitIndexBegin = lineHitIndices[ThreadIds.Y].X;
            var lineHitIndexEnd = lineHitIndices[ThreadIds.Y].Y;
            if (lineHitIndexEnd - lineHitIndexBegin < 1)
            {
                return;
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

            if (inout)
            {
                var pos = py * imageWidth + px;
                image[pos] = BlendMethods.Process(blendMode, image[pos], new Float4(color.XYZ, 1.0F));
            }
        }
    }
}
