using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Image;
using NiVE3.Numerics;
using NiVE3.PresetPlugin.Internal.Drawing.Primitive3D;
using NiVE3.Plugin.Interfaces;
using NiVE3.Shared.Extension;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using NiVE3.Plugin.Interfaces.RendererParams;
using System.Buffers;
using NiVE3.Image.Drawing;

namespace NiVE3.PresetPlugin.Internal.Drawing
{
    abstract class Renderer3DBase
    {
        // ビュー空間に持って行った後のカメラよりも後ろの部分のトリミング用ニアクリップ面
        //NOTE: 影用にカメラよりも前にニアクリップ面を持ってきているが、他含め描画上問題が
        //      あったら調整する(現状、カメラのZ -2666.66に対し、レイヤーのZ -2666.57で消える)
        const float NearZ = 5E-5F;

        protected const float Epsilon = 1E-7F;

        protected const float ShininessStrength = 120.0F;

        protected static readonly float[] EmptyTrackMatte = [1.0F];

        public Matrix4x4d ViewMatrix { get; set; }

        public double FieldOfView { get; set; }

        public int Size { get; }

        protected int Width { get; }

        protected int Height { get; }

        protected int OffsetX { get; }

        protected int OffsetY { get; }

        protected int LastId { get; set; } = 1;

        protected List<PointLight> PointLights { get; }

        protected List<SpotLight> SpotLights { get; }

        protected List<ParallelLight> ParallelLights { get; }

        protected List<AmbientLight> AmbientLights { get; }

        protected List<Triangle> Triangles { get; } = [];

        protected Dictionary<object, List<LightTriangle>> LightTriangles { get; }

        public Renderer3DBase(int width, int height, List<PointLight> pointLights, List<SpotLight> spotLights, List<ParallelLight> parallelLights, List<AmbientLight> ambientLights)
        {
            Size = Math.Max(width, height);
            Width = width;
            Height = height;
            OffsetX = (Size - width) / 2;
            OffsetY = (Size - height) / 2;
            PointLights = pointLights;
            SpotLights = spotLights;
            ParallelLights = parallelLights;
            AmbientLights = ambientLights;

            LightTriangles = pointLights.Cast<object>().Concat(spotLights).Concat(parallelLights).ToDictionary(k => k, _ => new List<LightTriangle>());
            foreach (var pointLight in pointLights)
            {
                LightTriangles.Add(new PointLightHolder(pointLight, PointLightShadowDirection.Front), []);
                LightTriangles.Add(new PointLightHolder(pointLight, PointLightShadowDirection.Back), []);
                LightTriangles.Add(new PointLightHolder(pointLight, PointLightShadowDirection.Left), []);
                LightTriangles.Add(new PointLightHolder(pointLight, PointLightShadowDirection.Right), []);
                LightTriangles.Add(new PointLightHolder(pointLight, PointLightShadowDirection.Top), []);
                LightTriangles.Add(new PointLightHolder(pointLight, PointLightShadowDirection.Bottom), []);
            }
        }

        public void AddRect(NImage texture, float opacity, BlendMode blendType, Matrix4x4d modelMatrix, bool isCastShadow, float lightTransmission, bool isAcceptShadow, bool isAcceptLight, float ambient, float diffuse, float specularIntensity, float specularShininess, float metal, RasterizedMaskImage? trackMatte)
        {
            var width = texture.Width;
            var height = texture.Height;
            var offsetX = (Size - Width) * 0.5 / Size;
            var offsetY = (Size - Height) * 0.5 / Size;
            var sv1 = Avx.Divide(Vector256.Create(0.0, 0.0, 0.0, Size), Vector256.Create((double)Size));
            var sv2 = Avx.Divide(Vector256.Create(0.0, height, 0.0, Size), Vector256.Create((double)Size));
            var sv3 = Avx.Divide(Vector256.Create(width, height, 0.0, Size), Vector256.Create((double)Size));
            var sv4 = Avx.Divide(Vector256.Create(width, 0.0, 0.0, Size), Vector256.Create((double)Size));

            modelMatrix = Matrix4x4d.CreateTranslate(-texture.Origin.X / Size, -texture.Origin.Y / Size, 0.0) * modelMatrix;
            var mv = modelMatrix * ViewMatrix;
            var mvt = mv * Matrix4x4d.CreateTranslate(offsetX, offsetY, 0.0);
            var v1 = mvt.Transform(sv1);
            var v2 = mvt.Transform(sv2);
            var v3 = mvt.Transform(sv3);
            var v4 = mvt.Transform(sv4);

            var uv1 = new UVVertex(v1, 0.0F, 0.0F);
            var uv2 = new UVVertex(v2, 0.0F, 1.0F);
            var uv3 = new UVVertex(v3, 1.0F, 1.0F);
            var uv4 = new UVVertex(v4, 1.0F, 0.0F);

            Matrix4x4d.Invert(mv, out var invertedModelViewMatrix);
            invertedModelViewMatrix = Matrix4x4d.Transpose(invertedModelViewMatrix);

            var farPoint = Avx.And(mv.Transform(Vector256.Create(0.0, 0.0, -10000.0, 1.0)), Vector256.Create(0xFFFFFFFFFFFFFFFFUL, 0xFFFFFFFFFFFFFFFFUL, 0xFFFFFFFFFFFFFFFFUL, 0).AsDouble());
            Triangles.Add(new Triangle(uv1, uv2, uv3, farPoint, invertedModelViewMatrix, texture, opacity, blendType, isCastShadow, lightTransmission, isAcceptShadow, isAcceptLight, ambient, diffuse, specularIntensity, specularShininess, metal, trackMatte, LastId));
            Triangles.Add(new Triangle(uv1, uv3, uv4, farPoint, invertedModelViewMatrix, texture, opacity, blendType, isCastShadow, lightTransmission, isAcceptShadow, isAcceptLight, ambient, diffuse, specularIntensity, specularShininess, metal, trackMatte, LastId));

            foreach (var spotLight in SpotLights)
            {
                if (!spotLight.IsEnableShadow)
                {
                    continue;
                }
                var (lt1, lt2) = CreateLightTriangle(LastId, texture, opacity, isCastShadow, lightTransmission, sv1, sv2, sv3, sv4, modelMatrix, mv, spotLight.LightViewMatrix, offsetX, offsetY);
                var triangles = LightTriangles[spotLight];
                triangles.Add(lt1);
                triangles.Add(lt2);
            }
            foreach (var parallelLight in ParallelLights)
            {
                if (!parallelLight.IsEnableShadow)
                {
                    continue;
                }
                var (lt1, lt2) = CreateLightTriangle(LastId, texture, opacity, isCastShadow, lightTransmission, sv1, sv2, sv3, sv4, modelMatrix, mv, parallelLight.LightViewMatrix, offsetX, offsetY);
                var triangles = LightTriangles[parallelLight];
                triangles.Add(lt1);
                triangles.Add(lt2);
            }
            foreach (var pointLight in PointLights)
            {
                if (!pointLight.IsEnableShadow)
                {
                    continue;
                }
                var (lt1, lt2) = CreateLightTriangle(LastId, texture, opacity, isCastShadow, lightTransmission, sv1, sv2, sv3, sv4, modelMatrix, mv, pointLight.FrontLightViewMatrix, offsetX, offsetY);
                var triangles = LightTriangles[new PointLightHolder(pointLight, PointLightShadowDirection.Front)];
                triangles.Add(lt1);
                triangles.Add(lt2);
                (lt1, lt2) = CreateLightTriangle(LastId, texture, opacity, isCastShadow, lightTransmission, sv1, sv2, sv3, sv4, modelMatrix, mv, pointLight.BackLightViewMatrix, offsetX, offsetY);
                triangles = LightTriangles[new PointLightHolder(pointLight, PointLightShadowDirection.Back)];
                triangles.Add(lt1);
                triangles.Add(lt2);
                (lt1, lt2) = CreateLightTriangle(LastId, texture, opacity, isCastShadow, lightTransmission, sv1, sv2, sv3, sv4, modelMatrix, mv, pointLight.LeftLightViewMatrix, offsetX, offsetY);
                triangles = LightTriangles[new PointLightHolder(pointLight, PointLightShadowDirection.Left)];
                triangles.Add(lt1);
                triangles.Add(lt2);
                (lt1, lt2) = CreateLightTriangle(LastId, texture, opacity, isCastShadow, lightTransmission, sv1, sv2, sv3, sv4, modelMatrix, mv, pointLight.RightLightViewMatrix, offsetX, offsetY);
                triangles = LightTriangles[new PointLightHolder(pointLight, PointLightShadowDirection.Right)];
                triangles.Add(lt1);
                triangles.Add(lt2);
                (lt1, lt2) = CreateLightTriangle(LastId, texture, opacity, isCastShadow, lightTransmission, sv1, sv2, sv3, sv4, modelMatrix, mv, pointLight.TopLightViewMatrix, offsetX, offsetY);
                triangles = LightTriangles[new PointLightHolder(pointLight, PointLightShadowDirection.Top)];
                triangles.Add(lt1);
                triangles.Add(lt2);
                (lt1, lt2) = CreateLightTriangle(LastId, texture, opacity, isCastShadow, lightTransmission, sv1, sv2, sv3, sv4, modelMatrix, mv, pointLight.BottomLightViewMatrix, offsetX, offsetY);
                triangles = LightTriangles[new PointLightHolder(pointLight, PointLightShadowDirection.Bottom)];
                triangles.Add(lt1);
                triangles.Add(lt2);
            }

            LastId++;
        }

        protected IEnumerable<T> GetClipAndDividedTriangles<T>(IEnumerable<T> triangles) where T : TriangleBase<T>
        {
            return ClipAndDivideTriangle(triangles.Where(t => !t.IsInvalidNormal && !t.IsDegenerate()).ToArray())
                // NOTE: ポリゴンの欠けが出たら消す事を検討する
                .Where(t => t.V1.Vertex.GetElement(2) > NearZ - Epsilon && t.V2.Vertex.GetElement(2) > NearZ - Epsilon && t.V3.Vertex.GetElement(2) > NearZ - Epsilon);
        }

