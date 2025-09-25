using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using ComputeSharp;
using NiVE3.Image;
using NiVE3.Numerics;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Property.Properties;
using NiVE3.Plugin.Resource;
using NiVE3.Plugin.ValueObject;
using NiVE3.PresetPlugin.Effect.Util;
using NiVE3.PresetPlugin.Extension;
using NiVE3.PresetPlugin.Internal;
using NiVE3.PresetPlugin.Internal.Drawing;
using NiVE3.PresetPlugin.Resource;
using NiVE3.Shared.Extension;

namespace NiVE3.PresetPlugin.Effect.Simulation
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.Simulation_Panorama_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_Simulation, LanguageResourceDictionary.Simulation_Panorama_Description, ID, IsSupportGpu = true, UseCompositionCamera = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class Panorama : IEffect
    {
        const string ID = "5BA3A7A9-8159-4A79-BA41-DD79B5501B59";

        const string PropertySourceLayerId = nameof(PropertySourceLayerId);

        const string PropertyRotateXId = nameof(PropertyRotateXId);

        const string PropertyRotateYId = nameof(PropertyRotateYId);

        const string PropertyRotateZId = nameof(PropertyRotateZId);

        IAcceleratorObject? AcceleratorObject { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            AcceleratorObject = accelerator;
        }

        public PropertyBase[] GetProperties(Int32Size sourceSize)
        {
            return
            [
                new UseLayerImageProperty(PropertySourceLayerId, LanguageResourceDictionary.ResourceKeys.Simulation_Panorama_SourceLayer, selectBoxWidth: 90.0),
                new AngleProperty(PropertyRotateXId, LanguageResourceDictionary.ResourceKeys.Simulation_Panorama_RotateX, 0.0, digit: 2),
                new AngleProperty(PropertyRotateYId, LanguageResourceDictionary.ResourceKeys.Simulation_Panorama_RotateY, 0.0, digit: 2),
                new AngleProperty(PropertyRotateZId, LanguageResourceDictionary.ResourceKeys.Simulation_Panorama_RotateZ, 0.0, digit: 2),
            ];
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, Time layerTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer, bool useGpu)
        {
            var targetLayerId = properties.GetValue(PropertySourceLayerId, layerTime, UseLayerImageTarget.Empty);
            using var sourceImage = targetLayerId.GetImage(composition, layerTime, downSamplingRateX, useGpu);
            if (sourceImage == null)
            {
                return image;
            }

            var rotateX = properties.GetValue(PropertyRotateXId, layerTime, 0.0);
            var rotateY = properties.GetValue(PropertyRotateYId, layerTime, 0.0);
            var rotateZ = properties.GetValue(PropertyRotateZId, layerTime, 0.0);

            var (viewMatrix, fov) = CameraProperties.GetActiveCameraViewMatrixAndFov(composition, layer, layerTime, roi, image.Width, image.Height, downSamplingRateX, downSamplingRateY);
            viewMatrix = viewMatrix.Translate(-viewMatrix.M41, -viewMatrix.M42, -viewMatrix.M43);
            var mv = Matrix4x4d.Identity.RotateZ(rotateZ).RotateY(rotateY).RotateX(rotateX) * viewMatrix;
            var projectionMatrix = Matrix4x4d.CreatePerspectiveFieldOfView(fov, 1.0, 1E-10, 1E10);
            var combinedMatrix = mv * projectionMatrix;
            if (!Matrix4x4d.Invert(combinedMatrix, out var invertedMatrix))
            {
                return image;
            }

            if (useGpu && AcceleratorObject != null)
            {
                return ProcessGpu(AcceleratorObject.CurrentDevice, image, roi, sourceImage, (Matrix4x4)invertedMatrix);
            }
            else
            {
                return ProcessCpu(image, roi, sourceImage, (Matrix4x4)invertedMatrix);
            }
        }

        public float[] Process(float[] audio, Time startTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }

        static NManagedImage ProcessCpu(NImage image, ROI roi, NImage sourceImage, Matrix4x4 matrix)
        {
            var managedImage = image.ToManaged();
            var managedSourceImage = sourceImage.ToManaged();

            var imageWidth = managedImage.Width;
            var imageHeight = managedImage.Height;
            var imageData = managedImage.Data;
            var sourceWidth = managedSourceImage.Width;
            var sourceHeight = managedSourceImage.Height;
            var sourceData = managedSourceImage.Data;
            Parallel.For(roi.Top, roi.Bottom, y =>
            {
                var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);

                for (var x = roi.Left; x < roi.Right; x++)
                {
                    var ndcX = (x + 0.5F) / imageWidth * 2.0F - 1.0F;
                    var ndcY = 1.0F - (y + 0.5F) / imageHeight * 2.0F;

                    var nearPoint = Vector4.Transform(new Vector4(ndcX, ndcY, -1.0F, 1.0F), matrix);
                    var farPoint = Vector4.Transform(new Vector4(ndcX, ndcY, 1.0F, 1.0F), matrix);
                    if (nearPoint.W > 1E-6F)
                    {
                        nearPoint /= nearPoint.W;
                    }
                    if (farPoint.W > 1E-6F)
                    {
                        farPoint /= farPoint.W;
                    }

                    var dir = Vector3.Normalize((farPoint - nearPoint).AsVector3());
                    var u = (MathF.Atan2(dir.X, dir.Z) + MathF.PI) / (MathF.PI * 2.0F) * sourceWidth;
                    var v = (MathF.PI * 0.5F - MathF.Asin(Math.Clamp(dir.Y, -1.0F, 1.0F))) / MathF.PI * sourceHeight;

                    imageDataSpan[x] = BilinearPanorama(sourceData, sourceWidth, sourceHeight, u, v);
                }
            });

            if (managedSourceImage != sourceImage)
            {
                managedSourceImage.Dispose();
            }

            return managedImage;
        }

        static NGPUImage ProcessGpu(GraphicsDevice device, NImage image, ROI roi, NImage sourceImage, Matrix4x4 matrix)
        {
            var gpuImage = image.ToGpu(device);
            var gpuSourceImage = sourceImage.ToGpu(device);

            device.For(roi.Width, roi.Height, new PanoramaProcess(gpuImage.Data, gpuImage.Width, gpuImage.Height, gpuSourceImage.Data, gpuSourceImage.Width, gpuSourceImage.Height, matrix.ToFloat4x4(), roi.Left, roi.Top));

            if (gpuSourceImage != sourceImage)
            {
                gpuSourceImage.Dispose();
            }

            return gpuImage;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector4 BilinearPanorama(ReadOnlySpan<Vector4> texture, int width, int height, float x, float y)
        {
            var ix = (int)Math.Floor(x);
            var iy = (int)Math.Floor(y);

            if (ix == x && iy == y)
            {
                return texture[CoordWrap.Wrap(iy, height) * width + CoordWrap.Repeat(ix, width)];
            }

            var c1 = texture[CoordWrap.Wrap(iy, height) * width + CoordWrap.Repeat(ix, width)];
            var c2 = texture[CoordWrap.Wrap(iy, height) * width + CoordWrap.Repeat(ix + 1, width)];
            var c3 = texture[CoordWrap.Wrap(iy + 1, height) * width + CoordWrap.Repeat(ix, width)];
            var c4 = texture[CoordWrap.Wrap(iy + 1, height) * width + CoordWrap.Repeat(ix + 1, width)];

            var pp = x - ix;
            var qq = y - iy;
            var ta = Vector4.Lerp(Vector4.Lerp(c1, c3, qq), Vector4.Lerp(c2, c4, qq), pp).W;
            if (ta <= 0.0F)
            {
                return Const.EmptyPixel;
            }
            var t = Vector4.Lerp(Vector4.Lerp(c1 * c1.W, c3 * c3.W, qq), Vector4.Lerp(c2 * c2.W, c4 * c4.W, qq), pp) / ta;
            t.W = ta;

            return t;
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct PanoramaProcess(ReadWriteBuffer<Float4> image, int width, int height, ReadWriteBuffer<Float4> sourceImage, int sourceWidth, int sourceHeight, Float4x4 matrix, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            var x = ThreadIds.X + startX;
            var y = ThreadIds.Y + startY;

            var ndcX = (x + 0.5F) / width * 2.0F - 1.0F;
            var ndcY = 1.0F - (y + 0.5F) / height * 2.0F;

            var nearPoint = new Float4(ndcX, ndcY, -1.0F, 1.0F) * matrix;
            var farPoint = new Float4(ndcX, ndcY, 1.0F, 1.0F) * matrix;
            if (nearPoint.W > 1E-6F)
            {
                nearPoint /= nearPoint.W;
            }
            if (farPoint.W > 1E-6F)
            {
                farPoint /= farPoint.W;
            }

            var dir = Hlsl.Normalize((farPoint - nearPoint).XYZ);
            var u = (Hlsl.Atan2(dir.X, dir.Z) + MathF.PI) / (MathF.PI * 2.0F) * sourceWidth;
            var v = (MathF.PI * 0.5F - Hlsl.Asin(Hlsl.Clamp(dir.Y, -1.0F, 1.0F))) / MathF.PI * sourceHeight;

            image[y * width + x] = SourceImageBilinear(u, v);
        }

        Float4 SourceImageBilinear(float x, float y)
        {
            var ix = (int)Hlsl.Floor(x);
            var iy = (int)Hlsl.Floor(y);
            if (ix == x && iy == y)
            {
                return sourceImage[CoordWrapGpu.Wrap(iy, sourceHeight) * sourceWidth + CoordWrapGpu.Wrap(ix, sourceWidth)];
            }

            var c1 = sourceImage[CoordWrapGpu.Wrap(iy, sourceHeight) * sourceWidth + CoordWrapGpu.Repeat(ix, sourceWidth)];
            var c2 = sourceImage[CoordWrapGpu.Wrap(iy, sourceHeight) * sourceWidth + CoordWrapGpu.Repeat(ix + 1, sourceWidth)];
            var c3 = sourceImage[CoordWrapGpu.Wrap(iy + 1, sourceHeight) * sourceWidth + CoordWrapGpu.Repeat(ix, sourceWidth)];
            var c4 = sourceImage[CoordWrapGpu.Wrap(iy + 1, sourceHeight) * sourceWidth + CoordWrapGpu.Repeat(ix + 1, sourceWidth)];

            var pp = x - ix;
            var qq = y - iy;
            var ta = Hlsl.Lerp(Hlsl.Lerp(c1, c3, qq), Hlsl.Lerp(c2, c4, qq), pp).W;
            if (ta <= 0.0F)
            {
                return Const.EmptyPixelFloat4;
            }
            else
            {
                var t = Hlsl.Lerp(Hlsl.Lerp(c1 * c1.W, c3 * c3.W, qq), Hlsl.Lerp(c2 * c2.W, c4 * c4.W, qq), pp) / ta;
                t.W = ta;
                return t;
            }
        }
    }
}
