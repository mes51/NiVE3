using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ComputeSharp;
using NiVE3.Image;
using NiVE3.Image.Drawing;
using NiVE3.Numerics;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.ValueObject;
using NiVE3.PresetPlugin.Extension;
using NiVE3.PresetPlugin.Internal.Drawing.ComputeShader.Render2D;
using NiVE3.Shared.Extension;

namespace NiVE3.PresetPlugin.Internal.Drawing
{
    abstract class Renderer2DBase
    {
        private List<ImageInfo> Images { get; } = [];

        protected IReadOnlyList<ImageInfo> RenderImages => Images;

        public void AddImage(Int32Point roiOrigin, NImage image, float opacity, Matrix3x3 transform, ImageInterpolationQuality interpolationQuality, BlendMode blendMode, RasterizedMaskImage? trackMatte)
        {
            transform = Matrix3x3.CreateTranslate((float)-(roiOrigin.X + image.Origin.X), (float)-(roiOrigin.Y + image.Origin.Y)) * transform;
            if (!Matrix3x3.Invert(transform, out var inverted))
            {
                return;
            }

            Images.Add(new ImageInfo(image, opacity, transform, inverted, interpolationQuality, blendMode, trackMatte));
        }

        public void Draw()
        {
            Render();
            Images.Clear();
        }

        protected abstract void Render();

        public void DrawSingleImage(Int32Point roiOrigin, NImage image, float opacity, Matrix3x3 transform, ImageInterpolationQuality interpolationQuality, BlendMode blendMode, RasterizedMaskImage? trackMatte)
        {
            AddImage(roiOrigin, image, opacity, transform, interpolationQuality, blendMode, trackMatte);
            Draw();
        }

        protected record ImageInfo(NImage Image, float Opacity, Matrix3x3 transform, Matrix3x3 InvertedTransform, ImageInterpolationQuality InterpolationQuality, BlendMode BlendMode, RasterizedMaskImage? TrackMatte);
    }

    abstract class MaskRender2DBase
    {
        public abstract void Draw(NImage image, float opacity, Matrix3x3 transform, ImageInterpolationQuality interpolationQuality, RasterizedMaskImage? trackMatte, TrackMatteMode trackMatteMode);
    }

    class CPURenderer2D : Renderer2DBase
    {
        NManagedImage Target { get; }

        public CPURenderer2D(NManagedImage target)
        {
            Target = target;
        }

