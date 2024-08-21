using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ComputeSharp;

namespace NiVE3.InternalShader.MotionBlur
{
    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    public readonly partial struct UnPremultiply(ReadWriteBuffer<Float4> image, int width) : IComputeShader
    {
        public void Execute()
        {
            var pos = ThreadIds.Y * width + ThreadIds.X;
            var color = image[pos];
            if (color.W > 0.0F)
            {
                image[pos] = new Float4(color.XYZ / color.W, color.W);
            }
        }
    }
}
