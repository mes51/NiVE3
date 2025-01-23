using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Numerics;
using NiVE3.Plugin.Interfaces.RendererParams;
using NiVE3.Plugin.Interfaces;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.ValueObject;
using System.Numerics;

namespace NiVE3.PresetPlugin.Internal.Util
{
    static class Transform3D
    {
        public static Matrix4x4d Calc3DModelMatrix(PropertyValueGroup transform, ParentTransform[] parentTransforms, double renderWidth, double renderHeight)
        {
            var size = Math.Max(renderWidth, renderHeight);
            var matrix = GetTransform3D(transform, size);

            foreach (var (type, parentTransform) in parentTransforms)
            {
                switch (type)
                {
                    case ParentType.Camera:
                        matrix *= GetInvertedCameraMatrix(parentTransform, renderWidth, renderHeight);
                        break;
                    case ParentType.SpotOrParallelLight:
                    case ParentType.PointLight:
                        matrix *= GetLightMatrix(type == ParentType.SpotOrParallelLight ? LightType.Spot : LightType.Point, parentTransform, renderWidth, renderHeight);
                        break;
                    case ParentType.AmbientLight:
                        break;
                    default:
                        matrix *= GetTransform3D(parentTransform, size);
                        break;
                }
            }

            return matrix;
        }

        public static Matrix4x4d Calc3DViewMatrix(CameraSetting cameraSetting, double renderWidth, double renderHeight)
        {
            var size = Math.Max(renderWidth, renderHeight);
            var view = GetCameraMatrix(cameraSetting, renderWidth, renderHeight);
            foreach (var (type, parentTransform) in cameraSetting.ParentTransforms)
            {
                switch (type)
                {
                    case ParentType.Camera:
                        view = GetCameraMatrix(parentTransform, renderWidth, renderHeight) * view;
                        break;
                    case ParentType.SpotOrParallelLight:
                    case ParentType.PointLight:
                        view = GetLightMatrix(type == ParentType.SpotOrParallelLight ? LightType.Spot : LightType.Point, parentTransform, renderWidth, renderHeight) * view;
                        break;
                    case ParentType.AmbientLight:
                        break;
                    default:
                        if (Matrix4x4d.Invert(GetTransform3D(parentTransform, size), out var inverted))
                        {
                            view = inverted * view;
                        }
                        break;
                }
            }
            return view.Translate(-(size - renderWidth) * 0.5 / size, -(size - renderHeight) * 0.5 / size, 0.0);
        }

        public static Matrix4x4d CalcLightMatrix(LightSetting lightSetting, double renderWidth, double renderHeight)
        {
            var size = Math.Max(renderWidth, renderHeight);
            var lightModelMatrix = GetLightMatrix(lightSetting, renderWidth, renderHeight);
            foreach (var (type, parentTransform) in lightSetting.ParentTransforms)
            {
                switch (type)
                {
                    case ParentType.Camera:
                        lightModelMatrix *= GetInvertedCameraMatrix(parentTransform, renderWidth, renderHeight);
                        break;
                    case ParentType.SpotOrParallelLight:
                    case ParentType.PointLight:
                        lightModelMatrix *= GetLightMatrix(type == ParentType.SpotOrParallelLight ? LightType.Spot : LightType.Point, parentTransform, renderWidth, renderHeight);
                        break;
                    case ParentType.AmbientLight:
                        break;
                    default:
                        lightModelMatrix *= GetTransform3D(parentTransform, size);
                        break;
                }
            }

            return lightModelMatrix;
        }

        public static Matrix4x4d CalcLightViewMatrixWithoutOffset(LightSetting lightSetting, double renderWidth, double renderHeight)
        {
            var size = Math.Max(renderWidth, renderHeight);
            var view = GetLightViewMatrix(lightSetting.LightType, lightSetting.Position, lightSetting.PointOfInterest, lightSetting.Orientation, lightSetting.AngleX, lightSetting.AngleY, lightSetting.AngleZ, renderWidth, renderHeight);
            foreach (var (type, parentTransform) in lightSetting.ParentTransforms)
            {
                switch (type)
                {
                    case ParentType.Camera:
                        view = GetCameraMatrix(parentTransform, renderWidth, renderHeight) * view;
                        break;
                    case ParentType.SpotOrParallelLight:
                    case ParentType.PointLight:
                        view = GetLightMatrix(type == ParentType.SpotOrParallelLight ? LightType.Spot : LightType.Point, parentTransform, renderWidth, renderHeight) * view;
                        break;
                    case ParentType.AmbientLight:
                        break;
                    default:
                        if (Matrix4x4d.Invert(GetTransform3D(parentTransform, size), out var inverted))
                        {
                            view = inverted * view;
                        }
                        break;
                }
            }
            return view;
        }

