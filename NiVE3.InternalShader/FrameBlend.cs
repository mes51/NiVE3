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
    readonly partial struct FrameBlend(ReadWriteBuffer<Float4> baseImage, ReadWriteBuffer<Float4> blendTargetImage, int width, float blendRate, float iBlendRate) : IComputeShader
    {
        public void Execute()
        {
            var pos = ThreadIds.Y * width + ThreadIds.X;
            baseImage[pos] = baseImage[pos] * iBlendRate +  blendTargetImage[pos] * blendRate;
        }
    }
}
