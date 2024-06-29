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
using NiVE3.PresetPlugin.Internal.Util;

namespace NiVE3.PresetPlugin.Internal.Drawing
{
    abstract class Renderer3DBase
    {
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
            AddRectInternal(texture, 0, 0, texture.Width, texture.Height, opacity, blendType, modelMatrix, isCastShadow, lightTransmission, isAcceptShadow, isAcceptLight, ambient, diffuse, specularIntensity, specularShininess, metal, trackMatte);
        }

        void AddRectInternal(NImage texture, int left, int top, int right, int bottom, float opacity, BlendMode blendType, Matrix4x4d modelMatrix, bool isCastShadow, float lightTransmission, bool isAcceptShadow, bool isAcceptLight, float ambient, float diffuse, float specularIntensity, float specularShininess, float metal, RasterizedMaskImage? trackMatte)
        {
            // TODO: ボリゴンの境目が見えたり、斜めの補間エラーが出たりしたら調整する
            const double MaxTriangleEdgeLength = 0.5;

            var width = texture.Width;
            var height = texture.Height;
            var offsetX = (Size - Width) * 0.5 / Size;
            var offsetY = (Size - Height) * 0.5 / Size;
            var sv1 = Vector256.Create(left, top, 0.0, Size) / Size;
            var sv2 = Vector256.Create(left, bottom, 0.0, Size) / Size;
            var sv3 = Vector256.Create(right, bottom, 0.0, Size) / Size;
            var sv4 = Vector256.Create(right, top, 0.0, Size) / Size;

            modelMatrix = Matrix4x4d.CreateTranslate(-texture.Origin.X / Size, -texture.Origin.Y / Size, 0.0) * modelMatrix;
            var mv = modelMatrix * ViewMatrix;
            var mvt = mv * Matrix4x4d.CreateTranslate(offsetX, offsetY, 0.0);
            var v1 = mvt.Transform(sv1);
            var v2 = mvt.Transform(sv2);
            var v3 = mvt.Transform(sv3);
            var v4 = mvt.Transform(sv4);

            if ((((v2 - v1) & Consts.WithoutWMask).LengthSquared() > MaxTriangleEdgeLength ||
                ((v3 - v1) & Consts.WithoutWMask).LengthSquared() > MaxTriangleEdgeLength ||
                ((v4 - v1) & Consts.WithoutWMask).LengthSquared() > MaxTriangleEdgeLength) &&
                right - left > 1 && bottom - top > 1)
            {
                var hSplit = (right - left) / 2 + left;
                var tSplit = (bottom - top) / 2 + top;
                AddRectInternal(texture, left, top, hSplit, tSplit, opacity, blendType, modelMatrix, isCastShadow, lightTransmission, isAcceptShadow, isAcceptLight, ambient, diffuse, specularIntensity, specularShininess, metal, trackMatte);
                AddRectInternal(texture, hSplit, top, right, tSplit, opacity, blendType, modelMatrix, isCastShadow, lightTransmission, isAcceptShadow, isAcceptLight, ambient, diffuse, specularIntensity, specularShininess, metal, trackMatte);
                AddRectInternal(texture, left, tSplit, hSplit, bottom, opacity, blendType, modelMatrix, isCastShadow, lightTransmission, isAcceptShadow, isAcceptLight, ambient, diffuse, specularIntensity, specularShininess, metal, trackMatte);
                AddRectInternal(texture, hSplit, tSplit, right, bottom, opacity, blendType, modelMatrix, isCastShadow, lightTransmission, isAcceptShadow, isAcceptLight, ambient, diffuse, specularIntensity, specularShininess, metal, trackMatte);
                return;
            }

            var uLeft = left / (double)width;
            var uRight = right / (double)width;
            var vTop = top / (double)height;
            var vBottom = bottom / (double)height;
            var uv1 = new UVVertex(v1, uLeft, vTop);
            var uv2 = new UVVertex(v2, uLeft, vBottom);
            var uv3 = new UVVertex(v3, uRight, vBottom);
            var uv4 = new UVVertex(v4, uRight, vTop);

            Matrix4x4d.Invert(mv, out var invertedModelViewMatrix);
            invertedModelViewMatrix = Matrix4x4d.Transpose(invertedModelViewMatrix);

            var farPoint = mv.Transform(Vector256.Create(0.0, 0.0, -10000.0, 1.0)) & Consts.WithoutWMask;
            Triangles.Add(new Triangle(uv1, uv2, uv3, farPoint, invertedModelViewMatrix, texture, opacity, blendType, isCastShadow, lightTransmission, isAcceptShadow, isAcceptLight, ambient, diffuse, specularIntensity, specularShininess, metal, trackMatte, LastId));
            Triangles.Add(new Triangle(uv1, uv3, uv4, farPoint, invertedModelViewMatrix, texture, opacity, blendType, isCastShadow, lightTransmission, isAcceptShadow, isAcceptLight, ambient, diffuse, specularIntensity, specularShininess, metal, trackMatte, LastId));

            foreach (var spotLight in SpotLights)
            {
                if (!spotLight.IsEnableShadow)
                {
                    continue;
                }
                var (lt1, lt2) = CreateLightTriangle(LastId, texture, opacity, isCastShadow, lightTransmission, sv1, sv2, sv3, sv4, uLeft, vTop, uRight, vBottom, modelMatrix, mv, spotLight.LightViewMatrix, offsetX, offsetY);
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
                var (lt1, lt2) = CreateLightTriangle(LastId, texture, opacity, isCastShadow, lightTransmission, sv1, sv2, sv3, sv4, uLeft, vTop, uRight, vBottom, modelMatrix, mv, parallelLight.LightViewMatrix, offsetX, offsetY);
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
                var (lt1, lt2) = CreateLightTriangle(LastId, texture, opacity, isCastShadow, lightTransmission, sv1, sv2, sv3, sv4, uLeft, vTop, uRight, vBottom, modelMatrix, mv, pointLight.FrontLightViewMatrix, offsetX, offsetY);
                var triangles = LightTriangles[new PointLightHolder(pointLight, PointLightShadowDirection.Front)];
                triangles.Add(lt1);
                triangles.Add(lt2);
                (lt1, lt2) = CreateLightTriangle(LastId, texture, opacity, isCastShadow, lightTransmission, sv1, sv2, sv3, sv4, uLeft, vTop, uRight, vBottom, modelMatrix, mv, pointLight.BackLightViewMatrix, offsetX, offsetY);
                triangles = LightTriangles[new PointLightHolder(pointLight, PointLightShadowDirection.Back)];
                triangles.Add(lt1);
                triangles.Add(lt2);
                (lt1, lt2) = CreateLightTriangle(LastId, texture, opacity, isCastShadow, lightTransmission, sv1, sv2, sv3, sv4, uLeft, vTop, uRight, vBottom, modelMatrix, mv, pointLight.LeftLightViewMatrix, offsetX, offsetY);
                triangles = LightTriangles[new PointLightHolder(pointLight, PointLightShadowDirection.Left)];
                triangles.Add(lt1);
                triangles.Add(lt2);
                (lt1, lt2) = CreateLightTriangle(LastId, texture, opacity, isCastShadow, lightTransmission, sv1, sv2, sv3, sv4, uLeft, vTop, uRight, vBottom, modelMatrix, mv, pointLight.RightLightViewMatrix, offsetX, offsetY);
                triangles = LightTriangles[new PointLightHolder(pointLight, PointLightShadowDirection.Right)];
                triangles.Add(lt1);
                triangles.Add(lt2);
                (lt1, lt2) = CreateLightTriangle(LastId, texture, opacity, isCastShadow, lightTransmission, sv1, sv2, sv3, sv4, uLeft, vTop, uRight, vBottom, modelMatrix, mv, pointLight.TopLightViewMatrix, offsetX, offsetY);
                triangles = LightTriangles[new PointLightHolder(pointLight, PointLightShadowDirection.Top)];
                triangles.Add(lt1);
                triangles.Add(lt2);
                (lt1, lt2) = CreateLightTriangle(LastId, texture, opacity, isCastShadow, lightTransmission, sv1, sv2, sv3, sv4, uLeft, vTop, uRight, vBottom, modelMatrix, mv, pointLight.BottomLightViewMatrix, offsetX, offsetY);
                triangles = LightTriangles[new PointLightHolder(pointLight, PointLightShadowDirection.Bottom)];
                triangles.Add(lt1);
                triangles.Add(lt2);
            }

            LastId++;
        }