        protected override void Render()
        {
            var convertedImages = new Dictionary<NImage, NManagedImage>();
            var convertedTrackMatte = new Dictionary<RasterizedMaskImage, ManagedRasterizedMaskImage>();

            foreach (var (image, opacity, transform, inverted, interpolationQuality, blendMode, trackMatte) in RenderImages)
            {
                NManagedImage managedImage;
                if (image is NGPUImage gpuImage)
                {
                    if (!convertedImages.ContainsKey(gpuImage))
                    {
                        convertedImages.Add(gpuImage, gpuImage.CopyToCpu());
                    }
                    managedImage = convertedImages[gpuImage];
                }
                else
                {
                    managedImage = (NManagedImage)image;
                }

                ManagedRasterizedMaskImage? managedTrackMatte;
                if (trackMatte is GPURasterizedMaskImage gpuTrackMatte)
                {
                    if (!convertedTrackMatte.ContainsKey(gpuTrackMatte))
                    {
                        convertedTrackMatte.Add(gpuTrackMatte, gpuTrackMatte.CopyToCpu());
                    }
                    managedTrackMatte = convertedTrackMatte[gpuTrackMatte];
                }
                else
                {
                    managedTrackMatte = (ManagedRasterizedMaskImage?)trackMatte;
                }

                var p1 = transform.Transform(new Vector2());
                var p2 = transform.Transform(new Vector2(image.Width, 0.0F));
                var p3 = transform.Transform(new Vector2(image.Width, image.Height));
                var p4 = transform.Transform(new Vector2(0.0F, image.Height));
                var minX = Math.Max((int)Math.Floor(Math.Min(Math.Min(Math.Min(p1.X, p2.X), p3.X), p4.X)), 0);
                var minY = Math.Max((int)Math.Floor(Math.Min(Math.Min(Math.Min(p1.Y, p2.Y), p3.Y), p4.Y)), 0);
                var maxX = Math.Min((int)Math.Ceiling(Math.Max(Math.Max(Math.Max(p1.X, p2.X), p3.X), p4.X)), Target.Width);
                var maxY = Math.Min((int)Math.Ceiling(Math.Max(Math.Max(Math.Max(p1.Y, p2.Y), p3.Y), p4.Y)), Target.Height);

                var width = Target.Width;
                if (managedTrackMatte != null)
                {
                    Parallel.For(minY, maxY, y =>
                    {
                        var targetData = Target.GetDataSpan();
                        var imageData = managedImage.GetDataSpan();
                        var trackMatteData = managedTrackMatte.GetDataSpan();
                        for (int x = minX, pos = y * Target.Width + minX; x < maxX; x++, pos++)
                        {
                            var (imageX, imageY) = inverted.Transform(x, y);
                            var p = interpolationQuality switch
                            {
                                ImageInterpolationQuality.Level2 => ImageInterpolation.Bilinear(imageData, image.Width, image.Height, imageX, imageY),
                                _ => ImageInterpolation.NearestNeighbor(imageData, image.Width, image.Height, imageX, imageY)
                            };

                            p.W *= opacity * trackMatteData[pos];
                            if (p.W <= 0.0F)
                            {
                                continue;
                            }

                            targetData[pos] = Image.Drawing.Blend.Process(blendMode, targetData[pos], p);
                        }
                    });
                }
                else
                {
                    Parallel.For(minY, maxY, y =>
                    {
                        var targetData = Target.GetDataSpan();
                        var imageData = managedImage.GetDataSpan();
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

                            targetData[pos] = Image.Drawing.Blend.Process(blendMode, targetData[pos], p);
                        }
                    });
                }
            }

