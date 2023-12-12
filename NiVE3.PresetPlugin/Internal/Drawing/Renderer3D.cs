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
using System.Buffers;

namespace NiVE3.PresetPlugin.Internal.Drawing
{
    class Renderer3D
    {
        const float NearZ = -5E-5F; //1.0F;

        public Matrix4x4d ViewMatrix { get; set; }

        public double FieldOfView { get; set; }

        public int Size { get; }

        int OffsetX { get; }

        int OffsetY { get; }

        int LastId { get; set; } = 1;

        NManagedImage RenderImage { get; }

        List<PointLight> PointLights { get; }

        List<SpotLight> SpotLights { get; }

        List<ParallelLight> ParallelLights { get; }

        List<AmbientLight> AmbientLights { get; }

        List<Triangle> Triangles { get; } = new List<Triangle>();

        Dictionary<object, List<LightTriangle>> LightTriangles { get; }

        public Renderer3D(NManagedImage renderImage, List<PointLight> pointLights, List<SpotLight> spotLights, List<ParallelLight> parallelLights, List<AmbientLight> ambientLights)
        {
            Size = Math.Max(renderImage.Width, renderImage.Height);
            OffsetX = (Size - renderImage.Width) / 2;
            OffsetY = (Size - renderImage.Height) / 2;
            RenderImage = renderImage;
            PointLights = pointLights;
            SpotLights = spotLights;
            ParallelLights = parallelLights;
            AmbientLights = ambientLights;

            LightTriangles = pointLights.Cast<object>().Concat(spotLights).Concat(parallelLights).ToDictionary(k => k, _ => new List<LightTriangle>());
        }

