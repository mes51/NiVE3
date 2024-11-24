using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ComputeSharp;
using NiVE3.Image;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.PresetPlugin.Internal;
using NiVE3.PresetPlugin.Internal.View;
using NiVE3.PresetPlugin.Internal.ViewModel;
using NiVE3.PresetPlugin.Resource;
using NiVE3.Shared.Extension;

namespace NiVE3.PresetPlugin.ToneMapper
{
    // SEE: https://64.github.io/tonemapping/#extended-reinhard-luminance-tone-map

    [Export(typeof(IToneMapper))]
    [ToneMapperMetadata(typeof(ACESFilmicToneMapper), LanguageResourceDictionary.ToneMapper_ReinhardExtendedToneMapper_Name, "mes51", LanguageResourceDictionary.ToneMapper_ReinhardExtendedToneMapper_Description, ID, HasSettingView = true, IsSupportGpu = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class ReinhardExtendedToneMapper : IToneMapper
    {
        const string ID = "7DEFA2FC-A1A6-49F5-BEF9-5E2D3C3ADB71";

        IAcceleratorObject? AcceleratorObject { get; set; }

        float MaxLuminance { get; set; } = 1.0F;

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            AcceleratorObject = accelerator;
        }

        public FrameworkElement? GetToneMapperSetting()
        {
            var viewModel = new ReinhardExtendedToneMapperSettingViewModel
            {
                MaxLuminance = MaxLuminance
            };
            return new ReinhardExtendedToneMapperSettingView { DataContext = viewModel };
        }

        public object? SaveSetting()
        {
            return new ReinhardExtendedSetting { MaxLuminance = MaxLuminance };
        }

        public bool LoadSetting(object? data)
        {
            if (data is ReinhardExtendedSetting setting)
            {
                MaxLuminance = setting.MaxLuminance;
                return true;
            }
            else if (data is IDictionary<string, object?> dictionary && dictionary.TryGetValue(nameof(ReinhardExtendedSetting.MaxLuminance), out var maxLuminanceData))
            {
                try
                {
                    MaxLuminance = Convert.ToSingle(maxLuminanceData ?? 1.0F);
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }

        public bool ApplySetting(object? setting)
        {
            if (setting is not ReinhardExtendedToneMapperSettingViewModel viewModel)
            {
                return false;
            }

            MaxLuminance = viewModel.MaxLuminance;
            return true;
        }

        public NImage ToneMapping(NImage image, bool useGpu)
        {
            var squaredMaxLuminance = MaxLuminance * MaxLuminance;
            if (useGpu && AcceleratorObject != null)
            {
                return ProcessGpu(AcceleratorObject.CurrentDevice, image, squaredMaxLuminance);
            }
            else
            {
                return ProcessCpu(image, squaredMaxLuminance);
            }
        }

        public void Dispose() { }

        static NManagedImage ProcessCpu(NImage image, float squaredMaxLuminance)
        {
            var managedImage = image switch
            {
                NGPUImage gpuImage => gpuImage.CopyToCpu(),
                _ => (NManagedImage)image
            };

            var imageWidth = managedImage.Width;
            var imageData = managedImage.Data;
            Parallel.For(0, managedImage.Height, y =>
            {
                var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                for (var x = 0; x < imageDataSpan.Length; x++)
                {
                    var color = imageDataSpan[x];
                    var oldLuminance = Vector4.Dot(color, Const.ConvertToGrayScale);
                    if (oldLuminance == 0.0F || oldLuminance == -1.0F)
                    {
                        imageDataSpan[x] = new Vector4(0.0F, 0.0F, 0.0F, color.W);
                    }
                    else
                    {
                        var num = oldLuminance * (1.0F + (oldLuminance / squaredMaxLuminance));
                        var newLuminance = num / (1.0F + oldLuminance);
                        var alpha = color.W;
                        color *= newLuminance / oldLuminance;
                        color.W = alpha;
                        imageDataSpan[x] = color;
                    }
                }
            });

            return managedImage;
        }

        static NGPUImage ProcessGpu(GraphicsDevice device, NImage image, float squaredMaxLuminance)
        {
            var gpuImage = image switch
            {
                NManagedImage managedImage => managedImage.CopyToGpu(device),
                _ => (NGPUImage)image
            };

            using var context = device.CreateComputeContext();
            context.For(gpuImage.Width, gpuImage.Height, new ReinhardExtendedProcess(gpuImage.Data, gpuImage.Width, squaredMaxLuminance));

            return gpuImage;
        }
    }

    class ReinhardExtendedSetting
    {
        public float MaxLuminance { get; set; } = 1.0F;
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct ReinhardExtendedProcess(ReadWriteBuffer<Float4> image, int width, float squaredMaxLuminance) : IComputeShader
    {
        public void Execute()
        {
            var pos = ThreadIds.Y * width + ThreadIds.X;

            var color = image[pos];
            var oldLuminance = Hlsl.Dot(color.XYZ, Const.ConvertToGrayScaleFloat3);
            if (oldLuminance == 0.0F || oldLuminance == -1.0F)
            {
                image[pos] = new Float4(0.0F, 0.0F, 0.0F, color.W);
            }
            else
            {
                var num = oldLuminance * (1.0F + (oldLuminance / squaredMaxLuminance));
                var newLuminance = num / (1.0F + oldLuminance);
                image[pos] = new Float4((color * (newLuminance / oldLuminance)).XYZ, color.W);
            }
        }
    }
}
