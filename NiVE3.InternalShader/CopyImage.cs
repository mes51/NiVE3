using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ComputeSharp;

namespace NiVE3.InternalShader
{
    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct CopyImage(ReadWriteBuffer<Float4> src, ReadWriteBuffer<Float4> dst, int srcWidth, int dstWidth, int dstHeight, int dstStartX, int dstStartY) : IComputeShader
    {
        public void Execute()
        {
            var dstX = ThreadIds.X + dstStartX;
            var dstY = ThreadIds.Y + dstStartY;
            if (dstX < 0 || dstX >= dstWidth || dstY < 0 || dstY >= dstHeight)
            {
                return;
            }

            dst[dstY * dstWidth + dstX] = src[ThreadIds.Y * srcWidth + ThreadIds.X];
        }
    }
}