        IEnumerable<T> ClipAndDivideTriangle<T>(T[] triangles) where  T : TriangleBase<T>
        {
            var dividedTriangles = new List<T>(DivideTriangles(triangles));

            var p = Vector256.Create(0.0, 0.0, NearZ, 0.0);
            var n = Vector256.Create(0.0, 0.0, Math.Sign(NearZ), 0.0);
            foreach (var triangle in dividedTriangles)
            {
                var (t1, t2, t3) = DivideTriangleByPlane(triangle, p, n);

                yield return t1;
                if (t2 != null)
                {
                    yield return t2;
                }
                if (t3 != null)
                {
                    yield return t3;
                }
            }
        }

        IEnumerable<T> DivideTriangles<T>(IEnumerable<T> triangles) where T : TriangleBase<T>
        {
            using var e = triangles.GetEnumerator();
            if (!e.MoveNext())
            {
                return triangles;
            }

            var divider = e.Current;
            var near = new List<T>();
            var far = new List<T>();

            while (e.MoveNext())
            {
                DivideTriangleByTriangle(e.Current, divider, near, far);
            }

            return DivideTriangles(far).Append(divider).Concat(DivideTriangles(near));
        }

        // from Javie
        static void DivideTriangleByTriangle<T>(T triangle, T divider, List<T> near, List<T> far) where T : TriangleBase<T>
        {
            const double Epsilon = 1E-10;
            var wClearMask = Vector256.Create(0xFFFFFFFFFFFFFFFFUL, 0xFFFFFFFFFFFFFFFFUL, 0xFFFFFFFFFFFFFFFFUL, 0).AsDouble();

            var dSign = Math.Sign(divider.PlaneD);
            var n = divider.Normal;
            var planeD = Vector256.Create(divider.PlaneD, divider.PlaneD, divider.PlaneD, 0.0F);
            var dd1 = Avx.And(Avx.Add(n.DotProduct(triangle.V1.Vertex), planeD), wClearMask);
            var dd2 = Avx.And(Avx.Add(n.DotProduct(triangle.V2.Vertex), planeD), wClearMask);
            var dd3 = Avx.And(Avx.Add(n.DotProduct(triangle.V3.Vertex), planeD), wClearMask);
            var maxD = MaxByAbs(MaxByAbs(dd1, dd2), dd3).GetElement(0);
            if (Math.Abs(maxD) < Epsilon)
            {
                if (divider.SignIsDifferent)
                {
                    near.Add(triangle);
                }
                else
                {
                    far.Add(triangle);
                }
                return;
            }

            var (t1, t2, t3) = DivideTriangleByPlane(triangle, divider.V1.Vertex, n);

            if (t2 == null || t3 == null)
            {
                if (Math.Sign(maxD) == dSign)
                {
                    near.Add(triangle);
                }
                else
                {
                    far.Add(triangle);
                }
            }
            else
            {
                foreach (var t in new[] { t1, t2, t3 })
                {
                    if (t.IsInvalidNormal)
                    {
                        continue;
                    }

                    var td1 = Avx.And(Avx.Add(t.V1.Vertex.DotProduct(n), planeD), wClearMask);
                    var td2 = Avx.And(Avx.Add(t.V2.Vertex.DotProduct(n), planeD), wClearMask);
                    var td3 = Avx.And(Avx.Add(t.V3.Vertex.DotProduct(n), planeD), wClearMask);

                    if (Math.Sign(MaxByAbs(MaxByAbs(td1, td2), td3).GetElement(0)) == dSign)
                    {
                        near.Add(t);
                    }
                    else
                    {
                        far.Add(t);
                    }
                }
            }
        }

        static (T, T?, T?) DivideTriangleByPlane<T>(T triangle, in Vector256<double> p, in Vector256<double> n) where T : TriangleBase<T>
        {
            const double Epsilon = -1E-10;
            var One = Vector256.Create(1.0);

            var p1 = triangle.V1.Vertex;
            var p2 = triangle.V2.Vertex;
            var p3 = triangle.V3.Vertex;

            var p12 = Avx.Subtract(p2, p1);
            var p23 = Avx.Subtract(p3, p2);
            var p31 = Avx.Subtract(p1, p3);

            var planeD = p.DotProduct(n);

            var d1 = Avx.Subtract(p1, p).DotProduct(n).GetElement(0);
            var d2 = Avx.Subtract(p2, p).DotProduct(n).GetElement(0);
            var d3 = Avx.Subtract(p3, p).DotProduct(n).GetElement(0);

            if (d1 * d2 <= Epsilon)
            {
                var dt1 = RoundCurrentDirection(Avx.Divide(Avx.Subtract(planeD, n.DotProduct(p1)), n.DotProduct(p12)), 10);
                var ep1 = new UVVertex(
                    Avx.Blend((Fma.IsSupported ? Fma.MultiplyAdd(p12, dt1, p1) : Avx.Add(p1, Avx.Multiply(p12, dt1))), One, 0b1000),
                    triangle.V1.U + (triangle.V2.U - triangle.V1.U) * dt1.GetElement(0),
                    triangle.V1.V + (triangle.V2.V - triangle.V1.V) * dt1.GetElement(0)
                );

                if (d2 * d3 <= Epsilon)
                {
                    var dt2 = RoundCurrentDirection(Avx.Divide(Avx.Subtract(planeD, n.DotProduct(p2)), n.DotProduct(p23)), 10);
                    var ep2 = new UVVertex(
                        Avx.Blend((Fma.IsSupported ? Fma.MultiplyAdd(p23, dt2, p2) : Avx.Add(p2, Avx.Multiply(p23, dt2))), One, 0b1000),
                        triangle.V2.U + (triangle.V3.U - triangle.V2.U) * dt2.GetElement(0),
                        triangle.V2.V + (triangle.V3.V - triangle.V2.V) * dt2.GetElement(0)
                    );

                    return (
                        triangle.CreateByNewVertex(triangle.V1, ep1, triangle.V3),
                        triangle.CreateByNewVertex(ep1, ep2, triangle.V3),
                        triangle.CreateByNewVertex(ep1, triangle.V2, ep2)
                    );
                }
                else
                {
                    var dt3 = RoundCurrentDirection(Avx.Divide(Avx.Subtract(planeD, n.DotProduct(p3)), n.DotProduct(p31)), 10);
                    var ep3 = new UVVertex(
                        Avx.Blend((Fma.IsSupported ? Fma.MultiplyAdd(p31, dt3, p3) : Avx.Add(p3, Avx.Multiply(p31, dt3))), One, 0b1000),
                        triangle.V3.U + (triangle.V1.U - triangle.V3.U) * dt3.GetElement(0),
                        triangle.V3.V + (triangle.V1.V - triangle.V3.V) * dt3.GetElement(0)
                    );

                    return (
                        triangle.CreateByNewVertex(triangle.V1, ep1, ep3),
                        triangle.CreateByNewVertex(ep1, triangle.V2, triangle.V3),
                        triangle.CreateByNewVertex(ep1, triangle.V3, ep3)
                    );
                }
            }
            else if (d2 * d3 <= Epsilon)
            {
                var dt2 = RoundCurrentDirection(Avx.Divide(Avx.Subtract(planeD, n.DotProduct(p2)), n.DotProduct(p23)), 10);
                var ep2 = new UVVertex(
                    Avx.Blend((Fma.IsSupported ? Fma.MultiplyAdd(p23, dt2, p2) : Avx.Add(p2, Avx.Multiply(p23, dt2))), One, 0b1000),
                    triangle.V2.U + (triangle.V3.U - triangle.V2.U) * dt2.GetElement(0),
                    triangle.V2.V + (triangle.V3.V - triangle.V2.V) * dt2.GetElement(0)
                );
                var dt3 = RoundCurrentDirection(Avx.Divide(Avx.Subtract(planeD, n.DotProduct(p3)), n.DotProduct(p31)), 10);
                var ep3 = new UVVertex(
                    Avx.Blend((Fma.IsSupported ? Fma.MultiplyAdd(p31, dt3, p3) : Avx.Add(p3, Avx.Multiply(p31, dt3))), One, 0b1000),
                    triangle.V3.U + (triangle.V1.U - triangle.V3.U) * dt3.GetElement(0),
                    triangle.V3.V + (triangle.V1.V - triangle.V3.V) * dt3.GetElement(0)
                );

                return (
                    triangle.CreateByNewVertex(triangle.V1, triangle.V2, ep2),
                    triangle.CreateByNewVertex(triangle.V1, ep2, ep3),
                    triangle.CreateByNewVertex(ep2, triangle.V3, ep3)
                );
            }
            else
            {
                return (triangle, null, null);
            }
        }

