using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Image;
using NiVE3.Image.Drawing;
using NiVE3.Numerics;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Interfaces.RendererParams;
using NiVE3.PresetPlugin.Internal.Drawing.Primitive3D;
using NiVE3.Shared.Extension;

namespace NiVE3.PresetPlugin.Internal.Drawing
{
    abstract class Renderer3DBase
    {
        protected const int DepthRoundingDigit = 5; // TODO: 要調整

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

        protected Dictionary<object, List<ShadowTriangle>> LightTriangles { get; }

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

            LightTriangles = pointLights.Cast<object>().Concat(spotLights).Concat(parallelLights).ToDictionary(k => k, _ => new List<ShadowTriangle>());
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

        public void AddRect(NImage texture, ImageInterpolationQuality interpolationQuality, float opacity, BlendMode blendType, Matrix4x4d modelMatrix, ShadowCastMode shadowCastMode, float lightTransmission, bool isAcceptShadow, bool isAcceptLight, float ambient, float diffuse, float specularIntensity, float specularShininess, float metal, RasterizedMaskImage? trackMatte)
        {
            AddRectInternal(texture, interpolationQuality, 0, 0, texture.Width, texture.Height, opacity, blendType, modelMatrix, shadowCastMode, lightTransmission, isAcceptShadow, isAcceptLight, ambient, diffuse, specularIntensity, specularShininess, metal, trackMatte);

            LastId++;
        }

