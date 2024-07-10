using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ComputeSharp;

namespace NiVE3.PresetPlugin.Internal.Drawing.ComputeShader
{
    [ThreadGroupSize(DefaultThreadGroupSizes.X)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct FillColor(ReadWriteBuffer<Float4> image, Float4 color) : IComputeShader
    {
        public void Execute()
        {
            image[ThreadIds.X] = color;
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.X)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct FillMask(ReadWriteBuffer<float> mask, float alpha) : IComputeShader
    {
        public void Execute()
        {
            mask[ThreadIds.X] = alpha;
        }
    }
}
