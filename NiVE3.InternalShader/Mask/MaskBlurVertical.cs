using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using ComputeSharp;

namespace NiVE3.InternalShader.Mask
{
    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct MaskBlurVertical(ReadWriteBuffer<float> mask, ReadWriteBuffer<float> result, int width, int height, float amount) : IComputeShader
    {
        public void Execute()
        {
            var range = (int)Hlsl.Floor(amount);
            var x = ThreadIds.X;
            var y = ThreadIds.Y;

            var a = 0.0F;
            for (var i = -range; i <= range; i++)
            {
                a += GetPixel(x, y + i);
            }

            var edge = (int)Hlsl.Ceil(amount);
            if (edge != range)
            {
                var fz = amount - range;
                a += GetPixel(x, y - edge) * fz;
                a += GetPixel(x, y + edge) * fz;
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

        float GetPixel(int x, int t)
        {
            if (t > -1 && t < height)
            {
                return mask[t * width + x];
            }
            else
            {
                return 0.0F;
            }
        }
    }
}
