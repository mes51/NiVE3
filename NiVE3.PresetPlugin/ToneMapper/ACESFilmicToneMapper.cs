using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ComputeSharp;
using NiVE3.Image;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.PresetPlugin.Resource;

namespace NiVE3.PresetPlugin.ToneMapper
{
    // SEE: https://knarkowicz.wordpress.com/2016/01/06/aces-filmic-tone-mapping-curve/

    [Export(typeof(IToneMapper))]
    [ToneMapperMetadata(typeof(ACESFilmicToneMapper), LanguageResourceDictionary.ToneMapper_ACESFilmicToneMapper_Name, "mes51", LanguageResourceDictionary.ToneMapper_ACESFilmicToneMapper_Description, ID, IsSupportGpu = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class ACESFilmicToneMapper : IToneMapper
    {
        const string ID = "3ED40944-167E-457D-BD50-E017BADED959";

        static readonly Vector<float> AlphaMask = new Vector<float>([.. Enumerable.Range(0, Vector<float>.Count / 4).SelectMany(_ => new float[] { 0.0F, 0.0F, 0.0F, BitConverter.Int32BitsToSingle(-1) })]);

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
            var managedImage = image switch
            {
                NGPUImage gpuImage => gpuImage.CopyToCpu(),
                _ => (NManagedImage)image
            };

            const float a = 2.51F;
            const float b = 0.03F;
            const float c = 2.43F;
            const float d = 0.59F;
            const float e = 0.14F;

            var imageWidth = managedImage.Width;
            var imageData = managedImage.Data;
            Parallel.For(0, managedImage.Height, y =>
            {
                var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                var x = 0;
                if (Vector<float>.Count > 4)
                {
                    var alphaMask = AlphaMask;
                    var invertedAlphaMask = new Vector<int>(-1).As<int, float>() ^ alphaMask;
                    var vectorLength = imageDataSpan.Length - (imageDataSpan.Length % (Vector<float>.Count / 4));
                    var vectorImageDataSpan = MemoryMarshal.Cast<Vector4, Vector<float>>(imageDataSpan[..vectorLength]);

                    for (var v = 0; v < vectorImageDataSpan.Length; v++)
                    {
                        var color = vectorImageDataSpan[v];
                        var alpha = color & alphaMask;
                        color = Vector.Max(Vector.Min((color * (a * color + new Vector<float>(b))) / (color * (c * color + new Vector<float>(d)) + new Vector<float>(e)), Vector<float>.One), Vector<float>.Zero);
                        vectorImageDataSpan[v] = (color & invertedAlphaMask) + alpha;
                    }

                    x = vectorLength;
                }
                for (; x < imageDataSpan.Length; x++)
                {
                    var color = imageDataSpan[x];
                    var alpha = Math.Clamp(color.W, 0.0F, 1.0F);
                    color = Vector4.Clamp((color * (a * color + new Vector4(b))) / (color * (c * color + new Vector4(d)) + new Vector4(e)), Vector4.Zero, Vector4.One);
                    color.W = alpha;
                    imageDataSpan[x] = color;
                }
            });

            return managedImage;
        }

        static NGPUImage ProcessGpu(GraphicsDevice device, NImage image)
        {
            var gpuImage = image switch
            {
                NManagedImage managedImage => managedImage.CopyToGpu(device),
                _ => (NGPUImage)image
            };

            using var context = device.CreateComputeContext();
            context.For(gpuImage.Width, gpuImage.Height, new ACESFilmicProcess(gpuImage.Data, gpuImage.Width));

            return gpuImage;
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct ACESFilmicProcess(ReadWriteBuffer<Float4> image, int width) : IComputeShader
    {
        public void Execute()
        {
            const float a = 2.51F;
            const float b = 0.03F;
            const float c = 2.43F;
            const float d = 0.59F;
            const float e = 0.14F;

            var pos = ThreadIds.Y * width + ThreadIds.X;
            var color = image[pos];
            var alpha = Hlsl.Saturate(color.W);
            var color3 = color.XYZ;
            image[pos] = new Float4(Hlsl.Saturate((color3 * (a * color3 + b)) / (color3 * (c * color3 + d) + e)), alpha);
        }
    }
}
