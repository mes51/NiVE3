using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Image;
using NiVE3.PresetPlugin.Internal.Drawing.Primitive3D;
using NiVE3.Plugin.Interfaces;
using NiVE3.Shared.Extension;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using NiVE3.Plugin.Numerics;
using NiVE3.Plugin.Interfaces.RendererParams;

namespace NiVE3.PresetPlugin.Internal.Drawing
{
    class Renderer3D
    {
        const float NearZ = -5E-5F; //1.0F;

        Matrix4x4d modelMatrix;
        public Matrix4x4d ModelMatrix
        {
            get => modelMatrix;
            set
            {
                modelMatrix = value;
                ModelViewMatrix = value * viewMatrix;
                Matrix4x4d invertedModelViewMatrix;
                Matrix4x4d.Invert(ModelViewMatrix, out invertedModelViewMatrix);
                InvertedModelViewMatrix = Matrix4x4d.Transpose(invertedModelViewMatrix);
            }
        }

        Matrix4x4d viewMatrix;
        public Matrix4x4d ViewMatrix
        {
            get => viewMatrix;
            set
            {
                viewMatrix = value;
                ModelViewMatrix = modelMatrix * value;
                Matrix4x4d invertedModelViewMatrix;
                Matrix4x4d.Invert(ModelViewMatrix, out invertedModelViewMatrix);
                InvertedModelViewMatrix = Matrix4x4d.Transpose(invertedModelViewMatrix);
            }
        }

        public Matrix4x4d ProjectionMatrix { get; set; } = Matrix4x4.Identity;

        public int Size { get; }

        Matrix4x4d ModelViewMatrix { get; set; }

        Matrix4x4d InvertedModelViewMatrix { get; set; }

        int OffsetX { get; }

        int OffsetY { get; }

        int LastId { get; set; }

        NManagedImage RenderImage { get; }

        List<Triangle> Triangles { get; } = new List<Triangle>();

        List<PointLight> PointLights { get; } = new List<PointLight>();

        List<SpotLight> SpotLights { get; } = new List<SpotLight>();

        List<AmbientLight> AmbientLights { get; } = new List<AmbientLight>();

        public Renderer3D(NManagedImage renderImage)
        {
            Size = Math.Max(renderImage.Width, renderImage.Height);
            OffsetX = (Size - renderImage.Width) / 2;
            OffsetY = (Size - renderImage.Height) / 2;
            RenderImage = renderImage;
        }

        public void AddRect(NImage texture, BlendMode blendType, float diffuse = 1.0F, float ambient = 1.0F, float mirror = 1.0F, float specular = 0.15F, float metal = 0.0F)
        {
            var width = texture.Width;
            var height = texture.Height;
            var offsetX = (Size - RenderImage.Width) * 0.5 / Size;
            var offsetY = (Size - RenderImage.Height) * 0.5 / Size;
            var v1 = Avx.Divide(Vector256.Create(0.0, 0.0, 0.0, Size), Vector256.Create((double)Size));
            var v2 = Avx.Divide(Vector256.Create(0.0, height, 0.0, Size), Vector256.Create((double)Size));
            var v3 = Avx.Divide(Vector256.Create(width, height, 0.0, Size), Vector256.Create((double)Size));
            var v4 = Avx.Divide(Vector256.Create(width, 0.0, 0.0, Size), Vector256.Create((double)Size));

            var mvt = ModelViewMatrix * Matrix4x4d.CreateTranslate(offsetX, offsetY, 0.0);
            v1 = mvt.Transform(v1);
            v2 = mvt.Transform(v2);
            v3 = mvt.Transform(v3);
            v4 = mvt.Transform(v4);

            var uv1 = new UVVertex(v1, 0.0F, 0.0F);
            var uv2 = new UVVertex(v2, 0.0F, 1.0F);
            var uv3 = new UVVertex(v3, 1.0F, 1.0F);
            var uv4 = new UVVertex(v4, 1.0F, 0.0F);

            var farPoint = Avx.And(ModelViewMatrix.Transform(Vector256.Create(0.0, 0.0, -10000.0, 1.0)), Vector256.Create(0xFFFFFFFFFFFFFFFFUL, 0xFFFFFFFFFFFFFFFFUL, 0xFFFFFFFFFFFFFFFFUL, 0).AsDouble());
            Triangles.Add(new Triangle(uv1, uv2, uv3, farPoint, InvertedModelViewMatrix, texture, blendType, diffuse, ambient, mirror, specular, metal, LastId));
            Triangles.Add(new Triangle(uv1, uv3, uv4, farPoint, InvertedModelViewMatrix, texture, blendType, diffuse, ambient, mirror, specular, metal, LastId));
            LastId++;
        }

