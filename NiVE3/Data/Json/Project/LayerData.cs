using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Media;
using NiVE3.Data.Json.Converter;
using NiVE3.Image.Drawing;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.ValueObject;

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

        [JsonConverter(typeof(TimeJsonConverter))]
        public Time SourceStartPoint { get; set; }

        [JsonConverter(typeof(TimeJsonConverter))]
        public Time InPoint { get; set; }

        [JsonConverter(typeof(TimeJsonConverter))]
        public Time OutPoint { get; set; }

        public bool IsEnableTimeRemap { get; set; }

        public bool IsFreezeFrame { get; set; }

        [JsonConverter(typeof(TimeJsonConverter))]
        public Time FreezeFrameTime { get; set; }

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

        public EffectData[] Effects { get; set; } = [];

        public MaskData[] Masks { get; set; } = [];

        public PropertyData? TransformProperties { get; set; }

        public PropertyData? LayerOptionProperties { get; set; }

        public PropertyData? TextProperties { get; set; }

        public PropertyData? ShapeProperties { get; set; }

        public PropertyData? SourceOptionProperties { get; set; }

        public PropertyData? AudioOptionProperties { get; set; }
    }
}
