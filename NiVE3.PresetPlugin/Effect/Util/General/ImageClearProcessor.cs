using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using ComputeSharp;
using NiVE3.Image;
using NiVE3.Plugin.ValueObject;
using NiVE3.PresetPlugin.Internal;

namespace NiVE3.PresetPlugin.Effect.Util.General
{
    static class ImageClearProcessor
    {
        public static void ClearCpu(NManagedImage image, ROI roi)
        {
            var imageWidth = image.Width;
            var imageData = image.Data;
            Parallel.For(roi.Top, roi.Bottom, y =>
            {
                imageData.AsSpan(y * imageWidth + roi.Left, roi.Width).Fill(Const.EmptyPixel);
            });
        }

        public static void ClearGpu(GraphicsDevice device, NGPUImage image, ROI roi)
        {
            device.For(roi.Width, roi.Height, new ImageClearProcess(image.Data, image.Width, roi.Left, roi.Top));
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct ImageClearProcess(ReadWriteBuffer<Float4> image, int width, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            image[(ThreadIds.Y + startY) * width + ThreadIds.X + startX] = Const.EmptyPixelFloat4;
        }
    }
}
