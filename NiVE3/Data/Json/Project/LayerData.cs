using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using NiVE3.Plugin.Interfaces;

namespace NiVE3.Data.Json.Project
{
    public class LayerData
    {
        public Guid LayerId { get; set; }

        public string Name { get; set; } = "";

        public string Comment { get; set; } = "";

        public Guid FootageId { get; set; }

        public bool IsCamera { get; set; }

        public bool IsLight { get; set; }

        public bool IsNullObject { get; set; }

        public bool IsText { get; set; }

        public double SourceStartPoint { get; set; }

        public double InPoint { get; set; }

        public double OutPoint { get; set; }

        public bool IsEnableTimeRemap { get; set; }

        public Color TagColor { get; set; }

        public bool IsEnableVideo { get; set; }

        public bool IsEnableAudio { get; set; }

        public bool IsEnableSolo { get; set; }

        public bool IsLock { get; set; }

        public bool IsEnableShy { get; set; }

        public bool IsEnableExplodeLayers { get; set; }

        public bool IsEnableEffect { get; set; }

        public bool IsEnableFrameBlend { get; set; }

        public bool IsEnableMotionBlur { get; set; }

        public bool IsEnableAdjustmentLayer { get; set; }

        public bool IsEnable3D { get; set; }

        public ImageInterpolationQuality InterpolationQuality { get; set; }

        public BlendMode BlendMode { get; set; }

        public Guid? TrackMatteLayerId { get; set; }

        public TrackMatteMode TrackMatteMode { get; set; }

        public Guid? ParentLayerId { get; set; }

        public EffectData[] Effects { get; set; } = Array.Empty<EffectData>();

        public PropertyData TransformProperties { get; set; } = new PropertyData();

        public PropertyData? LayerOptionProperties { get; set; }

        public PropertyData? TextProperties { get; set; }
    }
}
