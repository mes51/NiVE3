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
    readonly partial struct BlendTwoFrame(ReadWriteBuffer<Float4> baseImage, ReadWriteBuffer<Float4> blendTargetImage, int width, float blendRate, float iBlendRate) : IComputeShader
    {
        public void Execute()
        {
            var pos = ThreadIds.Y * width + ThreadIds.X;
            baseImage[pos] = baseImage[pos] * iBlendRate +  blendTargetImage[pos] * blendRate;
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct SumBlendFrame(ReadWriteBuffer<Float4> baseImage, ReadWriteBuffer<Float4> addFrame, int width, float rate) : IComputeShader
    {
        public void Execute()
        {
            var pos = ThreadIds.Y * width + ThreadIds.X;
            baseImage[pos] += addFrame[pos] * rate;
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct SumBlendResult(ReadWriteBuffer<Float4> baseImage, int width, float rate) : IComputeShader
    {
        public void Execute()
        {
            var pos = ThreadIds.Y * width + ThreadIds.X;
            baseImage[pos] /= rate;
        }
    }
}
