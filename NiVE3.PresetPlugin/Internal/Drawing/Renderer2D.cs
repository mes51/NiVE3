using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ComputeSharp;
using NiVE3.Image;
using NiVE3.Image.Drawing;
using NiVE3.Numerics;
using NiVE3.Plugin.Interfaces;
using NiVE3.PresetPlugin.Internal.Drawing.ComputeShader.Render2D;
using NiVE3.Shared.Extension;

namespace NiVE3.PresetPlugin.Internal.Drawing
{
    interface IRenderer2D
    {
        void Draw(NImage image, float opacity, Matrix3x3 transform, ImageInterpolationQuality interpolationQuality, BlendMode blendMode, RasterizedMaskImage? trackMatte);
    }

    interface IMaskRender2D
    {
        void Draw(NImage image, float opacity, Matrix3x3 transform, ImageInterpolationQuality interpolationQuality, RasterizedMaskImage? trackMatte, TrackMatteMode trackMatteMode);
    }

    class CpuRenderer2D : IRenderer2D
    {
        NManagedImage Target { get; }

        public CpuRenderer2D(NManagedImage target)
        {
            Target = target;
        }

        public void Draw(NImage image, float opacity, Matrix3x3 transform, ImageInterpolationQuality interpolationQuality, BlendMode blendMode, RasterizedMaskImage? trackMatte)
        {
            transform = Matrix3x3.CreateTranslate((float)-image.Origin.X, (float)-image.Origin.Y) * transform;
            if (!Matrix3x3.Invert(transform, out var inverted))
            {
                return;
            }

            var managedImage = image switch
            {
                NGPUImage gpuImage => gpuImage.CopyToCpu(),
                _ => (NManagedImage)image
            };
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

    class GpuRenderer2D : IRenderer2D
    {
        NGPUImage Target { get; }

        GraphicsDevice Device { get; }

        public GpuRenderer2D(NGPUImage target, GraphicsDevice device)
        {
            Target = target;
            Device = device;
        }

        public void Draw(NImage image, float opacity, Matrix3x3 transform, ImageInterpolationQuality interpolationQuality, BlendMode blendMode, RasterizedMaskImage? trackMatte)
        {
            transform = Matrix3x3.CreateTranslate((float)-image.Origin.X, (float)-image.Origin.Y) * transform;
            if (!Matrix3x3.Invert(transform, out var inverted))
            {
                return;
            }

            var gpuImage = image switch
            {
                NManagedImage managedImage => managedImage.CopyToGpu(Device),
                _ => (NGPUImage)image
            };
            var gpuTrackMatte = trackMatte switch
            {
                ManagedRasterizedMaskImage managedTrackMatte => managedTrackMatte.CopyToGpu(Device),
                _ => (GPURasterizedMaskImage?)trackMatte
            };

            using var canvas = new NGPUImage(Target.Width, Target.Height, Device);
            using (var context = Device.CreateComputeContext())
            {
                switch (interpolationQuality)
                {
                    case ImageInterpolationQuality.Level1:
                        context.For(canvas.Width, canvas.Height, new NearestNeighbor(canvas.Data, canvas.Width, gpuImage.Data, gpuImage.Width, gpuImage.Height, inverted.ToFloat3x3()));
                        break;
                    default:
                        context.For(canvas.Width, canvas.Height, new Bilinear(canvas.Data, canvas.Width, gpuImage.Data, gpuImage.Width, gpuImage.Height, inverted.ToFloat3x3()));
                        break;
                }

                if (gpuTrackMatte != null)
                {
                    context.For(Target.Data.Length, new BlendWithTrackMatte(Target.Data, canvas.Data, gpuTrackMatte.Data, (int)blendMode, opacity));
                }
                else
                {
                    context.For(Target.Data.Length, new ComputeShader.Render2D.Blend(Target.Data, canvas.Data, (int)blendMode, opacity));
                }
            }

            if (gpuImage != image)
            {
                gpuImage.Dispose();
            }
            if (gpuTrackMatte != trackMatte)
            {
                gpuTrackMatte?.Dispose();
            }
        }
    }

    class CpuMaskRender2D : IMaskRender2D
    {
        static readonly Vector4 ToGrayScale = new Vector4(0.114478F, 0.586611F, 0.298912F, 0.0F);

        ManagedRasterizedMaskImage Target { get; }

        public CpuMaskRender2D(ManagedRasterizedMaskImage target)
        {
            Target = target;
        }

        public void Draw(NImage image, float opacity, Matrix3x3 transform, ImageInterpolationQuality interpolationQuality, RasterizedMaskImage? trackMatte, TrackMatteMode trackMatteMode)
        {
            if (!Matrix3x3.Invert(transform, out var inverted))
            {
                return;
            }

            var managedImage = image switch
            {
                NGPUImage gpuImage => gpuImage.CopyToCpu(),
                _ => (NManagedImage)image
            };
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
                            TrackMatteMode.Luminance => (p * ToGrayScale).HorizontalAdd() * p.W,
                            TrackMatteMode.InvertAlpha => 1.0F - p.W,
                            TrackMatteMode.InvertLuminance => 1.0F - (p * ToGrayScale).HorizontalAdd() * p.W,
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
                            TrackMatteMode.Luminance => (p * ToGrayScale).HorizontalAdd() * p.W,
                            TrackMatteMode.InvertAlpha => 1.0F - p.W,
                            TrackMatteMode.InvertLuminance => 1.0F - (p * ToGrayScale).HorizontalAdd() * p.W,
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

    class GpuMaskRender2D : IMaskRender2D
    {
        static readonly Vector4 ToGrayScale = new Vector4(0.114478F, 0.586611F, 0.298912F, 0.0F);

        GPURasterizedMaskImage Target { get; }

        GraphicsDevice Device { get; }

        public GpuMaskRender2D(GPURasterizedMaskImage target, GraphicsDevice device)
        {
            Target = target;
            Device = device;
        }

        public void Draw(NImage image, float opacity, Matrix3x3 transform, ImageInterpolationQuality interpolationQuality, RasterizedMaskImage? trackMatte, TrackMatteMode trackMatteMode)
        {
            if (!Matrix3x3.Invert(transform, out var inverted))
            {
                return;
            }

            var gpuImage = image switch
            {
                NManagedImage managedImage => managedImage.CopyToGpu(Device),
                _ => (NGPUImage)image
            };
            var gpuTrackMatte = trackMatte switch
            {
                ManagedRasterizedMaskImage managedTrackMatte => managedTrackMatte.CopyToGpu(Device),
                _ => (GPURasterizedMaskImage?)trackMatte
            };

            using var canvas = new NGPUImage(Target.Width, Target.Height, Device);
            using (var context = Device.CreateComputeContext())
            {
                switch (interpolationQuality)
                {
                    case ImageInterpolationQuality.Level1:
                        context.For(canvas.Width, canvas.Height, new NearestNeighbor(canvas.Data, canvas.Width, gpuImage.Data, gpuImage.Width, gpuImage.Height, inverted.ToFloat3x3()));
                        break;
                    default:
                        context.For(canvas.Width, canvas.Height, new Bilinear(canvas.Data, canvas.Width, gpuImage.Data, gpuImage.Width, gpuImage.Height, inverted.ToFloat3x3()));
                        break;
                }

                if (gpuTrackMatte != null)
                {
                    context.For(Target.Data.Length, new CreateMatteWithTrackMatte(Target.Data, canvas.Data, gpuTrackMatte.Data, (int)trackMatteMode, opacity));
                }
                else
                {
                    context.For(Target.Data.Length, new CreateMatte(Target.Data, canvas.Data, (int)trackMatteMode, opacity));
                }
            }

            if (gpuImage != image)
            {
                gpuImage.Dispose();
            }
            if (gpuTrackMatte != trackMatte)
            {
                gpuTrackMatte?.Dispose();
            }
        }
    }

    static class Matrix3x3Extensions
    {
        public static Float3x3 ToFloat3x3(this in Matrix3x3 matrix)
        {
            return new Float3x3(
                matrix.M11, matrix.M21, matrix.M31,
                matrix.M12, matrix.M22, matrix.M32,
                matrix.M13, matrix.M23, matrix.M33
            );
        }
    }
}
