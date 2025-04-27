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
    public readonly partial struct BlendSubFrame(ReadWriteBuffer<Float4> image, ReadWriteBuffer<Float4> frame, int width, float frameBlendRatio) : IComputeShader
    {
        public void Execute()
        {
            var pos = ThreadIds.Y * width + ThreadIds.X;
            var color = frame[pos];
            var alpha = frame[pos].W * frameBlendRatio;
            image[pos] += new Float4(color.XYZ * alpha, alpha);
        }
    }
}
