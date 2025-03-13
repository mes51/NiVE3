using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Model;
using NiVE3.Plugin.ValueObject;

namespace NiVE3.Expression.Wrapper
{
    record CompositionWrapper(CompositionModel CompositionModel, Time GlobalTime)
    {
        #region Expression members
#pragma warning disable IDE1006 // NOTE: エクスプレッション用メソッドのため、命名規則は camelCase を許容する

        [ExpressionPublicMember]
        public string name => CompositionModel.Name;

        [ExpressionPublicMember]
        public int width => CompositionModel.Width;

        [ExpressionPublicMember]
        public int height => CompositionModel.Height;

        [ExpressionPublicMember]
        public double frameRate => CompositionModel.FrameRate;

        [ExpressionPublicMember]
        public double frameDuration => (double)CompositionModel.FrameDuration;

        [ExpressionPublicMember]
        public double duration => (double)CompositionModel.Duration;

        [ExpressionPublicMember]
        public bool isRetentionFrameRate => CompositionModel.IsRetentionFrameRate;

        [ExpressionPublicMember]
        public bool applyToneMappingWhenNested => CompositionModel.ApplyToneMappingWhenNested;

        [ExpressionPublicMember]
        public int shutterAngle => CompositionModel.ShutterAngle;

        [ExpressionPublicMember]
        public int shutterPhase => CompositionModel.ShutterPhase;

        [ExpressionPublicMember]
        public int motionBlurSampleCount => CompositionModel.MotionBlurSampleCount;

        [ExpressionPublicMember]
        public double workareaBegin => (double)CompositionModel.WorkareaBegin;

        [ExpressionPublicMember]
        public double workareaEnd => (double)CompositionModel.WorkareaEnd;

        //public bool isEnableFrameBlend => CompositionModel.IsEnableFrameBlend;

        [ExpressionPublicMember]
        public bool isEnableMotionBlur => CompositionModel.IsEnableMotionBlur;

        [ExpressionPublicMember]
        public bool isEnableShy => CompositionModel.IsEnableShy;

        [ExpressionPublicMember]
        public int layerCount => CompositionModel.Layers.Count;

        [ExpressionPublicMember]
        public LayerWrapper? activeCamera
        {
            get
            {
                var camera = CompositionModel.GetActiveCamera(GlobalTime);
                return camera!= null ? new LayerWrapper(camera, CompositionModel, GlobalTime) : null;
            }
        }

        [ExpressionPublicMember]
        public LayerWrapper? layer(object key)
        {
            if (key is string name)
            {
                foreach (var layer in CompositionModel.Layers)
                {
                    if (layer.Name == name)
                    {
                        return new LayerWrapper(layer, CompositionModel, GlobalTime);
                    }
                }
            }
            else if (ExpressionInternalUtil.TryConvertToIndex(key, out var index) && index > -1 && index < CompositionModel.Layers.Count)
            {
                return new LayerWrapper(CompositionModel.Layers[index], CompositionModel, GlobalTime);
            }

            return null;
        }

#pragma warning restore IDE1006 // 命名スタイル
        #endregion Expression members
    }
}