        static (LightTriangle, LightTriangle) CreateLightTriangle(int triangleId, NImage texture, float opacity, bool isCastShadow, float lightTransmission, in Vector256<double> sv1, in Vector256<double> sv2, in Vector256<double> sv3, in Vector256<double> sv4, in Matrix4x4d modelMatrix, in Matrix4x4d modelViewMatrix, in Matrix4x4d lightViewMatrix, double offsetX, double offsetY)
        {
            var lmv = modelMatrix * lightViewMatrix;
            var lmvt = lmv * Matrix4x4d.CreateTranslate(offsetX, offsetY, 0.0);
            var lv1 = lmvt.Transform(sv1);
            var lv2 = lmvt.Transform(sv2);
            var lv3 = lmvt.Transform(sv3);
            var lv4 = lmvt.Transform(sv4);

            var luv1 = new UVVertex(lv1, 0.0F, 0.0F);
            var luv2 = new UVVertex(lv2, 0.0F, 1.0F);
            var luv3 = new UVVertex(lv3, 1.0F, 1.0F);
            var luv4 = new UVVertex(lv4, 1.0F, 0.0F);

            Matrix4x4d.Invert(modelViewMatrix, out var invertedLightModelViewMatrix);
            invertedLightModelViewMatrix = Matrix4x4d.Transpose(invertedLightModelViewMatrix);

            var lfarPoint = Avx.And(lmv.Transform(Vector256.Create(0.0, 0.0, -10000.0, 1.0)), Vector256.Create(0xFFFFFFFFFFFFFFFFUL, 0xFFFFFFFFFFFFFFFFUL, 0xFFFFFFFFFFFFFFFFUL, 0).AsDouble());
            return (new LightTriangle(luv1, luv2, luv3, lfarPoint, invertedLightModelViewMatrix, texture, opacity, isCastShadow, lightTransmission, triangleId), new LightTriangle(luv1, luv3, luv4, lfarPoint, invertedLightModelViewMatrix, texture, opacity, isCastShadow, lightTransmission, triangleId));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static Vector256<double> MaxByAbs(in Vector256<double> a, in Vector256<double> b)
        {
            if (Sse2.MoveMask(Sse2.CompareGreaterThanOrEqual(Avx.ExtractVector128(a.Abs(), 0), Avx.ExtractVector128(b.Abs(), 0))) != 0)
            {
                return a;
            }
            else
            {
                return b;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static int MinClampedSize(int a, int max)
        {
            if (float.IsPositiveInfinity(a))
            {
                return max;
            }
            else if (float.IsNegativeInfinity(a))
            {
                return int.MinValue;
            }
            else
            {
                return Math.Min(a, max);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static int MaxClampedSize(int a, int min)
        {
            if (float.IsPositiveInfinity(a))
            {
                return int.MaxValue;
            }
            else if (float.IsNegative(a))
            {
                return min;
            }
            else
            {
                return Math.Max(a, min);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static Vector256<double> RoundCurrentDirection(in Vector256<double> v, int decimals)
        {
            var pow = Vector256.Create(Math.Pow(10.0, decimals));
            return Avx.Divide(Avx.RoundCurrentDirection(Avx.Multiply(v, pow)), pow);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static float CalcFalloff(in Vector3 diff, LightFalloffType type, float falloffStart, float falloffLength)
        {
            var length = diff.Length();
            if (length <= falloffStart)
            {
                return 1.0F;
            }
            length -= falloffStart;
            return type switch
            {
                LightFalloffType.Linear => Math.Max((falloffLength - length) / falloffLength, 0.0F),
                LightFalloffType.Exponential => Math.Min(1.0F / MathF.Pow(1.0F + length, 2.0F), 1.0F),
                _ => 1.0F
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static Vector128<float> CalcBarycentricCoord(in Vector128<float> x, in Vector128<float> y, in Vector128<float> z, in Vector128<float> e)
        {
            return Sse.Shuffle(
                Sse41.Blend(
                    Sse.Multiply(x, e).HorizontalAdd(),
                    Sse.Multiply(y, e).HorizontalAdd(),
                    0b1010
                ),
                Sse41.Blend(
                    Sse.Multiply(z, e).HorizontalAdd(),
                    Vector128.Create(1.0F),
                    0b1010
                ),
                0b01000100
            );
        }
    }

    class Renderer3D : Renderer3DBase
    {
        const int DepthRoundingDigit = 5; // TODO: 要調整

        NManagedImage RenderImage { get; }

        public Renderer3D(NManagedImage renderImage, List<PointLight> pointLights, List<SpotLight> spotLights, List<ParallelLight> parallelLights, List<AmbientLight> ambientLights)
            : base(renderImage.Width, renderImage.Height, pointLights, spotLights, parallelLights, ambientLights)
        {
            RenderImage = renderImage;
        }

        public void Render()
        {
            var renderImageWidth = RenderImage.Width;
            var renderImageHeight = RenderImage.Height;
            var triangles = GetClipAndDividedTriangles(Triangles).ToArray();
            if (triangles.Length < 1)
            {
                return;
            }

            var minZ = triangles.Select(t => Math.Min(Math.Min(t.V1.Vertex.GetElement(2), t.V2.Vertex.GetElement(2)), t.V3.Vertex.GetElement(2))).Min();
            var maxZ = triangles.Select(t => Math.Max(Math.Max(t.V1.Vertex.GetElement(2), t.V2.Vertex.GetElement(2)), t.V3.Vertex.GetElement(2))).Max();
            var projectionMatrix = Matrix4x4d.CreatePerspectiveFieldOfView(FieldOfView, 1.0, minZ, maxZ);

            var offsetX = (Size - RenderImage.Width) * 0.5 / Size;
            var offsetY = (Size - RenderImage.Height) * 0.5 / Size;
            Matrix4x4d.Invert(ViewMatrix, out var invtededViewMatrix);
            Matrix4x4d.Invert(projectionMatrix, out var invertedProjectionMatrix);
            var floatInvtededViewMatrix = (Matrix4x4)(invertedProjectionMatrix * Matrix4x4d.CreateTranslate(-offsetX, -offsetY, 0.0) * invtededViewMatrix);
            var convertedTexture = new Dictionary<NImage, NManagedImage>();
            var convertedTrackMatte = new Dictionary<RasterizedMaskImage, ManagedRasterizedMaskImage>();
            var hasLight = PointLights.Count > 0 || SpotLights.Count > 0 || ParallelLights.Count > 0 || AmbientLights.Count > 0;

            var shadowBuffer = new ShadowBuffer();
            var pointLightShadows = PointLights.Select(l => l.IsEnableShadow ? RenderPointLightShadow(l, shadowBuffer, Size, (float)offsetX, (float)offsetY) : null).ToArray();
            var spotLightShadows = SpotLights.Select(l => l.IsEnableShadow && LightTriangles[l].Count > 0 ? RenderSpotLightShadow(l, shadowBuffer, Size, (float)offsetX, (float)offsetY) : null).ToArray();
            var parallelLightShadows = ParallelLights.Select(l => l.IsEnableShadow && LightTriangles[l].Count > 0 ? RenderParallelLightShadow(l, shadowBuffer, Size, (float)offsetX, (float)offsetY) : null).ToArray();
            var hasShadow = pointLightShadows.Any(ss => ss != null && ss.Any(s => s != null)) || spotLightShadows.Any(s => s != null) || parallelLightShadows.Any(s => s != null);

            foreach (var triangle in triangles)
            {
                var uv1 = triangle.V1.Transform(projectionMatrix);
                var uv2 = triangle.V2.Transform(projectionMatrix);
                var uv3 = triangle.V3.Transform(projectionMatrix);
                var textureWidth = triangle.Texture.Width;
                var textureHeight = triangle.Texture.Height;

                var w1 = 1.0 / Math.Abs(uv1.Vertex.GetElement(3));
                var w2 = 1.0 / Math.Abs(uv2.Vertex.GetElement(3));
                var w3 = 1.0 / Math.Abs(uv3.Vertex.GetElement(3));
                uv1 *= w1;
                uv2 *= w2;
                uv3 *= w3;
                var dvv1 = Avx.Multiply(Avx.Add(uv1.Vertex, Vector256.Create(1.0, 1.0, 0.0, 0.0)), Vector256.Create(Size * 0.5, Size * 0.5, 1.0, 1.0));
                var dvv2 = Avx.Multiply(Avx.Add(uv2.Vertex, Vector256.Create(1.0, 1.0, 0.0, 0.0)), Vector256.Create(Size * 0.5, Size * 0.5, 1.0, 1.0));
                var dvv3 = Avx.Multiply(Avx.Add(uv3.Vertex, Vector256.Create(1.0, 1.0, 0.0, 0.0)), Vector256.Create(Size * 0.5, Size * 0.5, 1.0, 1.0));
                var vvX = Vector128.Create((float)triangle.V1.Vertex.GetElement(0), (float)triangle.V2.Vertex.GetElement(0), (float)triangle.V3.Vertex.GetElement(0), 0.0F);
                var vvY = Vector128.Create((float)triangle.V1.Vertex.GetElement(1), (float)triangle.V2.Vertex.GetElement(1), (float)triangle.V3.Vertex.GetElement(1), 0.0F);
                var vvZ = Vector128.Create((float)triangle.V1.Vertex.GetElement(2), (float)triangle.V2.Vertex.GetElement(2), (float)triangle.V3.Vertex.GetElement(2), 0.0F);
                var svvX = Vector128.Create((float)uv1.Vertex.GetElement(0), (float)uv2.Vertex.GetElement(0), (float)uv3.Vertex.GetElement(0), 0.0F);
                var svvY = Vector128.Create((float)uv1.Vertex.GetElement(1), (float)uv2.Vertex.GetElement(1), (float)uv3.Vertex.GetElement(1), 0.0F);
                var svvZ = Vector128.Create((float)uv1.Vertex.GetElement(2), (float)uv2.Vertex.GetElement(2), (float)uv3.Vertex.GetElement(2), 0.0F);
                var minX = MaxClampedSize((int)(Math.Min(Math.Min(dvv1.GetElement(0), dvv2.GetElement(0)), dvv3.GetElement(0))), OffsetX);
                var maxX = MinClampedSize((int)Math.Ceiling(Math.Max(Math.Max(dvv1.GetElement(0), dvv2.GetElement(0)), dvv3.GetElement(0))), renderImageWidth + OffsetX);
                var minY = MaxClampedSize((int)(Math.Min(Math.Min(dvv1.GetElement(1), dvv2.GetElement(1)), dvv3.GetElement(1))), OffsetY);
                var maxY = MinClampedSize((int)Math.Ceiling(Math.Max(Math.Max(dvv1.GetElement(1), dvv2.GetElement(1)), dvv3.GetElement(1))), renderImageHeight + OffsetY);
                var u = Vector128.Create((float)uv1.U, (float)uv2.U, (float)uv3.U, 0.0F);
                var v = Vector128.Create((float)uv1.V, (float)uv2.V, (float)uv3.V, 0.0F);
                var w = Vector128.Create((float)w1, (float)w2, (float)w3, 0.0F);

                var denom = Vector128.Create((float)(1.0 / (((dvv2.GetElement(0) - dvv1.GetElement(0)) * (dvv3.GetElement(1) - dvv1.GetElement(1))) - ((dvv2.GetElement(1) - dvv1.GetElement(1)) * (dvv3.GetElement(0) - dvv1.GetElement(0))))));
                var edgeX = Sse.Subtract(Vector128.Create((float)dvv3.GetElement(0), (float)dvv1.GetElement(0), (float)dvv2.GetElement(0), 0.0F), Vector128.Create((float)dvv2.GetElement(0), (float)dvv3.GetElement(0), (float)dvv1.GetElement(0), 0.0F));
                var edgeY = Sse.Subtract(Vector128.Create((float)dvv3.GetElement(1), (float)dvv1.GetElement(1), (float)dvv2.GetElement(1), 0.0F), Vector128.Create((float)dvv2.GetElement(1), (float)dvv3.GetElement(1), (float)dvv1.GetElement(1), 0.0F));
                var isFrontFace = triangle.Normal.DotProduct(Avx.Divide(Avx.Add(Avx.Add(triangle.V1.Vertex, triangle.V2.Vertex), triangle.V3.Vertex), Vector256.Create(3.0))).GetElement(0) <= 0.0;
                var vvEX = Vector128.Create((float)dvv2.GetElement(0), (float)dvv3.GetElement(0), (float)dvv1.GetElement(0), 0.0F);
                var vvEY = Vector128.Create((float)dvv2.GetElement(1), (float)dvv3.GetElement(1), (float)dvv1.GetElement(1), 0.0F);
                var useLight = hasLight && (triangle.IsAcceptLight || triangle.IsAcceptShadow);

                NManagedImage managedTexture;
                if (triangle.Texture is NGPUImage gpuImage)
                {
                    if (!convertedTexture.ContainsKey(gpuImage))
                    {
                        convertedTexture.Add(gpuImage, gpuImage.CopyToCpu());
                    }
                    managedTexture = convertedTexture[triangle.Texture];
                }
                else
                {
                    managedTexture = (NManagedImage)triangle.Texture;
                }

                ManagedRasterizedMaskImage? managedTrackMatte;
                if (triangle.TrackMatte is GPURasterizedMaskImage gpuRasterizedMask)
                {
                    if (!convertedTrackMatte.ContainsKey(gpuRasterizedMask))
                    {
                        convertedTrackMatte.Add(gpuRasterizedMask, gpuRasterizedMask.CopyToCpu());
                    }
                    managedTrackMatte = convertedTrackMatte[gpuRasterizedMask];
                }
                else
                {
                    managedTrackMatte = (ManagedRasterizedMaskImage?)triangle.TrackMatte;
                }

                Parallel.For(minY, maxY, y =>
                {
                    var renderImageSpan = RenderImage.GetDataSpan();
                    var trackMatteSpan = (managedTrackMatte?.Data ?? EmptyTrackMatte).AsSpan();
                    var texture = managedTexture.GetDataSpan();
                    var eY = Sse.Multiply(edgeX, Sse.Subtract(Vector128.Create(y, y, y, 0.0F), vvEY));

                    var offset = (y - OffsetY) * renderImageWidth;
                    var p = offset + (minX - OffsetX);

                    var pointLights = CollectionsMarshal.AsSpan(PointLights);
                    var spotLights = CollectionsMarshal.AsSpan(SpotLights);
                    var parallelLights = CollectionsMarshal.AsSpan(ParallelLights);
                    var ambientLights = CollectionsMarshal.AsSpan(AmbientLights);

                    for (int x = minX; x < maxX; x++, p++)
                    {
                        var eX = Sse.Subtract(Vector128.Create(x, x, x, 0.0F), vvEX);
                        var e = Sse.Multiply(Fma.IsSupported ? Fma.MultiplyAddNegated(edgeY, eX, eY) : Sse.Subtract(eY, Sse.Multiply(edgeY, eX)), denom);

                        var ae = Sse.And(e, Sse.CompareGreaterThanOrEqual(e.Abs(), Vector128.Create(Epsilon)));
                        if (!Avx.TestZ(Sse.CompareLessThan(ae, Vector128<float>.Zero), Vector128.Create(float.NaN)))
                        {
                            continue;
                        }

                        var tw = Sse.Multiply(w, e).HorizontalAdd();
                        var tx = Sse.Divide(Sse.Multiply(u, e), tw).HorizontalAdd().GetElement(0) * textureWidth;
                        var ty = Sse.Divide(Sse.Multiply(v, e), tw).HorizontalAdd().GetElement(0) * textureHeight;

                        var color = ImageInterpolation.Bilinear(texture, textureWidth, textureHeight, tx, ty);
                        color.W *= triangle.Opacity * trackMatteSpan[p % trackMatteSpan.Length];
                        if (color.W <= 0.0F)
                        {
                            continue;
                        }

                        if (useLight)
                        {
                            var alpha = color.W;
                            var position = CalcBarycentricCoord(vvX, vvY, vvZ, e);
                            var n = isFrontFace ? -triangle.FloatNormal : triangle.FloatNormal;
                            var shadowProjectionPos = Vector4.Zero;
                            if (hasShadow)
                            {
                                shadowProjectionPos = CalcBarycentricCoord(svvX, svvY, svvZ, e).AsVector4();
                            }

                            if (hasShadow && !triangle.IsAcceptLight && triangle.IsAcceptShadow)
                            {
                                for (var i = 0; i < PointLights.Count; i++)
                                {
                                    var l = pointLights[i];
                                    var shadows = pointLightShadows[i];
                                    if (shadows == null)
                                    {
                                        continue;
                                    }

                                    var face = PointLightShadowDirection.Front;
                                    var faceDir = Vector4.Transform(Vector4.Transform(shadowProjectionPos, floatInvtededViewMatrix), l.FaceDetectionMatrix);
                                    var absDir = Vector4.Abs(faceDir);
                                    if (absDir.Z >= absDir.X && absDir.Z >= absDir.Y)
                                    {
                                        face = faceDir.Z < 0.0F ? PointLightShadowDirection.Back : PointLightShadowDirection.Front;
                                    }
                                    else if (absDir.Y >= absDir.X)
                                    {
                                        face = faceDir.Y < 0.0F ? PointLightShadowDirection.Top : PointLightShadowDirection.Bottom;
                                    }
                                    else
                                    {
                                        face = faceDir.X < 0.0F ? PointLightShadowDirection.Right : PointLightShadowDirection.Left;
                                    }

                                    var shadow = shadows[(int)face];
                                    if (shadow != null)
                                    {
                                        var transmissionColor = GetShadowColor(triangle.Id, shadow, shadowBuffer, l.ShadowScatterSize, shadowProjectionPos, floatInvtededViewMatrix, shadow.LightViewProjectionMatrix);
                                        color *= transmissionColor;
                                    }
                                }

                                for (var i = 0; i < SpotLights.Count; i++)
                                {
                                    var l = spotLights[i];
                                    var lightDiff = Sse.Subtract(position, l.Position).AsVector3();
                                    var light = Vector3.Normalize(lightDiff);
                                    var spotCone = MathF.Acos(Vector3.Dot(l.Direction, light));

                                    if (spotCone <= l.OuterCone)
                                    {
                                        var shadow = spotLightShadows[i];
                                        if (shadow == null)
                                        {
                                            continue;
                                        }

                                        var attenuation = 1.0F;
                                        if (l.ConeAttenuationRate > 0.0)
                                        {
                                            attenuation = MathF.Cos((1.0F - Math.Min((MathF.Cos(spotCone) - l.OuterConeCos) * l.InvertInnerConeCos, 1.0F)) * MathF.PI * 0.5F);
                                        }
                                        var transmissionColor = GetShadowColor(triangle.Id, shadow, shadowBuffer, l.ShadowScatterSize, shadowProjectionPos, floatInvtededViewMatrix, shadow.LightViewProjectionMatrix);
                                        color *= Vector4.Lerp(Vector4.One, transmissionColor, attenuation);
                                    }
                                }

                                for (var i = 0; i < ParallelLights.Count; i++)
                                {
                                    var l = parallelLights[i];
                                    var shadow = parallelLightShadows[i];
                                    if (shadow == null)
                                    {
                                        continue;
                                    }

                                    var transmissionColor = GetShadowColor(triangle.Id, shadow, shadowBuffer, l.ShadowScatterSize, shadowProjectionPos, floatInvtededViewMatrix, shadow.LightViewProjectionMatrix);
                                    color *= transmissionColor;
                                }

                                color.W = alpha;
                            }
                            else if (triangle.IsAcceptLight)
                            {
                                var diffuse = Vector4.Zero;
                                var specular = Vector4.Zero;
                                var ambient = Vector4.Zero;

                                for (var i = 0; i < PointLights.Count; i++)
                                {
                                    var l = pointLights[i];
                                    var lightColor = l.Color;
                                    var lightDiff = Sse.Subtract(position, l.Position).AsVector3();
                                    var light = Vector3.Normalize(lightDiff);
                                    var falloff = CalcFalloff(lightDiff, l.FalloffType, l.FalloffStart, l.FalloffLength);
                                    var shadows = pointLightShadows[i];
                                    if (triangle.IsAcceptShadow && shadows != null)
                                    {
                                        var face = PointLightShadowDirection.Front;
                                        var faceDir = Vector4.Transform(Vector4.Transform(shadowProjectionPos, floatInvtededViewMatrix), l.FaceDetectionMatrix);
                                        var absDir = Vector4.Abs(faceDir);
                                        if (absDir.Z >= absDir.X && absDir.Z >= absDir.Y)
                                        {
                                            face = faceDir.Z < 0.0F ? PointLightShadowDirection.Back : PointLightShadowDirection.Front;
                                        }
                                        else if (absDir.Y >= absDir.X)
                                        {
                                            face = faceDir.Y < 0.0F ? PointLightShadowDirection.Top : PointLightShadowDirection.Bottom;
                                        }
                                        else
                                        {
                                            face = faceDir.X < 0.0F ? PointLightShadowDirection.Right : PointLightShadowDirection.Left;
                                        }

                                        var shadow = shadows[(int)face];
                                        if (shadow != null)
                                        {
                                            var transmissionColor = GetShadowColor(triangle.Id, shadow, shadowBuffer, l.ShadowScatterSize, shadowProjectionPos, floatInvtededViewMatrix, shadow.LightViewProjectionMatrix);
                                            if (!lightColor.CompareGreaterThanBy3Element(Vector3.Zero))
                                            {
                                                continue;
                                            }
                                            lightColor *= transmissionColor;
                                        }
                                    }

                                    var diffuseFactor = Vector3.Dot(light, n);
                                    var isBack = diffuseFactor < 0.0F;
                                    if (isBack)
                                    {
                                        diffuseFactor *= -triangle.LightTransmission;
                                    }
                                    diffuse += lightColor * color * diffuseFactor * falloff;

                                    var view = -Vector3.Normalize(position.AsVector3());
                                    var halfLE = Vector3.Normalize(view - light);
                                    var specularFactor = Math.Max(Vector3.Dot(-n, halfLE), 0.0F);
                                    if (isBack)
                                    {
                                        specularFactor *= -triangle.LightTransmission;
                                    }
                                    specular += Vector4.Lerp(lightColor, color * lightColor, triangle.Metal) * MathF.Pow(specularFactor, ShininessStrength * triangle.SpecularShininess) * triangle.SpecularIntensity * falloff;
                                }

                                for (var i = 0; i < SpotLights.Count; i++)
                                {
                                    var l = spotLights[i];
                                    var lightColor = l.Color;
                                    var lightDiff = Sse.Subtract(position, l.Position).AsVector3();
                                    var light = Vector3.Normalize(lightDiff);
                                    var spotCone = MathF.Acos(Vector3.Dot(l.Direction, light));

                                    if (spotCone <= l.OuterCone)
                                    {
                                        var shadow = spotLightShadows[i];
                                        if (triangle.IsAcceptShadow && shadow != null)
                                        {
                                            var transmissionColor = GetShadowColor(triangle.Id, shadow, shadowBuffer, l.ShadowScatterSize, shadowProjectionPos, floatInvtededViewMatrix, shadow.LightViewProjectionMatrix);
                                            if (!transmissionColor.CompareGreaterThanBy3Element(Vector3.Zero))
                                            {
                                                continue;
                                            }
                                            lightColor *= transmissionColor;
                                        }

                                        var attenuation = 1.0F;
                                        if (l.ConeAttenuationRate > 0.0)
                                        {
                                            attenuation = MathF.Cos((1.0F - Math.Min((MathF.Cos(spotCone) - l.OuterConeCos) * l.InvertInnerConeCos, 1.0F)) * MathF.PI * 0.5F);
                                        }

                                        var falloff = CalcFalloff(lightDiff, l.FalloffType, l.FalloffStart, l.FalloffLength);
                                        var diffuseFactor = Vector3.Dot(light, n);
                                        var isBack = diffuseFactor < 0.0F;
                                        if (isBack)
                                        {
                                            diffuseFactor *= -triangle.LightTransmission;
                                        }
                                        diffuse += lightColor * color * diffuseFactor * falloff * attenuation;

                                        var view = -Vector3.Normalize(position.AsVector3());
                                        var halfLE = Vector3.Normalize(view - light);
                                        var specularFactor = Math.Max(Vector3.Dot(-n, halfLE), 0.0F);
                                        if (isBack)
                                        {
                                            specularFactor *= -triangle.LightTransmission;
                                        }
                                        specular += Vector4.Lerp(lightColor, color * lightColor, triangle.Metal) * MathF.Pow(specularFactor, ShininessStrength * triangle.SpecularShininess) * triangle.SpecularIntensity * falloff * attenuation;
                                    }
                                }

                                for (var i = 0; i < ParallelLights.Count; i++)
                                {
                                    var l = parallelLights[i];
                                    var lightColor = l.Color;
                                    var lightDiff = Sse.Subtract(position, l.Position).AsVector3();
                                    var falloff = CalcFalloff(lightDiff, l.FalloffType, l.FalloffStart, l.FalloffLength);

                                    var shadow = parallelLightShadows[i];
                                    if (triangle.IsAcceptShadow && shadow != null)
                                    {
                                        var transmissionColor = GetShadowColor(triangle.Id, shadow, shadowBuffer, l.ShadowScatterSize, shadowProjectionPos, floatInvtededViewMatrix, shadow.LightViewProjectionMatrix);
                                        if (!transmissionColor.CompareGreaterThanBy3Element(Vector3.Zero))
                                        {
                                            continue;
                                        }
                                        lightColor *= transmissionColor;
                                    }

                                    var diffuseFactor = Vector3.Dot(l.Direction, n);
                                    var isBack = diffuseFactor < 0.0F;
                                    if (isBack)
                                    {
                                        diffuseFactor *= -triangle.LightTransmission;
                                    }
                                    diffuse += lightColor * color * diffuseFactor * falloff;

                                    var view = -Vector3.Normalize(position.AsVector3());
                                    var halfLE = Vector3.Normalize(view - l.Direction);
                                    var specularFactor = Math.Max(Vector3.Dot(-n, halfLE), 0.0F);
                                    if (isBack)
                                    {
                                        specularFactor *= -triangle.LightTransmission;
                                    }
                                    specular += Vector4.Lerp(lightColor, color * lightColor, triangle.Metal) * MathF.Pow(specularFactor, ShininessStrength * triangle.SpecularShininess) * triangle.SpecularIntensity * falloff;
                                }

                                for (var i = 0; i < AmbientLights.Count; i++)
                                {
                                    ambient += ambientLights[i].Color * color;
                                }

                                color = diffuse * triangle.Diffuse + specular + ambient * triangle.Ambient;
                                color.W = alpha;
                                color = Vector4.Max(Vector4.Min(color, Vector4.One), Vector4.Zero);
                            }
                        }

                        renderImageSpan[p] = Blend.Process(triangle.BlendMode, renderImageSpan[p], color);
                    }
                });
            }

            foreach (var (_, i) in convertedTexture)
            {
                i.Dispose();
            }
            foreach (var (_, i) in convertedTrackMatte)
            {
                i.Dispose();
            }

            foreach (var ss in pointLightShadows)
            {
                if (ss != null)
                {
                    foreach (var s in ss)
                    {
                        s?.Dispose();
                    }
                }
            }
            foreach (var s in spotLightShadows)
            {
                s?.Dispose();
            }
            foreach (var s in parallelLightShadows)
            {
                s?.Dispose();
            }
        }

        ShadowMap? RenderSpotLightShadow(SpotLight spotLight, ShadowBuffer shadowBuffer, int size, float offsetX, float offsetY)
        {
            var triangles = GetClipAndDividedTriangles(LightTriangles[spotLight]).ToArray();
            if (triangles.Length < 1 || triangles.All(t => !t.IsCastShadow))
            {
                return null;
            }

            var minZ = triangles.Select(t => Math.Min(Math.Min(t.V1.Vertex.GetElement(2), t.V2.Vertex.GetElement(2)), t.V3.Vertex.GetElement(2))).Min();
            var maxZ = triangles.Select(t => Math.Max(Math.Max(t.V1.Vertex.GetElement(2), t.V2.Vertex.GetElement(2)), t.V3.Vertex.GetElement(2))).Max();
            var lightProjectionMatrix = Matrix4x4d.CreatePerspectiveFieldOfView(spotLight.ConeRadian, 1.0, minZ, maxZ);

            return RenderShadow(shadowBuffer, size, offsetX, offsetY, triangles, spotLight.ShadowStrength, spotLight.FloatLightViewMatrix, lightProjectionMatrix);
        }

        ShadowMap? RenderParallelLightShadow(ParallelLight parallelLight, ShadowBuffer shadowBuffer, int size, float offsetX, float offsetY)
        {
            var triangles = GetClipAndDividedTriangles(LightTriangles[parallelLight]).ToArray();
            if (triangles.Length < 1 || triangles.All(t => !t.IsCastShadow))
            {
                return null;
            }

            var min = triangles.Select(t => Avx.Min(Avx.Min(t.V1.Vertex, t.V2.Vertex), t.V3.Vertex)).Aggregate(Avx.Min);
            var max = triangles.Select(t => Avx.Max(Avx.Max(t.V1.Vertex, t.V2.Vertex), t.V3.Vertex)).Aggregate(Avx.Max);
            if (min.GetElement(0) == max.GetElement(0) || min.GetElement(1) == max.GetElement(1))
            {
                return null;
            }

            var lightProjectionMatrix = Matrix4x4d.CreateOrthographic(min.GetElement(0), max.GetElement(0), min.GetElement(1), max.GetElement(1), min.GetElement(2), max.GetElement(2));

            return RenderShadow(shadowBuffer, size, offsetX, offsetY, triangles, parallelLight.ShadowStrength, parallelLight.FloatLightViewMatrix, lightProjectionMatrix);
        }

        ShadowMap?[] RenderPointLightShadow(PointLight pointLight, ShadowBuffer shadowBuffer, int size, float offsetX, float offsetY)
        {
            var result = new ShadowMap?[6];
            var lv = new Matrix4x4[]
            {
                pointLight.FloatFrontLightViewMatrix,
                pointLight.FloatBackLightViewMatrix,
                pointLight.FloatLeftLightViewMatrix,
                pointLight.FloatRightLightViewMatrix,
                pointLight.FloatTopLightViewMatrix,
                pointLight.FloatBottomLightViewMatrix
            };

            foreach (var (i, holder) in PointLightHolder.Directions.Select((d, i) => (i, new PointLightHolder(pointLight, d))))
            {
                var triangles = GetClipAndDividedTriangles(LightTriangles[holder]).ToArray();
                if (triangles.Length < 1 || triangles.All(t => !t.IsCastShadow))
                {
                    continue;
                }

                var minZ = triangles.Select(t => Math.Min(Math.Min(t.V1.Vertex.GetElement(2), t.V2.Vertex.GetElement(2)), t.V3.Vertex.GetElement(2))).Min();
                var maxZ = triangles.Select(t => Math.Max(Math.Max(t.V1.Vertex.GetElement(2), t.V2.Vertex.GetElement(2)), t.V3.Vertex.GetElement(2))).Max();
                var lightProjectionMatrix = Matrix4x4d.CreatePerspectiveFieldOfView(Math.PI * 0.5, 1.0, minZ, maxZ);

                result[i] = RenderShadow(shadowBuffer, size, offsetX, offsetY, triangles, pointLight.ShadowStrength, lv[i], lightProjectionMatrix);
            }

            return result;
        }

        static ShadowMap RenderShadow(ShadowBuffer shadowBuffer, int size, float offsetX, float offsetY, LightTriangle[] dividedLightTriangles, float shadowStrength, in Matrix4x4 lightViewMatrix, in Matrix4x4d lightProjectionMatrix)
        {
            var convertedTexture = new Dictionary<NImage, NManagedImage>();

            var shadowMap = new ShadowMap(shadowBuffer, size, lightViewMatrix * Matrix4x4.CreateTranslation(offsetX, offsetY, 0.0F) * (Matrix4x4)lightProjectionMatrix);

            foreach (var triangle in dividedLightTriangles)
            {
                if (!triangle.IsCastShadow)
                {
                    continue;
                }

                var uv1 = triangle.V1.Transform(lightProjectionMatrix);
                var uv2 = triangle.V2.Transform(lightProjectionMatrix);
                var uv3 = triangle.V3.Transform(lightProjectionMatrix);
                var textureWidth = triangle.Texture.Width;
                var textureHeight = triangle.Texture.Height;

                var w1 = 1.0 / Math.Abs(uv1.Vertex.GetElement(3));
                var w2 = 1.0 / Math.Abs(uv2.Vertex.GetElement(3));
                var w3 = 1.0 / Math.Abs(uv3.Vertex.GetElement(3));
                uv1 *= w1;
                uv2 *= w2;
                uv3 *= w3;
                var dvv1 = Avx.Multiply(Avx.Add(uv1.Vertex, Vector256.Create(1.0, 1.0, 0.0, 0.0)), Vector256.Create(size * 0.5, size * 0.5, 1.0, 1.0));
                var dvv2 = Avx.Multiply(Avx.Add(uv2.Vertex, Vector256.Create(1.0, 1.0, 0.0, 0.0)), Vector256.Create(size * 0.5, size * 0.5, 1.0, 1.0));
                var dvv3 = Avx.Multiply(Avx.Add(uv3.Vertex, Vector256.Create(1.0, 1.0, 0.0, 0.0)), Vector256.Create(size * 0.5, size * 0.5, 1.0, 1.0));
                var vvX = Vector128.Create((float)uv1.Vertex.GetElement(0), (float)uv2.Vertex.GetElement(0), (float)uv3.Vertex.GetElement(0), 0.0F);
                var vvY = Vector128.Create((float)uv1.Vertex.GetElement(1), (float)uv2.Vertex.GetElement(1), (float)uv3.Vertex.GetElement(1), 0.0F);
                var vvZ = Vector128.Create((float)uv1.Vertex.GetElement(2), (float)uv2.Vertex.GetElement(2), (float)uv3.Vertex.GetElement(2), 0.0F);
                var minX = MaxClampedSize((int)(Math.Min(Math.Min(dvv1.GetElement(0), dvv2.GetElement(0)), dvv3.GetElement(0))), 0);
                var maxX = MinClampedSize((int)Math.Ceiling(Math.Max(Math.Max(dvv1.GetElement(0), dvv2.GetElement(0)), dvv3.GetElement(0))), size);
                var minY = MaxClampedSize((int)(Math.Min(Math.Min(dvv1.GetElement(1), dvv2.GetElement(1)), dvv3.GetElement(1))), 0);
                var maxY = MinClampedSize((int)Math.Ceiling(Math.Max(Math.Max(dvv1.GetElement(1), dvv2.GetElement(1)), dvv3.GetElement(1))), size);
                var u = Vector128.Create((float)uv1.U, (float)uv2.U, (float)uv3.U, 0.0F);
                var v = Vector128.Create((float)uv1.V, (float)uv2.V, (float)uv3.V, 0.0F);
                var w = Vector128.Create((float)w1, (float)w2, (float)w3, 0.0F);

                var denom = Vector128.Create((float)(1.0 / (((dvv2.GetElement(0) - dvv1.GetElement(0)) * (dvv3.GetElement(1) - dvv1.GetElement(1))) - ((dvv2.GetElement(1) - dvv1.GetElement(1)) * (dvv3.GetElement(0) - dvv1.GetElement(0))))));
                var edgeX = Sse.Subtract(Vector128.Create((float)dvv3.GetElement(0), (float)dvv1.GetElement(0), (float)dvv2.GetElement(0), 0.0F), Vector128.Create((float)dvv2.GetElement(0), (float)dvv3.GetElement(0), (float)dvv1.GetElement(0), 0.0F));
                var edgeY = Sse.Subtract(Vector128.Create((float)dvv3.GetElement(1), (float)dvv1.GetElement(1), (float)dvv2.GetElement(1), 0.0F), Vector128.Create((float)dvv2.GetElement(1), (float)dvv3.GetElement(1), (float)dvv1.GetElement(1), 0.0F));
                var vvEX = Vector128.Create((float)dvv2.GetElement(0), (float)dvv3.GetElement(0), (float)dvv1.GetElement(0), 0.0F);
                var vvEY = Vector128.Create((float)dvv2.GetElement(1), (float)dvv3.GetElement(1), (float)dvv1.GetElement(1), 0.0F);

                NManagedImage managedTexture;
                if (triangle.Texture is NGPUImage gpuImage)
                {
                    if (!convertedTexture.ContainsKey(gpuImage))
                    {
                        convertedTexture.Add(gpuImage, gpuImage.CopyToCpu());
                    }
                    managedTexture = convertedTexture[triangle.Texture];
                }
                else
                {
                    managedTexture = (NManagedImage)triangle.Texture;
                }

                shadowMap.AllocBuffer();
                Parallel.For(minY, maxY, y =>
                {
                    var texture = managedTexture.GetDataSpan();
                    var eY = Sse.Multiply(edgeX, Sse.Subtract(Vector128.Create(y, y, y, 0.0F), vvEY));
                    var offset = y * size;
                    var indicesSpan = shadowMap.Indices.AsSpan(offset, size);
                    var bufferIndicesSpan = shadowMap.BufferIndices.AsSpan(offset, size);
                    for (int x = minX; x < maxX; x++)
                    {
                        var eX = Sse.Subtract(Vector128.Create(x, x, x, 0.0F), vvEX);
                        var e = Sse.Multiply(Fma.IsSupported ? Fma.MultiplyAddNegated(edgeY, eX, eY) : Sse.Subtract(eY, Sse.Multiply(edgeY, eX)), denom);
                        var ae = Sse.And(e, Sse.CompareGreaterThanOrEqual(e.Abs(), Vector128.Create(Epsilon)));
                        if (!Avx.TestZ(Sse.CompareLessThan(ae, Vector128<float>.Zero), Vector128.Create(float.NaN)))
                        {
                            continue;
                        }

                        var tw = Sse.Multiply(w, e).HorizontalAdd();
                        var tx = Sse.Divide(Sse.Multiply(u, e), tw).HorizontalAdd().GetElement(0) * textureWidth;
                        var ty = Sse.Divide(Sse.Multiply(v, e), tw).HorizontalAdd().GetElement(0) * textureHeight;

                        var color = ImageInterpolation.Bilinear(texture, textureWidth, textureHeight, tx, ty);

                        // α == 0 もしくはライト透過100%の白
                        if (color.W <= 0.0F || (triangle.LightTransmission >= 1.0F && color.X >= 1.0F && color.Y >= 1.0F && color.Z >= 1.0F))
                        {
                            continue;
                        }

                        var d = CalcBarycentricCoord(vvX, vvY, vvZ, e).AsVector4();

                        var shadowColor = Vector4.One - Vector4.Lerp(Vector4.One, Vector4.Lerp(Vector4.UnitW, Vector4.Clamp(color, Vector4.Zero, Vector4.One), triangle.LightTransmission), Math.Min(color.W, 1.0F) * triangle.Opacity);
                        shadowColor = Vector4.One - Vector4.Clamp(shadowColor * shadowStrength, Vector4.Zero, Vector4.One);
                        shadowColor.W = 1.0F;
                        var (bufferIndex, index) = shadowBuffer.GetEmptyIndex();
                        shadowBuffer.Buffers[bufferIndex][index] = new ShadowPixel(shadowColor, Math.Clamp(MathF.Round(d.Z, DepthRoundingDigit), 0.0F, 1.0F), triangle.Id, indicesSpan[x], bufferIndicesSpan[x]);
                        indicesSpan[x] = index;
                        bufferIndicesSpan[x] = bufferIndex;
                    }
                });
            }

            foreach (var (_, i) in convertedTexture)
            {
                i.Dispose();
            }

            return shadowMap;
        }

        static Vector4 GetShadowColor(int triangleId, ShadowMap shadowMap, ShadowBuffer shadowBuffer, float shadowScatterSize, in Vector4 shadowProjectionPos, in Matrix4x4 invtededViewMatrix, in Matrix4x4 lightViewProjectionMatrix)
        {
            var shadowPos = Vector4.Transform(Vector4.Transform(shadowProjectionPos, invtededViewMatrix), lightViewProjectionMatrix);
            shadowPos /= shadowPos.W;
            var shadowTexPos = shadowPos * 0.5F + new Vector4(0.5F, 0.5F, 0.0F, 0.0F);
            var depth = MathF.Round(shadowPos.Z, DepthRoundingDigit);
            var size = shadowMap.ShadowMapSize;

            var shadowTextureX = (int)(shadowTexPos.X * size);
            var shadowTextureY = (int)(shadowTexPos.Y * size);

            // TODO: 重くならないならfor文の方とまとめる
            if (shadowScatterSize <= 0.0F)
            {
                if (shadowTextureX < 0 || shadowTextureX >= size || shadowTextureY < 0 || shadowTextureY >= size)
                {
                    return Vector4.One;
                }
                else
                {
                    var tc = Vector4.One;
                    var si = shadowTextureY * size + shadowTextureX;
                    var index = shadowMap.Indices[si];
                    var bufferIndex = shadowMap.BufferIndices[si];
                    while (index >= 0 && tc.CompareGreaterThanBy3Element(Vector3.Zero))
                    {
                        var sp = shadowBuffer.Buffers[bufferIndex][index];
                        if (sp.TriangleId == triangleId || depth < sp.Depth)
                        {
                            break;
                        }

                        tc *= sp.Color;
                        index = sp.NextIndex;
                        bufferIndex = sp.NextBuffer;
                    }
                    return tc;
                }
            }
            else
            {
                var transmissionColor = Vector4.Zero;
                // TODO: ちゃんと距離に応じてぼけるようにする
                //        Deep Shadow Mapsと相性が悪いのであれば他のShadow Mappingアルゴリズムに切り替えることも検討する
                var samplingRange = (int)MathF.Ceiling(shadowScatterSize) * 2 + 1;
                var edgeRate = shadowScatterSize % 1.0F;
                if (edgeRate <= 0.0F)
                {
                    edgeRate = 1.0F;
                }
                for (int stsy = shadowTextureY - samplingRange / 2, cy = 0; cy < samplingRange; stsy++, cy++)
                {
                    var yRate = (cy == 0 || cy == samplingRange - 1 ? edgeRate : 1.0F);
                    if (stsy < 0 || stsy >= size)
                    {
                        transmissionColor += Vector4.One * ((samplingRange - 2) + edgeRate * 2.0F) * yRate;
                        continue;
                    }
                    for (int stsx = shadowTextureX - samplingRange / 2, cx = 0; cx < samplingRange; stsx++, cx++)
                    {
                        var rate = (cx == 0 || cx == samplingRange - 1 ? edgeRate : 1.0F) * yRate;
                        if (stsx < 0 || stsx >= size)
                        {
                            transmissionColor += Vector4.One * rate;
                            continue;
                        }

                        var tc = Vector4.One;
                        var si = stsy * size + stsx;
                        var index = shadowMap.Indices[si];
                        var bufferIndex = shadowMap.BufferIndices[si];
                        while (index >= 0 && tc.CompareGreaterThanBy3Element(Vector3.Zero))
                        {
                            var sp = shadowBuffer.Buffers[bufferIndex][index];
                            if (sp.TriangleId == triangleId || depth < sp.Depth)
                            {
                                break;
                            }

                            tc *= sp.Color;
                            index = sp.NextIndex;
                            bufferIndex = sp.NextBuffer;
                        }
                        transmissionColor += tc * rate;
                    }
                }

                return transmissionColor / ((shadowScatterSize * 2.0F + 1.0F) * (shadowScatterSize * 2.0F + 1.0F));
            }
        }

        #region Debug functions
#if DEBUG
#pragma warning disable IDE0051 // 使用されていないプライベート メンバーを削除する
        void DisplayShadowMapForDebug(ShadowMap? shadowMap, ShadowBuffer shadowBuffer)
#pragma warning restore IDE0051 // 使用されていないプライベート メンバーを削除する
        {
            if (shadowMap == null)
            {
                return;
            }

            var size = shadowMap.ShadowMapSize / 4;
            var image = RenderImage.GetDataSpan();
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var color = Vector4.Zero;
                    for (int sy = y * 4, cy = 0; cy < 4; sy++, cy++)
                    {
                        for (int sx = x * 4, cx = 0; cx < 4; sx++, cx++)
                        {
                            var spi = sy * shadowMap.ShadowMapSize + sx;
                            if (shadowMap.Indices[spi] < 0)
                            {
                                color += Vector4.One;
                                continue;
                            }

                            var sp = shadowBuffer.Buffers[shadowMap.BufferIndices[spi]][shadowMap.Indices[spi]];
                            color += sp.Color;
                        }
                    }
                    image[y * RenderImage.Width + x] = color / 16;
                }
            }
        }
#endif
        #endregion
    }

    class MaskRenderer3D : Renderer3DBase
    {
        static readonly Vector4 ToGrayScale = new Vector4(0.114478F, 0.586611F, 0.298912F, 0.0F);

        ManagedRasterizedMaskImage RenderImage { get; }

        public MaskRenderer3D(ManagedRasterizedMaskImage renderImage, List<PointLight> pointLights, List<SpotLight> spotLights, List<ParallelLight> parallelLights, List<AmbientLight> ambientLights)
            : base(renderImage.Width, renderImage.Height, pointLights, spotLights, parallelLights, ambientLights)
        {
            RenderImage = renderImage;
        }

        public void Render(TrackMatteMode trackMatteMode)
        {
            if (trackMatteMode == TrackMatteMode.InvertAlpha || trackMatteMode == TrackMatteMode.InvertLuminance)
            {
                RenderImage.GetDataSpan().Fill(1.0F);
            }

            var renderImageWidth = RenderImage.Width;
            var renderImageHeight = RenderImage.Height;
            var triangles = GetClipAndDividedTriangles(Triangles).ToArray();
            if (triangles.Length < 1)
            {
                return;
            }

            var minZ = triangles.Select(t => Math.Min(Math.Min(t.V1.Vertex.GetElement(2), t.V2.Vertex.GetElement(2)), t.V3.Vertex.GetElement(2))).Min();
            var maxZ = triangles.Select(t => Math.Max(Math.Max(t.V1.Vertex.GetElement(2), t.V2.Vertex.GetElement(2)), t.V3.Vertex.GetElement(2))).Max();
            var projectionMatrix = Matrix4x4d.CreatePerspectiveFieldOfView(FieldOfView, 1.0, minZ, maxZ);

            var offsetX = (Size - RenderImage.Width) * 0.5 / Size;
            var offsetY = (Size - RenderImage.Height) * 0.5 / Size;
            Matrix4x4d.Invert(ViewMatrix, out var invtededViewMatrix);
            Matrix4x4d.Invert(projectionMatrix, out var invertedProjectionMatrix);
            var floatInvtededViewMatrix = (Matrix4x4)(invertedProjectionMatrix * Matrix4x4d.CreateTranslate(-offsetX, -offsetY, 0.0) * invtededViewMatrix);
            var convertedTexture = new Dictionary<NImage, NManagedImage>();
            var convertedTrackMatte = new Dictionary<RasterizedMaskImage, ManagedRasterizedMaskImage>();
            var hasLight = PointLights.Count > 0 || SpotLights.Count > 0 || ParallelLights.Count > 0 || AmbientLights.Count > 0;

            foreach (var triangle in triangles)
            {
                var uv1 = triangle.V1.Transform(projectionMatrix);
                var uv2 = triangle.V2.Transform(projectionMatrix);
                var uv3 = triangle.V3.Transform(projectionMatrix);
                var textureWidth = triangle.Texture.Width;
                var textureHeight = triangle.Texture.Height;

                var w1 = 1.0 / Math.Abs(uv1.Vertex.GetElement(3));
                var w2 = 1.0 / Math.Abs(uv2.Vertex.GetElement(3));
                var w3 = 1.0 / Math.Abs(uv3.Vertex.GetElement(3));
                uv1 *= w1;
                uv2 *= w2;
                uv3 *= w3;
                var dvv1 = Avx.Multiply(Avx.Add(uv1.Vertex, Vector256.Create(1.0, 1.0, 0.0, 0.0)), Vector256.Create(Size * 0.5, Size * 0.5, 1.0, 1.0));
                var dvv2 = Avx.Multiply(Avx.Add(uv2.Vertex, Vector256.Create(1.0, 1.0, 0.0, 0.0)), Vector256.Create(Size * 0.5, Size * 0.5, 1.0, 1.0));
                var dvv3 = Avx.Multiply(Avx.Add(uv3.Vertex, Vector256.Create(1.0, 1.0, 0.0, 0.0)), Vector256.Create(Size * 0.5, Size * 0.5, 1.0, 1.0));
                var vvX = Vector128.Create((float)triangle.V1.Vertex.GetElement(0), (float)triangle.V2.Vertex.GetElement(0), (float)triangle.V3.Vertex.GetElement(0), 0.0F);
                var vvY = Vector128.Create((float)triangle.V1.Vertex.GetElement(1), (float)triangle.V2.Vertex.GetElement(1), (float)triangle.V3.Vertex.GetElement(1), 0.0F);
                var vvZ = Vector128.Create((float)triangle.V1.Vertex.GetElement(2), (float)triangle.V2.Vertex.GetElement(2), (float)triangle.V3.Vertex.GetElement(2), 0.0F);
                var svvX = Vector128.Create((float)uv1.Vertex.GetElement(0), (float)uv2.Vertex.GetElement(0), (float)uv3.Vertex.GetElement(0), 0.0F);
                var svvY = Vector128.Create((float)uv1.Vertex.GetElement(1), (float)uv2.Vertex.GetElement(1), (float)uv3.Vertex.GetElement(1), 0.0F);
                var svvZ = Vector128.Create((float)uv1.Vertex.GetElement(2), (float)uv2.Vertex.GetElement(2), (float)uv3.Vertex.GetElement(2), 0.0F);
                var minX = MaxClampedSize((int)(Math.Min(Math.Min(dvv1.GetElement(0), dvv2.GetElement(0)), dvv3.GetElement(0))), OffsetX);
                var maxX = MinClampedSize((int)Math.Ceiling(Math.Max(Math.Max(dvv1.GetElement(0), dvv2.GetElement(0)), dvv3.GetElement(0))), renderImageWidth + OffsetX);
                var minY = MaxClampedSize((int)(Math.Min(Math.Min(dvv1.GetElement(1), dvv2.GetElement(1)), dvv3.GetElement(1))), OffsetY);
                var maxY = MinClampedSize((int)Math.Ceiling(Math.Max(Math.Max(dvv1.GetElement(1), dvv2.GetElement(1)), dvv3.GetElement(1))), renderImageHeight + OffsetY);
                var u = Vector128.Create((float)uv1.U, (float)uv2.U, (float)uv3.U, 0.0F);
                var v = Vector128.Create((float)uv1.V, (float)uv2.V, (float)uv3.V, 0.0F);
                var w = Vector128.Create((float)w1, (float)w2, (float)w3, 0.0F);

                var denom = Vector128.Create((float)(1.0 / (((dvv2.GetElement(0) - dvv1.GetElement(0)) * (dvv3.GetElement(1) - dvv1.GetElement(1))) - ((dvv2.GetElement(1) - dvv1.GetElement(1)) * (dvv3.GetElement(0) - dvv1.GetElement(0))))));
                var edgeX = Sse.Subtract(Vector128.Create((float)dvv3.GetElement(0), (float)dvv1.GetElement(0), (float)dvv2.GetElement(0), 0.0F), Vector128.Create((float)dvv2.GetElement(0), (float)dvv3.GetElement(0), (float)dvv1.GetElement(0), 0.0F));
                var edgeY = Sse.Subtract(Vector128.Create((float)dvv3.GetElement(1), (float)dvv1.GetElement(1), (float)dvv2.GetElement(1), 0.0F), Vector128.Create((float)dvv2.GetElement(1), (float)dvv3.GetElement(1), (float)dvv1.GetElement(1), 0.0F));
                var isFrontFace = triangle.Normal.DotProduct(Avx.Divide(Avx.Add(Avx.Add(triangle.V1.Vertex, triangle.V2.Vertex), triangle.V3.Vertex), Vector256.Create(3.0))).GetElement(0) <= 0.0;
                var vvEX = Vector128.Create((float)dvv2.GetElement(0), (float)dvv3.GetElement(0), (float)dvv1.GetElement(0), 0.0F);
                var vvEY = Vector128.Create((float)dvv2.GetElement(1), (float)dvv3.GetElement(1), (float)dvv1.GetElement(1), 0.0F);
                var useLight = hasLight && triangle.IsAcceptLight && (trackMatteMode == TrackMatteMode.Luminance || trackMatteMode == TrackMatteMode.InvertLuminance);

                NManagedImage managedTexture;
                if (triangle.Texture is NGPUImage gpuImage)
                {
                    if (!convertedTexture.ContainsKey(gpuImage))
                    {
                        convertedTexture.Add(gpuImage, gpuImage.CopyToCpu());
                    }
                    managedTexture = convertedTexture[triangle.Texture];
                }
                else
                {
                    managedTexture = (NManagedImage)triangle.Texture;
                }

                ManagedRasterizedMaskImage? managedTrackMatte;
                if (triangle.TrackMatte is GPURasterizedMaskImage gpuRasterizedMask)
                {
                    if (!convertedTrackMatte.ContainsKey(gpuRasterizedMask))
                    {
                        convertedTrackMatte.Add(gpuRasterizedMask, gpuRasterizedMask.CopyToCpu());
                    }
                    managedTrackMatte = convertedTrackMatte[gpuRasterizedMask];
                }
                else
                {
                    managedTrackMatte = (ManagedRasterizedMaskImage?)triangle.TrackMatte;
                }

                Parallel.For(minY, maxY, y =>
                {
                    var renderImageSpan = RenderImage.GetDataSpan();
                    var trackMatteSpan = (managedTrackMatte?.Data ?? EmptyTrackMatte).AsSpan();
                    var texture = managedTexture.GetDataSpan();
                    var eY = Sse.Multiply(edgeX, Sse.Subtract(Vector128.Create(y, y, y, 0.0F), vvEY));

                    var offset = (y - OffsetY) * renderImageWidth;
                    var p = offset + (minX - OffsetX);

                    var pointLights = CollectionsMarshal.AsSpan(PointLights);
                    var spotLights = CollectionsMarshal.AsSpan(SpotLights);
                    var parallelLights = CollectionsMarshal.AsSpan(ParallelLights);
                    var ambientLights = CollectionsMarshal.AsSpan(AmbientLights);

                    for (int x = minX; x < maxX; x++, p++)
                    {
                        var eX = Sse.Subtract(Vector128.Create(x, x, x, 0.0F), vvEX);
                        var e = Sse.Multiply(Fma.IsSupported ? Fma.MultiplyAddNegated(edgeY, eX, eY) : Sse.Subtract(eY, Sse.Multiply(edgeY, eX)), denom);

                        var ae = Sse.And(e, Sse.CompareGreaterThanOrEqual(e.Abs(), Vector128.Create(Epsilon)));
                        if (!Avx.TestZ(Sse.CompareLessThan(ae, Vector128<float>.Zero), Vector128.Create(float.NaN)))
                        {
                            continue;
                        }

                        var tw = Sse.Multiply(w, e).HorizontalAdd();
                        var tx = Sse.Divide(Sse.Multiply(u, e), tw).HorizontalAdd().GetElement(0) * textureWidth;
                        var ty = Sse.Divide(Sse.Multiply(v, e), tw).HorizontalAdd().GetElement(0) * textureHeight;

                        var color = ImageInterpolation.Bilinear(texture, textureWidth, textureHeight, tx, ty);
                        color.W *= trackMatteSpan[p % trackMatteSpan.Length];
                        if (color.W <= 0.0F)
                        {
                            continue;
                        }

                        if (useLight)
                        {
                            var alpha = color.W;
                            var position = CalcBarycentricCoord(vvX, vvY, vvZ, e);
                            var n = isFrontFace ? -triangle.FloatNormal : triangle.FloatNormal;

                            if (triangle.IsAcceptLight)
                            {
                                var diffuse = Vector4.Zero;
                                var specular = Vector4.Zero;
                                var ambient = Vector4.Zero;

                                for (var i = 0; i < PointLights.Count; i++)
                                {
                                    var l = pointLights[i];
                                    var lightColor = l.Color;
                                    var lightDiff = Sse.Subtract(position, l.Position).AsVector3();
                                    var light = Vector3.Normalize(lightDiff);
                                    var falloff = CalcFalloff(lightDiff, l.FalloffType, l.FalloffStart, l.FalloffLength);

                                    var diffuseFactor = Vector3.Dot(light, n);
                                    var isBack = diffuseFactor < 0.0F;
                                    if (isBack)
                                    {
                                        diffuseFactor *= -triangle.LightTransmission;
                                    }
                                    diffuse += lightColor * color * diffuseFactor * falloff;

                                    var view = -Vector3.Normalize(position.AsVector3());
                                    var halfLE = Vector3.Normalize(view - light);
                                    var specularFactor = Math.Max(Vector3.Dot(-n, halfLE), 0.0F);
                                    if (isBack)
                                    {
                                        specularFactor *= -triangle.LightTransmission;
                                    }
                                    specular += Vector4.Lerp(lightColor, color * lightColor, triangle.Metal) * MathF.Pow(specularFactor, ShininessStrength * triangle.SpecularShininess) * triangle.SpecularIntensity * falloff;
                                }

                                for (var i = 0; i < SpotLights.Count; i++)
                                {
                                    var l = spotLights[i];
                                    var lightColor = l.Color;
                                    var lightDiff = Sse.Subtract(position, l.Position).AsVector3();
                                    var light = Vector3.Normalize(lightDiff);
                                    var spotCone = MathF.Acos(Vector3.Dot(l.Direction, light));

                                    if (spotCone <= l.OuterCone)
                                    {
                                        var attenuation = 1.0F;
                                        if (l.ConeAttenuationRate > 0.0)
                                        {
                                            attenuation = MathF.Cos((1.0F - Math.Min((MathF.Cos(spotCone) - l.OuterConeCos) * l.InvertInnerConeCos, 1.0F)) * MathF.PI * 0.5F);
                                        }

                                        var falloff = CalcFalloff(lightDiff, l.FalloffType, l.FalloffStart, l.FalloffLength);
                                        var diffuseFactor = Vector3.Dot(light, n);
                                        var isBack = diffuseFactor < 0.0F;
                                        if (isBack)
                                        {
                                            diffuseFactor *= -triangle.LightTransmission;
                                        }
                                        diffuse += lightColor * color * diffuseFactor * falloff * attenuation;

                                        var view = -Vector3.Normalize(position.AsVector3());
                                        var halfLE = Vector3.Normalize(view - light);
                                        var specularFactor = Math.Max(Vector3.Dot(-n, halfLE), 0.0F);
                                        if (isBack)
                                        {
                                            specularFactor *= -triangle.LightTransmission;
                                        }
                                        specular += Vector4.Lerp(lightColor, color * lightColor, triangle.Metal) * MathF.Pow(specularFactor, ShininessStrength * triangle.SpecularShininess) * triangle.SpecularIntensity * falloff * attenuation;
                                    }
                                }

                                for (var i = 0; i < ParallelLights.Count; i++)
                                {
                                    var l = parallelLights[i];
                                    var lightColor = l.Color;
                                    var lightDiff = Sse.Subtract(position, l.Position).AsVector3();
                                    var falloff = CalcFalloff(lightDiff, l.FalloffType, l.FalloffStart, l.FalloffLength);

                                    var diffuseFactor = Vector3.Dot(l.Direction, n);
                                    var isBack = diffuseFactor < 0.0F;
                                    if (isBack)
                                    {
                                        diffuseFactor *= -triangle.LightTransmission;
                                    }
                                    diffuse += lightColor * color * diffuseFactor * falloff;

                                    var view = -Vector3.Normalize(position.AsVector3());
                                    var halfLE = Vector3.Normalize(view - l.Direction);
                                    var specularFactor = Math.Max(Vector3.Dot(-n, halfLE), 0.0F);
                                    if (isBack)
                                    {
                                        specularFactor *= -triangle.LightTransmission;
                                    }
                                    specular += Vector4.Lerp(lightColor, color * lightColor, triangle.Metal) * MathF.Pow(specularFactor, ShininessStrength * triangle.SpecularShininess) * triangle.SpecularIntensity * falloff;
                                }

                                for (var i = 0; i < AmbientLights.Count; i++)
                                {
                                    ambient += ambientLights[i].Color * color;
                                }

                                color = diffuse * triangle.Diffuse + specular + ambient * triangle.Ambient;
                                color.W = alpha;
                                color = Vector4.Max(Vector4.Min(color, Vector4.One), Vector4.Zero);
                            }
                        }

                        renderImageSpan[p] = trackMatteMode switch
                        {
                            TrackMatteMode.Alpha => color.W,
                            TrackMatteMode.Luminance => (color * ToGrayScale).HorizontalAdd(),
                            TrackMatteMode.InvertAlpha => 1.0F - color.W,
                            TrackMatteMode.InvertLuminance => 1.0F - (color * ToGrayScale).HorizontalAdd(),
                            _ => 0.0F
                        } * triangle.Opacity;
                    }
                });
            }

            foreach (var (_, i) in convertedTexture)
            {
                i.Dispose();
            }
            foreach (var (_, i) in convertedTrackMatte)
            {
                i.Dispose();
            }
        }
    }

    file enum PointLightShadowDirection : int
    {
        Front = 0,
        Back,
        Left,
        Right,
        Top,
        Bottom
    }

    file record PointLightHolder(PointLight Light, PointLightShadowDirection Direction)
    {
        public static readonly PointLightShadowDirection[] Directions = Enum.GetValues(typeof(PointLightShadowDirection)).Cast<PointLightShadowDirection>().ToArray();
    }
}