        public void Render()
        {
            var renderImageWidth = RenderImage.Width;
            var triangles = GetClipAndDividedTriangles();

            var convertedTexture = new Dictionary<NImage, NManagedImage>();
            foreach (var triangle in triangles)
            {
                if (triangle.V1.Vertex.GetElement(2) > 0.0F || triangle.V2.Vertex.GetElement(2) > 0.0F || triangle.V3.Vertex.GetElement(2) > 0.0F)
                {
                    continue;
                }

                var uv1 = triangle.V1.Transform(ProjectionMatrix);
                var uv2 = triangle.V2.Transform(ProjectionMatrix);
                var uv3 = triangle.V3.Transform(ProjectionMatrix);
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
                var minX = MaxClampedSize((int)(Math.Min(Math.Min(dvv1.GetElement(0), dvv2.GetElement(0)), dvv3.GetElement(0))), OffsetX);
                var maxX = MinClampedSize((int)Math.Ceiling(Math.Max(Math.Max(dvv1.GetElement(0), dvv2.GetElement(0)), dvv3.GetElement(0))), renderImageWidth + OffsetX);
                var minY = MaxClampedSize((int)(Math.Min(Math.Min(dvv1.GetElement(1), dvv2.GetElement(1)), dvv3.GetElement(1))), OffsetY);
                var maxY = MinClampedSize((int)Math.Ceiling(Math.Max(Math.Max(dvv1.GetElement(1), dvv2.GetElement(1)), dvv3.GetElement(1))), RenderImage.Height + OffsetY);
                var u = Vector128.Create((float)uv1.U, (float)uv2.U, (float)uv3.U, 0.0F);
                var v = Vector128.Create((float)uv1.V, (float)uv2.V, (float)uv3.V, 0.0F);
                var w = Vector128.Create((float)w1, (float)w2, (float)w3, 0.0F);

                var denom = Vector128.Create((float)(1.0 / (((dvv2.GetElement(0) - dvv1.GetElement(0)) * (dvv3.GetElement(1) - dvv1.GetElement(1))) - ((dvv2.GetElement(1) - dvv1.GetElement(1)) * (dvv3.GetElement(0) - dvv1.GetElement(0))))));
                var edgeX = Sse.Subtract(Vector128.Create((float)dvv3.GetElement(0), (float)dvv1.GetElement(0), (float)dvv2.GetElement(0), 0.0F), Vector128.Create((float)dvv2.GetElement(0), (float)dvv3.GetElement(0), (float)dvv1.GetElement(0), 0.0F));
                var edgeY = Sse.Subtract(Vector128.Create((float)dvv3.GetElement(1), (float)dvv1.GetElement(1), (float)dvv2.GetElement(1), 0.0F), Vector128.Create((float)dvv2.GetElement(1), (float)dvv3.GetElement(1), (float)dvv1.GetElement(1), 0.0F));
                var isFrontFace = triangle.Normal.DotProduct(Vector256.Create(0.0, 0.0, 1.0, 0.0)).GetElement(0) <= 0.0;
                var vvEX = Vector128.Create((float)dvv2.GetElement(0), (float)dvv3.GetElement(0), (float)dvv1.GetElement(0), 0.0F);
                var vvEY = Vector128.Create((float)dvv2.GetElement(1), (float)dvv3.GetElement(1), (float)dvv1.GetElement(1), 0.0F);
                var hasLight = PointLights.Count > 0 || SpotLights.Count > 0 || AmbientLights.Count > 0;

                NManagedImage managedTexture;
                if (triangle.Texture is NCudaImage cudaImage)
                {
                    if (!convertedTexture.ContainsKey(cudaImage))
                    {
                        convertedTexture.Add(cudaImage, cudaImage.CopyToCpu());
                    }
                    managedTexture = convertedTexture[triangle.Texture];
                }
                else
                {
                    managedTexture = (NManagedImage)triangle.Texture;
                }
                Parallel.For(minY, maxY, y =>
                {
                    var renderImageSpan = MemoryMarshal.Cast<float, Vector4>(RenderImage.GetDataSpan());
                    var texture = MemoryMarshal.Cast<float, Vector4>(managedTexture.GetDataSpan());
                    var eY = Sse.Multiply(edgeX, Sse.Subtract(Vector128.Create(y, y, y, 0.0F), vvEY));

                    var offset = (y - OffsetY) * renderImageWidth;
                    var p = offset + (minX - OffsetX);
                    var eX = Sse.Subtract(Vector128.Create(minX, minX, minX, 0.0F), vvEX);
                    var addX = Vector128.Create(1.0F, 1.0F, 1.0F, 0.0F);

                    var pointLights = CollectionsMarshal.AsSpan(PointLights);
                    var spotLights = CollectionsMarshal.AsSpan(SpotLights);
                    var ambientLights = CollectionsMarshal.AsSpan(AmbientLights);

                    for (int x = minX; x < maxX; x++, p++, eX = Sse.Add(eX, addX))
                    {
                        var e = Sse.Multiply(Fma.IsSupported ? Fma.MultiplyAddNegated(edgeY, eX, eY) : Sse.Subtract(eY, Sse.Multiply(edgeY, eX)), denom);
                        if (!Avx.TestZ(Sse.CompareLessThan(e, Vector128<float>.Zero), Vector128.Create(float.NaN)))
                        {
                            continue;
                        }

                        var tw = Sse.Multiply(w, e).HorizontalAdd();
                        var tx = Sse.Divide(Sse.Multiply(u, e), tw).HorizontalAdd().GetElement(0) * textureWidth;
                        var ty = Sse.Divide(Sse.Multiply(v, e), tw).HorizontalAdd().GetElement(0) * textureHeight;

                        var color = ImageInterpolation.Bilinear(texture, textureWidth, textureHeight, tx, ty);

                        if (hasLight)
                        {
                            var diffuse = Vector4.Zero;
                            var specular = Vector4.Zero;
                            var ambient = Vector4.Zero;
                            var alpha = color.W;
                            var position = Sse.Shuffle(
                                Sse41.Blend(
                                    Sse.Multiply(vvX, e).HorizontalAdd(),
                                    Sse.Multiply(vvY, e).HorizontalAdd(),
                                    0b1010
                                ),
                                Sse.Multiply(vvZ, e).HorizontalAdd(),
                                0b01000100
                            );
                            var n = triangle.InvertNormal;
                            if (!isFrontFace)
                            {
                                n = -n;
                            }

                            for (var i = 0; i < PointLights.Count; i++)
                            {
                                var l = pointLights[i];
                                var lightColor = l.Color;
                                var lightDiff = Sse.Subtract(l.FloatPosition, position).AsVector3();
                                var light = Vector3.Normalize(lightDiff);
                                var falloff = CalcFalloff(lightDiff, l.FalloffType, l.FalloffStart, l.FalloffLength);
                                diffuse += lightColor * color * Math.Max(Vector3.Dot(light, n), 0.0F) * falloff;

                                var view = -Vector3.Normalize(position.AsVector3());
                                var reflect = Vector3.Reflect(-light, -n);
                                specular += Vector4.Lerp(lightColor, color, triangle.Metal) * MathF.Pow(Math.Max(Vector3.Dot(view, reflect), 0.0F), 1200.0F * triangle.Specular) * triangle.Mirror / falloff;
                            }

                            for (var i = 0; i < SpotLights.Count; i++)
                            {
                                var l = spotLights[i];
                                var lightColor = l.Color;
                                var lightDiff = Sse.Subtract(l.FloatPosition, position).AsVector3();
                                var light = Vector3.Normalize(lightDiff);
                                var spot = Vector3.Normalize(Sse.Subtract(l.FloatTarget, l.FloatPosition).AsVector3());
                                var spotCone = Math.Acos(Vector3.Dot(spot, -light));

                                if (spotCone <= l.OuterCone)
                                {
                                    var attenuation = 1.0F;
                                    if (!l.IsParallel && l.ConeAttenuationRate > 0.0)
                                    {
                                        attenuation = (float)(1.0 - Math.Clamp(Math.Max(spotCone - l.InnerCone, 0.0) / l.ConeAttenuationRate, 0.0, 1.0));
                                    }

                                    var falloff = CalcFalloff(lightDiff, l.FalloffType, l.FalloffStart, l.FalloffLength);
                                    diffuse += lightColor * color * Math.Max(Vector3.Dot(light, n), 0.0F) / falloff * attenuation;

                                    var view = -Vector3.Normalize(position.AsVector3());
                                    var reflect = Vector3.Reflect(-light, -n);
                                    specular += Vector4.Lerp(lightColor, color, triangle.Metal) * MathF.Pow(Math.Max(Vector3.Dot(view, reflect), 0.0F), 1200.0F * triangle.Specular) * triangle.Mirror / falloff * attenuation;
                                }
                            }

                            for (var i = 0; i < AmbientLights.Count; i++)
                            {
                                ambient += ambientLights[i].Color * color;
                            }

                            color = diffuse * triangle.Diffuse + specular + ambient * triangle.Ambient;
                            color.W = alpha;
                            color = Vector4.Max(Vector4.Min(color, Vector4.One), Vector4.Zero);
                        }

                        Blend.Process(triangle.BlendMode, renderImageSpan, color, p);
                    }
                });
            }

            foreach (var (_, i) in convertedTexture)
            {
                i.Dispose();
            }
        }

