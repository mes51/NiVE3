using System;
using System.Collections.Generic;
using System.Text;
using NiVE3.Model;
using NiVE3.Numerics;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.ValueObject;

namespace NiVE3.View.Primitive.PreviewText
{
    class PreviewTextCoordTransformer
    {
        ITransformer Transformer { get; }

        CompositionModel CompositionModel { get; }

        LayerModel LayerModel { get; }

        Time CurrentTime { get; }

        public PreviewTextCoordTransformer(ITransformer transformer, CompositionModel composition, LayerModel layer, Time currentTime)
        {
            Transformer = transformer;
            CompositionModel = composition;
            LayerModel = layer;
            CurrentTime = currentTime;
        }

        public Vector2d LocalCoordToScreenCoord(Vector3d localCoord)
        {
            var layerSkeleton = LayerModel.GetLayerSkeletonWithoutContainsTime(CurrentTime);
            var cameraSetting = CompositionModel.GetActiveCameraSetting(CurrentTime);

            if (layerSkeleton != null)
            {
                return Transformer.LocalCoordToScreenCoord(cameraSetting, layerSkeleton, localCoord);
            }
            else
            {
                return Transformer.WorldCoordToScreenCoord(cameraSetting, localCoord);
            }
        }

        public Vector3d ScreenCoordToLocalCoord(Vector2d screenPosition, Vector2d scale, Vector2d origin)
        {
            var layerSkeleton = LayerModel.GetLayerSkeletonWithoutContainsTime(CurrentTime);
            if (layerSkeleton == null)
            {
                return Vector3d.Zero;
            }

            return Transformer.ScreenCoordToLocalCoord(CompositionModel.GetActiveCameraSetting(CurrentTime), layerSkeleton, screenPosition);
        }
    }
}
