using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Model;

namespace NiVE3.Expression.Wrapper
{
    record CompositionWrapper(CompositionModel CompositionModel, double GlobalTime)
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
        public double frameDuration => CompositionModel.FrameDuration;

        [ExpressionPublicMember]
        public double duration => CompositionModel.Duration;

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
        public double workareaBegin => CompositionModel.WorkareaBegin;

        [ExpressionPublicMember]
        public double workareaEnd => CompositionModel.WorkareaEnd;

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
            else if (key is int index && index > 0 && CompositionModel.Layers.Count <= index)
            {
                return new LayerWrapper(CompositionModel.Layers[index - 1], CompositionModel, GlobalTime);
            }

            return null;
        }

#pragma warning restore IDE1006 // 命名スタイル
        #endregion Expression members
    }
}
