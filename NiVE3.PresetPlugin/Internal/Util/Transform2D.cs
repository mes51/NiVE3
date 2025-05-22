using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Numerics;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.ValueObject;

namespace NiVE3.PresetPlugin.Internal.Util
{
    static class Transform2D
    {
        public static Matrix3x3 CalcTransform2D(PropertyValueGroup transform, ParentTransform[] parentTransforms)
        {
            var matrix = GetTransform2D(transform);
            foreach (var (_, parentTransform) in parentTransforms)
            {
                matrix *= GetTransform2D(parentTransform);
            }

            return matrix;
        }

        public static Matrix3x3 GetTransform2D(PropertyValueGroup transform)
        {
            var anchorPoint = (Vector3d)(transform[ILayerObject.TransformAnchorPointId] ?? transform[ILayerObject.TransformPointOfInterestId] ?? new Vector3d());
            var scale = (Vector3d)(transform[ILayerObject.TransformScaleId] ?? new Vector3d(100.0, 100.0, 100.0)) * 0.01;
            var angle = (double)(transform[ILayerObject.TransformZAngleId] ?? 0.0);
            var translate = (Vector3d)(transform[ILayerObject.TransformPositionId] ?? new Vector3d());
            return Matrix3x3.AffineTransform((Vector2)anchorPoint.AsVector2d(), (Vector2)scale.AsVector2d(), (float)angle, (Vector2)translate.AsVector2d());
        }
    }
}