        static (LightTriangle, LightTriangle) CreateLightTriangle(
            int triangleId,
            NImage texture,
            float opacity,
            bool isCastShadow,
            float lightTransmission,
            in Vector256<double> sv1,
            in Vector256<double> sv2,
            in Vector256<double> sv3,
            in Vector256<double> sv4,
            double uLeft,
            double vTop,
            double uRight,
            double vBottom,
            in Matrix4x4d modelMatrix,
            in Matrix4x4d modelViewMatrix,
            in Matrix4x4d lightViewMatrix,
            double offsetX,
            double offsetY
        )
        {
            var lmv = modelMatrix * lightViewMatrix;
            var lmvt = lmv * Matrix4x4d.CreateTranslate(offsetX, offsetY, 0.0);
            var lv1 = lmvt.Transform(sv1);
            var lv2 = lmvt.Transform(sv2);
            var lv3 = lmvt.Transform(sv3);
            var lv4 = lmvt.Transform(sv4);

            var luv1 = new UVVertex(lv1, uLeft, vTop);
            var luv2 = new UVVertex(lv2, uLeft, vBottom);
            var luv3 = new UVVertex(lv3, uRight, vBottom);
            var luv4 = new UVVertex(lv4, uRight, vTop);

            Matrix4x4d.Invert(modelViewMatrix, out var invertedLightModelViewMatrix);
            invertedLightModelViewMatrix = Matrix4x4d.Transpose(invertedLightModelViewMatrix);

            var lfarPoint = lmv.Transform(Vector256.Create(0.0, 0.0, -10000.0, 1.0)) & Consts.WithoutWMask;
            return (new LightTriangle(luv1, luv2, luv3, lfarPoint, invertedLightModelViewMatrix, texture, opacity, isCastShadow, lightTransmission, triangleId), new LightTriangle(luv1, luv3, luv4, lfarPoint, invertedLightModelViewMatrix, texture, opacity, isCastShadow, lightTransmission, triangleId));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static Vector256<double> MaxByAbs(in Vector256<double> a, in Vector256<double> b)
        {
            if (Vector128.GreaterThanOrEqualAny(Vector256.GetLower(Vector256.Abs(a)), Vector256.GetLower(Vector256.Abs(b))))
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
            return Avx.RoundCurrentDirection(v * pow) / pow;
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
            return Vector128.Create(
                Vector128.Sum(x * e),
                Vector128.Sum(y * e),
                Vector128.Sum(z * e),
                1.0F
            );
        }
    }

