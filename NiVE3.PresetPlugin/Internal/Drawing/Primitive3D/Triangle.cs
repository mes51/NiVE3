using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Shared.Extension;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Image;
using NiVE3.Plugin.Numerics;

namespace NiVE3.PresetPlugin.Internal.Drawing.Primitive3D
{
    abstract class TriangleBase<T> where T : TriangleBase<T>
    {
        public readonly UVVertex V1;

        public readonly UVVertex V2;

        public readonly UVVertex V3;

        public readonly NImage Texture;

        public readonly int Id;

        public readonly float Opacity;

        public readonly bool IsCastShadow;

        public readonly bool SignIsDifferent;

        public readonly bool IsInvalidNormal;

        public readonly float LightTransmission;

        public readonly Vector256<double> Normal;

        public readonly Vector3 FloatNormal;

        public readonly Vector3 InvertNormal;

        public readonly double PlaneD;

        readonly Vector256<double> FarPoint;

        readonly Matrix4x4d InvertMatrix;

        protected TriangleBase(in UVVertex v1, in UVVertex v2, in UVVertex v3, in Vector256<double> farPoint, Matrix4x4d invertMatrix, NImage texture, float opacity, bool isCastShadow, float lightTransmission, int id)
        {
            V1 = v1;
            V2 = v2;
            V3 = v3;
            Texture = texture;
            Opacity = opacity;
            IsCastShadow = isCastShadow;
            LightTransmission = lightTransmission;
            Id = id;
            FarPoint = farPoint;
            InvertMatrix = invertMatrix;

            Normal = Avx.Subtract(v2.Vertex, v1.Vertex).CrossProduct(Avx.Subtract(v3.Vertex, v1.Vertex)).Normalize();
            FloatNormal = Normal.AsVector3();
            if (!double.IsNaN(Normal.GetElement(0)) && !double.IsNaN(Normal.GetElement(1)) && !double.IsNaN(Normal.GetElement(2)))
            {
                var invertedNormal = invertMatrix.Transform(Vector256.Create(0.0, 0.0, -1.0, 0.0));
                InvertNormal = -Avx.Subtract(invertedNormal, Vector256.Create(0.0, 0.0, 0.0, invertedNormal.GetElement(3))).Normalize().AsVector3();
                PlaneD = -Normal.DotProduct(v1.Vertex).GetElement(0);

                SignIsDifferent = Math.Sign(PlaneD) != Math.Sign(Normal.DotProduct(farPoint).GetElement(0) - PlaneD);
                IsInvalidNormal = false;
            }
            else
            {
                IsInvalidNormal = true;
            }
        }

        protected TriangleBase(TriangleBase<T> baseTriangle, in UVVertex v1, in UVVertex v2, in UVVertex v3)
        {
            V1 = v1;
            V2 = v2;
            V3 = v3;
            Texture = baseTriangle.Texture;
            Opacity = baseTriangle.Opacity;
            IsCastShadow = baseTriangle.IsCastShadow;
            LightTransmission = baseTriangle.LightTransmission;
            Id = baseTriangle.Id;
            FarPoint = baseTriangle.FarPoint;
            InvertMatrix = baseTriangle.InvertMatrix;

            Normal = Avx.Subtract(v2.Vertex, v1.Vertex).CrossProduct(Avx.Subtract(v3.Vertex, v1.Vertex)).Normalize();
            if (!double.IsNaN(Normal.GetElement(0)) && !double.IsNaN(Normal.GetElement(1)) && !double.IsNaN(Normal.GetElement(2)))
            {
                FloatNormal = Normal.AsVector3();
                var invertedNormal = InvertMatrix.Transform(Vector256.Create(0.0, 0.0, -1.0, 0.0));
                InvertNormal = -Avx.Subtract(invertedNormal, Vector256.Create(0.0, 0.0, 0.0, invertedNormal.GetElement(3))).Normalize().AsVector3();
                PlaneD = -Normal.DotProduct(v1.Vertex).GetElement(0);

                SignIsDifferent = Math.Sign(PlaneD) != Math.Sign(Normal.DotProduct(baseTriangle.FarPoint).GetElement(0) - PlaneD);
                IsInvalidNormal = false;
            }
            else
            {
                IsInvalidNormal = true;
            }
        }