        IEnumerable<Triangle> GetClipAndDividedTriangles()
        {
            return ClipAndDivideTriangle(Triangles.Where(t => !t.IsInvalidNormal && !t.IsDegenerate()).ToArray());
        }

        IEnumerable<Triangle> ClipAndDivideTriangle(Triangle[] triangles)
        {
            var dividedTriangles = new List<Triangle>(DivideTriangles(triangles));

            var p = Vector256.Create(0.0, 0.0, NearZ, 0.0);
            var n = Vector256.Create(0.0, 0.0, -1.0, 0.0);
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

        IEnumerable<Triangle> DivideTriangles(IEnumerable<Triangle> triangles)
        {
            using var e = triangles.GetEnumerator();
            if (!e.MoveNext())
            {
                return triangles;
            }

            var divider = e.Current;
            var near = new List<Triangle>();
            var far = new List<Triangle>();

            while (e.MoveNext())
            {
                DivideTriangleByTriangle(e.Current, divider, near, far);
            }

            return DivideTriangles(far).Append(divider).Concat(DivideTriangles(near));
        }

        // from Javie
        void DivideTriangleByTriangle(in Triangle triangle, in Triangle divider, List<Triangle> near, List<Triangle> far)
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

        (Triangle, Triangle?, Triangle?) DivideTriangleByPlane(in Triangle triangle, in Vector256<double> p, in Vector256<double> n)
        {
            var Epsilon = -1E-10;
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
                        new Triangle(triangle, triangle.V1, ep1, triangle.V3),
                        new Triangle(triangle, ep1, ep2, triangle.V3),
                        new Triangle(triangle, ep1, triangle.V2, ep2)
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
                        new Triangle(triangle, triangle.V1, ep1, ep3),
                        new Triangle(triangle, ep1, triangle.V2, triangle.V3),
                        new Triangle(triangle, ep1, triangle.V3, ep3)
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
                    new Triangle(triangle, triangle.V1, triangle.V2, ep2),
                    new Triangle(triangle, triangle.V1, ep2, ep3),
                    new Triangle(triangle, ep2, triangle.V3, ep3)
                );
            }
            else
            {
                return (triangle, null, null);
            }
        }

        Vector256<double> MaxByAbs(in Vector256<double> a, in Vector256<double> b)
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

        static int MinClampedSize(int a, int max)
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

        static int MaxClampedSize(int a, int min)
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

        static Vector256<double> RoundCurrentDirection(in Vector256<double> v, int decimals)
        {
            var pow = Vector256.Create(Math.Pow(10.0, decimals));
            return Avx.Divide(Avx.RoundCurrentDirection(Avx.Multiply(v, pow)), pow);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float CalcFalloff(in Vector3 diff, LightFalloffType type, float falloffStart, float falloffLength)
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
                LightFalloffType.Exponential => 1.0F / Math.Min(MathF.Pow(length, 2.0F), 1.0F),
                _ => 1.0F
            };
        }
    }
}