            foreach (var i in convertedImages.Values)
            {
                i.Dispose();
            }
            foreach (var t in convertedTrackMatte.Values)
            {
                t.Dispose();
            }
        }
    }

    class GPURenderer2D : Renderer2DBase
    {
        NGPUImage Target { get; }

        GraphicsDevice Device { get; }

        public GPURenderer2D(NGPUImage target, GraphicsDevice device)
        {
            Target = target;
            Device = device;
        }

        protected override void Render()
        {
            var convertedImage = new Dictionary<NImage, NGPUImage>();
            var convertedTrackMatte = new Dictionary<RasterizedMaskImage, GPURasterizedMaskImage>();

            foreach (var (image, _, _, _, _, _, trackMatte) in RenderImages)
            {
                NGPUImage gpuImage;
                if (image is NManagedImage managedImage)
                {
                    if (!convertedImage.ContainsKey(managedImage))
                    {
                        convertedImage.Add(managedImage, managedImage.CopyToGpu(Device));
                    }
                    gpuImage = convertedImage[managedImage];
                }
                else
                {
                    gpuImage = (NGPUImage)image;
                }

                GPURasterizedMaskImage? gpuTrackMatte;
                if (trackMatte is ManagedRasterizedMaskImage managedTrackMatte)
                {
                    if (!convertedTrackMatte.ContainsKey(managedTrackMatte))
                    {
                        convertedTrackMatte.Add(managedTrackMatte, managedTrackMatte.CopyToGpu(Device));
                    }
                    gpuTrackMatte = convertedTrackMatte[managedTrackMatte];
                }
                else
                {
                    gpuTrackMatte = (GPURasterizedMaskImage?)trackMatte;
                }
            }

            using (var emptyTrackMatte = Device.AllocateReadWriteBuffer([1.0F]))
            using (var context = Device.CreateComputeContext())
            {
                foreach (var (image, opacity, _, inverted, interpolationQuality, blendMode, trackMatte) in RenderImages)
                {
                    var gpuImage = image switch
                    {
                        NManagedImage => convertedImage[image],
                        _ => (NGPUImage)image
                    };
                    var gpuTrackMatte = trackMatte switch
                    {
                        ManagedRasterizedMaskImage => convertedTrackMatte[trackMatte],
                        _ => (GPURasterizedMaskImage?)trackMatte
                    };
                    var trackMatteData = gpuTrackMatte?.Data ?? emptyTrackMatte;

                    context.For(
                        Target.Width,
                        Target.Height,
                        new Render2D(
                            Target.Data,
                            Target.Width,
                            gpuImage.Data,
                            gpuImage.Width,
                            gpuImage.Height,
                            (int)interpolationQuality,
                            trackMatteData,
                            opacity,
                            (int)blendMode,
                            inverted.ToFloat3x3()
                        )
                    );

                    context.Barrier(Target.Data);
                }

                context.Barrier(Target.Data);
            }

            foreach (var i in convertedImage.Values)
            {
                i.Dispose();
            }
            foreach (var t in convertedTrackMatte.Values)
            {
                t.Dispose();
            }
        }
    }

    class CPUMaskRender2D : MaskRender2DBase
    {
        ManagedRasterizedMaskImage Target { get; }

        public CPUMaskRender2D(ManagedRasterizedMaskImage target)
        {
            Target = target;
        }

        public override void Draw(NImage image, float opacity, Matrix3x3 transform, ImageInterpolationQuality interpolationQuality, RasterizedMaskImage? trackMatte, TrackMatteMode trackMatteMode)
        {
            if (!Matrix3x3.Invert(transform, out var inverted))
            {
                return;
            }

            var managedImage = image.ToManaged();
            var managedTrackMatte = trackMatte switch
            {
                GPURasterizedMaskImage gpuTrackMatte => gpuTrackMatte.CopyToCpu(),
                _ => (ManagedRasterizedMaskImage?)trackMatte
            };

            var p1 = transform.Transform(new Vector2());
            var p2 = transform.Transform(new Vector2(image.Width, 0.0F));
            var p3 = transform.Transform(new Vector2(image.Width, image.Height));
            var p4 = transform.Transform(new Vector2(0.0F, image.Height));
            var minX = Math.Max((int)Math.Floor(Math.Min(Math.Min(Math.Min(p1.X, p2.X), p3.X), p4.X)), 0);
            var minY = Math.Max((int)Math.Floor(Math.Min(Math.Min(Math.Min(p1.Y, p2.Y), p3.Y), p4.Y)), 0);
            var maxX = Math.Min((int)Math.Ceiling(Math.Max(Math.Max(Math.Max(p1.X, p2.X), p3.X), p4.X)), Target.Width);
            var maxY = Math.Min((int)Math.Ceiling(Math.Max(Math.Max(Math.Max(p1.Y, p2.Y), p3.Y), p4.Y)), Target.Height);

            if (trackMatteMode == TrackMatteMode.InvertAlpha || trackMatteMode == TrackMatteMode.InvertLuminance)
            {
                Target.GetDataSpan().Fill(1.0F);
            }

            var width = Target.Width;
            if (managedTrackMatte != null)
            {
                Parallel.For(minY, maxY, y =>
                {
                    var targetData = Target.GetDataSpan();
                    var trackMatteData = managedTrackMatte.GetDataSpan();
                    var imageData = managedImage.GetDataSpan();
                    for (int x = minX, pos = y * Target.Width + minX; x < maxX; x++, pos++)
                    {
                        var (imageX, imageY) = inverted.Transform(x, y);
                        var p = interpolationQuality switch
                        {
                            ImageInterpolationQuality.Level2 => ImageInterpolation.Bilinear(imageData, image.Width, image.Height, imageX, imageY),
                            _ => ImageInterpolation.NearestNeighbor(imageData, image.Width, image.Height, imageX, imageY)
                        };

                        targetData[pos] = trackMatteMode switch
                        {
                            TrackMatteMode.Alpha => p.W,
                            TrackMatteMode.Luminance => (p * Const.ConvertToGrayScale).HorizontalAdd() * p.W,
                            TrackMatteMode.InvertAlpha => 1.0F - p.W,
                            TrackMatteMode.InvertLuminance => 1.0F - (p * Const.ConvertToGrayScale).HorizontalAdd() * p.W,
                            _ => 0.0F
                        } * opacity * trackMatteData[pos];
                    }
                });
            }
            else
            {
                Parallel.For(minY, maxY, y =>
                {
                    var targetData = Target.GetDataSpan();
                    var imageData = managedImage.GetDataSpan();
                    for (int x = minX, pos = y * Target.Width + minX; x < maxX; x++, pos++)
                    {
                        var (imageX, imageY) = inverted.Transform(x, y);
                        var p = interpolationQuality switch
                        {
                            ImageInterpolationQuality.Level2 => ImageInterpolation.Bilinear(imageData, image.Width, image.Height, imageX, imageY),
                            _ => ImageInterpolation.NearestNeighbor(imageData, image.Width, image.Height, imageX, imageY)
                        };

                        targetData[pos] = trackMatteMode switch
                        {
                            TrackMatteMode.Alpha => p.W,
                            TrackMatteMode.Luminance => (p * Const.ConvertToGrayScale).HorizontalAdd() * p.W,
                            TrackMatteMode.InvertAlpha => 1.0F - p.W,
                            TrackMatteMode.InvertLuminance => 1.0F - (p * Const.ConvertToGrayScale).HorizontalAdd() * p.W,
                            _ => 0.0F
                        } * opacity;
                    }
                });
            }

            if (managedImage != image)
            {
                managedImage.Dispose();
            }
            if (managedTrackMatte != trackMatte)
            {
                managedTrackMatte?.Dispose();
            }
        }
    }

    class GPUMaskRender2D : MaskRender2DBase
    {
        GPURasterizedMaskImage Target { get; }

        GraphicsDevice Device { get; }

        public GPUMaskRender2D(GPURasterizedMaskImage target, GraphicsDevice device)
        {
            Target = target;
            Device = device;
        }

        public override void Draw(NImage image, float opacity, Matrix3x3 transform, ImageInterpolationQuality interpolationQuality, RasterizedMaskImage? trackMatte, TrackMatteMode trackMatteMode)
        {
            if (!Matrix3x3.Invert(transform, out var inverted))
            {
                return;
            }

            var gpuImage = image.ToGpu(Device);
            var gpuTrackMatte = trackMatte switch
            {
                ManagedRasterizedMaskImage managedTrackMatte => managedTrackMatte.CopyToGpu(Device),
                _ => (GPURasterizedMaskImage?)trackMatte
            };

            var trackMatteData = gpuTrackMatte?.Data ?? Device.AllocateReadWriteBuffer([1.0F]);
            using (var context = Device.CreateComputeContext())
            {
                context.For(
                    Target.Width,
                    Target.Height,
                    new RenderMatte2D(
                        Target.Data,
                        Target.Width,
                        gpuImage.Data,
                        gpuImage.Width,
                        gpuImage.Height,
                        (int)interpolationQuality,
                        trackMatteData,
                        opacity,
                        (int)trackMatteMode,
                        inverted.ToFloat3x3()
                    )
                );

                context.Barrier(Target.Data);
            }

            if (gpuImage != image)
            {
                gpuImage.Dispose();
            }
            if (gpuTrackMatte == null)
            {
                trackMatteData.Dispose();
            }
            else if (gpuTrackMatte != trackMatte)
            {
                gpuTrackMatte?.Dispose();
            }
        }
    }
}
