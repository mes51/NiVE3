using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Image;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.PresetPlugin.Resource;
using NiVE3.Shared.Extension;
using NiVE3.PresetPlugin.Internal.Drawing;
using System.Runtime.Intrinsics;
using NiVE3.Plugin.Interfaces.RendererParams;
using System.Windows.Media.Media3D;
using System.Runtime.Intrinsics.X86;
using System.Security.Principal;
using NiVE3.Plugin.Numerics;
using NiVE3.Plugin.ValueObject;

namespace NiVE3.PresetPlugin.Renderer
{
    [Export(typeof(IRenderer))]
    [RendererMetadata(typeof(DefaultRenderer), LanguageResourceDictionary.Renderer_DefaultRenderer_Name, LanguageResourceDictionary.Renderer_DefaultRenderer_Description, "mes51", "D67AC3F-A137-45B1-99F7-3E68A0B910E6", LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public class DefaultRenderer : IRenderer
    {
        const double DefaultFov = 0.691111986546211; // 39.5978 / 180.0 * Math.PI

        int Width { get; set; }

        int Height { get; set; }

        NManagedImage? CurrentFrame { get; set; }

        bool UseGpu { get; set; }

        Matrix4x4d ViewMatrix { get; set; }

        Matrix4x4d ProjectionMatrix { get; set; }

        List<PointLight> PointLights { get; } = new List<PointLight>();

        List<SpotLight> SpotLights { get; } = new List<SpotLight>();

        List<AmbientLight> AmbientLights { get; } = new List<AmbientLight>();

        public void SetupAccelerator(IAcceleratorObject accelerator) { }

        public void SetSize(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public void BeginRendering(double downSamplingRate, bool useGpu)
        {
            if (CurrentFrame != null)
            {
                throw new InvalidOperationException("rendering is already started"); // bug
            }

            CurrentFrame = new NManagedImage(Width, Height, true);
            UseGpu = useGpu;

            var zoom = Width / Math.Tan(DefaultFov * 0.5) * 0.5;
            ViewMatrix = Matrix4x4d.CreateLookAt(Vector256.Create(0.5, 0.5, -zoom / Width, 0.0), Vector256.Create(0.5, 0.5, 0.0, 0.0), Vector256.Create(0.0, 1.0, 0.0, 0.0));
            ProjectionMatrix = Matrix4x4d.CreatePerspectiveFieldOfView(DefaultFov, 1.0, double.Epsilon, double.PositiveInfinity);
            PointLights.Clear();
            SpotLights.Clear();
            AmbientLights.Clear();
        }

        public void SetCamera(CameraSetting cameraSetting)
        {
            var size = Math.Max(Width, Height);
            var fov = Math.Atan((Width / cameraSetting.Zoom) * 0.5) * 2.0;

            ViewMatrix = Calc3DViewMatrix(cameraSetting, Width, Height);
            ProjectionMatrix = Matrix4x4d.CreatePerspectiveFieldOfView(fov, 1.0, double.Epsilon, double.PositiveInfinity);
        }

        public void AddLight(LightSetting lightSetting)
        {
        }

        public void Render(RenderableImage[] images)
        {
            if (CurrentFrame == null)
            {
                return;
            }

            foreach (var group in images.GroupByPrev(i => i.IsEnable3D))
            {
                if (group.First().IsEnable3D)
                {
                    var renderer = new Renderer3D(CurrentFrame);

                    renderer.ViewMatrix = ViewMatrix;
                    renderer.ProjectionMatrix = ProjectionMatrix;

                    foreach (var i in group)
                    {
                        var opacity = (double)(i.Transform[ILayerObject.TransformPropertyOpacityId] ?? 0.0) * 0.01;

                        renderer.ModelMatrix = Calc3DModelMatrix(i.Transform, i.ParentTransforms, Width, Height);
                        renderer.AddRect(i.Image, i.BlendMode);
                    }

                    renderer.Render();
                }
                else
                {
                    var renderer = new Renderer2D(CurrentFrame);

                    foreach (var i in group)
                    {
                        var opacity = (double)(i.Transform[ILayerObject.TransformPropertyOpacityId] ?? 0.0) * 0.01;
                        var matrix = CalcTransform2D(i.Transform, i.ParentTransforms);

                        renderer.Draw(i.Image, (float)opacity, matrix, i.InterpolationQuality, i.BlendMode);
                    }
                }
            }
        }

        public NImage GetCurrentRenderedImage()
        {
            if (CurrentFrame == null)
            {
                throw new InvalidOperationException("rendering not started"); // bug
            }
            
            return CurrentFrame.Copy();
        }

        public NImage FinishRendering()
        {
            if (CurrentFrame == null)
            {
                throw new InvalidOperationException("rendering not started"); // bug
            }

            var result = CurrentFrame;
            CurrentFrame = null;
            return result;
        }

        public PreviewBoundingBox CalcBoundingBox2D(int width, int height, PropertyValueGroup transform, ParentTransform[] parentTransforms)
        {
            var matrix = CalcTransform2D(transform, parentTransforms);
            var anchorPoint = (Vector3d)(transform[ILayerObject.TransformAnchorPointId] ?? new Vector3d());

            return new PreviewBoundingBox(
                (Vector2d)matrix.Transform(new Vector2(0.0F, 0.0F)),
                (Vector2d)matrix.Transform(new Vector2(width, 0.0F)),
                (Vector2d)matrix.Transform(new Vector2(0.0F, height)),
                (Vector2d)matrix.Transform(new Vector2(width, height)),
                (Vector2d)matrix.Transform((Vector2)anchorPoint.AsVector2d())
            );
        }

        public PreviewBoundingBox CalcBoundingBox3D(int width, int height, PropertyValueGroup transform, ParentTransform[] parentTransforms, CameraSetting cameraSetting)
        {
            var size = Math.Max(Width, Height);
            var fov = Math.Atan((Width / cameraSetting.Zoom) * 0.5) * 2.0;
            var anchorPoint = (Vector3d)(transform[ILayerObject.TransformAnchorPointId] ?? new Vector3d());

            var projectionMatrix = Matrix4x4d.CreatePerspectiveFieldOfView(fov, 1.0, double.Epsilon, double.PositiveInfinity);
            var modelMatrix = Calc3DModelMatrix(transform, parentTransforms, Width, Height);
            var viewMatrix = Calc3DViewMatrix(cameraSetting, Width, Height);

            var mv = modelMatrix * viewMatrix * Matrix4x4d.CreateTranslate((size - Width) * 0.5 / size, (size - Height) * 0.5 / size, 0.0);

            var v1 = mv.Transform(Avx.Divide(Vector256.Create(0.0, 0.0, 0.0, size), Vector256.Create((double)size)));
            var v2 = mv.Transform(Avx.Divide(Vector256.Create(0.0, height, 0.0, size), Vector256.Create((double)size)));
            var v3 = mv.Transform(Avx.Divide(Vector256.Create(width, height, 0.0, size), Vector256.Create((double)size)));
            var v4 = mv.Transform(Avx.Divide(Vector256.Create(width, 0.0, 0.0, size), Vector256.Create((double)size)));

            var t1Normal = Avx.Subtract(v2, v1).CrossProduct(Avx.Subtract(v3, v1)).Normalize();
            var t2Normal = Avx.Subtract(v3, v1).CrossProduct(Avx.Subtract(v4, v1)).Normalize();
            if (!Avx.TestZ(Avx.CompareNotEqual(t1Normal, t1Normal), Vector256.Create(double.NaN)) || !Avx.TestZ(Avx.CompareNotEqual(t2Normal, t2Normal), Vector256.Create(double.NaN)))
            {
                return PreviewBoundingBox.Empty;
            }

            v1 = projectionMatrix.Transform(v1);
            v2 = projectionMatrix.Transform(v2);
            v3 = projectionMatrix.Transform(v3);
            v4 = projectionMatrix.Transform(v4);

            v1 = Avx.Divide(v1, Vector256.Create(v1.GetElement(3)));
            v2 = Avx.Divide(v2, Vector256.Create(v2.GetElement(3)));
            v3 = Avx.Divide(v3, Vector256.Create(v3.GetElement(3)));
            v4 = Avx.Divide(v4, Vector256.Create(v4.GetElement(3)));
            var av = projectionMatrix.Transform(mv.Transform(Avx.Divide(Avx.Add(anchorPoint.AsVector256(), Vector256.Create(0.0, 0.0, 0.0, size)), Vector256.Create((double)size))));
            av = Avx.Divide(av, Vector256.Create(av.GetElement(3)));

            var s = new Vector2d(size, size) * 0.5;
            var offset = new Vector2d(Width, Height) * 0.5;
            var bbAnchorPoint = ((Vector2d)av) * s + offset;

            if (IntersectLine((Vector2d)v1, (Vector2d)v4, (Vector2d)v2, (Vector2d)v3) ||
                IntersectLine((Vector2d)v1, (Vector2d)v4, (Vector2d)v1, (Vector2d)v3) ||
                IntersectLine((Vector2d)v1, (Vector2d)v4, (Vector2d)v4, (Vector2d)v3))
            {
                // バウンディングボックスがクロスしているためアンカーポイントのみ表示
                return new PreviewBoundingBox(
                    new Vector2d(),
                    new Vector2d(),
                    new Vector2d(),
                    new Vector2d(),
                    bbAnchorPoint
                );
            }
            else
            {
                return new PreviewBoundingBox(
                    ((Vector2d)v1) * s + offset,
                    ((Vector2d)v4) * s + offset,
                    ((Vector2d)v2) * s + offset,
                    ((Vector2d)v3) * s + offset,
                    bbAnchorPoint
                );
            }
        }

        static Matrix3x3 CalcTransform2D(PropertyValueGroup transform, ParentTransform[] parentTransforms)
        {
            var matrix = GetTransform2D(transform);
            foreach (var (_, parentTransform) in parentTransforms)
            {
                matrix *= GetTransform2D(parentTransform);
            }

            return matrix;
        }

        static Matrix4x4d Calc3DModelMatrix(PropertyValueGroup transform, ParentTransform[] parentTransforms, double renderWidth, double renderHeight)
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
                        matrix *= GetInvertedLightMatrix(type == ParentType.SpotOrParallelLight ? LightType.Spot : LightType.Point, parentTransform, renderWidth, renderHeight);
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

        static Matrix4x4d Calc3DViewMatrix(CameraSetting cameraSetting, double renderWidth, double renderHeight)
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

        static Matrix3x3 GetTransform2D(PropertyValueGroup transform)
        {
            var anchorPoint = (Vector3d)(transform[ILayerObject.TransformAnchorPointId] ?? transform[ILayerObject.TransformPointOfInterestId] ?? new Vector3d());
            var scale = (Vector3d)(transform[ILayerObject.TransformScaleId] ?? new Vector3d(100.0, 100.0, 100.0)) * 0.01;
            var angle = (double)(transform[ILayerObject.TransformZAngleId] ?? 0.0);
            var translate = (Vector3d)(transform[ILayerObject.TransformPositionId] ?? new Vector3d());
            return Matrix3x3.AffineTransform((Vector2)anchorPoint.AsVector2d(), (Vector2)scale.AsVector2d(), (float)angle, (Vector2)translate.AsVector2d());
        }

        static Matrix4x4d GetTransform3D(PropertyValueGroup transform, double rendererSize)
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

        static Matrix4x4d GetCameraMatrix(CameraSetting cameraSetting, double renderWidth, double renderHeight)
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
            //var viewMatrix = Matrix4x4d.CreateLookAt(pos, poi, Vector256.Create(0.0, 1.0, 0.0, 0.0));

            var diff = Avx.Subtract(poi256, pos256);
            var x = diff.GetElement(0);
            var y = diff.GetElement(1);
            var z = diff.GetElement(2);

            // flipZ: true
            var viewMatrix = Matrix4x4d.Identity
                .Scale(1.0, 1.0, -1.0)
                .Translate(-pos256.GetElement(0), -pos256.GetElement(1), pos256.GetElement(2))
                .RotateY(Math.Atan2(x, z) / Math.PI * 180.0)
                .RotateX(-Math.Atan2(y, Math.Sqrt(x * x + z * z)) / Math.PI * 180.0);

            return viewMatrix.RotateX(orientation.X)
                .RotateY(orientation.Y)
                .RotateZ(orientation.Z)
                .RotateX(angleX)
                .RotateY(angleY)
                .RotateZ(angleZ);
        }

        static Matrix4x4d GetInvertedCameraMatrix(PropertyValueGroup transform, double renderWidth, double renderHeight)
        {
            var poi = (Vector3d)(transform[ILayerObject.TransformPointOfInterestId] ?? new Vector3d());
            var pos = (Vector3d)(transform[ILayerObject.TransformPositionId] ?? new Vector3d());
            var orientation = (Vector3d)(transform[ILayerObject.TransformOrientationId] ?? new Vector3d());
            var angleX = (double)(transform[ILayerObject.TransformXAngleId] ?? 0.0);
            var angleY = (double)(transform[ILayerObject.TransformYAngleId] ?? 0.0);
            var angleZ = (double)(transform[ILayerObject.TransformZAngleId] ?? 0.0);

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
                .RotateX(Math.Atan2(y, Math.Sqrt(x * x + z * z)) / Math.PI * 180.0)
                .RotateY(-Math.Atan2(x, z) / Math.PI * 180.0)
                .Translate(pos256.GetElement(0), pos256.GetElement(1), -pos256.GetElement(2))
                .Scale(1.0, 1.0, -1.0);
        }

        static Matrix4x4d GetLightMatrix(LightSetting lightSetting, double renderWidth, double renderHeight)
        {
            return GetLightMatrix(lightSetting.LightType, lightSetting.Position, lightSetting.PointOfInterest, lightSetting.Orientation, lightSetting.AngleX, lightSetting.AngleY, lightSetting.AngleZ, renderWidth, renderHeight);
        }

        static Matrix4x4d GetLightMatrix(LightType lightType, PropertyValueGroup transform, double renderWidth, double renderHeight)
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
                case LightType.Spot:
                case LightType.Parallel:
                    {
                        var poi256 = Avx.Divide(poi.AsVector256(), Vector256.Create(size));

                        var diff = Avx.Subtract(poi256, pos256);
                        var x = diff.GetElement(0);
                        var y = diff.GetElement(1);
                        var z = diff.GetElement(2);

                        var viewMatrix = Matrix4x4d.Identity
                            .Scale(1.0, 1.0, -1.0)
                            .Translate(-pos256.GetElement(0), -pos256.GetElement(1), pos256.GetElement(2))
                            .RotateY(Math.Atan2(x, z) / Math.PI * 180.0)
                            .RotateX(-Math.Atan2(y, Math.Sqrt(x * x + z * z)) / Math.PI * 180.0);

                        return viewMatrix.RotateX(orientation.X)
                            .RotateY(orientation.Y)
                            .RotateZ(orientation.Z)
                            .RotateX(angleX)
                            .RotateY(angleY)
                            .RotateZ(angleZ);
                    }
                case LightType.Point:
                    return Matrix4x4d.CreateTranslate(-pos256.GetElement(0), -pos256.GetElement(1), -pos256.GetElement(2));
                default:
                    return Matrix4x4d.Identity;
            }
        }

        static Matrix4x4d GetInvertedLightMatrix(LightType lightType, PropertyValueGroup transform, double renderWidth, double renderHeight)
        {
            var pos = (Vector3d)(transform[ILayerObject.TransformPositionId] ?? new Vector3d());

            var size = Math.Max(renderWidth, renderHeight);
            var pos256 = Avx.Divide(pos.AsVector256(), Vector256.Create(size));
            switch (lightType)
            {
                case LightType.Spot:
                case LightType.Parallel:
                    {
                        var poi = (Vector3d)(transform[ILayerObject.TransformPointOfInterestId] ?? new Vector3d());
                        var orientation = (Vector3d)(transform[ILayerObject.TransformOrientationId] ?? new Vector3d());
                        var angleX = (double)(transform[ILayerObject.TransformXAngleId] ?? 0.0);
                        var angleY = (double)(transform[ILayerObject.TransformYAngleId] ?? 0.0);
                        var angleZ = (double)(transform[ILayerObject.TransformZAngleId] ?? 0.0);

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
                            .RotateX(Math.Atan2(y, Math.Sqrt(x * x + z * z)) / Math.PI * 180.0)
                            .RotateY(-Math.Atan2(x, z) / Math.PI * 180.0)
                            .Translate(pos256.GetElement(0), pos256.GetElement(1), -pos256.GetElement(2))
                            .Scale(1.0, 1.0, -1.0);
                    }
                case LightType.Point:
                    return Matrix4x4d.CreateTranslate(pos256.GetElement(0), pos256.GetElement(1), pos256.GetElement(2));
                default:
                    return Matrix4x4d.Identity;
            }
        }

        static bool IntersectLine(in Vector2d v1, in Vector2d v2, in Vector2d v3, in Vector2d v4)
        {
            var ab = v2 - v1;
            var ac = v3 - v1;
            var ad = v4 - v1;
            if (((ab.X * ac.Y) - (ab.Y * ac.X)) * ((ab.X * ad.Y) - (ab.Y * ad.X)) >= 0.0)
            {
                return false;
            }

            var cd = v4 - v3;
            var ca = v1 - v3;
            var cb = v2 - v3;
            if (((cd.X * ca.Y) - (cd.Y * ca.X)) * ((cd.X * cb.Y) - (cd.Y * cb.X)) >= 0.0)
            {
                return false;
            }

            return true;
        }

        public void Dispose()
        {
            CurrentFrame?.Dispose();
        }
    }
}