        public static Matrix4x4d GetTransform3D(PropertyValueGroup transform, double rendererSize)
        {
            var anchorPoint = (Vector3d)(transform[ILayerObject.TransformAnchorPointId] ?? new Vector3d()) / rendererSize;
            var scale = (Vector3d)(transform[ILayerObject.TransformScaleId] ?? new Vector3d()) * 0.01;
            var direction = (Vector3d)(transform[ILayerObject.TransformDirectionId] ?? new Vector3d());
            var angleX = (double)(transform[ILayerObject.TransformXAngleId] ?? 0.0);
            var angleY = (double)(transform[ILayerObject.TransformYAngleId] ?? 0.0);
            var angleZ = (double)(transform[ILayerObject.TransformZAngleId] ?? 0.0);
            var translate = (Vector3d)(transform[ILayerObject.TransformPositionId] ?? new Vector3d()) / rendererSize;

            return Matrix4x4d.AffineTransform(anchorPoint, scale, direction, angleX, angleY, angleZ, translate);
        }

        public static Matrix4x4d GetCameraMatrix(CameraSetting cameraSetting, double renderWidth, double renderHeight)
        {
            return GetCameraMatrix(cameraSetting.Position, cameraSetting.PointOfInterest, cameraSetting.Orientation, cameraSetting.AngleX, cameraSetting.AngleY, cameraSetting.AngleZ, renderWidth, renderHeight);
        }

        static Matrix4x4d GetCameraMatrix(PropertyValueGroup transform, double renderWidth, double renderHeight)
        {
            return GetCameraMatrix(
                (Vector3d)(transform[ILayerObject.TransformPositionId] ?? new Vector3d()),
                (Vector3d)(transform[ILayerObject.TransformPointOfInterestId] ?? new Vector3d()),
                (Vector3d)(transform[ILayerObject.TransformOrientationId] ?? new Vector3d()),
                (double)(transform[ILayerObject.TransformXAngleId] ?? 0.0),
                (double)(transform[ILayerObject.TransformXAngleId] ?? 0.0),
                (double)(transform[ILayerObject.TransformXAngleId] ?? 0.0),
                renderWidth,
                renderHeight
            );
        }

        static Matrix4x4d GetCameraMatrix(in Vector3d pos, in Vector3d poi, in Vector3d orientation, double angleX, double angleY, double angleZ, double renderWidth, double renderHeight)
        {
            var size = Math.Max(renderWidth, renderHeight);
            var pos256 = Avx.Divide(pos.AsVector256(), Vector256.Create(size));
            var poi256 = Avx.Divide(poi.AsVector256(), Vector256.Create(size));

            var diff = Avx.Subtract(poi256, pos256);
            var x = diff.GetElement(0);
            var y = diff.GetElement(1);
            var z = diff.GetElement(2);

            return Matrix4x4d.Identity
                .Translate(-pos256.GetElement(0), -pos256.GetElement(1), -pos256.GetElement(2))
                .RotateY(-Math.Atan2(x, z) / Math.PI * 180.0)
                .RotateX(Math.Atan2(y, Math.Sqrt(x * x + z * z)) / Math.PI * 180.0)
                .RotateX(orientation.X)
                .RotateY(orientation.Y)
                .RotateZ(orientation.Z)
                .RotateX(angleX)
                .RotateY(angleY)
                .RotateZ(angleZ);
        }

        public static Matrix4x4d GetInvertedCameraMatrix(CameraSetting cameraSetting, double renderWidth, double renderHeight)
        {
            return GetInvertedCameraMatrix(cameraSetting.PointOfInterest, cameraSetting.Position, cameraSetting.Orientation, cameraSetting.AngleX, cameraSetting.AngleY, cameraSetting.AngleZ, renderWidth, renderHeight);
        }

        public static Matrix4x4d GetInvertedCameraMatrix(PropertyValueGroup transform, double renderWidth, double renderHeight)
        {
            return GetInvertedCameraMatrix(
                (Vector3d)(transform[ILayerObject.TransformPositionId] ?? new Vector3d()),
                (Vector3d)(transform[ILayerObject.TransformPointOfInterestId] ?? new Vector3d()),
                (Vector3d)(transform[ILayerObject.TransformOrientationId] ?? new Vector3d()),
                (double)(transform[ILayerObject.TransformXAngleId] ?? 0.0),
                (double)(transform[ILayerObject.TransformYAngleId] ?? 0.0),
                (double)(transform[ILayerObject.TransformZAngleId] ?? 0.0),
                renderWidth,
                renderHeight
            );
        }