        public void AddRect(NImage texture, float opacity, BlendMode blendType, in Matrix4x4d modelMatrix, bool isCastShadow, float lightTransmission, bool isAcceptShadow, bool isAcceptLight, float ambient, float diffuse, float specularIntensity, float specularShininess, float metal)
        {
            var width = texture.Width;
            var height = texture.Height;
            var offsetX = (Size - RenderImage.Width) * 0.5 / Size;
            var offsetY = (Size - RenderImage.Height) * 0.5 / Size;
            var sv1 = Avx.Divide(Vector256.Create(0.0, 0.0, 0.0, Size), Vector256.Create((double)Size));
            var sv2 = Avx.Divide(Vector256.Create(0.0, height, 0.0, Size), Vector256.Create((double)Size));
            var sv3 = Avx.Divide(Vector256.Create(width, height, 0.0, Size), Vector256.Create((double)Size));
            var sv4 = Avx.Divide(Vector256.Create(width, 0.0, 0.0, Size), Vector256.Create((double)Size));

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
            Triangles.Add(new Triangle(uv1, uv2, uv3, farPoint, invertedModelViewMatrix, texture, opacity, blendType, isCastShadow, lightTransmission, isAcceptShadow, isAcceptLight, ambient, diffuse, specularIntensity, specularShininess, metal, LastId));
            Triangles.Add(new Triangle(uv1, uv3, uv4, farPoint, invertedModelViewMatrix, texture, opacity, blendType, isCastShadow, lightTransmission, isAcceptShadow, isAcceptLight, ambient, diffuse, specularIntensity, specularShininess, metal, LastId));

            if (isCastShadow)
            {
                foreach (var spotLight in SpotLights)
                {
                    var lmv = modelMatrix * spotLight.LightViewMatrix;
                    var lmvt = lmv * Matrix4x4d.CreateTranslate(offsetX, offsetY, 0.0);
                    var lv1 = lmvt.Transform(sv1);
                    var lv2 = lmvt.Transform(sv2);
                    var lv3 = lmvt.Transform(sv3);
                    var lv4 = lmvt.Transform(sv4);

                    var luv1 = new UVVertex(lv1, 0.0F, 0.0F);
                    var luv2 = new UVVertex(lv2, 0.0F, 1.0F);
                    var luv3 = new UVVertex(lv3, 1.0F, 1.0F);
                    var luv4 = new UVVertex(lv4, 1.0F, 0.0F);

                    Matrix4x4d.Invert(mv, out var invertedLightModelViewMatrix);
                    invertedLightModelViewMatrix = Matrix4x4d.Transpose(invertedLightModelViewMatrix);

                    var triangles = LightTriangles[spotLight];
                    var lfarPoint = Avx.And(lmv.Transform(Vector256.Create(0.0, 0.0, -10000.0, 1.0)), Vector256.Create(0xFFFFFFFFFFFFFFFFUL, 0xFFFFFFFFFFFFFFFFUL, 0xFFFFFFFFFFFFFFFFUL, 0).AsDouble());
                    triangles.Add(new LightTriangle(luv1, luv2, luv3, lfarPoint, invertedLightModelViewMatrix, texture, opacity, lightTransmission, LastId));
                    triangles.Add(new LightTriangle(luv1, luv3, luv4, lfarPoint, invertedLightModelViewMatrix, texture, opacity, lightTransmission, LastId));
                }
            }

            LastId++;
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
            var hasLight = PointLights.Count > 0 || SpotLights.Count > 0 || ParallelLights.Count > 0 || AmbientLights.Count > 0;

            var spotLightShadows = SpotLights.Where(l => l.IsEnableShadow && LightTriangles[l].Count > 0).ToDictionary(l => l, l => RenderShadow(l, Size, (float)offsetX, (float)offsetY));

            foreach (var triangle in triangles)
            {
                if (triangle.V1.Vertex.GetElement(2) < 0.0F || triangle.V2.Vertex.GetElement(2) < 0.0F || triangle.V3.Vertex.GetElement(2) < 0.0F)
                {
                    continue;
                }

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
                var pvvX = Vector128.Create((float)uv1.Vertex.GetElement(0), (float)uv2.Vertex.GetElement(0), (float)uv3.Vertex.GetElement(0), 0.0F);
                var pvvY = Vector128.Create((float)uv1.Vertex.GetElement(1), (float)uv2.Vertex.GetElement(1), (float)uv3.Vertex.GetElement(1), 0.0F);
                var pvvZ = Vector128.Create((float)uv1.Vertex.GetElement(2), (float)uv2.Vertex.GetElement(2), (float)uv3.Vertex.GetElement(2), 0.0F);
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
                var isFrontFace = triangle.Normal.DotProduct(Vector256.Create(0.0, 0.0, 1.0, 0.0)).GetElement(0) <= 0.0;
                var vvEX = Vector128.Create((float)dvv2.GetElement(0), (float)dvv3.GetElement(0), (float)dvv1.GetElement(0), 0.0F);
                var vvEY = Vector128.Create((float)dvv2.GetElement(1), (float)dvv3.GetElement(1), (float)dvv1.GetElement(1), 0.0F);
                var useLight = hasLight && (triangle.IsAcceptLight || triangle.IsAcceptShadow);

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
                    var parallelLights = CollectionsMarshal.AsSpan(ParallelLights);
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
                        color.W *= triangle.Opacity;
                        if (color.W <= 0.0F)
                        {
                            continue;
                        }

                        if (useLight)
                        {
                            const float ShininessStrength = 120.0F;

                            // TODO: IsAcceptLight == false && IsCastShadow == trueの時の処理
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
                                Sse41.Blend(
                                    Sse.Multiply(vvZ, e).HorizontalAdd(),
                                    Vector128.Create(1.0F),
                                    0b1010
                                ),
                                0b01000100
                            );
                            var n = -triangle.FloatNormal;
                            if (!isFrontFace)
                            {
                                n = -n;
                            }
                            var shadowProjectionPos = Vector4.Zero;
                            if (spotLightShadows.Count > 0)
                            {
                                shadowProjectionPos = Sse.Shuffle(
                                    Sse41.Blend(
                                        Sse.Multiply(pvvX, e).HorizontalAdd(),
                                        Sse.Multiply(pvvY, e).HorizontalAdd(),
                                        0b1010
                                    ),
                                    Sse41.Blend(
                                        Sse.Multiply(pvvZ, e).HorizontalAdd(),
                                        Vector128.Create(1.0F),
                                        0b1010
                                    ),
                                    0b01000100
                                ).AsVector4();
                            }

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
                                    if (triangle.IsAcceptShadow && spotLightShadows.TryGetValue(l, out var shadow))
                                    {
                                        var (depth, depthIds, lightViewProjectionMatrix) = shadow;
                                        var shadowPos = Vector4.Transform(Vector4.Transform(shadowProjectionPos, floatInvtededViewMatrix), lightViewProjectionMatrix);
                                        shadowPos /= shadowPos.W;
                                        var shadowTexPos = shadowPos * 0.5F + new Vector4(0.5F, 0.5F, 0.0F, 0.0F);

                                        var shadowTextureX = (int)(shadowTexPos.X * Size);
                                        var shadowTextureY = (int)(shadowTexPos.Y * Size);

                                        if (shadowTextureX > -1 && shadowTextureX < Size && shadowTextureY > -1 && shadowTextureY < Size)
                                        {
                                            var si = shadowTextureY * Size + shadowTextureX;
                                            // NOTE: ポリゴンが無かったかどうかを判定する必要がある場合は depthIds[si] != 0 で判定する
                                            if (depthIds[si] != triangle.Id && depth[si] > shadowPos.Z)
                                            {
                                                continue;
                                            }
                                        }
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

                        Blend.Process(triangle.BlendMode, renderImageSpan, color, p);
                    }
                });
            }