        void AddRectInternal(
            NImage texture,
            ImageInterpolationQuality interpolationQuality,
            int left,
            int top,
            int right,
            int bottom,
            float opacity,
            BlendMode blendType,
            Matrix4x4d modelMatrix,
            ShadowCastMode shadowCastMode,
            float lightTransmission,
            bool isAcceptShadow,
            bool isAcceptLight,
            float ambient,
            float diffuse,
            float specularIntensity,
            float specularShininess,
            float metal,
            RasterizedMaskImage? trackMatte
        )
        {
            // TODO: ボリゴンの境目が見えたり、斜めの補間エラーが出たりしたら調整する
            const double MaxTriangleEdgeLength = 0.25;

            var width = texture.Width;
            var height = texture.Height;
            var offsetX = (Size - Width) * 0.5 / Size;
            var offsetY = (Size - Height) * 0.5 / Size;
            var sv1 = Vector256.Create(left, top, 0.0, Size) / Size;
            var sv2 = Vector256.Create(left, bottom, 0.0, Size) / Size;
            var sv3 = Vector256.Create(right, bottom, 0.0, Size) / Size;
            var sv4 = Vector256.Create(right, top, 0.0, Size) / Size;

            var originOffsetedModelMatrix = Matrix4x4d.CreateTranslate(-texture.Origin.X / Size, -texture.Origin.Y / Size, 0.0) * modelMatrix;
            var mv = originOffsetedModelMatrix * ViewMatrix;
            var mvt = mv * Matrix4x4d.CreateTranslate(offsetX, offsetY, 0.0);
            var v1 = mvt.Transform(sv1);
            var v2 = mvt.Transform(sv2);
            var v3 = mvt.Transform(sv3);
            var v4 = mvt.Transform(sv4);

            if ((((v2 - v1) & Const.WithoutWMask256).LengthSquared() > MaxTriangleEdgeLength ||
                ((v3 - v1) & Const.WithoutWMask256).LengthSquared() > MaxTriangleEdgeLength ||
                ((v4 - v1) & Const.WithoutWMask256).LengthSquared() > MaxTriangleEdgeLength) &&
                right - left > 1 && bottom - top > 1)
            {
                var hSplit = (right - left) / 2 + left;
                var tSplit = (bottom - top) / 2 + top;
                AddRectInternal(texture, interpolationQuality, left, top, hSplit, tSplit, opacity, blendType, modelMatrix, shadowCastMode, lightTransmission, isAcceptShadow, isAcceptLight, ambient, diffuse, specularIntensity, specularShininess, metal, trackMatte);
                AddRectInternal(texture, interpolationQuality, hSplit, top, right, tSplit, opacity, blendType, modelMatrix, shadowCastMode, lightTransmission, isAcceptShadow, isAcceptLight, ambient, diffuse, specularIntensity, specularShininess, metal, trackMatte);
                AddRectInternal(texture, interpolationQuality, left, tSplit, hSplit, bottom, opacity, blendType, modelMatrix, shadowCastMode, lightTransmission, isAcceptShadow, isAcceptLight, ambient, diffuse, specularIntensity, specularShininess, metal, trackMatte);
                AddRectInternal(texture, interpolationQuality, hSplit, tSplit, right, bottom, opacity, blendType, modelMatrix, shadowCastMode, lightTransmission, isAcceptShadow, isAcceptLight, ambient, diffuse, specularIntensity, specularShininess, metal, trackMatte);
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

            var farPoint = mv.Transform(Vector256.Create(0.0, 0.0, -10000.0, 1.0)) & Const.WithoutWMask256;
            if (shadowCastMode != ShadowCastMode.ShadowOnly)
            {
                Triangles.Add(new Triangle(uv1, uv2, uv3, farPoint, invertedModelViewMatrix, texture, interpolationQuality, opacity, blendType, lightTransmission, isAcceptShadow, isAcceptLight, ambient, diffuse, specularIntensity, specularShininess, metal, trackMatte, LastId));
                Triangles.Add(new Triangle(uv1, uv3, uv4, farPoint, invertedModelViewMatrix, texture, interpolationQuality, opacity, blendType, lightTransmission, isAcceptShadow, isAcceptLight, ambient, diffuse, specularIntensity, specularShininess, metal, trackMatte, LastId));
            }

            if (shadowCastMode != ShadowCastMode.None)
            {
                foreach (var spotLight in SpotLights)
                {
                    if (!spotLight.IsEnableShadow)
                    {
                        continue;
                    }
                    var (lt1, lt2) = CreateLightTriangle(LastId, texture, interpolationQuality, opacity, lightTransmission, sv1, sv2, sv3, sv4, uLeft, vTop, uRight, vBottom, originOffsetedModelMatrix, mv, spotLight.LightViewMatrix, offsetX, offsetY);
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
                    var (lt1, lt2) = CreateLightTriangle(LastId, texture, interpolationQuality, opacity, lightTransmission, sv1, sv2, sv3, sv4, uLeft, vTop, uRight, vBottom, originOffsetedModelMatrix, mv, parallelLight.LightViewMatrix, offsetX, offsetY);
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
                    var (lt1, lt2) = CreateLightTriangle(LastId, texture, interpolationQuality, opacity, lightTransmission, sv1, sv2, sv3, sv4, uLeft, vTop, uRight, vBottom, originOffsetedModelMatrix, mv, pointLight.FrontLightViewMatrix, offsetX, offsetY);
                    var triangles = LightTriangles[new PointLightHolder(pointLight, PointLightShadowDirection.Front)];
                    triangles.Add(lt1);
                    triangles.Add(lt2);
                    (lt1, lt2) = CreateLightTriangle(LastId, texture, interpolationQuality, opacity, lightTransmission, sv1, sv2, sv3, sv4, uLeft, vTop, uRight, vBottom, originOffsetedModelMatrix, mv, pointLight.BackLightViewMatrix, offsetX, offsetY);
                    triangles = LightTriangles[new PointLightHolder(pointLight, PointLightShadowDirection.Back)];
                    triangles.Add(lt1);
                    triangles.Add(lt2);
                    (lt1, lt2) = CreateLightTriangle(LastId, texture, interpolationQuality, opacity, lightTransmission, sv1, sv2, sv3, sv4, uLeft, vTop, uRight, vBottom, originOffsetedModelMatrix, mv, pointLight.LeftLightViewMatrix, offsetX, offsetY);
                    triangles = LightTriangles[new PointLightHolder(pointLight, PointLightShadowDirection.Left)];
                    triangles.Add(lt1);
                    triangles.Add(lt2);
                    (lt1, lt2) = CreateLightTriangle(LastId, texture, interpolationQuality, opacity, lightTransmission, sv1, sv2, sv3, sv4, uLeft, vTop, uRight, vBottom, originOffsetedModelMatrix, mv, pointLight.RightLightViewMatrix, offsetX, offsetY);
                    triangles = LightTriangles[new PointLightHolder(pointLight, PointLightShadowDirection.Right)];
                    triangles.Add(lt1);
                    triangles.Add(lt2);
                    (lt1, lt2) = CreateLightTriangle(LastId, texture, interpolationQuality, opacity, lightTransmission, sv1, sv2, sv3, sv4, uLeft, vTop, uRight, vBottom, originOffsetedModelMatrix, mv, pointLight.TopLightViewMatrix, offsetX, offsetY);
                    triangles = LightTriangles[new PointLightHolder(pointLight, PointLightShadowDirection.Top)];
                    triangles.Add(lt1);
                    triangles.Add(lt2);
                    (lt1, lt2) = CreateLightTriangle(LastId, texture, interpolationQuality, opacity, lightTransmission, sv1, sv2, sv3, sv4, uLeft, vTop, uRight, vBottom, originOffsetedModelMatrix, mv, pointLight.BottomLightViewMatrix, offsetX, offsetY);
                    triangles = LightTriangles[new PointLightHolder(pointLight, PointLightShadowDirection.Bottom)];
                    triangles.Add(lt1);
                    triangles.Add(lt2);
                }
            }
        }

        static (ShadowTriangle, ShadowTriangle) CreateLightTriangle(
            int triangleId,
            NImage texture,
            ImageInterpolationQuality interpolationQuality,
            float opacity,
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

            var lfarPoint = lmv.Transform(Vector256.Create(0.0, 0.0, -10000.0, 1.0)) & Const.WithoutWMask256;
            return (
                new ShadowTriangle(luv1, luv2, luv3, lfarPoint, invertedLightModelViewMatrix, texture, interpolationQuality, opacity, lightTransmission, triangleId),
                new ShadowTriangle(luv1, luv3, luv4, lfarPoint, invertedLightModelViewMatrix, texture, interpolationQuality, opacity, lightTransmission, triangleId)
            );
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
            else if (float.IsNegativeInfinity(a))
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

    enum PointLightShadowDirection : int
    {
        Front = 0,
        Back,
        Left,
        Right,
        Top,
        Bottom
    }

    record PointLightHolder(PointLight Light, PointLightShadowDirection Direction)
    {
        public static readonly PointLightShadowDirection[] Directions = Enum.GetValues(typeof(PointLightShadowDirection)).Cast<PointLightShadowDirection>().ToArray();
    }
}