    class Renderer3D : Renderer3DBase
    {
        const int DepthRoundingDigit = 5; // TODO: 要調整

        NManagedImage RenderImage { get; }

        public Renderer3D(NManagedImage renderImage, int width, int height, List<PointLight> pointLights, List<SpotLight> spotLights, List<ParallelLight> parallelLights, List<AmbientLight> ambientLights)
            : base(width, height, pointLights, spotLights, parallelLights, ambientLights)
        {
            RenderImage = renderImage;
        }

        public void Render()
        {
            var renderImageWidth = RenderImage.Width;
            var renderImageHeight = RenderImage.Height;
            var triangles = TriangleDivider.ClipAndDivide(Triangles).ToArray();
            if (triangles.Length < 1)
            {
                return;
            }

            var scaleRateX = Width / (float)renderImageWidth;
            var scaleRateY = Height / (float)renderImageHeight;
            var minZ = triangles.Select(t => Math.Min(Math.Min(t.V1.Vertex.GetElement(2), t.V2.Vertex.GetElement(2)), t.V3.Vertex.GetElement(2))).Min();
            var maxZ = triangles.Select(t => Math.Max(Math.Max(t.V1.Vertex.GetElement(2), t.V2.Vertex.GetElement(2)), t.V3.Vertex.GetElement(2))).Max();
            var projectionMatrix = Matrix4x4d.CreatePerspectiveFieldOfView(FieldOfView, 1.0, minZ, maxZ);

            var offsetX = (Size - Width) * 0.5 / Size;
            var offsetY = (Size - Height) * 0.5 / Size;
            Matrix4x4d.Invert(ViewMatrix, out var invtededViewMatrix);
            Matrix4x4d.Invert(projectionMatrix, out var invertedProjectionMatrix);
            var floatInvtededViewMatrix = (Matrix4x4)(invertedProjectionMatrix * Matrix4x4d.CreateTranslate(-offsetX, -offsetY, 0.0) * invtededViewMatrix);
            var convertedTexture = new Dictionary<NImage, NManagedImage>();
            var convertedTrackMatte = new Dictionary<RasterizedMaskImage, ManagedRasterizedMaskImage>();
            var hasLight = PointLights.Count > 0 || SpotLights.Count > 0 || ParallelLights.Count > 0 || AmbientLights.Count > 0;

            var shadowBuffer = new ShadowBuffer();
            var shadowSize = Size == Width ? (int)(Size / scaleRateX) : (int)(Size / scaleRateY);
            var pointLightShadows = PointLights.Select(l => l.IsEnableShadow ? RenderPointLightShadow(l, shadowBuffer, shadowSize, (float)offsetX, (float)offsetY) : null).ToArray();
            var spotLightShadows = SpotLights.Select(l => l.IsEnableShadow && LightTriangles[l].Count > 0 ? RenderSpotLightShadow(l, shadowBuffer, shadowSize, (float)offsetX, (float)offsetY) : null).ToArray();
            var parallelLightShadows = ParallelLights.Select(l => l.IsEnableShadow && LightTriangles[l].Count > 0 ? RenderParallelLightShadow(l, shadowBuffer, shadowSize, (float)offsetX, (float)offsetY) : null).ToArray();
            var hasShadow = pointLightShadows.Any(ss => ss != null && ss.Any(s => s != null)) || spotLightShadows.Any(s => s != null) || parallelLightShadows.Any(s => s != null);

            var renderImageOffsetX = (int)(OffsetX / scaleRateX);
            var renderImageOffsetY = (int)(OffsetY / scaleRateY);
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
                var dvv1 = (uv1.Vertex + Vector256.Create(1.0, 1.0, 0.0, 0.0)) * Vector256.Create(Size * 0.5, Size * 0.5, 1.0, 1.0);
                var dvv2 = (uv2.Vertex + Vector256.Create(1.0, 1.0, 0.0, 0.0)) * Vector256.Create(Size * 0.5, Size * 0.5, 1.0, 1.0);
                var dvv3 = (uv3.Vertex + Vector256.Create(1.0, 1.0, 0.0, 0.0)) * Vector256.Create(Size * 0.5, Size * 0.5, 1.0, 1.0);
                var vvX = Vector128.Create((float)triangle.V1.Vertex.GetElement(0), (float)triangle.V2.Vertex.GetElement(0), (float)triangle.V3.Vertex.GetElement(0), 0.0F);
                var vvY = Vector128.Create((float)triangle.V1.Vertex.GetElement(1), (float)triangle.V2.Vertex.GetElement(1), (float)triangle.V3.Vertex.GetElement(1), 0.0F);
                var vvZ = Vector128.Create((float)triangle.V1.Vertex.GetElement(2), (float)triangle.V2.Vertex.GetElement(2), (float)triangle.V3.Vertex.GetElement(2), 0.0F);
                var svvX = Vector128.Create((float)uv1.Vertex.GetElement(0), (float)uv2.Vertex.GetElement(0), (float)uv3.Vertex.GetElement(0), 0.0F);
                var svvY = Vector128.Create((float)uv1.Vertex.GetElement(1), (float)uv2.Vertex.GetElement(1), (float)uv3.Vertex.GetElement(1), 0.0F);
                var svvZ = Vector128.Create((float)uv1.Vertex.GetElement(2), (float)uv2.Vertex.GetElement(2), (float)uv3.Vertex.GetElement(2), 0.0F);
                var minX = (int)(MaxClampedSize((int)(Math.Min(Math.Min(dvv1.GetElement(0), dvv2.GetElement(0)), dvv3.GetElement(0))), OffsetX) / scaleRateX);
                var maxX = (int)(MinClampedSize((int)Math.Ceiling(Math.Max(Math.Max(dvv1.GetElement(0), dvv2.GetElement(0)), dvv3.GetElement(0))), Width + OffsetX) / scaleRateX);
                var minY = (int)(MaxClampedSize((int)(Math.Min(Math.Min(dvv1.GetElement(1), dvv2.GetElement(1)), dvv3.GetElement(1))), OffsetY) / scaleRateY);
                var maxY = (int)(MinClampedSize((int)Math.Ceiling(Math.Max(Math.Max(dvv1.GetElement(1), dvv2.GetElement(1)), dvv3.GetElement(1))), Height + OffsetY) / scaleRateY);
                var u = Vector128.Create((float)uv1.U, (float)uv2.U, (float)uv3.U, 0.0F);
                var v = Vector128.Create((float)uv1.V, (float)uv2.V, (float)uv3.V, 0.0F);
                var w = Vector128.Create((float)w1, (float)w2, (float)w3, 0.0F);

                var denom = Vector128.Create((float)(1.0 / (((dvv2.GetElement(0) - dvv1.GetElement(0)) * (dvv3.GetElement(1) - dvv1.GetElement(1))) - ((dvv2.GetElement(1) - dvv1.GetElement(1)) * (dvv3.GetElement(0) - dvv1.GetElement(0))))));
                var edgeX = Vector128.Create((float)dvv3.GetElement(0), (float)dvv1.GetElement(0), (float)dvv2.GetElement(0), 0.0F) - Vector128.Create((float)dvv2.GetElement(0), (float)dvv3.GetElement(0), (float)dvv1.GetElement(0), 0.0F);
                var edgeY = Vector128.Create((float)dvv3.GetElement(1), (float)dvv1.GetElement(1), (float)dvv2.GetElement(1), 0.0F) - Vector128.Create((float)dvv2.GetElement(1), (float)dvv3.GetElement(1), (float)dvv1.GetElement(1), 0.0F);
                var isFrontFace = Vector256.Dot(triangle.Normal, (triangle.V1.Vertex + triangle.V2.Vertex + triangle.V3.Vertex) / 3.0) <= 0.0;
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
                    var eY = edgeX * (Vector128.Create(y, y, y, 0.0F) * scaleRateY - vvEY);

                    var offset = (y - renderImageOffsetY) * renderImageWidth;
                    var p = offset + (minX - renderImageOffsetX);

                    var pointLights = CollectionsMarshal.AsSpan(PointLights);
                    var spotLights = CollectionsMarshal.AsSpan(SpotLights);
                    var parallelLights = CollectionsMarshal.AsSpan(ParallelLights);
                    var ambientLights = CollectionsMarshal.AsSpan(AmbientLights);

                    for (var x = minX; x < maxX; x++, p++)
                    {
                        var eX = Vector128.Create(x, x, x, 0.0F) * scaleRateX - vvEX;
                        var e = (Fma.IsSupported ? Fma.MultiplyAddNegated(edgeY, eX, eY) : (eY - (edgeY * eX))) * denom;

                        var ae = e & Vector128.GreaterThanOrEqual(Vector128.Abs(e), Vector128.Create(TriangleDivider.Epsilon));
                        if (Vector128.LessThanAny(ae, Vector128<float>.Zero))
                        {
                            continue;
                        }

                        var tw = Vector128.Sum(w * e);
                        var tx = Vector128.Sum(u * e / tw) * textureWidth;
                        var ty = Vector128.Sum(v * e / tw) * textureHeight;

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
                                    var lightDiff = (position - l.Position).AsVector3();
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
                                    var lightDiff = (position - l.Position).AsVector3();
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
                                    var lightDiff = (position - l.Position).AsVector3();
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
                                    var lightDiff = (position - l.Position).AsVector3();
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
            var triangles = TriangleDivider.ClipAndDivide(LightTriangles[spotLight]).ToArray();
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
            var triangles = TriangleDivider.ClipAndDivide(LightTriangles[parallelLight]).ToArray();
            if (triangles.Length < 1 || triangles.All(t => !t.IsCastShadow))
            {
                return null;
            }

            var min = triangles.Select(t => Vector256.Min(Vector256.Min(t.V1.Vertex, t.V2.Vertex), t.V3.Vertex)).Aggregate(Vector256.Min);
            var max = triangles.Select(t => Vector256.Max(Vector256.Max(t.V1.Vertex, t.V2.Vertex), t.V3.Vertex)).Aggregate(Vector256.Max);
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
                var triangles = TriangleDivider.ClipAndDivide(LightTriangles[holder]).ToArray();
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
                var dvv1 = (uv1.Vertex + Vector256.Create(1.0, 1.0, 0.0, 0.0)) * Vector256.Create(size * 0.5, size * 0.5, 1.0, 1.0);
                var dvv2 = (uv2.Vertex + Vector256.Create(1.0, 1.0, 0.0, 0.0)) * Vector256.Create(size * 0.5, size * 0.5, 1.0, 1.0);
                var dvv3 = (uv3.Vertex + Vector256.Create(1.0, 1.0, 0.0, 0.0)) * Vector256.Create(size * 0.5, size * 0.5, 1.0, 1.0);
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
                var edgeX = Vector128.Create((float)dvv3.GetElement(0), (float)dvv1.GetElement(0), (float)dvv2.GetElement(0), 0.0F) - Vector128.Create((float)dvv2.GetElement(0), (float)dvv3.GetElement(0), (float)dvv1.GetElement(0), 0.0F);
                var edgeY = Vector128.Create((float)dvv3.GetElement(1), (float)dvv1.GetElement(1), (float)dvv2.GetElement(1), 0.0F) - Vector128.Create((float)dvv2.GetElement(1), (float)dvv3.GetElement(1), (float)dvv1.GetElement(1), 0.0F);
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
                    var eY = edgeX * (Vector128.Create(y, y, y, 0.0F) - vvEY);
                    var offset = y * size;
                    var indicesSpan = shadowMap.Indices.AsSpan(offset, size);
                    var bufferIndicesSpan = shadowMap.BufferIndices.AsSpan(offset, size);
                    for (var x = minX; x < maxX; x++)
                    {
                        var eX = Vector128.Create(x, x, x, 0.0F) - vvEX;
                        var e = (Fma.IsSupported ? Fma.MultiplyAddNegated(edgeY, eX, eY) : (eY - (edgeY * eX))) * denom;
                        var ae = e & Vector128.GreaterThanOrEqual(Vector128.Abs(e), Vector128.Create(TriangleDivider.Epsilon));
                        if (Vector128.LessThanAny(ae, Vector128<float>.Zero))
                        {
                            continue;
                        }

                        var tw = Vector128.Sum(w * e);
                        var tx = Vector128.Sum(u * e / tw) * textureWidth;
                        var ty = Vector128.Sum(v * e / tw) * textureHeight;

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

        public MaskRenderer3D(ManagedRasterizedMaskImage renderImage, int width, int height, List<PointLight> pointLights, List<SpotLight> spotLights, List<ParallelLight> parallelLights, List<AmbientLight> ambientLights)
            : base(width, height, pointLights, spotLights, parallelLights, ambientLights)
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
            var triangles = TriangleDivider.ClipAndDivide(Triangles).ToArray();
            if (triangles.Length < 1)
            {
                return;
            }

            var scaleRateX = Width / (float)renderImageWidth;
            var scaleRateY = Height / (float)renderImageHeight;
            var minZ = triangles.Select(t => Math.Min(Math.Min(t.V1.Vertex.GetElement(2), t.V2.Vertex.GetElement(2)), t.V3.Vertex.GetElement(2))).Min();
            var maxZ = triangles.Select(t => Math.Max(Math.Max(t.V1.Vertex.GetElement(2), t.V2.Vertex.GetElement(2)), t.V3.Vertex.GetElement(2))).Max();
            var projectionMatrix = Matrix4x4d.CreatePerspectiveFieldOfView(FieldOfView, 1.0, minZ, maxZ);

            var offsetX = (Size - Width) * 0.5 / Size;
            var offsetY = (Size - Height) * 0.5 / Size;
            Matrix4x4d.Invert(ViewMatrix, out var invtededViewMatrix);
            Matrix4x4d.Invert(projectionMatrix, out var invertedProjectionMatrix);
            var floatInvtededViewMatrix = (Matrix4x4)(invertedProjectionMatrix * Matrix4x4d.CreateTranslate(-offsetX, -offsetY, 0.0) * invtededViewMatrix);
            var convertedTexture = new Dictionary<NImage, NManagedImage>();
            var convertedTrackMatte = new Dictionary<RasterizedMaskImage, ManagedRasterizedMaskImage>();
            var hasLight = PointLights.Count > 0 || SpotLights.Count > 0 || ParallelLights.Count > 0 || AmbientLights.Count > 0;

            var renderImageOffsetX = (int)(OffsetX / scaleRateX);
            var renderImageOffsetY = (int)(OffsetY / scaleRateY);
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
                var dvv1 = (uv1.Vertex + Vector256.Create(1.0, 1.0, 0.0, 0.0)) * Vector256.Create(Size * 0.5, Size * 0.5, 1.0, 1.0);
                var dvv2 = (uv2.Vertex + Vector256.Create(1.0, 1.0, 0.0, 0.0)) * Vector256.Create(Size * 0.5, Size * 0.5, 1.0, 1.0);
                var dvv3 = (uv3.Vertex + Vector256.Create(1.0, 1.0, 0.0, 0.0)) * Vector256.Create(Size * 0.5, Size * 0.5, 1.0, 1.0);
                var vvX = Vector128.Create((float)triangle.V1.Vertex.GetElement(0), (float)triangle.V2.Vertex.GetElement(0), (float)triangle.V3.Vertex.GetElement(0), 0.0F);
                var vvY = Vector128.Create((float)triangle.V1.Vertex.GetElement(1), (float)triangle.V2.Vertex.GetElement(1), (float)triangle.V3.Vertex.GetElement(1), 0.0F);
                var vvZ = Vector128.Create((float)triangle.V1.Vertex.GetElement(2), (float)triangle.V2.Vertex.GetElement(2), (float)triangle.V3.Vertex.GetElement(2), 0.0F);
                var svvX = Vector128.Create((float)uv1.Vertex.GetElement(0), (float)uv2.Vertex.GetElement(0), (float)uv3.Vertex.GetElement(0), 0.0F);
                var svvY = Vector128.Create((float)uv1.Vertex.GetElement(1), (float)uv2.Vertex.GetElement(1), (float)uv3.Vertex.GetElement(1), 0.0F);
                var svvZ = Vector128.Create((float)uv1.Vertex.GetElement(2), (float)uv2.Vertex.GetElement(2), (float)uv3.Vertex.GetElement(2), 0.0F);
                var minX = (int)(MaxClampedSize((int)(Math.Min(Math.Min(dvv1.GetElement(0), dvv2.GetElement(0)), dvv3.GetElement(0))), OffsetX) / scaleRateX);
                var maxX = (int)(MinClampedSize((int)Math.Ceiling(Math.Max(Math.Max(dvv1.GetElement(0), dvv2.GetElement(0)), dvv3.GetElement(0))), Width + OffsetX) / scaleRateX);
                var minY = (int)(MaxClampedSize((int)(Math.Min(Math.Min(dvv1.GetElement(1), dvv2.GetElement(1)), dvv3.GetElement(1))), OffsetY) / scaleRateY);
                var maxY = (int)(MinClampedSize((int)Math.Ceiling(Math.Max(Math.Max(dvv1.GetElement(1), dvv2.GetElement(1)), dvv3.GetElement(1))), Height + OffsetY) / scaleRateY);
                var u = Vector128.Create((float)uv1.U, (float)uv2.U, (float)uv3.U, 0.0F);
                var v = Vector128.Create((float)uv1.V, (float)uv2.V, (float)uv3.V, 0.0F);
                var w = Vector128.Create((float)w1, (float)w2, (float)w3, 0.0F);

                var denom = Vector128.Create((float)(1.0 / (((dvv2.GetElement(0) - dvv1.GetElement(0)) * (dvv3.GetElement(1) - dvv1.GetElement(1))) - ((dvv2.GetElement(1) - dvv1.GetElement(1)) * (dvv3.GetElement(0) - dvv1.GetElement(0))))));
                var edgeX = Vector128.Create((float)dvv3.GetElement(0), (float)dvv1.GetElement(0), (float)dvv2.GetElement(0), 0.0F) - Vector128.Create((float)dvv2.GetElement(0), (float)dvv3.GetElement(0), (float)dvv1.GetElement(0), 0.0F);
                var edgeY = Vector128.Create((float)dvv3.GetElement(1), (float)dvv1.GetElement(1), (float)dvv2.GetElement(1), 0.0F) - Vector128.Create((float)dvv2.GetElement(1), (float)dvv3.GetElement(1), (float)dvv1.GetElement(1), 0.0F);
                var isFrontFace = Vector256.Dot(triangle.Normal, (triangle.V1.Vertex + triangle.V2.Vertex + triangle.V3.Vertex) / 3.0) <= 0.0;
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
                    var eY = edgeX * (Vector128.Create(y, y, y, 0.0F) * scaleRateY - vvEY);

                    var offset = (y - renderImageOffsetY) * renderImageWidth;
                    var p = offset + (minX - renderImageOffsetX);

                    var pointLights = CollectionsMarshal.AsSpan(PointLights);
                    var spotLights = CollectionsMarshal.AsSpan(SpotLights);
                    var parallelLights = CollectionsMarshal.AsSpan(ParallelLights);
                    var ambientLights = CollectionsMarshal.AsSpan(AmbientLights);

                    for (var x = minX; x < maxX; x++, p++)
                    {
                        var eX = Vector128.Create(x, x, x, 0.0F) * scaleRateX - vvEX;
                        var e = (Fma.IsSupported ? Fma.MultiplyAddNegated(edgeY, eX, eY) : (eY - (edgeY * eX))) * denom;

                        var ae = e & Vector128.GreaterThanOrEqual(Vector128.Abs(e), Vector128.Create(TriangleDivider.Epsilon));
                        if (Vector128.LessThanAny(ae, Vector128<float>.Zero))
                        {
                            continue;
                        }

                        var tw = Vector128.Sum(w * e);
                        var tx = Vector128.Sum((u * e) / tw) * textureWidth;
                        var ty = Vector128.Sum((v * e) / tw) * textureHeight;

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
                                    var lightDiff = (position - l.Position).AsVector3();
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
                                    var lightDiff = (position - l.Position).AsVector3();
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
                                    var lightDiff = (position - l.Position).AsVector3();
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
