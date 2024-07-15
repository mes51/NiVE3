using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ComputeSharp;

namespace NiVE3.InternalShader
{
    [ThreadGroupSize(DefaultThreadGroupSizes.X)]
    [GeneratedComputeShaderDescriptor]
    public readonly partial struct ConvertToPreviewImage(ReadWriteBuffer<Float4> image, ReadWriteBuffer<int> result, int mode) : IComputeShader
    {
        public void Execute()
        {
            var pixel = Hlsl.Round(Hlsl.Clamp(image[ThreadIds.X], 0.0F, 1.0F) * 255.0F);
            var converted = new Int4();

            switch (mode)
            {
                case 1:
                    converted = new Int4((Int3)pixel.ZZZ, 255);
                    break;
                case 2:
                    converted = new Int4((Int3)pixel.YYY, 255);
                    break;
                case 3:
                    converted = new Int4((Int3)pixel.XXX, 255);
                    break;
                case 4:
                    converted = new Int4((Int3)pixel.WWW, 255);
                    break;
                case 5:
                    converted = new Int4((Int3)(pixel.XYZ * pixel.W / 255.0F), 255);
                    break;
                default:
                    converted = (Int4)pixel;
                    break;
            }

            result[ThreadIds.X] = (converted.X & 0xFF) | (converted.Y & 0xFF) << 8 | (converted.Z & 0xFF) << 16 | (converted.W & 0xFF) << 24;
        }
    }
}
