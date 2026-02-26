using System;
using System.Collections.Generic;
using System.Text;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.ValueObject;

namespace NiVE3.PresetPlugin.Extension
{
    static class UseMaskPathTargetExtensions
    {
        public static BezierPath? GetMask(this UseMaskPathTarget target, ILayerObject layer, Time layerTime, double downSamplingRate)
        {
            if (target == UseMaskPathTarget.Empty)
            {
                return null;
            }

            return layer.GetMask(target.MaskId)?.GetPath(layerTime + layer.SourceStartPoint, downSamplingRate);
        }
    }
}
