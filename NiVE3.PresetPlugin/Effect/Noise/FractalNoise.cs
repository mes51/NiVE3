using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;
using ComputeSharp;
using NiVE3.Image;
using NiVE3.Image.Drawing;
using NiVE3.Numerics;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Property.Properties;
using NiVE3.Plugin.Resource;
using NiVE3.Plugin.ValueObject;
using NiVE3.PresetPlugin.Extension;
using NiVE3.PresetPlugin.Internal.ComputeShader;
using NiVE3.PresetPlugin.Internal.Effect;
using NiVE3.PresetPlugin.Resource;
using NiVE3.Shared.Extension;

namespace NiVE3.PresetPlugin.Effect.Noise
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.Noise_FractalNoise_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_Noise, LanguageResourceDictionary.Noise_FractalNoise_Description, ID, IsSupportGpu = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class FractalNoise : IEffect
    {
        const string ID = "1A578CBE-81BB-42CA-BF41-E0911DFCBDF7";

        const string PropertyFractalTypeId = nameof(PropertyFractalTypeId);

        const string PropertyNoiseTypeId = nameof(PropertyNoiseTypeId);

        const string PropertyInvertId = nameof(PropertyInvertId);

        const string PropertyContrastId = nameof(PropertyContrastId);

        const string PropertyLuminanceId = nameof(PropertyLuminanceId);

        const string PropertyTransformGroupId = nameof(PropertyTransformGroupId);

        const string PropertyTransformPositionId = nameof(PropertyTransformPositionId);

        const string PropertyTransformScaleId = nameof(PropertyTransformScaleId);

        const string PropertyTransformAngleId = nameof(PropertyTransformAngleId);

        const string PropertyOctaveId = nameof(PropertyOctaveId);

        const string PropertyOctaveSettingGroupId = nameof(PropertyOctaveSettingGroupId);

        const string PropertyOctaveSettingAmountId = nameof(PropertyOctaveSettingAmountId);

        const string PropertyOctaveSettingPositionOffsetId = nameof(PropertyOctaveSettingPositionOffsetId);

        const string PropertyOctaveSettingScaleId = nameof(PropertyOctaveSettingScaleId);

        const string PropertyOctaveSettingAngleId = nameof(PropertyOctaveSettingAngleId);

        const string PropertyOctaveSettingCenteringScaleId = nameof(PropertyOctaveSettingCenteringScaleId);

        const string PropertyEvolutionId = nameof(PropertyEvolutionId);

        const string PropertyRandomSeedId = nameof(PropertyRandomSeedId);

        const string PropertyOpacityId = nameof(PropertyOpacityId);

        const string PropertyBlendModeId = nameof(PropertyBlendModeId);

        static readonly Vector2 AnchorPointOffset = new Vector2(0.3F, 0.7F);

        const uint CoordLimit = 1 << 16;

        IAcceleratorObject? AcceleratorObject { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            AcceleratorObject = accelerator;
        }

        public PropertyBase[] GetProperties(Int32Size sourceSize)
        {
            return
            [
                new EnumProperty(PropertyFractalTypeId, LanguageResourceDictionary.ResourceKeys.Noise_FractalNoise_FractalType, typeof(FractalType), typeof(LanguageResourceDictionary), FractalType.Normal, selectBoxWidth: 90.0),
                new EnumProperty(PropertyNoiseTypeId, LanguageResourceDictionary.ResourceKeys.Noise_FractalNoise_NoiseType, typeof(NoiseType), typeof(LanguageResourceDictionary), NoiseType.SmoothLinear, selectBoxWidth: 90.0),
                new CheckBoxProperty(PropertyInvertId, LanguageResourceDictionary.ResourceKeys.Noise_FractalNoise_Invert, false),
                new DoubleProperty(PropertyContrastId, LanguageResourceDictionary.ResourceKeys.Noise_FractalNoise_Contrast, 100.0, 0.0, 10000.0, digit: 2),
                new DoubleProperty(PropertyLuminanceId, LanguageResourceDictionary.ResourceKeys.Noise_FractalNoise_Luminance, 0.0, -10000.0, 10000.0, digit: 2),
                new PropertyGroup(PropertyTransformGroupId, LanguageResourceDictionary.ResourceKeys.Noise_FractalNoise_Transform,
                [
                    new Vector3dProperty(PropertyTransformPositionId, LanguageResourceDictionary.ResourceKeys.Noise_FractalNoise_Transform_Position, new Vector3d(), digit: 2),
                    new Vector3dProperty(PropertyTransformScaleId, LanguageResourceDictionary.ResourceKeys.Noise_FractalNoise_Transform_Scale, new Vector3d(100.0), new Vector3d(0.01), digit: 2, useLinkRatio: true),
                    new AngleProperty(PropertyTransformAngleId, LanguageResourceDictionary.ResourceKeys.Noise_FractalNoise_Transform_Angle, 0.0, digit: 2),
                ]),
                new DoubleProperty(PropertyOctaveId, LanguageResourceDictionary.ResourceKeys.Noise_FractalNoise_Octave, 6.0, 1.0, 20.0, slideChangeValue: 0.1, digit: 1),
                new PropertyGroup(PropertyOctaveSettingGroupId, LanguageResourceDictionary.ResourceKeys.Noise_FractalNoise_OctaveSetting,
                [
                    new DoubleProperty(PropertyOctaveSettingAmountId, LanguageResourceDictionary.ResourceKeys.Noise_FractalNoise_OctaveSetting_Amount, 70.0, 0.0, 1000.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent),
                    new Vector3dProperty(PropertyOctaveSettingPositionOffsetId, LanguageResourceDictionary.ResourceKeys.Noise_FractalNoise_OctaveSetting_PositionOffset, new Vector3d(), digit: 2),
                    new Vector3dProperty(PropertyOctaveSettingScaleId, LanguageResourceDictionary.ResourceKeys.Noise_FractalNoise_OctaveSetting_Scale, new Vector3d(56.0), new Vector3d(0.01), digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent, useLinkRatio: true),
                    new AngleProperty(PropertyOctaveSettingAngleId, LanguageResourceDictionary.ResourceKeys.Noise_FractalNoise_OctaveSetting_Angle, 0.0, digit: 2),
                    new CheckBoxProperty(PropertyOctaveSettingCenteringScaleId, LanguageResourceDictionary.ResourceKeys.Noise_FractalNoise_OctaveSetting_CenteringScale, false)
                ]),
                new AngleProperty(PropertyEvolutionId, LanguageResourceDictionary.ResourceKeys.Noise_FractalNoise_Evolution, 0.0, digit: 2, isOnlyPositiveDirection: true),
                new DoubleProperty(PropertyRandomSeedId, LanguageResourceDictionary.ResourceKeys.Noise_FractalNoise_RandomSeed, 0, 0, uint.MaxValue, digit: 0),
                new DoubleProperty(PropertyOpacityId, LanguageResourceDictionary.ResourceKeys.Noise_FractalNoise_Opacity, 100.0, 0.0, 100.0, digit: 2),
                new EnumProperty(PropertyBlendModeId, LanguageResourceDictionary.ResourceKeys.Noise_FractalNoise_BlendMode, typeof(BlendMode), typeof(LanguageResourceDictionary), BlendMode.Normal, selectBoxWidth: 90.0)
            ];
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, double layerTime, IPropertyObject[] properties, ICompositionObject composition, bool useGpu)
        {
            var fractalType = properties.GetValue(PropertyFractalTypeId, layerTime, FractalType.Normal);
            var noiseType = properties.GetValue(PropertyNoiseTypeId, layerTime, NoiseType.Parlin);
            var invert = properties.GetValue(PropertyInvertId, layerTime, false);
            var contrast = (float)properties.GetValue(PropertyContrastId, layerTime, 100.0) * 0.01F;
            var luminance = (float)properties.GetValue(PropertyLuminanceId, layerTime, 0.0) * 0.01F;
            var octave = (float)properties.GetValue(PropertyOctaveId, layerTime, 6.0);
            var evolution = (float)properties.GetValue(PropertyEvolutionId, layerTime, 0.0) * 0.1F;
            var randomSeed = (uint)properties.GetValue(PropertyRandomSeedId, layerTime, 0.0);
            var opacity = (float)properties.GetValue(PropertyOpacityId, layerTime, 100.0) * 0.01F;
            var blendMode = properties.GetValue(PropertyBlendModeId, layerTime, BlendMode.Normal);

            var transformGroup = properties.First(p => p.Id == PropertyTransformGroupId).GetChildren() ?? [];
            var position = (Vector3)transformGroup.GetValue(PropertyTransformPositionId, layerTime, new Vector3d());
            var scale = (Vector3)transformGroup.GetValue(PropertyTransformScaleId, layerTime, new Vector3d());
            var angle = (float)transformGroup.GetValue(PropertyTransformAngleId, layerTime, 0.0);

            var octaveSetting = properties.First(p => p.Id == PropertyOctaveSettingGroupId).GetChildren() ?? [];
            var octaveAmount = (float)octaveSetting.GetValue(PropertyOctaveSettingAmountId, layerTime, 70.0) * 0.01F;
            var octavePositionOffset = (Vector3)octaveSetting.GetValue(PropertyOctaveSettingPositionOffsetId, layerTime, new Vector3d());
            var octaveScale = (Vector3)octaveSetting.GetValue(PropertyOctaveSettingScaleId, layerTime, new Vector3d()) * 0.01F;
            var octaveAngle = (float)octaveSetting.GetValue(PropertyOctaveSettingAngleId, layerTime, 0.0);
            var octaveCenteringScale = octaveSetting.GetValue(PropertyOctaveSettingCenteringScaleId, layerTime, false);

            if (useGpu && AcceleratorObject != null)
            {
                return ProcessGpu(
                    AcceleratorObject.CurrentDevice,
                    image,
                    roi,
                    fractalType,
                    noiseType,
                    invert,
                    contrast,
                    luminance,
                    position,
                    scale,
                    angle,
                    octave,
                    octaveAmount,
                    octavePositionOffset,
                    octaveScale,
                    octaveAngle,
                    octaveCenteringScale,
                    evolution,
                    randomSeed,
                    opacity,
                    blendMode
                );
            }
            else
            {
                return ProcessCpu(
                    image,
                    roi,
                    fractalType,
                    noiseType,
                    invert,
                    contrast,
                    luminance,
                    position,
                    scale,
                    angle,
                    octave,
                    octaveAmount,
                    octavePositionOffset,
                    octaveScale,
                    octaveAngle,
                    octaveCenteringScale,
                    evolution,
                    randomSeed,
                    opacity,
                    blendMode
                );
            }
        }

        public float[] Process(float[] audio, double startTime, IPropertyObject[] properties, ICompositionObject composition)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }

        static NManagedImage ProcessCpu(
            NImage image,
            ROI roi,
            FractalType fractalType,
            NoiseType noiseType,
            bool isInvert,
            float contrast,
            float luminance,
            Vector3 position,
            Vector3 scale,
            float angle,
            float octave,
            float octaveAmount,
            Vector3 octavePositionOffset,
            Vector3 octaveScale,
            float octaveAngle,
            bool octaveIsCenteringScale,
            float evolution,
            uint randomSeed,
            float opacity,
            BlendMode blendMode
        )
        {
            var managedImage = image switch
            {
                NGPUImage gpuImage => gpuImage.CopyToCpu(),
                _ => (NManagedImage)image
            };

            var center = new Vector2(roi.OriginalImageSize.Width, roi.OriginalImageSize.Height) * 0.5F + AnchorPointOffset;
            var transform = Matrix3x3.AffineTransform(center, scale.AsVector2(), angle, center + position.AsVector2());

            var bx = roi.Left;
            var ex = roi.Right;
            var uEvolution = ((uint)(int)evolution) % CoordLimit;
            var diffEvolution = Frac(evolution);
            var noiseData = ArrayPool<float>.Shared.Rent(managedImage.DataLength);
            noiseData.AsSpan(0, managedImage.DataLength).Clear();
            var temp = ArrayPool<float>.Shared.Rent(managedImage.DataLength);

            var denom = 0.0F;
            for (int o = 0, limit = (int)MathF.Ceiling(octave); o < limit; o++)
            {
                var subTransform = Matrix3x3.AffineTransform(
                    center * (1.0F + (octaveIsCenteringScale ? 0.0F : o * 2.0F)),
                    new Vector2(MathF.Pow(octaveScale.X, o), MathF.Pow(octaveScale.Y, o)),
                    octaveAngle * o,
                    center + octavePositionOffset.AsVector2() * o
                );
                if (!Matrix3x3.Invert(subTransform * transform, out var inverted))
                {
                    return managedImage;
                }

                var amount = MathF.Pow(octaveAmount, Math.Min(o, (int)octave)) * octaveAmount * Math.Min(octave - o, 1.0F);
                if (amount <= 0.0F)
                {
                    if (o > 0)
                    {
                        break;
                    }
                    else
                    {
                        amount = 1.0F;
                    }
                }
                denom += amount;
                switch (noiseType)
                {
                    case NoiseType.Block:
                        Parallel.For(roi.Top, roi.Bottom, y =>
                        {
                            var tempSpan = temp.AsSpan(y * managedImage.Width, managedImage.Width);
                            for (var x = bx; x < ex; x++)
                            {
                                var (nx, ny) = inverted.Transform(x, y);
                                var ux = ((uint)(int)nx) % CoordLimit;
                                var uy = ((uint)(int)ny) % CoordLimit;
                                var pnoise = NoiseFunction.Pcg3D1FloatCpu(ux, uy, uEvolution, randomSeed);
                                var nnoise = NoiseFunction.Pcg3D1FloatCpu(ux, uy, (uEvolution + 1) % CoordLimit, randomSeed);
                                tempSpan[x] = float.Lerp(pnoise, nnoise, diffEvolution);
                            }
                        });
                        break;
                    case NoiseType.Linear:
                        Parallel.For(roi.Top, roi.Bottom, y =>
                        {
                            var tempSpan = temp.AsSpan(y * managedImage.Width, managedImage.Width);
                            for (var x = bx; x < ex; x++)
                            {
                                var (nx, ny) = inverted.Transform(x, y);
                                nx -= 0.5F;
                                ny -= 0.5F;
                                var ux = ((uint)(int)nx) % CoordLimit;
                                var uy = ((uint)(int)ny) % CoordLimit;
                                var diffX = Frac(nx);
                                var diffY = Frac(ny);
                                var pnoise = NoiseFunction.Pcg3D1Vector4Cpu(ux, uy, uEvolution, randomSeed);
                                var nnoise = NoiseFunction.Pcg3D1Vector4Cpu(ux, uy, (uEvolution + 1) % CoordLimit, randomSeed);
                                var enoise = Vector4.Lerp(pnoise, nnoise, diffEvolution);
                                tempSpan[x] = float.Lerp(
                                    float.Lerp(enoise.X, enoise.Y, diffX),
                                    float.Lerp(enoise.Z, enoise.W, diffX),
                                    diffY
                                );
                            }
                        });
                        break;
                    case NoiseType.SmoothLinear:
                        Parallel.For(roi.Top, roi.Bottom, y =>
                        {
                            var tempSpan = temp.AsSpan(y * managedImage.Width, managedImage.Width);
                            for (var x = bx; x < ex; x++)
                            {
                                var (nx, ny) = inverted.Transform(x, y);
                                nx -= 0.5F;
                                ny -= 0.5F;
                                var ux = ((uint)(int)nx) % CoordLimit;
                                var uy = ((uint)(int)ny) % CoordLimit;
                                var diffX = SmoothFade(Frac(nx));
                                var diffY = SmoothFade(Frac(ny));
                                var pnoise = NoiseFunction.Pcg3D1Vector4Cpu(ux, uy, uEvolution, randomSeed);
                                var nnoise = NoiseFunction.Pcg3D1Vector4Cpu(ux, uy, (uEvolution + 1) % CoordLimit, randomSeed);
                                var enoise = Vector4.Lerp(pnoise, nnoise, SmoothFade(diffEvolution));
                                tempSpan[x] = float.Lerp(
                                    float.Lerp(enoise.X, enoise.Y, diffX),
                                    float.Lerp(enoise.Z, enoise.W, diffX),
                                    diffY
                                );
                            }
                        });
                        break;
                    case NoiseType.Parlin:
                        Parallel.For(roi.Top, roi.Bottom, y =>
                        {
                            var tempSpan = temp.AsSpan(y * managedImage.Width, managedImage.Width);
                            for (var x = bx; x < ex; x++)
                            {
                                var (nx, ny) = inverted.Transform(x, y);
                                var ux = ((uint)(int)nx) % CoordLimit;
                                var uy = ((uint)(int)ny) % CoordLimit;
                                var diffX = Frac(nx);
                                var diffY = Frac(ny);
                                var u = PerlinFade(diffX);
                                var v = PerlinFade(diffY);
                                var w = PerlinFade(diffEvolution);

                                var pnoise = NoiseFunction.Pcg3D1Vector128UIntCpu(ux, uy, uEvolution, randomSeed);
                                var nnoise = NoiseFunction.Pcg3D1Vector128UIntCpu(ux, uy, (uEvolution + 1) % CoordLimit, randomSeed);

                                var fz = Vector4.Lerp(
                                    new Vector4(
                                        PerlinGrad(pnoise.GetElement(0), diffX, diffY, diffEvolution),
                                        PerlinGrad(pnoise.GetElement(1), diffX - 1.0F, diffY, diffEvolution),
                                        PerlinGrad(pnoise.GetElement(2), diffX, diffY - 1.0F, diffEvolution),
                                        PerlinGrad(pnoise.GetElement(3), diffX - 1.0F, diffY - 1.0F, diffEvolution)
                                    ),
                                    new Vector4(
                                        PerlinGrad(nnoise.GetElement(0), diffX, diffY, diffEvolution - 1.0F),
                                        PerlinGrad(nnoise.GetElement(1), diffX - 1.0F, diffY, diffEvolution - 1.0F),
                                        PerlinGrad(nnoise.GetElement(2), diffX, diffY - 1.0F, diffEvolution - 1.0F),
                                        PerlinGrad(nnoise.GetElement(3), diffX - 1.0F, diffY - 1.0F, diffEvolution - 1.0F)
                                    ),
                                    w
                                ).AsVector128();
                                var fy = Vector2.Lerp(Vector128.GetLower(fz).AsVector2(), Vector128.GetUpper(fz).AsVector2(), v);
                                // NOTE: 範囲は-√0.5～√0.5
                                tempSpan[x] = (float.Lerp(fy.X, fy.Y, u) + 0.7071067811865476F) / 1.4142135623730951F;
                            }
                        });
                        break;
                }

                switch (fractalType)
                {
                    case FractalType.Turbulent:
                        Parallel.For(roi.Top, roi.Bottom, y =>
                        {
                            var tempSpan = temp.AsSpan(y * managedImage.Width, managedImage.Width);
                            var noiseDataSpan = noiseData.AsSpan(y * managedImage.Width, managedImage.Width);
                            for (var x = bx; x < ex; x++)
                            {
                                noiseDataSpan[x] += Math.Abs(tempSpan[x] - 0.5F) * 2.0F * amount;
                            }
                        });
                        break;
                    case FractalType.Max:
                        Parallel.For(roi.Top, roi.Bottom, y =>
                        {
                            var tempSpan = temp.AsSpan(y * managedImage.Width, managedImage.Width);
                            var noiseDataSpan = noiseData.AsSpan(y * managedImage.Width, managedImage.Width);
                            for (var x = bx; x < ex; x++)
                            {
                                noiseDataSpan[x] += Math.Max(noiseDataSpan[x], (tempSpan[x] - 0.5F) * 2.0F) * amount;
                            }
                        });
                        break;
                    default:
                        Parallel.For(roi.Top, roi.Bottom, y =>
                        {
                            var tempSpan = temp.AsSpan(y * managedImage.Width, managedImage.Width);
                            var noiseDataSpan = noiseData.AsSpan(y * managedImage.Width, managedImage.Width);
                            for (var x = bx; x < ex; x++)
                            {
                                noiseDataSpan[x] += tempSpan[x] * amount;
                            }
                        });
                        break;
                }
            }

            Parallel.For(roi.Top, roi.Bottom, y =>
            {
                var noiseDataSpan = noiseData.AsSpan(y * managedImage.Width, managedImage.Width);
                var imageDataSpan = managedImage.GetDataSpan().Slice(y * managedImage.Width, managedImage.Width);
                for (var x = bx; x < ex; x++)
                {
                    var value = noiseDataSpan[x] / denom;
                    if (isInvert)
                    {
                        value = 1.0F - value;
                    }
                    value = (value - 0.5F) * contrast + 0.5F + luminance;
                    imageDataSpan[x] = Blend.Process(blendMode, imageDataSpan[x], new Vector4(value, value, value, opacity));
                }
            });

            return managedImage;
        }

        static NGPUImage ProcessGpu(
            GraphicsDevice device,
            NImage image,
            ROI roi,
            FractalType fractalType,
            NoiseType noiseType,
            bool isInvert,
            float contrast,
            float luminance,
            Vector3 position,
            Vector3 scale,
            float angle,
            float octave,
            float octaveAmount,
            Vector3 octavePositionOffset,
            Vector3 octaveScale,
            float octaveAngle,
            bool octaveIsCenteringScale,
            float evolution,
            uint randomSeed,
            float opacity,
            BlendMode blendMode
        )
        {
            var gpuImage = image switch
            {
                NManagedImage managedImage => managedImage.CopyToGpu(device),
                _ => (NGPUImage)image
            };

            var center = new Vector2(roi.OriginalImageSize.Width, roi.OriginalImageSize.Height) * 0.5F + AnchorPointOffset;
            var transform = Matrix3x3.AffineTransform(center, scale.AsVector2(), angle, center + position.AsVector2());
            var denom = 0.0F;

            using var noise = device.AllocateReadWriteBuffer<float>(image.DataLength);
            using (var context = device.CreateComputeContext())
            {
                for (int o = 0, limit = (int)MathF.Ceiling(octave); o < limit; o++)
                {
                    var subTransform = Matrix3x3.AffineTransform(
                        center * (1.0F + (octaveIsCenteringScale ? 0.0F : o * 2.0F)),
                        new Vector2(MathF.Pow(octaveScale.X, o), MathF.Pow(octaveScale.Y, o)),
                        octaveAngle * o,
                        center + octavePositionOffset.AsVector2() * o
                    );
                    if (!Matrix3x3.Invert(subTransform * transform, out var inverted))
                    {
                        return gpuImage;
                    }

                    var amount = MathF.Pow(octaveAmount, Math.Min(o, (int)octave)) * octaveAmount * Math.Min(octave - o, 1.0F);
                    if (amount <= 0.0F)
                    {
                        if (o > 0)
                        {
                            break;
                        }
                        else
                        {
                            amount = 1.0F;
                        }
                    }
                    denom += amount;

                    context.For(
                        roi.Width,
                        roi.Height,
                        new GenerateNoiseProcess(
                            noise,
                            gpuImage.Width,
                            (int)fractalType,
                            (int)noiseType,
                            inverted.ToFloat3x3(),
                            amount,
                            evolution,
                            randomSeed,
                            roi.Left,
                            roi.Top
                        )
                    );
                    context.Barrier(noise);
                }

                context.For(
                    roi.Width,
                    roi.Height,
                    new FractalNoiseBlendProcess(
                        gpuImage.Data,
                        gpuImage.Width,
                        noise,
                        denom,
                        contrast,
                        luminance,
                        isInvert,
                        opacity,
                        (int)blendMode,
                        roi.Left,
                        roi.Top
                    )
                );
            }

            return gpuImage;
        }

        // http://riven8192.blogspot.com/2010/08/calculate-perlinnoise-twice-as-fast.html
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float PerlinGrad(uint hash, float x, float y, float z)
        {
            return (hash & 0xF) switch
            {
                0x0 => x + y,
                0x1 => -x + y,
                0x2 => x - y,
                0x3 => -x - y,
                0x4 => x + z,
                0x5 => -x + z,
                0x6 => x - z,
                0x7 => -x - z,
                0x8 => y + z,
                0x9 => -y + z,
                0xA => y - z,
                0xB => -y - z,
                0xC => y + x,
                0xD => -y + z,
                0xE => y - x,
                0xF => -y - z,
                _ => 0.0F
            };
        }

        // https://mrl.cs.nyu.edu/~perlin/noise/
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float PerlinFade(float t)
        {
            return t * t * t * (t * (t * 6.0F - 15.0F) + 10.0F);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float SmoothFade(float t)
        {
            return t * t * (3.0F - 2.0F * t);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float Frac(float v)
        {
            return v - MathF.Floor(v);
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct GenerateNoiseProcess(
        ReadWriteBuffer<float> noise,
        int width,
        int fractalType,
        int noiseType,
        Float3x3 invertedTransform,
        float amount,
        float evolution,
        uint randomSeed,
        int startX,
        int startY
    ) : IComputeShader
    {
        const uint CoordLimit = 1 << 16;

        public void Execute()
        {
            var uEvolution = ((uint)(int)evolution) % CoordLimit;
            var noisePos = invertedTransform * new Float3(ThreadIds.X, ThreadIds.Y, 1.0F);
            var noiseValue = 0.0F;

            switch (noiseType)
            {
                case 1:
                    {
                        noisePos -= 0.5F;
                        var ux = ((uint)(int)noisePos.X) % CoordLimit;
                        var uy = ((uint)(int)noisePos.Y) % CoordLimit;
                        var diff = Hlsl.Frac(noisePos);
                        var pnoise = NoiseFunction.Pcg3D1Float4Gpu(new UInt3(ux, uy, uEvolution), randomSeed);
                        var nnoise = NoiseFunction.Pcg3D1Float4Gpu(new UInt3(ux, uy, (uEvolution + 1) % CoordLimit), randomSeed);
                        var enoise = Hlsl.Lerp(pnoise, nnoise, Hlsl.Frac(evolution));
                        noiseValue = Hlsl.Lerp(
                            Hlsl.Lerp(enoise.X, enoise.Y, diff.X),
                            Hlsl.Lerp(enoise.Z, enoise.W, diff.X),
                            diff.Y
                        );
                    }
                    break;
                case 2:
                    {
                        noisePos -= 0.5F;
                        var ux = ((uint)(int)noisePos.X) % CoordLimit;
                        var uy = ((uint)(int)noisePos.Y) % CoordLimit;
                        var diff = Hlsl.SmoothStep(Float3.Zero, Float3.One, Hlsl.Frac(new Float3(noisePos.XY, evolution)));
                        var pnoise = NoiseFunction.Pcg3D1Float4Gpu(new UInt3(ux, uy, uEvolution), randomSeed);
                        var nnoise = NoiseFunction.Pcg3D1Float4Gpu(new UInt3(ux, uy, (uEvolution + 1) % CoordLimit), randomSeed);
                        var enoise = Hlsl.Lerp(pnoise, nnoise, diff.Z);
                        noiseValue = Hlsl.Lerp(
                            Hlsl.Lerp(enoise.X, enoise.Y, diff.X),
                            Hlsl.Lerp(enoise.Z, enoise.W, diff.X),
                            diff.Y
                        );
                    }
                    break;
                case 3:
                    {
                        noisePos -= 0.5F;
                        var ux = ((uint)(int)noisePos.X) % CoordLimit;
                        var uy = ((uint)(int)noisePos.Y) % CoordLimit;
                        var diff = Hlsl.Frac(new Float3(noisePos.XY, evolution));
                        var fade = PerlinFade(diff);

                        var pnoise = NoiseFunction.Pcg3D1UInt4Gpu(new UInt3(ux, uy, uEvolution), randomSeed);
                        var nnoise = NoiseFunction.Pcg3D1UInt4Gpu(new UInt3(ux, uy, (uEvolution + 1) % CoordLimit), randomSeed);

                        var fz = Hlsl.Lerp(
                            new Float4(
                                PerlinGrad(pnoise.X, diff),
                                PerlinGrad(pnoise.Y, diff - Float3.UnitX),
                                PerlinGrad(pnoise.Z, diff - Float3.UnitY),
                                PerlinGrad(pnoise.W, diff - Float3.UnitX - Float3.UnitY)
                            ),
                            new Float4(
                                PerlinGrad(nnoise.X, diff - Float3.UnitZ),
                                PerlinGrad(nnoise.Y, diff - Float3.UnitX - Float3.UnitZ),
                                PerlinGrad(nnoise.Z, diff - Float3.UnitY - Float3.UnitZ),
                                PerlinGrad(nnoise.W, diff - Float3.One)
                            ),
                            fade.Z
                        );
                        var fy = Hlsl.Lerp(fz.XY, fz.ZW, fade.Y);
                        // NOTE: 範囲は-√0.5～√0.5
                        noiseValue = (Hlsl.Lerp(fy.X, fy.Y, fade.X) + 0.7071067811865476F) / 1.4142135623730951F;
                    }
                    break;
                default:
                    {
                        var ux = ((uint)(int)noisePos.X) % CoordLimit;
                        var uy = ((uint)(int)noisePos.Y) % CoordLimit;
                        var pnoise = NoiseFunction.Pcg3D1FloatGpu(new UInt3(ux, uy, uEvolution), randomSeed);
                        var nnoise = NoiseFunction.Pcg3D1FloatGpu(new UInt3(ux, uy, (uEvolution + 1) % CoordLimit), randomSeed);
                        noiseValue = Hlsl.Lerp(pnoise, nnoise, Hlsl.Frac(evolution));
                    }
                    break;
            }

            var pos = (ThreadIds.Y + startY) * width + ThreadIds.X + startX;
            switch (fractalType)
            {
                case 1:
                    noise[pos] += Hlsl.Abs(noiseValue - 0.5F) * 2.0F * amount;
                    break;
                case 2:
                    noise[pos] += Hlsl.Max(noise[pos], (noiseValue - 0.5F) * 2.0F) * amount;
                    break;
                default:
                    noise[pos] += noiseValue * amount;
                    break;
            }
        }

        // https://mrl.cs.nyu.edu/~perlin/noise/
        static Float3 PerlinFade(Float3 t)
        {
            return t * t * t * (t * (t * 6.0F - 15.0F) + 10.0F);
        }

        // http://riven8192.blogspot.com/2010/08/calculate-perlinnoise-twice-as-fast.html
        static float PerlinGrad(uint hash, Float3 pos)
        {
            switch (hash & 0xF)
            {
                case 0x0:
                    return pos.X + pos.Y;
                case 0x1:
                    return -pos.X + pos.Y;
                case 0x2:
                    return pos.X - pos.Y;
                case 0x3:
                    return -pos.X - pos.Y;
                case 0x4:
                    return pos.X + pos.Z;
                case 0x5:
                    return -pos.X + pos.Z;
                case 0x6:
                    return pos.X - pos.Z;
                case 0x7:
                    return -pos.X - pos.Z;
                case 0x8:
                    return pos.Y + pos.Z;
                case 0x9:
                    return -pos.Y + pos.Z;
                case 0xA:
                    return pos.Y - pos.Z;
                case 0xB:
                    return -pos.Y - pos.Z;
                case 0xC:
                    return pos.Y + pos.X;
                case 0xD:
                    return -pos.Y + pos.Z;
                case 0xE:
                    return pos.Y - pos.X;
                case 0xF:
                    return -pos.Y - pos.Z;
                default:
                    return 0.0F;
            }
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct FractalNoiseBlendProcess(ReadWriteBuffer<Float4> image, int width, ReadWriteBuffer<float> noise, float denom, float contrast, float luminance, Bool isInvert, float opacity, int blendMode, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            var pos = (ThreadIds.Y + startY) * width + ThreadIds.X + startX;
            var value = noise[pos] / denom;
            if (isInvert)
            {
                value = 1.0F - value;
            }
            value = (value - 0.5F) * contrast + 0.5F + luminance;
            image[pos] = BlendMethods.Process(blendMode, image[pos], new Float4(value, value, value, opacity));
        }
    }

    enum FractalType
    {
        Normal,
        Turbulent,
        Max
    }

    enum NoiseType
    {
        Block,
        Linear,
        SmoothLinear,
        Parlin
    }
}
