using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ComputeSharp;

namespace NiVE3.PresetPlugin.Internal.ComputeShader.Input
{
    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct ConvertImageToBGRA128(ReadOnlyBuffer<int> src, ReadWriteBuffer<Float4> dst, int width) : IComputeShader
    {
        public void Execute()
        {
            var pos = ThreadIds.Y * width + ThreadIds.X;
            var srcPixel = src[pos];
            var dstColor = new Float4(
                srcPixel & 0xFF,
                (srcPixel >> 8) & 0xFF,
                (srcPixel >> 16) & 0xFF,
                (srcPixel >> 24) & 0xFF
            ) / 255.0F;
            if (dstColor.W <= 0.0F)
            {
                dstColor = Const.EmptyPixelFloat4;
            }
            dst[pos] = dstColor;
        }
    }
}
