using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ComputeSharp;

namespace NiVE3.InternalShader.Mask
{
    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct MaskImage(ReadWriteBuffer<Float4> image, int width, ReadWriteBuffer<float> mask, int maskWidth, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            var imagePos = ((startY + ThreadIds.Y) * width) + startX + ThreadIds.X;
            var maskPos = ThreadIds.Y * maskWidth + ThreadIds.X;

            image[imagePos].W *= mask[maskPos];
        }
    }
}