            foreach (var (_, i) in convertedTexture)
            {
                i.Dispose();
            }

            foreach (var s in spotLightShadows.Values)
            {
                s.Dispose();
            }
        }

        DepthMap RenderShadow(SpotLight spotLight, int size, float offsetX, float offsetY)
        {
            var triangles = GetClipAndDividedTriangles(LightTriangles[spotLight]).ToArray();
            var convertedTexture = new Dictionary<NImage, NManagedImage>();
            var renderedTriangleIds = ArrayPool<int>.Shared.Rent(size * size);
            var depth = ArrayPool<float>.Shared.Rent(size * size);
            depth.AsSpan().Fill(float.NegativeInfinity);

            var minZ = triangles.Select(t => Math.Min(Math.Min(t.V1.Vertex.GetElement(2), t.V2.Vertex.GetElement(2)), t.V3.Vertex.GetElement(2))).Min();
            var maxZ = triangles.Select(t => Math.Max(Math.Max(t.V1.Vertex.GetElement(2), t.V2.Vertex.GetElement(2)), t.V3.Vertex.GetElement(2))).Max();
            var lightProjectionMatrix = Matrix4x4d.CreatePerspectiveFieldOfView(spotLight.ConeRadian, 1.0, minZ, maxZ);
            var floatLightProjectionMatrix = (Matrix4x4)lightProjectionMatrix;

            foreach (var triangle in triangles)
            {
                if (triangle.V1.Vertex.GetElement(2) < 0.0F || triangle.V2.Vertex.GetElement(2) < 0.0F || triangle.V3.Vertex.GetElement(2) < 0.0F)
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
                var dvv1 = Avx.Multiply(Avx.Add(uv1.Vertex, Vector256.Create(1.0, 1.0, 0.0, 0.0)), Vector256.Create(Size * 0.5, Size * 0.5, 1.0, 1.0));
                var dvv2 = Avx.Multiply(Avx.Add(uv2.Vertex, Vector256.Create(1.0, 1.0, 0.0, 0.0)), Vector256.Create(Size * 0.5, Size * 0.5, 1.0, 1.0));
                var dvv3 = Avx.Multiply(Avx.Add(uv3.Vertex, Vector256.Create(1.0, 1.0, 0.0, 0.0)), Vector256.Create(Size * 0.5, Size * 0.5, 1.0, 1.0));
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
                var isFrontFace = triangle.Normal.DotProduct(Vector256.Create(0.0, 0.0, 1.0, 0.0)).GetElement(0) <= 0.0;
                var vvEX = Vector128.Create((float)dvv2.GetElement(0), (float)dvv3.GetElement(0), (float)dvv1.GetElement(0), 0.0F);
                var vvEY = Vector128.Create((float)dvv2.GetElement(1), (float)dvv3.GetElement(1), (float)dvv1.GetElement(1), 0.0F);

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
                    var texture = MemoryMarshal.Cast<float, Vector4>(managedTexture.GetDataSpan());
                    var eY = Sse.Multiply(edgeX, Sse.Subtract(Vector128.Create(y, y, y, 0.0F), vvEY));

                    var offset = y * size;
                    var eX = Sse.Subtract(Vector128.Create(minX, minX, minX, 0.0F), vvEX);
                    var addX = Vector128.Create(1.0F, 1.0F, 1.0F, 0.0F);

                    var depthSpan = depth.AsSpan(offset, size);
                    var idSpan = renderedTriangleIds.AsSpan(offset, size);

                    for (int x = minX; x < maxX; x++, eX = Sse.Add(eX, addX))
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

                        if (color.W <= 0.0F)
                        {
                            continue;
                        }

                        var d = Sse.Shuffle(
                            Sse41.Blend(
                                Sse.Multiply(vvX, e).HorizontalAdd(),
                                Sse.Multiply(vvY, e).HorizontalAdd(),
                                0b1010
                            ),
                            Sse41.Blend(
                                Sse.Multiply(vvZ, e).HorizontalAdd(),
                                Vector128.Create(1.0F),
                                0b1010
                            ),
                            0b01000100
                        ).AsVector4();

                        depthSpan[x] = d.Z;
                        idSpan[x] = triangle.Id;
                    }
                });
            }

            foreach (var (_, i) in convertedTexture)
            {
                i.Dispose();
            }

            return new DepthMap(depth, renderedTriangleIds, spotLight.FloatLightViewMatrix * Matrix4x4.CreateTranslation(offsetX, offsetY, 0.0F) * floatLightProjectionMatrix);
        }

        IEnumerable<T> GetClipAndDividedTriangles<T>(IEnumerable<T> triangles) where T : TriangleBase<T>
        {
            return ClipAndDivideTriangle(triangles.Where(t => !t.IsInvalidNormal && !t.IsDegenerate()).ToArray());
        }

        IEnumerable<T> ClipAndDivideTriangle<T>(T[] triangles) where  T : TriangleBase<T>
        {
            var dividedTriangles = new List<T>(DivideTriangles(triangles));

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
        void DivideTriangleByTriangle<T>(T triangle, T divider, List<T> near, List<T> far) where T : TriangleBase<T>
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

        (T, T?, T?) DivideTriangleByPlane<T>(T triangle, in Vector256<double> p, in Vector256<double> n) where T : TriangleBase<T>
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
                LightFalloffType.Exponential => Math.Min(1.0F / MathF.Pow(1.0F + length, 2.0F), 1.0F),
                _ => 1.0F
            };
        }
    }

    record DepthMap(float[] Depth, int[] DepthId, Matrix4x4 LightViewProjectionMatrix) : IDisposable
    {
        public readonly float[] Depth = Depth;

        public readonly int[] DepthId = DepthId;

        public readonly Matrix4x4 LightViewProjectionMatrix = LightViewProjectionMatrix;

        public void Dispose()
        {
            ArrayPool<float>.Shared.Return(Depth);
            ArrayPool<int>.Shared.Return(DepthId, true);
        }
    }
}
