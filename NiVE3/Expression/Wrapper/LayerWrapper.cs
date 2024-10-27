using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Model;
using NiVE3.Shared.Extension;

namespace NiVE3.Expression.Wrapper
{
    record LayerWrapper(LayerModel LayerModel, CompositionModel CompositionModel, double GlobalTime)
    {
        #region Expression members
#pragma warning disable IDE1006 // NOTE: エクスプレッション用メソッドのため、命名規則は camelCase を許容する

        [ExpressionPublicMember]
        public string name => LayerModel.Name;

        [ExpressionPublicMember]
        public int index => LayerModel.Index;

        [ExpressionPublicMember]
        public int width => LayerModel.SourceWidth;

        [ExpressionPublicMember]
        public int height => LayerModel.SourceHeight;

        [ExpressionPublicMember]
        public string comment => LayerModel.Comment;

        [ExpressionPublicMember]
        public double duration => LayerModel.Duration;

        [ExpressionPublicMember]
        public double sourceStartPoint => LayerModel.SourceStartPoint;

        [ExpressionPublicMember]
        public double inPoint => LayerModel.InPoint;

        [ExpressionPublicMember]
        public double outPoint => LayerModel.OutPoint;

        //public bool isEnableTimeRemap => LayerModel.IsEnableTimeRemap;

        [ExpressionPublicMember]
        public bool hasImage => LayerModel.HasImage;

        [ExpressionPublicMember]
        public bool hasAudio => LayerModel.HasAudio;

        [ExpressionPublicMember]
        public Vector4 tagColor => LayerModel.TagColor.ToVector4();

        [ExpressionPublicMember]
        public bool isEnableVideo => LayerModel.IsEnableVideo;

        [ExpressionPublicMember]
        public bool isEnableAudio => LayerModel.IsEnableAudio;

        [ExpressionPublicMember]
        public bool isEnableSolo => LayerModel.IsEnableSolo;

        [ExpressionPublicMember]
        public bool isLock => LayerModel.IsLock;

        [ExpressionPublicMember]
        public bool isEnableShy => LayerModel.IsEnableShy;

        //public bool isEnableCollapse => LayerModel.IsEnableCollapse;

        [ExpressionPublicMember]
        public bool isEnableEffect => LayerModel.IsEnableEffect;

        //public bool isEnableFrameBlend => LayerModel.IsEnableFrameBlend;

        [ExpressionPublicMember]
        public bool isEnableMotionBlur => LayerModel.IsEnableMotionBlur;

        [ExpressionPublicMember]
        public bool isEnableAdjustmentLayer => LayerModel.IsEnableAdjustmentLayer;

        [ExpressionPublicMember]
        public bool isEnable3D => LayerModel.IsEnable3D;

        [ExpressionPublicMember]
        public string interpolationQuality => LayerModel.InterpolationQuality.ToString();

        [ExpressionPublicMember]
        public string blendMode => LayerModel.BlendMode.ToString();

        [ExpressionPublicMember]
        public LayerWrapper? parent
        {
            get
            {
                var parentLayer = CompositionModel.Layers.FirstOrDefault(l => l.LayerId == LayerModel.ParentLayerId);
                return parentLayer != null ? new LayerWrapper(parentLayer, CompositionModel, GlobalTime) : null;
            }
        }

        [ExpressionPublicMember]
        public LayerWrapper? trackMatte
        {
            get
            {
                var trackMatteLayer = CompositionModel.Layers.FirstOrDefault(l => l.LayerId == LayerModel.TrackMatteLayerId);
                return trackMatteLayer != null ? new LayerWrapper(trackMatteLayer, CompositionModel, GlobalTime) : null;
            }
        }

        [ExpressionPublicMember]
        public string trackMatteMode => LayerModel.TrackMatteMode.ToString();

        [ExpressionPublicMember]
        public LayerTransformPropertiesWrapper? transform { get; } = LayerModel.TransformProperties != null ? new LayerTransformPropertiesWrapper(LayerModel.TransformProperties, GlobalTime) : null;

        [ExpressionPublicMember]
        public LayerOptionPropertiesWrapper? layerOptions { get; } = LayerModel.LayerOptionProperties != null ? new LayerOptionPropertiesWrapper(LayerModel.LayerOptionProperties, GlobalTime) : null;

        [ExpressionPublicMember]
        public LayerAudioLevelPropertiesWrapper? audioLevel { get; } = LayerModel.AudioOptionProperties != null ? new LayerAudioLevelPropertiesWrapper(LayerModel.AudioOptionProperties, GlobalTime) : null;

        [ExpressionPublicMember]
        public PropertyGroupWrapper? text { get; } = LayerModel.TextProperties != null ? new PropertyGroupWrapper(LayerModel.TextProperties, GlobalTime) : null;

        [ExpressionPublicMember]
        public PropertyGroupWrapper? shape { get; } = LayerModel.ShapeProperties != null ? new PropertyGroupWrapper(LayerModel.ShapeProperties, GlobalTime) : null;

        [ExpressionPublicMember]
        public PropertyGroupWrapper? sourceOption { get; } = LayerModel.SourceOptionProperties != null ? new PropertyGroupWrapper(LayerModel.SourceOptionProperties, GlobalTime) : null;

        [ExpressionPublicMember]
        public EffectWrapper? effect(object key)
        {
            if (key is string name)
            {
                foreach (var effect in LayerModel.Effects)
                {
                    if (effect.Name == name)
                    {
                        return new EffectWrapper(effect, GlobalTime);
                    }
                }
            }
            else if (key is double index && index > 0 && index <= LayerModel.Effects.Count)
            {
                return new EffectWrapper(LayerModel.Effects[(int)index - 1], GlobalTime);
            }

            return null;
        }

        [ExpressionPublicMember]
        public object[] getSourceRect(double time)
        {
            var rect = LayerModel.GetSourceFootageRect(time);

            return [rect.Origin.X, rect.Origin.Y, rect.Width, rect.Height];
        }

#pragma warning restore IDE1006 // 命名スタイル
        #endregion Expression members
    }
}