        public bool IsDegenerate()
        {
            var v1 = Avx.ExtractVector128(V1.Vertex, 0);
            var v2 = Avx.ExtractVector128(V2.Vertex, 0);
            var v3 = Avx.ExtractVector128(V3.Vertex, 0); //new Vector2(V3.Vertex.X, V3.Vertex.Y);
            var a = Math.Sqrt(Sse41.DotProduct(v2, v3, 0b01110111).GetElement(0));
            var b = Math.Sqrt(Sse41.DotProduct(v1, v3, 0b01110111).GetElement(0));
            var c = Math.Sqrt(Sse41.DotProduct(v1, v2, 0b01110111).GetElement(0));
            var s = (a + b + c) * 0.5F;

            return s == 0.0F || s == a || s == b || s == c;
        }

        public bool IsClipped()
        {
            return V1.Vertex.GetElement(3) <= 0.0 || V2.Vertex.GetElement(3) <= 0.0 || V3.Vertex.GetElement(3) <= 0.0;
        }

        public abstract T CreateByNewVertex(in UVVertex v1, in UVVertex v2, in UVVertex v3);
    }

    class Triangle : TriangleBase<Triangle>
    {
        public readonly BlendMode BlendMode;

        public readonly bool IsAcceptShadow;

        public readonly bool IsAcceptLight;

        public readonly float Ambient;

        public readonly float Diffuse;

        public readonly float SpecularIntensity;

        public readonly float SpecularShininess;

        public readonly float Metal;

        public Triangle(in UVVertex v1, in UVVertex v2, in UVVertex v3, in Vector256<double> farPoint, in Matrix4x4d invertMatrix, NImage texture, float opacity, BlendMode blendMode, bool isCastShadow, float lightTransmission, bool isAcceptShadow, bool isAcceptLight, float ambient, float diffuse, float specularIntensity, float specularShininess, float metal, int id)
            : base(v1, v2, v3, farPoint, invertMatrix, texture, opacity, isCastShadow, lightTransmission, id)
        {
            BlendMode = blendMode;
            IsAcceptShadow = isAcceptShadow;
            IsAcceptLight = isAcceptLight;
            Ambient = ambient;
            Diffuse = diffuse;
            SpecularIntensity = specularIntensity;
            SpecularShininess = specularShininess;
            Metal = metal;
        }

        public Triangle(in Triangle baseTriangle, in UVVertex v1, in UVVertex v2, in UVVertex v3) : base(baseTriangle, v1, v2, v3)
        {
            BlendMode = baseTriangle.BlendMode;
            IsAcceptShadow = baseTriangle.IsAcceptShadow;
            IsAcceptLight = baseTriangle.IsAcceptLight;
            Ambient = baseTriangle.Ambient;
            Diffuse = baseTriangle.Diffuse;
            SpecularIntensity = baseTriangle.SpecularIntensity;
            SpecularShininess = baseTriangle.SpecularShininess;
            Metal = baseTriangle.Metal;
        }

        public override Triangle CreateByNewVertex(in UVVertex v1, in UVVertex v2, in UVVertex v3)
        {
            return new Triangle(this, v1, v2, v3);
        }
    }

    class LightTriangle : TriangleBase<LightTriangle>
    {
        public LightTriangle(in UVVertex v1, in UVVertex v2, in UVVertex v3, in Vector256<double> farPoint, in Matrix4x4d invertMatrix, NImage texture, float opacity, bool isCastShadow, float lightTransmission, int id)
            : base(v1, v2, v3, farPoint, invertMatrix, texture, opacity, isCastShadow, lightTransmission, id) { }

        public LightTriangle(LightTriangle baseTriangle, in UVVertex v1, in UVVertex v2, in UVVertex v3)
            : base(baseTriangle, v1, v2, v3) { }

        public override LightTriangle CreateByNewVertex(in UVVertex v1, in UVVertex v2, in UVVertex v3)
        {
            return new LightTriangle(this, v1, v2, v3);
        }
    }
}
