using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using ComputeSharp;
using NiVE3.Image;
using NiVE3.Numerics;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Property.Properties;
using NiVE3.Plugin.Resource;
using NiVE3.Plugin.ValueObject;
using NiVE3.PresetPlugin.Extension;
using NiVE3.PresetPlugin.Internal.Drawing;
using NiVE3.PresetPlugin.Internal.Effect;
using NiVE3.PresetPlugin.Resource;

namespace NiVE3.PresetPlugin.Effect.Distortion
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.Distortion_WaveCurve_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_Distortion, LanguageResourceDictionary.Distortion_WaveCurve_Description, ID, IsSupportGpu = true, IsRenderEveryFrame = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class WaveCurve : IEffect
    {
        const string ID = "CF55D043-4440-418F-A62F-429EA63402B7";

        const string PropertyTypeId = nameof(PropertyTypeId);

        const string PropertyAmplitudeId = nameof(PropertyAmplitudeId);

        const string PropertyIntervalId = nameof(PropertyIntervalId);

        const string PropertySpeedId = nameof(PropertySpeedId);

        const string PropertyAngleId = nameof(PropertyAngleId);

        const string PropertyPhaseId = nameof(PropertyPhaseId);

        const string PropertyRandomSeedId = nameof(PropertyRandomSeedId);

        IAcceleratorObject? AcceleratorObject { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            AcceleratorObject = accelerator;
        }

        public PropertyBase[] GetProperties()
        {
            return
            [
                new EnumProperty(PropertyTypeId, LanguageResourceDictionary.ResourceKeys.Distortion_WaveCurve_Type, typeof(WaveCurveType), typeof(LanguageResourceDictionary), WaveCurveType.Sin, selectBoxWidth: 90.0),
                new DoubleProperty(PropertyAmplitudeId, LanguageResourceDictionary.ResourceKeys.Distortion_WaveCurve_Amplitude, 10.0, 0.0, double.MaxValue, digit: 2),
                new DoubleProperty(PropertyIntervalId, LanguageResourceDictionary.ResourceKeys.Distortion_WaveCurve_Interval, 40.0, 1.0, double.MaxValue, digit: 2),
                new DoubleProperty(PropertySpeedId, LanguageResourceDictionary.ResourceKeys.Distortion_WaveCurve_Speed, 10.0, 0.0, double.MaxValue, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_AnglePerSec),
                new AngleProperty(PropertyAngleId, LanguageResourceDictionary.ResourceKeys.Distortion_WaveCurve_Angle, 0.0),
                new AngleProperty(PropertyPhaseId, LanguageResourceDictionary.ResourceKeys.Distortion_WaveCurve_Phase, 0.0),
                new DoubleProperty(PropertyRandomSeedId, LanguageResourceDictionary.ResourceKeys.Distortion_WaveCurve_RandomSeed, 0.0, 0.0, double.MaxValue, digit: 0)
            ];
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, double layerTime, IPropertyObject[] properties, ICompositionObject composition, bool useGpu)
        {
            var type = properties.GetValue(PropertyTypeId, layerTime, WaveCurveType.Sin);
            var amp = (float)(properties.GetValue(PropertyAmplitudeId, layerTime, 0.0) / downSamplingRateY);
            var interval = (float)(properties.GetValue(PropertyIntervalId, layerTime, 1.0) / downSamplingRateX);
            var speed = (float)properties.GetValue(PropertySpeedId, layerTime, 0.0);
            var angle = (float)properties.GetValue(PropertyAngleId, layerTime, 0.0);
            var phase = (float)properties.GetValue(PropertyPhaseId, layerTime, 0.0);
            var randomSeed = (uint)properties.GetValue(PropertyRandomSeedId, layerTime, 0.0);

            if (amp <= 0.0F || interval < 1.0F)
            {
                return image;
            }

            if (useGpu && AcceleratorObject != null)
            {
                return ProcessGpu(AcceleratorObject.CurrentDevice, image, roi, (float)downSamplingRateX, (float)downSamplingRateY, type, amp, interval, speed, angle, phase, randomSeed, (float)layerTime);
            }
            else
            {
                return ProcessCpu(image, roi, (float)downSamplingRateX, (float)downSamplingRateY, type, amp, interval, speed, angle, phase, randomSeed, (float)layerTime);
            }
        }

        public float[] Process(float[] audio, double startTime, IPropertyObject[] properties, ICompositionObject composition)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }

        static NManagedImage ProcessCpu(NImage image, ROI roi, float downSamplingRateX, float downSamplingRateY, WaveCurveType type, float amp, float interval, float speed, float angle, float phase, uint randomSeed, float time)
        {
            var managedImage = image switch
            {
                NGPUImage gpuImage => gpuImage.CopyToCpu(),
                _ => (NManagedImage)image
            };

            using var sourceImage = (NManagedImage)managedImage.Copy();

            var centerX = (roi.OriginalImagePosition.X + (roi.OriginalImageSize.Width * 0.5F)) / downSamplingRateX;
            var centerY = (roi.OriginalImagePosition.Y + (roi.OriginalImageSize.Height * 0.5F)) / downSamplingRateY;
            var transform = Matrix3x3.CreateRotateAt(angle, centerX, centerY);
            var iTransform = Matrix3x3.CreateRotateAt(-angle, centerX, centerY);

            var imageWidth = managedImage.Width;
            var imageHeight = managedImage.Height;
            var imageData = managedImage.Data;
            var sourceImageData = sourceImage.Data;

            var basePhase = phase + speed * time;

            Parallel.For(roi.Top, roi.Bottom, y =>
            {
                var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);

                for (var x = roi.Left; x < roi.Right; x++)
                {
                    var (waveX, waveY) = transform.Transform(x, y);
                    var wave = GenerateWave(type, basePhase, waveX - imageWidth * 0.5F, interval, randomSeed) * amp;
                    var (sourceX, sourceY) = iTransform.Transform(waveX, waveY + wave);
                    imageDataSpan[x] = ImageInterpolation.Bilinear(sourceImageData, imageWidth, imageHeight, sourceX, sourceY);
                }
            });

            return managedImage;
        }

        static NGPUImage ProcessGpu(GraphicsDevice device, NImage image, ROI roi, float downSamplingRateX, float downSamplingRateY, WaveCurveType type, float amp, float interval, float speed, float angle, float phase, uint randomSeed, float time)
        {
            var gpuImage = image switch
            {
                NManagedImage managedImage => managedImage.CopyToGpu(device),
                _ => (NGPUImage)image
            };

            using var sourceImage = new NGPUImage(gpuImage.Width, gpuImage.Height, device);
            gpuImage.CopyTo(sourceImage);

            var centerX = (roi.OriginalImagePosition.X + (roi.OriginalImageSize.Width * 0.5F)) / downSamplingRateX;
            var centerY = (roi.OriginalImagePosition.Y + (roi.OriginalImageSize.Height * 0.5F)) / downSamplingRateY;
            var transform = Matrix3x3.CreateRotateAt(angle, centerX, centerY);
            var iTransform = Matrix3x3.CreateRotateAt(-angle, centerX, centerY);

            var basePhase = phase + speed * time;

            using var context = device.CreateComputeContext();
            context.For(roi.Width, roi.Height, new WaveCurveProcess(gpuImage.Data, gpuImage.Width, gpuImage.Height, sourceImage.Data, (int)type, basePhase, amp, interval, randomSeed, transform.ToFloat3x3(), iTransform.ToFloat3x3(), roi.Left, roi.Top));

            return gpuImage;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float GenerateWave(WaveCurveType type, float baseAngle, float x, float interval, uint randomSeed)
        {
            const float AngleInterval = 180.0F;
            const uint CoordLimit = 0xFFF;

            var phaseAngle = baseAngle + x * 360.0F / interval;
            switch (type)
            {
                case WaveCurveType.Sin:
                    return MathF.Sin(phaseAngle / AngleInterval * MathF.PI);
                case WaveCurveType.Rectangle:
                    if (phaseAngle < 0.0F)
                    {
                        return ((-phaseAngle / AngleInterval) % 2.0F) < 1.0F ? 1.0F : -1.0F;
                    }
                    else
                    {
                        return ((phaseAngle / AngleInterval) % 2.0F) > 1.0F ? 1.0F : -1.0F;
                    }
                case WaveCurveType.Triangle:
                    {
                        var a = Math.Abs(phaseAngle / AngleInterval);
                        var b = a % 2.0F;
                        return (b - Math.Max(b - 1.0F, 0.0F) * 2.0F - 0.5F) * 2.0F;
                    }
                case WaveCurveType.Saw:
                    if (phaseAngle < 0.0F)
                    {
                        return (1.0F - (-phaseAngle / AngleInterval % 1.0F) - 0.5F) * 2.0F;
                    }
                    else
                    {
                        return ((phaseAngle / AngleInterval % 1.0F) - 0.5F) * 2.0F;
                    }
                case WaveCurveType.Noise:
                    {
                        var count = phaseAngle / AngleInterval;
                        var uCount = unchecked((uint)(int)MathF.Floor(count)) % CoordLimit;
                        return (NoiseFunction.Pcg3D1FloatCpu(uCount, 0, 0, randomSeed) - 0.5F) * 2.0F;
                    }
                case WaveCurveType.SmoothNoise:
                    {
                        var count = phaseAngle / AngleInterval;
                        var diff = SmoothFade(count - MathF.Floor(count));
                        var pnoise = NoiseFunction.Pcg3D1FloatCpu(unchecked((uint)(int)MathF.Floor(count)) % CoordLimit, 0, 0, randomSeed);
                        var nnoise = NoiseFunction.Pcg3D1FloatCpu(unchecked((uint)(int)MathF.Floor(count + 1.0F)) % CoordLimit, 0, 0, randomSeed);
                        return (float.Lerp(pnoise, nnoise, diff) - 0.5F) * 2.0F;
                    }
            }
            return 0.0F;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float SmoothFade(float t)
        {
            return t * t * (3.0F - 2.0F * t);
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct WaveCurveProcess(ReadWriteBuffer<Float4> image, int width, int height, ReadWriteBuffer<Float4> originalImage, int type, float baseAngle, float amp, float interval, uint randomSeed, Float3x3 transform, Float3x3 iTransform, int startX, int startY) : IComputeShader
    {
        const float PI = MathF.PI;

        public void Execute()
        {
            var x = ThreadIds.X + startX;
            var y = ThreadIds.Y + startY;

            var wavePos = transform * new Float3(x, y, 1.0F);
            var wave = GenerateWave(wavePos.X - width * 0.5F) * amp;
            var sourcePos = iTransform * new Float3(wavePos.X, wavePos.Y + wave, 1.0F);

            image[(y * width) + x] = OriginalImageBilinear(sourcePos.X, sourcePos.Y);
        }

        Float4 OriginalImageBilinear(float x, float y)
        {
            var ix = (int)x;
            var iy = (int)y;

            if (ix == x && iy == y)
            {
                if (ix > -1 && iy > -1 && ix < width && iy < height)
                {
                    return originalImage[iy * width + ix];
                }
                else
                {
                    return new Float4(1.0F, 1.0F, 1.0F, 0.0F);
                }
            }
            else if (ix < -1 || iy < -1 || ix >= width || iy >= height)
            {
                return new Float4(1.0F, 1.0F, 1.0F, 0.0F);
            }

            var pp = x - ix;
            var qq = y - iy;
            var ip = 1.0F - pp;
            var iq = 1.0F - qq;
            var mw = width - 1;
            var mh = height - 1;

            var c1 = new Float4(1.0F, 1.0F, 1.0F, 0.0F);
            var c2 = new Float4(1.0F, 1.0F, 1.0F, 0.0F);
            var c3 = new Float4(1.0F, 1.0F, 1.0F, 0.0F);
            var c4 = new Float4(1.0F, 1.0F, 1.0F, 0.0F);
            var pos = iy * width + ix;

            if (ix > -1)
            {
                if (ix < mw)
                {
                    if (iy > -1)
                    {
                        c1 = originalImage[pos];
                        c2 = originalImage[pos + 1];
                        if (iy < mh)
                        {
                            pos += width;
                            c3 = originalImage[pos];
                            c4 = originalImage[pos + 1];
                        }
                    }
                    else
                    {
                        pos += width;
                        c3 = originalImage[pos];
                        c4 = originalImage[pos + 1];
                    }
                }
                else
                {
                    if (iy > -1)
                    {
                        c1 = originalImage[pos];
                        if (iy < mh)
                        {
                            c3 = originalImage[pos + width];
                        }
                    }
                    else
                    {
                        c3 = originalImage[pos + width];
                    }
                }
            }
            else
            {
                pos++;
                if (iy > -1)
                {
                    c2 = originalImage[pos];
                    if (iy < mh)
                    {
                        c4 = originalImage[pos + width];
                    }
                }
                else
                {
                    c4 = originalImage[pos + width];
                }
            }

            var ta = Hlsl.Lerp(Hlsl.Lerp(c1, c3, qq), Hlsl.Lerp(c2, c4, qq), pp).W;
            if (ta <= 0.0F)
            {
                return new Float4(1.0F, 1.0F, 1.0F, 0.0F);
            }
            else
            {
                var t = Hlsl.Lerp(Hlsl.Lerp(c1 * c1.W, c3 * c3.W, qq), Hlsl.Lerp(c2 * c2.W, c4 * c4.W, qq), pp) / ta;
                t.W = ta;
                return t;
            }
        }

        float GenerateWave(float x)
        {
            const float AngleInterval = 180.0F;
            const uint CoordLimit = 0xFFF;

            var phaseAngle = baseAngle + x * 360.0F / interval;
            switch (type)
            {
                case 0: // WaveCurveType.Sin
                    return Hlsl.Sin(phaseAngle / AngleInterval * PI);
                case 1: // WaveCurveType.Rectangle:
                    if (phaseAngle < 0.0F)
                    {
                        return ((-phaseAngle / AngleInterval) % 2.0F) < 1.0F ? 1.0F : -1.0F;
                    }
                    else
                    {
                        return ((phaseAngle / AngleInterval) % 2.0F) > 1.0F ? 1.0F : -1.0F;
                    }
                case 2: // WaveCurveType.Triangle:
                    {
                        var a = Hlsl.Abs(phaseAngle / AngleInterval);
                        var b = a % 2.0F;
                        return (b - Hlsl.Max(b - 1.0F, 0.0F) * 2.0F - 0.5F) * 2.0F;
                    }
                case 3: // WaveCurveType.Saw:
                    if (phaseAngle < 0.0F)
                    {
                        return (1.0F - (-phaseAngle / AngleInterval % 1.0F) - 0.5F) * 2.0F;
                    }
                    else
                    {
                        return ((phaseAngle / AngleInterval % 1.0F) - 0.5F) * 2.0F;
                    }
                case 4: // WaveCurveType.Noise:
                    {
                        var count = phaseAngle / AngleInterval;
                        var uCount = ((uint)(int)Hlsl.Floor(count)) % CoordLimit;
                        return (NoiseFunction.Pcg3D1FloatCpu(uCount, 0, 0, randomSeed) - 0.5F) * 2.0F;
                    }
                case 5: // WaveCurveType.SmoothNoise:
                    {
                        var count = phaseAngle / AngleInterval;
                        var diff = SmoothFade(count - Hlsl.Floor(count));
                        var pnoise = NoiseFunction.Pcg3D1FloatGpu(new UInt3(((uint)(int)Hlsl.Floor(count)) % CoordLimit, 0, 0), randomSeed);
                        var nnoise = NoiseFunction.Pcg3D1FloatGpu(new UInt3(((uint)(int)Hlsl.Floor(count + 1.0F)) % CoordLimit, 0, 0), randomSeed);
                        return (Hlsl.Lerp(pnoise, nnoise, diff) - 0.5F) * 2.0F;
                    }
            }
            return 0.0F;
        }

        static float SmoothFade(float t)
        {
            return t * t * (3.0F - 2.0F * t);
        }
    }

    enum WaveCurveType : int
    {
        Sin,
        Rectangle,
        Triangle,
        Saw,
        Noise,
        SmoothNoise
    }
}
