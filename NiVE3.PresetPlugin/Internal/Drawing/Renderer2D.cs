using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Image;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Struct;

namespace NiVE3.PresetPlugin.Internal.Drawing
{
    class Renderer2D
    {
        static readonly Vector4 EmptyPixel = new Vector4(255.0F, 255.0F, 255.0F, 0.0F);

        NManagedImage Target { get; }

        public Renderer2D(NManagedImage target)
        {
            Target = target;
        }

        public void Draw(NImage image, float opacity, Matrix3x3 transform, ImageInterpolationQuality interpolationQuality, BlendMode blendMode)
        {
            if (!Matrix3x3.Invert(transform, out var inverted))
            {
                return;
            }

            var managedImage = image switch
            {
                NCudaImage cudaImage => cudaImage.CopyToCpu(),
                _ => (NManagedImage)image
            };

            var p1 = transform.Transform(new Vector2());
            var p2 = transform.Transform(new Vector2(image.Width, 0.0F));
            var p3 = transform.Transform(new Vector2(image.Width, image.Height));
            var p4 = transform.Transform(new Vector2(0.0F, image.Height));
            var minX = Math.Max((int)Math.Floor(Math.Min(Math.Min(Math.Min(p1.X, p2.X), p3.X), p4.X)), 0);
            var minY = Math.Max((int)Math.Floor(Math.Min(Math.Min(Math.Min(p1.Y, p2.Y), p3.Y), p4.Y)), 0);
            var maxX = Math.Min((int)Math.Ceiling(Math.Max(Math.Max(Math.Max(p1.X, p2.X), p3.X), p4.X)), Target.Width);
            var maxY = Math.Min((int)Math.Ceiling(Math.Max(Math.Max(Math.Max(p1.Y, p2.Y), p3.Y), p4.Y)), Target.Height);

            var width = Target.Width;
            Parallel.For(minY, maxY, y =>
            {
                var targetData = MemoryMarshal.Cast<float, Vector4>(Target.GetDataSpan());
                var imageData = MemoryMarshal.Cast<float, Vector4>(managedImage.GetDataSpan());
                for (int x = minX, pos = y * Target.Width + minX; x < maxX; x++, pos++)
                {
                    var (imageX, imageY) = inverted.Transform(x, y);
                    var p = interpolationQuality switch
                    {
                        ImageInterpolationQuality.Level2 => ImageInterpolation.Bilinear(imageData, image.Width, image.Height, imageX, imageY),
                        _ => ImageInterpolation.NearestNeighbor(imageData, image.Width, image.Height, imageX, imageY)
                    };

                    p.W *= opacity;
                    if (p.W <= 0.0F)
                    {
                        continue;
                    }

                    Blend.Process(blendMode, targetData, p, pos);
                }
            });

            if (managedImage != image)
            {
                managedImage.Dispose();
            }
        }
    }
}
