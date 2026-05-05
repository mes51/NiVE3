using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Image;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.ValueObject;

namespace NiVE3.PresetPlugin.Extension
{
    static class UseLayerImageTargetExtensions
    {
        public static NImage? GetImage(this UseLayerImageTarget target, ICompositionObject composition, Time time, double downSamplingRate, bool useGpu)
        {
            if (target == UseLayerImageTarget.Empty)
            {
                return null;
            }

            var layer = composition.GetLayer(target.LayerId);
            if (layer == null)
            {
                return null;
            }

            return target.ImageProcessType switch
            {
                LayerImageProcessType.Masked => layer.GetMaskedImage(time, downSamplingRate, useGpu),
                LayerImageProcessType.Effected => layer.GetEffectedImage(time, downSamplingRate, useGpu),
                _ => layer.GetRawImage(time, downSamplingRate, useGpu)
            };
        }

        public static NImage? GetImageReferenceTime(this UseLayerImageTarget target, ICompositionObject composition, Time time, double downSamplingRate, bool useGpu)
        {
            if (target == UseLayerImageTarget.Empty)
            {
                return null;
            }

            var layer = composition.GetLayer(target.LayerId);
            if (layer == null)
            {
                return null;
            }

            return target.ImageProcessType switch
            {
                LayerImageProcessType.Masked => layer.GetMaskedImage(time + layer.SourceStartPoint, downSamplingRate, useGpu),
                LayerImageProcessType.Effected => layer.GetEffectedImage(time + layer.SourceStartPoint, downSamplingRate, useGpu),
                _ => layer.GetRawImage(time + layer.SourceStartPoint, downSamplingRate, useGpu)
            };
        }
    }
}