        static Matrix4x4d GetInvertedCameraMatrix(in Vector3d pos, in Vector3d poi, in Vector3d orientation, double angleX, double angleY, double angleZ, double renderWidth, double renderHeight)
        {
            var size = Math.Max(renderWidth, renderHeight);
            var pos256 = Avx.Divide(pos.AsVector256(), Vector256.Create(size));
            var poi256 = Avx.Divide(poi.AsVector256(), Vector256.Create(size));

            var diff = Avx.Subtract(poi256, pos256);
            var x = diff.GetElement(0);
            var y = diff.GetElement(1);
            var z = diff.GetElement(2);

            return Matrix4x4d.Identity
                .RotateZ(-angleZ)
                .RotateY(-angleY)
                .RotateX(-angleX)
                .RotateZ(-orientation.Z)
                .RotateY(-orientation.Y)
                .RotateX(-orientation.X)
                .RotateX(-Math.Atan2(y, Math.Sqrt(x * x + z * z)) / Math.PI * 180.0)
                .RotateY(Math.Atan2(x, z) / Math.PI * 180.0)
                .Translate(pos256.GetElement(0), pos256.GetElement(1), pos256.GetElement(2));
        }

        static Matrix4x4d GetLightMatrix(LightSetting lightSetting, double renderWidth, double renderHeight)
        {
            return GetLightMatrix(lightSetting.LightType, lightSetting.Position, lightSetting.PointOfInterest, lightSetting.Orientation, lightSetting.AngleX, lightSetting.AngleY, lightSetting.AngleZ, renderWidth, renderHeight);
        }

        public static Matrix4x4d GetLightMatrix(LightType lightType, PropertyValueGroup transform, double renderWidth, double renderHeight)
        {
            return GetLightMatrix(
                lightType,
                (Vector3d)(transform[ILayerObject.TransformPositionId] ?? new Vector3d()),
                (Vector3d)(transform[ILayerObject.TransformPointOfInterestId] ?? new Vector3d()),
                (Vector3d)(transform[ILayerObject.TransformOrientationId] ?? new Vector3d()),
                (double)(transform[ILayerObject.TransformXAngleId] ?? 0.0),
                (double)(transform[ILayerObject.TransformXAngleId] ?? 0.0),
                (double)(transform[ILayerObject.TransformXAngleId] ?? 0.0),
                renderWidth,
                renderHeight
            );
        }

        static Matrix4x4d GetLightMatrix(LightType lightType, in Vector3d pos, in Vector3d poi, in Vector3d orientation, double angleX, double angleY, double angleZ, double renderWidth, double renderHeight)
        {
            var size = Math.Max(renderWidth, renderHeight);
            var pos256 = Avx.Divide(pos.AsVector256(), Vector256.Create(size));
            switch (lightType)
            {
                case LightType.Point:
                    return Matrix4x4d.CreateTranslate(pos256.GetElement(0), pos256.GetElement(1), pos256.GetElement(2));
                case LightType.Spot:
                case LightType.Parallel:
                    {
                        var poi256 = Avx.Divide(poi.AsVector256(), Vector256.Create(size));

                        var diff = Avx.Subtract(poi256, pos256);
                        var x = diff.GetElement(0);
                        var y = diff.GetElement(1);
                        var z = diff.GetElement(2);

                        return Matrix4x4d.Identity
                            .RotateZ(-angleZ)
                            .RotateY(-angleY)
                            .RotateX(-angleX)
                            .RotateZ(-orientation.Z)
                            .RotateY(-orientation.Y)
                            .RotateX(-orientation.X)
                            .RotateX(-Math.Atan2(y, Math.Sqrt(x * x + z * z)) / Math.PI * 180.0)
                            .RotateY(Math.Atan2(x, z) / Math.PI * 180.0)
                            .Translate(pos256.GetElement(0), pos256.GetElement(1), pos256.GetElement(2));
                    }
                default:
                    return Matrix4x4d.Identity;
            }
        }

        static Matrix4x4d GetLightViewMatrix(LightType lightType, in Vector3d pos, in Vector3d poi, in Vector3d orientation, double angleX, double angleY, double angleZ, double renderWidth, double renderHeight)
        {
            var size = Math.Max(renderWidth, renderHeight);
            var pos256 = Avx.Divide(pos.AsVector256(), Vector256.Create(size));
            switch (lightType)
            {
                case LightType.Point:
                    return Matrix4x4d.CreateTranslate(-pos256.GetElement(0), -pos256.GetElement(1), -pos256.GetElement(2));
                case LightType.Spot:
                case LightType.Parallel:
                    {
                        var poi256 = Avx.Divide(poi.AsVector256(), Vector256.Create(size));

                        var diff = Avx.Subtract(poi256, pos256);
                        var x = diff.GetElement(0);
                        var y = diff.GetElement(1);
                        var z = diff.GetElement(2);

                        return Matrix4x4d.Identity
                            .Translate(-pos256.GetElement(0), -pos256.GetElement(1), -pos256.GetElement(2))
                            .RotateY(-Math.Atan2(x, z) / Math.PI * 180.0)
                            .RotateX(Math.Atan2(y, Math.Sqrt(x * x + z * z)) / Math.PI * 180.0)
                            .RotateX(orientation.X)
                            .RotateY(orientation.Y)
                            .RotateZ(orientation.Z)
                            .RotateX(angleX)
                            .RotateY(angleY)
                            .RotateZ(angleZ);
                    }
                default:
                    return Matrix4x4d.Identity;
            }
        }
    }
}
