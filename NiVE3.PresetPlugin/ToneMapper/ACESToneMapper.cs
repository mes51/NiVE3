using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ComputeSharp;
using NiVE3.Image;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.PresetPlugin.Resource;

namespace NiVE3.PresetPlugin.ToneMapper
{
    // SEE: https://github.com/TheRealMJP/BakingLab/blob/master/BakingLab/ACES.hlsl

    [Export(typeof(IToneMapper))]
    [ToneMapperMetadata(typeof(ACESFilmicToneMapper), LanguageResourceDictionary.ToneMapper_ACESToneMapper_Name, "mes51", LanguageResourceDictionary.ToneMapper_ACESToneMapper_Description, ID, IsSupportGpu = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class ACESToneMapper : IToneMapper
    {
        const string ID = "25DC98C5-5953-42DF-998D-B8F08249D24B";

        IAcceleratorObject? AcceleratorObject { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            AcceleratorObject = accelerator;
        }

        public NImage ToneMapping(NImage image, bool useGpu)
        {
            if (useGpu && AcceleratorObject != null)
            {
                return ProcessGpu(AcceleratorObject.CurrentDevice, image);
            }
            else
            {
                return ProcessCpu(image);
            }
        }

        public void Dispose() { }

        static NManagedImage ProcessCpu(NImage image)
        {
            var managedImage = image.ToManaged();

            var imageWidth = managedImage.Width;
            var imageData = managedImage.Data;
            Parallel.For(0, managedImage.Height, y =>
            {
                var ACESInputRow1 = new Vector4(0.04823F, 0.35458F, 0.59719F, 0.0F);
                var ACESInputRow2 = new Vector4(0.01566F, 0.90834F, 0.07600F, 0.0F);
                var ACESInputRow3 = new Vector4(0.83777F, 0.13383F, 0.02840F, 0.0F);
                var ACESOutputRow1 = new Vector4(-0.07367F, -0.53108F, 1.60475F, 0.0F);
                var ACESOutputRow2 = new Vector4(-0.00605F, 1.10813F, -0.10208F, 0.0F);
                var ACESOutputRow3 = new Vector4(1.07602F, -0.07276F, -0.00327F, 0.0F);

                var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                for (var x = 0; x < imageDataSpan.Length; x++)
                {
                    var color = imageDataSpan[x];
                    var alpha = color.W;
                    var v = new Vector4(
                        Vector4.Dot(color, ACESInputRow1),
                        Vector4.Dot(color, ACESInputRow2),
                        Vector4.Dot(color, ACESInputRow3),
                        0.0F
                    );

                    var a = v * (v + new Vector4(0.0245786F)) - new Vector4(0.000090537F);
                    var b = v * (new Vector4(0.983729F) * v + new Vector4(0.4329510F)) + new Vector4(0.238081F);
                    v = a / b;

                    imageDataSpan[x] = new Vector4(
                        Vector4.Dot(v, ACESOutputRow1),
                        Vector4.Dot(v, ACESOutputRow2),
                        Vector4.Dot(v, ACESOutputRow3),
                        alpha
                    );
                }
            });

            return managedImage;
        }

        static NGPUImage ProcessGpu(GraphicsDevice device, NImage image)
        {
            var gpuImage = image.ToGpu(device);

            using var context = device.CreateComputeContext();
            context.For(gpuImage.Width, gpuImage.Height, new ACESProcess(gpuImage.Data, gpuImage.Width));

            return gpuImage;
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct ACESProcess(ReadWriteBuffer<Float4> image, int width) : IComputeShader
    {
        static readonly Float3x3 ACESInputMatrix = new Float3x3(
            0.59719F, 0.35458F, 0.04823F,
            0.07600F, 0.90834F, 0.01566F,
            0.02840F, 0.13383F, 0.83777F
        );
        static readonly Float3x3 ACESOutputMatrix = new Float3x3(
            1.60475F, -0.53108F, -0.07367F,
            -0.10208F, 1.10813F, -0.00605F,
            -0.00327F, -0.07276F, 1.07602F
        );

        public void Execute()
        {
            var pos = ThreadIds.Y * width + ThreadIds.X;

            var color = image[pos];
            var alpha = color.W;

            var v = Hlsl.Mul(ACESInputMatrix, color.ZYX);

            var a = v * (v + 0.0245786F) - 0.000090537F;
            var b = v * (0.983729F * v + 0.4329510F) + 0.238081F;
            v = a / b;

            image[pos] = new Float4(Hlsl.Mul(ACESOutputMatrix, v).ZYX, alpha);
        }
    }
}
