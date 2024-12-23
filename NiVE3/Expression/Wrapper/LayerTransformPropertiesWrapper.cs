using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Model;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.ValueObject;

namespace NiVE3.Expression.Wrapper
{
    class LayerTransformPropertiesWrapper
    {
        public LayerTransformPropertiesWrapper(PropertyGroupModel transform, Time globalTime)
        {
            if (transform.FindProperty(ILayerObject.TransformAnchorPointId) is PropertyModel anchorPointProperty)
            {
                anchorPoint = new PropertyWrapper(anchorPointProperty, globalTime);
            }
            if (transform.FindProperty(ILayerObject.TransformPositionId) is PropertyModel positionProperty)
            {
                position = new PropertyWrapper(positionProperty, globalTime);
            }
            if (transform.FindProperty(ILayerObject.TransformDirectionId) is PropertyModel directionProperty)
            {
                direction = new PropertyWrapper(directionProperty, globalTime);
            }
            if (transform.FindProperty(ILayerObject.TransformXAngleId) is PropertyModel xAngleProperty)
            {
                xAngle = new PropertyWrapper(xAngleProperty, globalTime);
            }
            if (transform.FindProperty(ILayerObject.TransformYAngleId) is PropertyModel yAngleProperty)
            {
                yAngle = new PropertyWrapper(yAngleProperty, globalTime);
            }
            if (transform.FindProperty(ILayerObject.TransformZAngleId) is PropertyModel angleProperty)
            {
                angle = new PropertyWrapper(angleProperty, globalTime);
            }
            if (transform.FindProperty(ILayerObject.TransformScaleId) is PropertyModel scaleProperty)
            {
                scale = new PropertyWrapper(scaleProperty, globalTime);
            }
            if (transform.FindProperty(ILayerObject.TransformPropertyOpacityId) is PropertyModel opacityProperty)
            {
                opacity = new PropertyWrapper(opacityProperty, globalTime);
            }
            if (transform.FindProperty(ILayerObject.TransformPointOfInterestId) is PropertyModel pointOfInterestProperty)
            {
                pointOfInterest = new PropertyWrapper(pointOfInterestProperty, globalTime);
            }
            if (transform.FindProperty(ILayerObject.TransformOrientationId) is PropertyModel orientationProperty)
            {
                orientation = new PropertyWrapper(orientationProperty, globalTime);
            }
        }

        #region Expression members
#pragma warning disable IDE1006 // NOTE: エクスプレッション用メソッドのため、命名規則は camelCase を許容する

        [ExpressionPublicMember]
        public IPropertyWrapper? anchorPoint { get; }

        [ExpressionPublicMember]
        public IPropertyWrapper? position { get; }

        [ExpressionPublicMember]
        public IPropertyWrapper? direction { get; }

        [ExpressionPublicMember]
        public IPropertyWrapper? xAngle { get; }

        [ExpressionPublicMember]
        public IPropertyWrapper? yAngle { get; }

        [ExpressionPublicMember]
        public IPropertyWrapper? angle { get; }

        // normal layer
        [ExpressionPublicMember]
        public IPropertyWrapper? scale { get; }

        [ExpressionPublicMember]
        public IPropertyWrapper? opacity { get; }

        // camera, light
        [ExpressionPublicMember]
        public IPropertyWrapper? pointOfInterest { get; }

        [ExpressionPublicMember]
        public IPropertyWrapper? orientation { get; }

#pragma warning restore IDE1006 // 命名スタイル
        #endregion Expression members
    }
}
