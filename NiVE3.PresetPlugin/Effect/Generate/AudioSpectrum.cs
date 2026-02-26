using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shapes;
using NAudio.Dsp;
using NiVE3.Image;
using NiVE3.Image.Drawing;
using NiVE3.Numerics;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Property.Properties;
using NiVE3.Plugin.Resource;
using NiVE3.Plugin.ValueObject;
using NiVE3.PresetPlugin.Effect.Util;
using NiVE3.PresetPlugin.Effect.Util.General;
using NiVE3.PresetPlugin.Extension;
using NiVE3.PresetPlugin.Internal;
using NiVE3.PresetPlugin.Resource;
using NiVE3.Shape;
using NWaves.Features;
using NWaves.Transforms;
using NWaves.Utils;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using static Vanara.PInvoke.Gdi32;
using Complex = NAudio.Dsp.Complex;
using Polygon = NiVE3.Shape.Polygon;

namespace NiVE3.PresetPlugin.Effect.Generate
{
    [EffectMetadata(LanguageResourceDictionary.Generate_AudioSpectrum_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_Generate, LanguageResourceDictionary.Generate_AudioSpectrum_Description, ID, IsRenderEveryFrame = true, IsSupportGpu = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    [Export(typeof(IEffect))]
    public sealed class AudioSpectrum : IEffect
    {
        const string ID = "FE73651C-6CD8-4E77-9F0D-3793C8E16F9E";

        const string PropertyLayerId = nameof(PropertyLayerId);

        const string PropertyAudioLengthId = nameof(PropertyAudioLengthId);

        const string PropertyWindowFunctionTypeId = nameof(PropertyWindowFunctionTypeId);

        const string PropertyAudioOffsetId = nameof(PropertyAudioOffsetId);

        const string PropertyFrequencyBandCountId = nameof(PropertyFrequencyBandCountId);

        const string PropertyBeginPointId = nameof(PropertyBeginPointId);

        const string PropertyEndPointId = nameof(PropertyEndPointId);

        const string PropertyUseMaskPathId = nameof(PropertyUseMaskPathId);

        const string PropertyMaskPathId = nameof(PropertyMaskPathId);

        const string PropertyMaxHeightId = nameof(PropertyMaxHeightId);

        const string PropertySpectrumWidthId = nameof(PropertySpectrumWidthId);

        const string PropertyFrequencyScaleTypeId = nameof(PropertyFrequencyScaleTypeId);

        const string PropertyDisplayModeId = nameof(PropertyDisplayModeId);

        const string PropertySpectrumShapeTypeId = nameof(PropertySpectrumShapeTypeId);

        const string PropertyColorId = nameof(PropertyColorId);

        const string PropertyBlendModeId = nameof(PropertyBlendModeId);

        const double MinimumDecibel = -60.0;

        IAcceleratorObject? AcceleratorObject { get; set; }

        RealFft64? Fft { get; set; }

        int LastAudioLength { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            AcceleratorObject = accelerator;
        }

        public PropertyBase[] GetProperties(Int32Size sourceSize)
        {
            var center = new Vector3d(sourceSize.Width, sourceSize.Height, 0.0) * 0.5;
            return
            [
                new UseLayerAudioProperty(PropertyLayerId, LanguageResourceDictionary.ResourceKeys.Generate_AudioSpectrum_Layer, selectBoxWidth: 90.0),
                new EnumProperty(PropertyAudioLengthId, LanguageResourceDictionary.ResourceKeys.Generate_AudioSpectrum_AudioLength, typeof(AudioSpectrumAudioLengthType), typeof(LanguageResourceDictionary), AudioSpectrumAudioLengthType.Length1024, selectBoxWidth: 90.0),
                new EnumProperty(PropertyWindowFunctionTypeId, LanguageResourceDictionary.ResourceKeys.Generate_AudioSpectrum_WindowFunction, typeof(AudioSpectrumWindowFunctionType), typeof(LanguageResourceDictionary), AudioSpectrumWindowFunctionType.Hamming, selectBoxWidth: 90.0),
                new DoubleProperty(PropertyAudioOffsetId, LanguageResourceDictionary.ResourceKeys.Generate_AudioSpectrum_AudioOffset, 0.0, double.MinValue, double.MaxValue, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_MilliSecond),
                new DoubleProperty(PropertyFrequencyBandCountId, LanguageResourceDictionary.ResourceKeys.Generate_AudioSpectrum_FrequencyBandCount, 64, 1, 8192, digit: 0),
                new Vector3dProperty(PropertyBeginPointId, LanguageResourceDictionary.ResourceKeys.Generate_AudioSpectrum_BeginPoint, center - new Vector3d(center.X, 0.0, 0.0) * 0.5, digit: 2, useInteraction : true),
                new Vector3dProperty(PropertyEndPointId, LanguageResourceDictionary.ResourceKeys.Generate_AudioSpectrum_BeginPoint, center + new Vector3d(center.X, 0.0, 0.0) * 0.5, digit: 2, useInteraction : true),
                new CheckBoxProperty(PropertyUseMaskPathId, LanguageResourceDictionary.ResourceKeys.Generate_AudioSpectrum_UseMaskPath, false),
                new UseMaskPathProperty(PropertyMaskPathId, LanguageResourceDictionary.ResourceKeys.Generate_AudioSpectrum_MaskPath, selectBoxWidth: 90.0),
                new DoubleProperty(PropertyMaxHeightId, LanguageResourceDictionary.ResourceKeys.Generate_AudioSpectrum_MaxHeight, 100.0, 0.0, double.MaxValue, digit: 2),
                new DoubleProperty(PropertySpectrumWidthId, LanguageResourceDictionary.ResourceKeys.Generate_AudioSpectrum_SpectrumWidth, 3.0, 0.0, double.MaxValue, digit: 2),
                new EnumProperty(PropertyFrequencyScaleTypeId, LanguageResourceDictionary.ResourceKeys.Generate_AudioSpectrum_FrequencyScaleType, typeof(AudioSpectrumFrequencyScaleType), typeof(LanguageResourceDictionary), AudioSpectrumFrequencyScaleType.Mel, selectBoxWidth: 90.0),
                new EnumProperty(PropertyDisplayModeId, LanguageResourceDictionary.ResourceKeys.Generate_AudioSpectrum_DisplayMode, typeof(AudioSpectrumDisplayMode), typeof(LanguageResourceDictionary), AudioSpectrumDisplayMode.Both, selectBoxWidth: 90.0),
                new EnumProperty(PropertySpectrumShapeTypeId, LanguageResourceDictionary.ResourceKeys.Generate_AudioSpectrum_SpectrumShapeType, typeof(AudioSpectrumShapeType), typeof(LanguageResourceDictionary), AudioSpectrumShapeType.Bar, selectBoxWidth: 90.0),
                new ColorProperty(PropertyColorId, LanguageResourceDictionary.ResourceKeys.Generate_AudioSpectrum_Color, LanguageResourceDictionary.ResourceKeys.Dialog_ColorDialog_Title_Color, LanguageResourceDictionary.ResourceKeys.Dialog_OK, LanguageResourceDictionary.ResourceKeys.Dialog_Cancel, Vector4.One),
                new EnumProperty(PropertyBlendModeId, LanguageResourceDictionary.ResourceKeys.Generate_AudioSpectrum_BlendMode, typeof(BlendMode), typeof(LanguageResourceDictionary), BlendMode.Normal),

            ];
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, Time layerTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer, bool useGpu)
        {
            var targetLayerId = properties.GetValue(PropertyLayerId, layerTime, UseLayerAudioTarget.Empty);
            var targetLayer = targetLayerId != UseLayerAudioTarget.Empty ? composition.GetLayer(targetLayerId.LayerId) : null;
            if (targetLayer == null)
            {
                return image;
            }

            var audioLength = (int)properties.GetValue(PropertyAudioLengthId, layerTime, AudioSpectrumAudioLengthType.Length1024);
            var windowFunctionType = properties.GetValue(PropertyWindowFunctionTypeId, layerTime, AudioSpectrumWindowFunctionType.Hamming);
            var audioOffset = properties.GetValue(PropertyAudioOffsetId, layerTime, 0.0);
            var frequencyBandCount = Math.Min((int)properties.GetValue(PropertyFrequencyBandCountId, layerTime, 0.0), audioLength);
            var beginPoint = (Vector2)(properties.GetValue(PropertyBeginPointId, layerTime, Vector3d.Zero) / new Vector3d(downSamplingRateX, downSamplingRateY, 1.0));
            var endPoint = (Vector2)(properties.GetValue(PropertyEndPointId, layerTime, Vector3d.Zero) / new Vector3d(downSamplingRateX, downSamplingRateY, 1.0));
            var useMaskPath = properties.GetValue(PropertyUseMaskPathId, layerTime, false);
            var maskPathId = properties.GetValue(PropertyMaskPathId, layerTime, UseMaskPathTarget.Empty);
            var maxHeight = (float)(properties.GetValue(PropertyMaxHeightId, layerTime, 0.0) / downSamplingRateX);
            var spectrumWidth = (float)(properties.GetValue(PropertySpectrumWidthId, layerTime, 0.0) / downSamplingRateX);
            var frequencyScaleType = properties.GetValue(PropertyFrequencyScaleTypeId, layerTime, AudioSpectrumFrequencyScaleType.Linear);
            var displayMode = properties.GetValue(PropertyDisplayModeId, layerTime, AudioSpectrumDisplayMode.Both);
            var shapeType = properties.GetValue(PropertySpectrumShapeTypeId, layerTime, AudioSpectrumShapeType.Bar);
            var color = properties.GetValue(PropertyColorId, layerTime, Vector4.Zero);
            var blendMode = properties.GetValue(PropertyBlendModeId, layerTime, BlendMode.Normal);

            if (spectrumWidth <= 0.0F || color.W <= 0.0F || (shapeType == AudioSpectrumShapeType.Line && frequencyBandCount < 2))
            {
                return image;
            }

            if (Fft == null || LastAudioLength != audioLength)
            {
                Fft = new RealFft64(audioLength);
                LastAudioLength = audioLength;
            }

            var length = Time.FromTime(audioLength * Const.AudioSampleTime);
            var globalTime = layerTime + layer.SourceStartPoint + audioOffset;
            var audio = targetLayerId.AudioProcessType switch
            {
                LayerAudioProcessType.Effected => targetLayer.GetEffectedAudio(globalTime, length),
                _ => targetLayer.GetRawAudio(globalTime, length)
            };

            var input = ArrayPool<double>.Shared.Rent(audioLength);
            var audioSampleLimit = Math.Min(audioLength, audio.Length / 2);
            switch (windowFunctionType)
            {
                case AudioSpectrumWindowFunctionType.Hann:
                    for (int i = 0, m = 0; m < audioSampleLimit; i += 2, m++)
                    {
                        input[m] = (audio[i] + audio[i + 1]) * 0.5 * FastFourierTransform.HannWindow(m, audioLength);
                    }
                    break;
                case AudioSpectrumWindowFunctionType.Hamming:
                    for (int i = 0, m = 0; m < audioSampleLimit; i += 2, m++)
                    {
                        input[m] = (audio[i] + audio[i + 1]) * 0.5 * FastFourierTransform.HammingWindow(m, audioLength);
                    }
                    break;
                case AudioSpectrumWindowFunctionType.BlackmannHarris:
                    for (int i = 0, m = 0; m < audioSampleLimit; i += 2, m++)
                    {
                        input[m] = (audio[i] + audio[i + 1]) * 0.5 * FastFourierTransform.BlackmannHarrisWindow(m, audioLength);
                    }
                    break;
            }

            var real = ArrayPool<double>.Shared.Rent(audioLength + 1);
            var imaginary = ArrayPool<double>.Shared.Rent(audioLength + 1);
            real.AsSpan(0, audioLength + 1).Clear();
            imaginary.AsSpan(0, audioLength + 1).Clear();
            Fft.Direct(input, real, imaginary);
            var spectrum = ArrayPool<float>.Shared.Rent(frequencyBandCount);
            var spectrumSpan = spectrum.AsSpan(0, frequencyBandCount);
            for (var i = 0; i < frequencyBandCount; i++)
            {
                var spectrumIndex = frequencyScaleType switch
                {
                    AudioSpectrumFrequencyScaleType.Log => GetLogScaleSpectrumIndex(i, frequencyBandCount, audioLength),
                    AudioSpectrumFrequencyScaleType.Mel => GetMelScaleSpectrumIndex(i, frequencyBandCount, audioLength),
                    _ => GetLinearScaleSpectrumIndex(i, frequencyBandCount, audioLength)
                };
                var s = Math.Log10((real[spectrumIndex] * real[spectrumIndex] + imaginary[spectrumIndex] * imaginary[spectrumIndex]) / audioLength) * 10.0; // NOTE: power spectrum
                spectrumSpan[i] = (float)(1.0 - (s < MinimumDecibel ? 1.0 : s / MinimumDecibel));
            }

            var layoutPathBuilder = new PathBuilder();
            var mask = useMaskPath ? maskPathId.GetMask(layer, layerTime, downSamplingRateX)?.BuildPath()?.Flatten()?.FirstOrDefault() : null;
            layoutPathBuilder.StartFigure();
            if (mask != null && mask.Points.Length > 1)
            {
                var points = mask.Points.Span;
                layoutPathBuilder.MoveTo(points[0]);
                for (var i = 1; i < points.Length; i++)
                {
                    layoutPathBuilder.LineTo(points[i]);
                }
                if (mask.IsClosed)
                {
                    layoutPathBuilder.CloseFigure();
                }
            }
            else
            {
                layoutPathBuilder.MoveTo(beginPoint);
                layoutPathBuilder.LineTo(endPoint);
            }
            var layoutPath = new LayoutPath(layoutPathBuilder, false, false, 0.0);

            var polygons = shapeType switch
            {
                AudioSpectrumShapeType.Line => BuildSpectrumLine(spectrumSpan, maxHeight, spectrumWidth, displayMode, layoutPath),
                AudioSpectrumShapeType.Dot => BuildSpectrumDot(spectrumSpan, maxHeight, spectrumWidth, displayMode, layoutPath),
                _ => BuildSpectrumBar(spectrumSpan, maxHeight, spectrumWidth, displayMode, layoutPath)
            };

            ArrayPool<float>.Shared.Return(spectrum);
            ArrayPool<double>.Shared.Return(imaginary);
            ArrayPool<double>.Shared.Return(real);
            ArrayPool<double>.Shared.Return(input);

            if (polygons.Length < 1)
            {
                return image;
            }

            if (useGpu && AcceleratorObject != null)
            {
                var device = AcceleratorObject.CurrentDevice;
                var gpuImage = image.ToGpu(device);
                using var spectrumImage = new NGPUImage(gpuImage.Width, gpuImage.Height, device);
                ShapeRendererGPU.FillPolygonNonZero(device, polygons, spectrumImage, new SolidBrush(color));
                ImageBlendProcessor.SameSizeGpu(device, gpuImage, spectrumImage, roi, blendMode);
                return gpuImage;
            }
            else
            {
                var managedImage = image.ToManaged();
                using var spectrumImage = new NManagedImage(managedImage.Width, managedImage.Height);
                ShapeRendererCPU.FillPolygonNonZero(polygons, spectrumImage, new SolidBrush(color));
                ImageBlendProcessor.SameSizeCpu(managedImage, spectrumImage, roi, blendMode);
                return managedImage;
            }
        }

        public float[] Process(float[] audio, Time startTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }

        static Polygon[] BuildSpectrumBar(ReadOnlySpan<float> spectrum, float maxHeight, float spectrumWidth, AudioSpectrumDisplayMode displayMode, LayoutPath layoutPath)
        {
            var gap = layoutPath.TotalLength / spectrum.Length;
            var pathBuilder = new PathBuilder();

            switch (displayMode)
            {
                case AudioSpectrumDisplayMode.Up:
                    for (var i = 0; i < spectrum.Length; i++)
                    {
                        var v = spectrum[i] * maxHeight;
                        var transform = layoutPath.GetAlignToPathMatrix(i * gap);
                        pathBuilder.StartFigure();
                        pathBuilder.AddLine(Vector2.Transform(new Vector2(0.0F, -v), transform), Vector2.Transform(new Vector2(0.0F, 0.0F), transform));
                    }
                    break;
                case AudioSpectrumDisplayMode.Down:
                    for (var i = 0; i < spectrum.Length; i++)
                    {
                        var v = spectrum[i] * maxHeight;
                        var transform = layoutPath.GetAlignToPathMatrix(i * gap);
                        pathBuilder.StartFigure();
                        pathBuilder.AddLine(Vector2.Transform(new Vector2(0.0F, 0.0F), transform), Vector2.Transform(new Vector2(0.0F, v), transform));
                    }
                    break;
                default:
                    for (var i = 0; i < spectrum.Length; i++)
                    {
                        var v = spectrum[i] * maxHeight;
                        var transform = layoutPath.GetAlignToPathMatrix(i * gap);
                        pathBuilder.StartFigure();
                        pathBuilder.AddLine(Vector2.Transform(new Vector2(0.0F, -v), transform), Vector2.Transform(new Vector2(0.0F, v), transform));
                    }
                    break;
            }

            return [..pathBuilder.Build().GenerateOutline(spectrumWidth).Flatten().Select(p => new Polygon(p.Points.Span))];
        }

        static Polygon[] BuildSpectrumLine(ReadOnlySpan<float> spectrum, float maxHeight, float spectrumWidth, AudioSpectrumDisplayMode displayMode, LayoutPath layoutPath)
        {
            var gap = layoutPath.TotalLength / spectrum.Length;
            var pathBuilder = new PathBuilder();
            pathBuilder.StartFigure();

            pathBuilder.MoveTo(layoutPath.AlignToPath(0.0F, new Vector2(0.0F, 0.0F)));
            switch (displayMode)
            {
                case AudioSpectrumDisplayMode.Up:
                    for (var i = 0; i < spectrum.Length; i++)
                    {
                        var v = spectrum[i] * -maxHeight;
                        pathBuilder.LineTo(layoutPath.AlignToPath(i * gap + gap, new Vector2(0.0F, v)));
                    }
                    break;
                case AudioSpectrumDisplayMode.Down:
                    for (var i = 0; i < spectrum.Length; i++)
                    {
                        var v = spectrum[i] * maxHeight;
                        pathBuilder.LineTo(layoutPath.AlignToPath(i * gap + gap, new Vector2(0.0F, v)));
                    }
                    break;
                default:
                    {
                        var sign = -1.0F;
                        for (var i = 0; i < spectrum.Length; i++)
                        {
                            var v = spectrum[i] * maxHeight * sign;
                            pathBuilder.LineTo(layoutPath.AlignToPath(i * gap + gap, new Vector2(0.0F, v)));
                            sign = (i % 2) == 0 ? -1.0F : 1.0F;
                        }
                    }
                    break;
            }

            if (layoutPath.IsClosed)
            {
                pathBuilder.CloseFigure();
            }

            return [..pathBuilder.Build().GenerateOutline(spectrumWidth).Flatten().Select(p => new Polygon(p.Points.Span))];
        }

        static Polygon[] BuildSpectrumDot(ReadOnlySpan<float> spectrum, float maxHeight, float spectrumWidth, AudioSpectrumDisplayMode displayMode, LayoutPath layoutPath)
        {
            var gap = layoutPath.TotalLength / spectrum.Length;
            var dots = new List<EllipsePolygon>();

            switch (displayMode)
            {
                case AudioSpectrumDisplayMode.Up:
                    for (var i = 0; i < spectrum.Length; i++)
                    {
                        var v = spectrum[i] * maxHeight;
                        var transform = layoutPath.GetAlignToPathMatrix(i * gap);
                        dots.Add(new EllipsePolygon(Vector2.Transform(new Vector2(0.0F, -v), transform), spectrumWidth));
                    }
                    break;
                case AudioSpectrumDisplayMode.Down:
                    for (var i = 0; i < spectrum.Length; i++)
                    {
                        var v = spectrum[i] * maxHeight;
                        var transform = layoutPath.GetAlignToPathMatrix(i * gap);
                        dots.Add(new EllipsePolygon(Vector2.Transform(new Vector2(0.0F, v), transform), spectrumWidth));
                    }
                    break;
                default:
                    for (var i = 0; i < spectrum.Length; i++)
                    {
                        var v = spectrum[i] * maxHeight;
                        var transform = layoutPath.GetAlignToPathMatrix(i * gap);
                        dots.Add(new EllipsePolygon(Vector2.Transform(new Vector2(0.0F, -v), transform), spectrumWidth));
                        dots.Add(new EllipsePolygon(Vector2.Transform(new Vector2(0.0F, v), transform), spectrumWidth));
                    }
                    break;
            }

            return [..dots.SelectMany(e => e.Flatten()).Select(p => new Polygon(p.Points.Span))];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int GetLinearScaleSpectrumIndex(int i, int frequencyBandCount, int audioLength)
        {
            var fftLength = audioLength / 2 - 1;
            return (int)(Math.Clamp(i, 0, frequencyBandCount) / (double)frequencyBandCount * fftLength) + 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int GetLogScaleSpectrumIndex(int i, int frequencyBandCount, int audioLength)
        {
            const double LogScaleRange = 7.090076835776092; // Math.Log(Const.AudioSamplingRate / 2.0) - Math.Log(20.0)
            const double Log20Hz = 2.995732273553991; // Math.Log(20.0);
            const double FrequencyRange = Const.AudioSamplingRate / 2.0;

            var fftLength = audioLength / 2 - 1;
            var freq = Math.Pow(Math.E, LogScaleRange / frequencyBandCount * Math.Clamp(i, 0, frequencyBandCount) + Log20Hz) / FrequencyRange;
            return (int)(freq * fftLength) + 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int GetMelScaleSpectrumIndex(int i, int frequencyBandCount, int audioLength)
        {
            const double MelRange = 3984.2913390529257; // Scale.HerzToMel(24000.0) - Scale.HerzToMel(20.0)
            const double Mel20Hz = 31.748578341466644; // Scale.HerzToMel(20.0)
            const double FrequencyRange = Const.AudioSamplingRate / 2.0;

            var fftLength = audioLength / 2 - 1;
            return (int)(Scale.MelToHerz(MelRange / frequencyBandCount * Math.Clamp(i, 0, frequencyBandCount) + Mel20Hz) / FrequencyRange * fftLength) + 1;
        }
    }

    enum AudioSpectrumAudioLengthType
    {
        Length128 = 128,
        Length256 = 256,
        Length512 = 512,
        Length1024 = 1024,
        Length2048 = 2048,
        Length4096 = 4096,
        Length8192 = 8192,
        Length16384 = 16384,
        Length32768 = 32768,
        Length65536 = 65536
    }

    enum AudioSpectrumWindowFunctionType
    {
        Hann,
        Hamming,
        BlackmannHarris
    }

    enum AudioSpectrumDisplayMode
    {
        Up,
        Down,
        Both
    }

    enum AudioSpectrumShapeType
    {
        Bar,
        Line,
        Dot
    }

    enum AudioSpectrumFrequencyScaleType
    {
        Linear,
        Log,
        Mel
    }
}
