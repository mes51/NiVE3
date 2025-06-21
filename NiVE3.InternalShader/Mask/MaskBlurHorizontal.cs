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
    readonly partial struct MaskBlurHorizontal(ReadWriteBuffer<float> mask, ReadWriteBuffer<float> result, int width, float amount) : IComputeShader
    {
        public void Execute()
        {
            var range = (int)Hlsl.Floor(amount);
            var x = ThreadIds.X;
            var y = ThreadIds.Y;

            var a = 0.0F;
            for (var i = -range; i <= range; i++)
            {
                a += GetPixel(x + i, y);
            }

            var edge = (int)Hlsl.Ceil(amount);
            if (edge != range)
            {
                var fz = amount - range;
                a += GetPixel(x - edge, y) * fz;
                a += GetPixel(x + edge, y) * fz;
            }

            if (a > 0.0F)
            {
                result[y * width + x] = a / (amount * 2.0F + 1.0F);
            }
            else
            {
                result[y * width + x] = 0.0F;
            }
        }

        float GetPixel(int l, int y)
        {
            if (l > -1 && l < width)
            {
                return mask[y * width + l];
            }
            else
            {
                return 0.0F;
            }
        }
    }
}
