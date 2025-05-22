using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Numerics;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Interfaces.RendererParams;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.ValueObject;
using NiVE3.PresetPlugin.Internal.Drawing;
using NiVE3.PresetPlugin.Internal.Drawing.Primitive3D;
using NiVE3.PresetPlugin.Internal.Util;
using NiVE3.Shared.Extension;

namespace NiVE3.PresetPlugin.Renderer
{
    public class DefaultTransformer : ITransformer
    {
        const double DefaultFov = 0.691111986546211; // 39.5978 / 180.0 * Math.PI

        const double EpsilonZDiff = 1E-10;

        int Width { get; set; }

        int Height { get; set; }

        public void SetSize(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public PreviewBoundingBox GetBoundingBox2D(Vector2d origin, int width, int height, PropertyValueGroup transform, ParentTransform[] parentTransforms)
        {
            var matrix = Transform2D.CalcTransform2D(transform, parentTransforms);
            var anchorPoint = (Vector3d)(transform[ILayerObject.TransformAnchorPointId] ?? new Vector3d());
            var transformedAnchorPoint = (Vector2d)matrix.Transform((Vector2)anchorPoint.AsVector2d());

            matrix = Matrix3x3.CreateTranslate(-(float)origin.X, -(float)origin.Y) * matrix;
            var leftTop = (Vector2d)matrix.Transform(new Vector2(0.0F, 0.0F));
            var rightTop = (Vector2d)matrix.Transform(new Vector2(width, 0.0F));
            var leftBottom = (Vector2d)matrix.Transform(new Vector2(0.0F, height));
            var rightBottom = (Vector2d)matrix.Transform(new Vector2(width, height));
            return new PreviewBoundingBox(
                transformedAnchorPoint,
                [new BoundingBoxShape([leftTop, rightTop, rightBottom, leftBottom], true, false)],
                (leftTop - rightTop).IsZero && (leftTop - leftBottom).IsZero && (rightTop - rightBottom).IsZero && (leftBottom - rightBottom).IsZero,
                transformedAnchorPoint.IsNaN() || transformedAnchorPoint.IsInfinty()
            );
        }

        public PreviewBoundingBox GetBoundingBox3D(Vector2d origin, int width, int height, PropertyValueGroup transform, ParentTransform[] parentTransforms, CameraSetting cameraSetting)
        {
            var size = Math.Max(Width, Height);
            var fov = Math.Atan((Width / cameraSetting.Zoom) * 0.5) * 2.0;
            var anchorPoint = (Vector3d)(transform[ILayerObject.TransformAnchorPointId] ?? new Vector3d());

            var projectionMatrix = Matrix4x4d.CreatePerspectiveFieldOfView(fov, 1.0, double.Epsilon, double.PositiveInfinity);
            var modelMatrix = Transform3D.Calc3DModelMatrix(transform, parentTransforms, Width, Height);
            var viewMatrix = Transform3D.Calc3DViewMatrix(cameraSetting, Width, Height);
            var offsetX = (size - Width) * 0.5 / size;
            var offsetY = (size - Height) * 0.5 / size;
            var offsetMatrix = Matrix4x4d.CreateTranslate(offsetX, offsetY, 0.0);

            var mv = modelMatrix * viewMatrix;
            var anchorPointMv = mv * offsetMatrix;

            // ヌルオブジェクト
            if (width == 0 && height == 0)
            {
                var nav = projectionMatrix.Transform(anchorPointMv.Transform(Avx.Divide(Avx.Add(anchorPoint.AsVector256(), Vector256.Create(0.0, 0.0, 0.0, size)), Vector256.Create((double)size))));
                nav = Avx.Divide(nav, Vector256.Create(nav.GetElement(3)));

                var nullObjectAnchorPoint = ((Vector2d)nav) * (new Vector2d(size, size) * 0.5) + (new Vector2d(Width, Height) * 0.5);
                return new PreviewBoundingBox(nullObjectAnchorPoint, [], true, nullObjectAnchorPoint.IsNaN() || nullObjectAnchorPoint.IsInfinty());
            }

            mv = Matrix4x4d.CreateTranslate(-origin.X / size, -origin.Y / size, 0.0) * mv;
            var mvt = mv * offsetMatrix;

            var sv1 = Vector256.Create(0.0, 0.0, 0.0, size) / size;
            var sv2 = Vector256.Create(0.0, height, 0.0, size) / size;
            var sv3 = Vector256.Create(width, height, 0.0, size) / size;
            var sv4 = Vector256.Create(width, 0.0, 0.0, size) / size;
            var v1 = mvt.Transform(sv1);
            var v2 = mvt.Transform(sv2);
            var v3 = mvt.Transform(sv3);
            var v4 = mvt.Transform(sv4);

            Matrix4x4d.Invert(mv, out var invertedModelViewMatrix);
            invertedModelViewMatrix = Matrix4x4d.Transpose(invertedModelViewMatrix);

            var farPoint = Avx.And(mv.Transform(Vector256.Create(0.0, 0.0, -10000.0, 1.0)), Vector256.Create(0xFFFFFFFFFFFFFFFFUL, 0xFFFFFFFFFFFFFFFFUL, 0xFFFFFFFFFFFFFFFFUL, 0).AsDouble());
            var triangles = TriangleDivider.ClipAndDivide([new BoundingBoxTriangle(v1, v2, v3, farPoint, invertedModelViewMatrix), new BoundingBoxTriangle(v1, v3, v4, farPoint, invertedModelViewMatrix)]).ToArray();
            var projectionOffset = Vector256.Create(offsetX, offsetY, 0.0, 0.0) * size;

            var av = projectionMatrix.Transform(anchorPointMv.Transform(Avx.Divide(Avx.Add(anchorPoint.AsVector256(), Vector256.Create(0.0, 0.0, 0.0, size)), Vector256.Create((double)size))));
            av = Avx.Divide(av, Vector256.Create(av.GetElement(3)));
            var s = new Vector2d(size, size) * 0.5;
            var bbAnchorPoint = ((Vector2d)av) * s + (new Vector2d(Width, Height) * 0.5);

            var points = new List<Vector128<double>>();
            foreach (var triangle in triangles)
            {
                var uv1 = triangle.V1.Transform(projectionMatrix).Vertex;
                var uv2 = triangle.V2.Transform(projectionMatrix).Vertex;
                var uv3 = triangle.V3.Transform(projectionMatrix).Vertex;

                uv1 /= Math.Abs(uv1.GetElement(3));
                uv2 /= Math.Abs(uv2.GetElement(3));
                uv3 /= Math.Abs(uv3.GetElement(3));
                var dvv1 = Avx.ExtractVector128((uv1 + Vector256.Create(1.0, 1.0, 0.0, 0.0)) * Vector256.Create(size * 0.5, size * 0.5, 1.0, 1.0) - projectionOffset, 0);
                var dvv2 = Avx.ExtractVector128((uv2 + Vector256.Create(1.0, 1.0, 0.0, 0.0)) * Vector256.Create(size * 0.5, size * 0.5, 1.0, 1.0) - projectionOffset, 0);
                var dvv3 = Avx.ExtractVector128((uv3 + Vector256.Create(1.0, 1.0, 0.0, 0.0)) * Vector256.Create(size * 0.5, size * 0.5, 1.0, 1.0) - projectionOffset, 0);

                points.Add(dvv1);
                points.Add(dvv2);
                points.Add(dvv3);
            }

            points = [.. points.Distinct()];
            if (points.Count < 3)
            {
                // 空
                return new PreviewBoundingBox(bbAnchorPoint, [], true, bbAnchorPoint.IsNaN() || bbAnchorPoint.IsInfinty());
            }

            var pointSpan = CollectionsMarshal.AsSpan(points);
            var orderedPoints = new Vector128<double>[points.Count];
            var used = 1;
            orderedPoints[0] = points.MinBy(p => p.GetElement(0));
            var prev = orderedPoints[0];
            while (used < orderedPoints.Length)
            {
                var b = pointSpan[0];
                for (var i = 1; i < pointSpan.Length; i++)
                {
                    var c = pointSpan[i];
                    if (b == prev)
                    {
                        b = c;
                    }
                    else
                    {
                        var ab = b - prev;
                        var ac = c - prev;
                        var v = ab.CrossProduct(ac);
                        if (v > 0.0 || (v == 0.0 && ac.LengthSquared() > ab.LengthSquared()))
                        {
                            b = c;
                        }
                    }
                }

                orderedPoints[used] = b;
                prev = b;
                used++;
            }

            var shape = new BoundingBoxShape([.. orderedPoints.Select(v => (Vector2d)v)], true, false);
            return new PreviewBoundingBox(bbAnchorPoint, [shape], false, bbAnchorPoint.IsNaN() || bbAnchorPoint.IsInfinty());
        }

        public PreviewBoundingBox GetCameraBoundingBox(CameraSetting targetCameraSetting, CameraSetting cameraSetting)
        {
            var size = Math.Max(Width, Height);
            var fov = Math.Atan((Width / cameraSetting.Zoom) * 0.5) * 2.0;

            var projectionMatrix = Matrix4x4d.CreatePerspectiveFieldOfView(fov, 1.0, double.Epsilon, double.PositiveInfinity);
            var viewMatrix = Transform3D.Calc3DViewMatrix(cameraSetting, Width, Height);
            var modelMatrix = Transform3D.GetInvertedCameraMatrix(targetCameraSetting, Width, Height);
            foreach (var (type, parentTransform) in targetCameraSetting.ParentTransforms)
            {
                switch (type)
                {
                    case ParentType.Camera:
                        modelMatrix *= Transform3D.GetInvertedCameraMatrix(parentTransform, Width, Height);
                        break;
                    case ParentType.SpotOrParallelLight:
                    case ParentType.PointLight:
                        modelMatrix *= Transform3D.GetLightMatrix(type == ParentType.SpotOrParallelLight ? LightType.Spot : LightType.Point, parentTransform, Width, Height);
                        break;
                    case ParentType.AmbientLight:
                        break;
                    default:
                        modelMatrix *= Transform3D.GetTransform3D(parentTransform, size);
                        break;
                }
            }

            var mv = modelMatrix * viewMatrix * Matrix4x4d.CreateTranslate((size - Width) * 0.5 / size, (size - Height) * 0.5 / size, 0.0);

            var offset = new Vector2d(Width, Height) * 0.5;
            var s = new Vector2d(size, size) * 0.5;
            var length = (targetCameraSetting.PointOfInterest - targetCameraSetting.Position).Length() / size;
            var pos = (Vector2d)projectionMatrix.Transform(mv.Transform(Vector256.Create(0.0, 0.0, length, 1.0))) * s + offset;
            var poi = (Vector2d)projectionMatrix.Transform(mv.Transform(Vector256.Create(0.0, 0.0, 0.0, 1.0))) * s + offset;
            var zoomLength = targetCameraSetting.Zoom / size;
            var frustumSize = new Vector2d(Width, Height) / s * zoomLength * Math.Tan(fov * 0.5) * 0.5;

            var frustum = new Vector256<double>[][]
            {
                [Vector256.Create(-frustumSize.X, -frustumSize.Y, length - zoomLength, 1.0), Vector256.Create(frustumSize.X, -frustumSize.Y, length - zoomLength, 1.0)],
                [Vector256.Create(-frustumSize.X, -frustumSize.Y, length - zoomLength, 1.0), Vector256.Create(-frustumSize.X, frustumSize.Y, length - zoomLength, 1.0)],
                [Vector256.Create(frustumSize.X, -frustumSize.Y, length - zoomLength, 1.0), Vector256.Create(frustumSize.X, frustumSize.Y, length - zoomLength, 1.0)],
                [Vector256.Create(-frustumSize.X, frustumSize.Y, length - zoomLength, 1.0), Vector256.Create(frustumSize.X, frustumSize.Y, length - zoomLength, 1.0)]
            }.Select(shape => new BoundingBoxShape(shape.Select(v => projectionMatrix.Transform(mv.Transform(v))).Select(v => (Vector2d)Avx.Divide(v, v.Permute4x64(0b11_11_11_11)) * s + offset).Prepend(pos).ToArray(), true, false))
            .Prepend(new BoundingBoxShape([poi, pos], false, false))
            .ToArray();

            return new PreviewBoundingBox(poi, frustum, false, double.IsNaN(fov) || double.IsInfinity(fov) || fov >= 180.0);
        }

        public PreviewBoundingBox GetLightBoundingBox(LightSetting lightSetting, CameraSetting cameraSetting)
        {
            var size = Math.Max(Width, Height);
            var fov = Math.Atan((Width / cameraSetting.Zoom) * 0.5) * 2.0;

            var projectionMatrix = Matrix4x4d.CreatePerspectiveFieldOfView(fov, 1.0, double.Epsilon, double.PositiveInfinity);
            var modelMatrix = Transform3D.CalcLightMatrix(lightSetting, Width, Height);
            var viewMatrix = Transform3D.Calc3DViewMatrix(cameraSetting, Width, Height);

            var mv = modelMatrix * viewMatrix * Matrix4x4d.CreateTranslate((size - Width) * 0.5 / size, (size - Height) * 0.5 / size, 0.0);

            var offset = new Vector2d(Width, Height) * 0.5;
            var s = new Vector2d(size, size) * 0.5;
            var length = (lightSetting.PointOfInterest - lightSetting.Position).Length() / size;
            var pos = (Vector2d)projectionMatrix.Transform(mv.Transform(Vector256.Create(0.0, 0.0, 0.0, 1.0))) * s + offset;
            var poi = (Vector2d)projectionMatrix.Transform(mv.Transform(Vector256.Create(0.0, 0.0, length, 1.0))) * s + offset;

            const double PositionMarkSize = 5.0;
            const int PositionMarkStepCount = 8;
            const double PositionMarkStep = Math.PI * 2.0 / PositionMarkStepCount;
            var positionMark = Enumerable.Range(0, PositionMarkStepCount).Select(i => new Vector2d(Math.Cos(PositionMarkStep * i), Math.Sin(PositionMarkStep * i)) * PositionMarkSize + pos).ToArray();

            return new PreviewBoundingBox(
                lightSetting.LightType == LightType.Point ? pos : poi,
                [new BoundingBoxShape([pos, poi], false, false), new BoundingBoxShape(positionMark, true, true, pos)],
                lightSetting.LightType == LightType.Point,
                lightSetting.LightType == LightType.Ambient || double.IsNaN(lightSetting.ConeAngle) || double.IsInfinity(lightSetting.ConeAngle)
            );
        }

        public Guid? SelectLayer(CameraSetting cameraSetting, LayerSkeleton[] layers, Vector2d pos)
        {
            var result = (Guid?)null;

            var size = Math.Max(Width, Height);
            var offsetX = (size - Width) * 0.5 / size;
            var offsetY = (size - Height) * 0.5 / size;
            var offsetMatrix = Matrix4x4d.CreateTranslate(offsetX, offsetY, 0.0);
            var projectionOffset = Vector256.Create(offsetX, offsetY, 0.0, 0.0) * size;
            var clickPoint = Vector256.Create(pos.X, pos.Y, 0.0, 0.0);

            var viewMatrix = Transform3D.Calc3DViewMatrix(cameraSetting, Width, Height);
            var fov = Math.Atan((Width / (cameraSetting.Zoom)) * 0.5) * 2.0;
            var projectionMatrix = Matrix4x4d.CreatePerspectiveFieldOfView(fov, 1.0, double.Epsilon, double.PositiveInfinity);

            foreach (var group in layers.GroupByPrev(l => l.IsEnable3D))
            {
                if (group.First().IsEnable3D)
                {
                    var triangles = new List<BoundingBoxTriangle>();
                    var ids = new Dictionary<int, Guid>();
                    foreach (var (layer, i) in group.Reverse().ZipWithIndex())
                    {
                        var (t1, t2) = CreateBoundingBoxTriangles(layer, Width, Height, viewMatrix, offsetMatrix, i);
                        triangles.Add(t1);
                        triangles.Add(t2);
                        ids.Add(i, layer.LayerId);
                    }

                    var hit = new HashSet<int>();
                    foreach (var triangle in TriangleDivider.ClipAndDivide(triangles))
                    {
                        if (hit.Contains(triangle.Id))
                        {
                            continue;
                        }

                        var uv1 = triangle.V1.Transform(projectionMatrix).Vertex;
                        var uv2 = triangle.V2.Transform(projectionMatrix).Vertex;
                        var uv3 = triangle.V3.Transform(projectionMatrix).Vertex;

                        var w1 = 1.0 / Math.Abs(uv1.GetElement(3));
                        var w2 = 1.0 / Math.Abs(uv2.GetElement(3));
                        var w3 = 1.0 / Math.Abs(uv3.GetElement(3));
                        uv1 *= w1;
                        uv2 *= w2;
                        uv3 *= w3;
                        var dvv1 = (uv1 + Vector256.Create(1.0, 1.0, 0.0, 0.0)) * Vector256.Create(size * 0.5, size * 0.5, 1.0, 1.0) - projectionOffset;
                        var dvv2 = (uv2 + Vector256.Create(1.0, 1.0, 0.0, 0.0)) * Vector256.Create(size * 0.5, size * 0.5, 1.0, 1.0) - projectionOffset;
                        var dvv3 = (uv3 + Vector256.Create(1.0, 1.0, 0.0, 0.0)) * Vector256.Create(size * 0.5, size * 0.5, 1.0, 1.0) - projectionOffset;

                        var ab = Avx.ExtractVector128(dvv2 - dvv1, 0);
                        var bc = Avx.ExtractVector128(dvv3 - dvv2, 0);
                        var ca = Avx.ExtractVector128(dvv1 - dvv3, 0);
                        var ap = Avx.ExtractVector128(clickPoint - dvv1, 0);
                        var bp = Avx.ExtractVector128(clickPoint - dvv2, 0);
                        var cp = Avx.ExtractVector128(clickPoint - dvv3, 0);

                        var abp = ab.CrossProduct(bp);
                        var bcp = bc.CrossProduct(cp);
                        var cap = ca.CrossProduct(ap);

                        // TODO: Z座標を比較する
                        if ((abp > 0.0 && bcp > 0.0 && cap > 0.0) || (abp < 0.0 && bcp < 0.0 && cap < 0.0))
                        {
                            result = ids[triangle.Id];
                            hit.Add(triangle.Id);
                        }
                    }
                }
                else
                {
                    foreach (var (layerId, (origin, width, height), isEnable3D, transformProperty, parentTransformProperties) in group.Reverse())
                    {
                        var transform = Transform2D.CalcTransform2D(transformProperty, parentTransformProperties);
                        transform = Matrix3x3.CreateTranslate(-(float)origin.X, -(float)origin.Y) * transform;
                        if (!Matrix3x3.Invert(transform, out var inverted))
                        {
                            continue;
                        }

                        var (imageX, imageY) = inverted.Transform((float)pos.X, (float)pos.Y);
                        if (imageX > -1.0F && imageY > -1.0F && imageX < width && imageY < height)
                        {
                            result = layerId;
                            break;
                        }
                    }
                }
            }

            return result;
        }

        public Vector2d LocalCoordToScreenCoord(CameraSetting cameraSetting, LayerSkeleton baseLayer, Vector3d pos)
        {
            if (baseLayer.IsEnable3D)
            {
                var size = Math.Max(Width, Height);
                var fov = Math.Atan((Width / cameraSetting.Zoom) * 0.5) * 2.0;
                var projectionMatrix = Matrix4x4d.CreatePerspectiveFieldOfView(fov, 1.0, double.Epsilon, double.PositiveInfinity);
                var modelMatrix = Transform3D.Calc3DModelMatrix(baseLayer.Transform, baseLayer.ParentTransform, Width, Height);
                var viewMatrix = Transform3D.Calc3DViewMatrix(cameraSetting, Width, Height);
                var offsetX = (size - Width) * 0.5 / size;
                var offsetY = (size - Height) * 0.5 / size;
                var offsetMatrix = Matrix4x4d.CreateTranslate(offsetX, offsetY, 0.0);
                var mvt = modelMatrix * viewMatrix * offsetMatrix;

                var pp = projectionMatrix.Transform(mvt.Transform((pos.AsVector256() + Vector256.Create(0.0, 0.0, 0.0, size)) / Vector256.Create((double)size)));
                return (Vector2d)(pp / pp.GetElement(3) * size * 0.5) + (new Vector2d(Width, Height) * 0.5);
            }
            else
            {
                var transform = Transform2D.CalcTransform2D(baseLayer.Transform, baseLayer.ParentTransform);
                var (screenX, screenY) = transform.Transform((float)pos.X, (float)pos.Y);
                return new Vector2d(screenX, screenY);
            }
        }

        public Vector2d WorldCoordToScreenCoord(CameraSetting cameraSetting, Vector3d pos)
        {
            var size = Math.Max(Width, Height);
            var fov = Math.Atan((Width / cameraSetting.Zoom) * 0.5) * 2.0;
            var projectionMatrix = Matrix4x4d.CreatePerspectiveFieldOfView(fov, 1.0, double.Epsilon, double.PositiveInfinity);
            var viewMatrix = Transform3D.Calc3DViewMatrix(cameraSetting, Width, Height);
            var offsetX = (size - Width) * 0.5 / size;
            var offsetY = (size - Height) * 0.5 / size;
            var offsetMatrix = Matrix4x4d.CreateTranslate(offsetX, offsetY, 0.0);
            var mvt = viewMatrix * offsetMatrix;

            var pp = projectionMatrix.Transform(mvt.Transform((pos.AsVector256() + Vector256.Create(0.0, 0.0, 0.0, size)) / Vector256.Create((double)size)));
            return (Vector2d)(pp / pp.GetElement(3) * size * 0.5) + (new Vector2d(Width, Height) * 0.5);
        }

        public Vector3d ScreenCoordToLocalCoord(CameraSetting cameraSetting, LayerSkeleton baseLayer, Vector2d pos)
        {
            if (baseLayer.IsEnable3D)
            {
                var size = Math.Max(Width, Height);
                var offset = Vector256.Create(size - Width, size - Height, 0.0, 0.0) * 0.5 / size;
                var viewMatrix = Transform3D.Calc3DViewMatrix(cameraSetting, Width, Height);
                var fov = Math.Atan((Width / (cameraSetting.Zoom)) * 0.5) * 2.0;

                var minZ = (double)TriangleDivider.NearZ;
                var maxZ = cameraSetting.Zoom / size;
                var offsetX = (size - Width) * 0.5 / size;
                var offsetY = (size - Height) * 0.5 / size;
                var offsetMatrix = Matrix4x4d.CreateTranslate(offsetX, offsetY, 0.0);

                var (t1, t2) = CreateBoundingBoxTriangles(baseLayer, Width, Height, viewMatrix, offsetMatrix);
                var triangles = TriangleDivider.ClipAndDivide([t1, t2]).ToArray();

                if (triangles.Length > 0)
                {
                    minZ = triangles.Select(t => Math.Min(Math.Min(t.V1.Vertex.GetElement(2), t.V2.Vertex.GetElement(2)), t.V3.Vertex.GetElement(2))).Min();
                    maxZ = triangles.Select(t => Math.Max(Math.Max(t.V1.Vertex.GetElement(2), t.V2.Vertex.GetElement(2)), t.V3.Vertex.GetElement(2))).Max();
                }
                if (maxZ - minZ < EpsilonZDiff)
                {
                    maxZ += EpsilonZDiff;
                }

                var projectionMatrix = Matrix4x4d.CreatePerspectiveFieldOfView(fov, 1.0, minZ, maxZ);
                Matrix4x4d.Invert(viewMatrix * projectionMatrix, out var invertedViewProjection);

                var p = Vector256.Create(pos.X, pos.Y, size * 0.5, size * 0.5) / (size * 0.5) - Vector256.Create(1.0, 1.0, 0.0, 0.0);
                var result = invertedViewProjection.Transform(p);
                var w = result.GetElement(3);
                return (Vector3d)(result / (w != 0.0 ? w : 1.0) * size);
            }
            else
            {
                var transform = Transform2D.CalcTransform2D(baseLayer.Transform, baseLayer.ParentTransform);
                Matrix3x3.Invert(transform, out var inverted);
                var (worldX, worldY) = inverted.Transform((float)pos.X, (float)pos.Y);
                return new Vector3d(worldX, worldY, 0.0);
            }
        }

        public Vector3d ScreenCoordToWorldCoord(CameraSetting cameraSetting, Vector2d pos)
        {
            var size = Math.Max(Width, Height);
            var offset = Vector256.Create(size - Width, size - Height, 0.0, 0.0) * 0.5 / size;
            var viewMatrix = Transform3D.Calc3DViewMatrix(cameraSetting, Width, Height);
            var fov = Math.Atan((Width / (cameraSetting.Zoom)) * 0.5) * 2.0;

            var offsetX = (size - Width) * 0.5 / size;
            var offsetY = (size - Height) * 0.5 / size;
            var offsetMatrix = Matrix4x4d.CreateTranslate(offsetX, offsetY, 0.0);

            var projectionMatrix = Matrix4x4d.CreatePerspectiveFieldOfView(fov, 1.0, TriangleDivider.NearZ, cameraSetting.Zoom / size);
            Matrix4x4d.Invert(viewMatrix * projectionMatrix, out var invertedViewProjection);

            var p = Vector256.Create(pos.X, pos.Y, size * 0.5, size * 0.5) / (size * 0.5) - Vector256.Create(1.0, 1.0, 0.0, 0.0);
            var result = invertedViewProjection.Transform(p);
            var w = result.GetElement(3);
            return (Vector3d)(result / (w != 0.0 ? w : 1.0) * size);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static (BoundingBoxTriangle, BoundingBoxTriangle) CreateBoundingBoxTriangles(LayerSkeleton layerSkeleton, int compositionWidth, int compositionHeight, in Matrix4x4d viewMatrix, in Matrix4x4d offsetMatrix, int index = 0)
        {
            var size = Math.Max(compositionWidth, compositionHeight);
            var (_, (origin, width, height), isEnable3D, transformProperty, parentTransformProperties) = layerSkeleton;

            var modelMatrix = Matrix4x4d.CreateTranslate(-origin.X / size, -origin.Y / size, 0.0) * Transform3D.Calc3DModelMatrix(transformProperty, parentTransformProperties, compositionWidth, compositionHeight);
            var mv = modelMatrix * viewMatrix;
            var mvt = mv * offsetMatrix;
            var sv1 = Vector256.Create(0.0, 0.0, 0.0, size) / size;
            var sv2 = Vector256.Create(0.0, height, 0.0, size) / size;
            var sv3 = Vector256.Create(width, height, 0.0, size) / size;
            var sv4 = Vector256.Create(width, 0.0, 0.0, size) / size;
            var v1 = mvt.Transform(sv1);
            var v2 = mvt.Transform(sv2);
            var v3 = mvt.Transform(sv3);
            var v4 = mvt.Transform(sv4);

            Matrix4x4d.Invert(mv, out var invertedModelViewMatrix);
            invertedModelViewMatrix = Matrix4x4d.Transpose(invertedModelViewMatrix);

            var farPoint = Avx.And(mv.Transform(Vector256.Create(0.0, 0.0, -10000.0, 1.0)), Vector256.Create(0xFFFFFFFFFFFFFFFFUL, 0xFFFFFFFFFFFFFFFFUL, 0xFFFFFFFFFFFFFFFFUL, 0).AsDouble());

            return (new BoundingBoxTriangle(v1, v2, v3, farPoint, invertedModelViewMatrix, index), new BoundingBoxTriangle(v1, v3, v4, farPoint, invertedModelViewMatrix, index));
        }
    }
}
