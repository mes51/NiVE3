using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ComputeSharp;

namespace NiVE3.PresetPlugin.Internal.Drawing.ComputeShader
{
    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct FillColor(ReadWriteBuffer<Float4> image, int width, Float4 color) : IComputeShader
    {
        public void Execute()
        {
            image[ThreadIds.Y * width + ThreadIds.X] = color;
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct FillMask(ReadWriteBuffer<float> mask, int width, float alpha) : IComputeShader
    {
        public void Execute()
        {
            mask[ThreadIds.Y * width + ThreadIds.X] = alpha;
        }
    }
}
