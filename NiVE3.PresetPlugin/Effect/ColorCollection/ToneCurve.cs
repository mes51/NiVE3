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
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Resource;
using NiVE3.Plugin.ValueObject;
using NiVE3.PresetPlugin.Extension;
using NiVE3.PresetPlugin.Internal.Util;
using NiVE3.PresetPlugin.Property;
using NiVE3.PresetPlugin.Property.Properties;
using NiVE3.PresetPlugin.Resource;

namespace NiVE3.PresetPlugin.Effect.ColorCollection
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.ColorCollection_ToneCurve_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_ColorCollection, LanguageResourceDictionary.ColorCollection_ToneCurve_Description, ID, IsSupportGpu = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class ToneCurve : IEffect
    {
        const string ID = "4315FB86-07EA-44E0-87F3-006D24BE1F23";

        const string PropertyToneCurveId = nameof(PropertyToneCurveId);

        IAcceleratorObject? AcceleratorObject { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            AcceleratorObject = accelerator;
        }

        public PropertyBase[] GetProperties(Int32Size sourceSize)
        {
            return
            [
                new ToneCurveProperty(PropertyToneCurveId, LanguageResourceDictionary.ResourceKeys.ColorCollection_ToneCurve_ToneCurve)
            ];
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, Time layerTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer, bool useGpu)
        {
            var parameters = properties.GetValue(PropertyToneCurveId, layerTime, ToneCurveParameters.Empty);
            if (parameters.Equals(ToneCurveParameters.Empty))
            {
                return image;
            }

            var rgbSpline = new Spline(parameters.Rgb);
            var rSpline = new Spline(parameters.R);
            var gSpline = new Spline(parameters.G);
            var bSpline = new Spline(parameters.B);
            var aSpline = new Spline(parameters.A);

            if (useGpu && AcceleratorObject != null)
            {
                return ProcessGpu(AcceleratorObject.CurrentDevice, image, roi, rgbSpline, rSpline, gSpline, bSpline, aSpline);
            }
            else
            {
                return ProcessCpu(image, roi, rgbSpline, rSpline, gSpline, bSpline, aSpline);
            }
        }

        public float[] Process(float[] audio, Time startTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }

        static NManagedImage ProcessCpu(NImage image, ROI roi, Spline rgbSpline, Spline rSpline, Spline gSpline, Spline bSpline, Spline aSpline)
        {
            var managedImage = image.ToManaged();

            var imageWidth = managedImage.Width;
            var imageData = managedImage.Data;
            Parallel.For(roi.Top, roi.Bottom, y =>
            {
                var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);

                for (var x = roi.Left; x < roi.Right; x++)
                {
                    var color = imageDataSpan[x];
                    var b = bSpline.Interpolate(rgbSpline.Interpolate(color.X));
                    var g = gSpline.Interpolate(rgbSpline.Interpolate(color.Y));
                    var r = rSpline.Interpolate(rgbSpline.Interpolate(color.Z));
                    var a = aSpline.Interpolate(color.W);

                    imageDataSpan[x] = new Vector4(b, g, r, a);
                }
            });

            return managedImage;
        }

        static NGPUImage ProcessGpu(GraphicsDevice device, NImage image, ROI roi, Spline rgbSpline, Spline rSpline, Spline gSpline, Spline bSpline, Spline aSpline)
        {
            var gpuImage = image.ToGpu(device);

            using var rgbSplinePoints = device.AllocateReadOnlyBuffer(rgbSpline.ToSplinePoints());
            using var rSplinePoints = device.AllocateReadOnlyBuffer(rSpline.ToSplinePoints());
            using var gSplinePoints = device.AllocateReadOnlyBuffer(gSpline.ToSplinePoints());
            using var bSplinePoints = device.AllocateReadOnlyBuffer(bSpline.ToSplinePoints());
            using var aSplinePoints = device.AllocateReadOnlyBuffer(aSpline.ToSplinePoints());

            using var context = device.CreateComputeContext();
            context.For(roi.Width, roi.Height, new ToneCurveProcess(gpuImage.Data, gpuImage.Width, rgbSplinePoints, rSplinePoints, gSplinePoints, bSplinePoints, aSplinePoints, roi.Left, roi.Top));

            return gpuImage;
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct ToneCurveProcess(
        ReadWriteBuffer<Float4> image,
        int width,
        ReadOnlyBuffer<SplinePoint> rgbSplinePoints,
        ReadOnlyBuffer<SplinePoint> rSplinePoints,
        ReadOnlyBuffer<SplinePoint> gSplinePoints,
        ReadOnlyBuffer<SplinePoint> bSplinePoints,
        ReadOnlyBuffer<SplinePoint> aSplinePoints,
        int startX,
        int startY
    ) : IComputeShader
    {
        public void Execute()
        {
            var pos = (ThreadIds.Y + startY) * width + ThreadIds.X + startX;

            var color = image[pos];
            var b = BInterpolate(RgbInterpolate(color.X));
            var g = GInterpolate(RgbInterpolate(color.Y));
            var r = RInterpolate(RgbInterpolate(color.Z));
            var a = AInterpolate(color.W);

            image[pos] = new Float4(b, g, r, a);
        }

        float RgbInterpolate(float x)
        {
            if (rgbSplinePoints[0].X > x)
            {
                return rgbSplinePoints[0].D;
            }

            for (int i = 0, limit = rgbSplinePoints.Length - 1; i < limit; i++)
            {
                var currentPoint = rgbSplinePoints[i];
                if (currentPoint.X <= x && rgbSplinePoints[i + 1].X >= x)
                {
                    var dx = x - rgbSplinePoints[i].X;
                    return ((currentPoint.A * dx + currentPoint.B) * dx + currentPoint.C) * dx + currentPoint.D;
                }
            }

            return rgbSplinePoints[rgbSplinePoints.Length - 1].D;
        }

        float RInterpolate(float x)
        {
            if (rSplinePoints[0].X > x)
            {
                return rSplinePoints[0].D;
            }

            for (int i = 0, limit = rSplinePoints.Length - 1; i < limit; i++)
            {
                var currentPoint = rSplinePoints[i];
                if (currentPoint.X <= x && rSplinePoints[i + 1].X >= x)
                {
                    var dx = x - rSplinePoints[i].X;
                    return ((currentPoint.A * dx + currentPoint.B) * dx + currentPoint.C) * dx + currentPoint.D;
                }
            }

            return rSplinePoints[rSplinePoints.Length - 1].D;
        }

        float GInterpolate(float x)
        {
            if (gSplinePoints[0].X > x)
            {
                return gSplinePoints[0].D;
            }

            for (int i = 0, limit = gSplinePoints.Length - 1; i < limit; i++)
            {
                var currentPoint = gSplinePoints[i];
                if (currentPoint.X <= x && gSplinePoints[i + 1].X >= x)
                {
                    var dx = x - gSplinePoints[i].X;
                    return ((currentPoint.A * dx + currentPoint.B) * dx + currentPoint.C) * dx + currentPoint.D;
                }
            }

            return gSplinePoints[gSplinePoints.Length - 1].D;
        }

        float BInterpolate(float x)
        {
            if (bSplinePoints[0].X > x)
            {
                return bSplinePoints[0].D;
            }

            for (int i = 0, limit = bSplinePoints.Length - 1; i < limit; i++)
            {
                var currentPoint = bSplinePoints[i];
                if (currentPoint.X <= x && bSplinePoints[i + 1].X >= x)
                {
                    var dx = x - bSplinePoints[i].X;
                    return ((currentPoint.A * dx + currentPoint.B) * dx + currentPoint.C) * dx + currentPoint.D;
                }
            }

            return bSplinePoints[bSplinePoints.Length - 1].D;
        }

        float AInterpolate(float x)
        {
            if (aSplinePoints[0].X > x)
            {
                return aSplinePoints[0].D;
            }

            for (int i = 0, limit = aSplinePoints.Length - 1; i < limit; i++)
            {
                var currentPoint = aSplinePoints[i];
                if (currentPoint.X <= x && aSplinePoints[i + 1].X >= x)
                {
                    var dx = x - aSplinePoints[i].X;
                    return ((currentPoint.A * dx + currentPoint.B) * dx + currentPoint.C) * dx + currentPoint.D;
                }
            }

            return aSplinePoints[aSplinePoints.Length - 1].D;
        }
    }
}
