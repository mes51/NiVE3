using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;
using ComputeSharp;
using NiVE3.Image;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Resource;
using NiVE3.Plugin.ValueObject;
using NiVE3.PresetPlugin.Resource;
using NiVE3.Shared.Extension;

namespace NiVE3.PresetPlugin.Effect.Channel
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.Channel_Unmult_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_Channel, LanguageResourceDictionary.Channel_Unmult_Description, ID, IsSupportGpu = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class Unmult : IEffect
    {
        const string ID = "D8C5C374-9A30-44E3-9FED-14558B777736";

        IAcceleratorObject? AcceleratorObject { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            AcceleratorObject = accelerator;
        }

        public PropertyBase[] GetProperties(Int32Size sourceSize)
        {
            return [];
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, double layerTime, IPropertyObject[] properties, ICompositionObject composition, bool useGpu)
        {
            if (useGpu && AcceleratorObject != null)
            {
                return ProcessGpu(AcceleratorObject.CurrentDevice, image, roi);
            }
            else
            {
                return ProcessCpu(image, roi);
            }
        }

        public float[] Process(float[] audio, double startTime, IPropertyObject[] properties, ICompositionObject composition)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }

        static NManagedImage ProcessCpu(NImage image, ROI roi)
        {
            var managedImage = image switch
            {
                NGPUImage gpuImage => gpuImage.CopyToCpu(),
                _ => (NManagedImage)image
            };

            var imageData = managedImage.Data;
            Parallel.For(roi.Top, roi.Bottom, y =>
            {
                var imageDataSpan = imageData.AsSpan(y * managedImage.Width, managedImage.Width);
                for (var x = roi.Left; x < roi.Right; x++)
                {
                    var c = imageDataSpan[x];
                    c *= c.W;

                    var irate = c.HorizontalMaxBy3Element();
                    if (irate <= 0.0F)
                    {
                        imageDataSpan[x] = new Vector4(1.0F, 1.0F, 1.0F, 0.0F);
                        continue;
                    }

                    var t = c / irate;
                    var ta = 0.0F;
                    if (t.X > 0.0F)
                    {
                        ta = c.X / t.X;
                    }
                    else if (t.Y > 0.0F)
                    {
                        ta = c.Y / t.Y;
                    }
                    else if (t.Z > 0.0F)
                    {
                        ta = c.Z / t.Z;
                    }

                    if (ta > 0.0F)
                    {
                        t.W = ta;
                        imageDataSpan[x] = t;
                    }
                    else
                    {
                        imageDataSpan[x] = new Vector4(1.0F, 1.0F, 1.0F, 0.0F);
                    }
                }
            });

            return managedImage;
        }

        static NGPUImage ProcessGpu(GraphicsDevice device, NImage image, ROI roi)
        {
            var gpuImage = image switch
            {
                NManagedImage managedImage => managedImage.CopyToGpu(device),
                _ => (NGPUImage)image
            };

            using var context = device.CreateComputeContext();
            context.For(roi.Width, roi.Height, new UnmultProcess(gpuImage.Data, gpuImage.Width, roi.Left, roi.Top));

            return gpuImage;
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct UnmultProcess(ReadWriteBuffer<Float4> image, int width, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            var pos = (ThreadIds.Y + startY) * width + ThreadIds.X + startX;
            var c = image[pos];
            c *= c.W;

            var irate = Hlsl.Max(Hlsl.Max(c.X, c.Y), c.Z);
            if (irate <= 0.0F)
            {
                image[pos] = new Float4(1.0F, 1.0F, 1.0F, 0.0F);
                return;
            }

            var t = c / irate;
            var tav = c / t;
            var ta = 0.0F;
            if (t.X > 0.0F)
            {
                ta = tav.X;
            }
            else if (t.Y > 0.0F)
            {
                ta = tav.Y;
            }
            else if (t.Z > 0.0F)
            {
                ta = tav.Z;
            }

            if (ta > 0.0F)
            {
                image[pos] = new Float4(t.XYZ, ta);
            }
            else
            {
                image[pos] = new Float4(1.0F, 1.0F, 1.0F, 0.0F);
            }
        }
    }
}
