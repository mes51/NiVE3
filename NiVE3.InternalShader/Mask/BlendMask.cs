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
    readonly partial struct BlendMask(ReadWriteBuffer<float> back, ReadWriteBuffer<float> front, int width, float opacity, int blendMode, bool isInvert) : IComputeShader
    {
        public void Execute()
        {
            var pos = ThreadIds.Y * width + ThreadIds.X;

            var value = front[pos];
            if (isInvert)
            {
                value = 1.0F - value;
            }

            back[pos] = MaskBlendMethods.Process(blendMode, back[pos], value * opacity);
        }
    }
}
