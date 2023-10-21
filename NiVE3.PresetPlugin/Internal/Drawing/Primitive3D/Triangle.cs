using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Struct;
using NiVE3.Shared.Extension;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Image;

namespace NiVE3.PresetPlugin.Internal.Drawing.Primitive3D
{
    class Triangle
    {
        public readonly UVVertex V1;

        public readonly UVVertex V2;

        public readonly UVVertex V3;

        public readonly Vector256<double> Normal;

        public readonly Vector3 InvertNormal;

        public readonly double PlaneD;

        public readonly NImage Texture;

        public readonly BlendMode BlendMode;

        public readonly float Diffuse;

        public readonly float Ambient;

        public readonly float Mirror;

        public readonly float Specular;

        public readonly float Metal;

        public readonly int Id;

        public readonly bool SignIsDifferent;

        readonly Vector256<double> FarPoint;

        readonly Matrix4x4d InvertMatrix;

        public Triangle(in UVVertex v1, in UVVertex v2, in UVVertex v3, in Vector256<double> farPoint, in Matrix4x4d invertMatrix, NImage texture, BlendMode blendMode, float diffuse, float ambient, float mirror, float specular, float metal, int id)
        {
            V1 = v1;
            V2 = v2;
            V3 = v3;
            Texture = texture;
            BlendMode = blendMode;
            Diffuse = diffuse;
            Ambient = ambient;
            Mirror = mirror;
            Specular = specular;
            Metal = metal;
            Id = id;
            FarPoint = farPoint;
            InvertMatrix = invertMatrix;

            Normal = Avx.Subtract(v2.Vertex, v1.Vertex).CrossProduct(Avx.Subtract(v3.Vertex, v1.Vertex)).Normalize();
            var invertedNormal = InvertMatrix.Transform(Vector256.Create(0.0, 0.0, -1.0, 0.0));
            InvertNormal = -Avx.Subtract(invertedNormal, Vector256.Create(0.0, 0.0, 0.0, invertedNormal.GetElement(3))).Normalize().AsVector3();
            PlaneD = -Normal.DotProduct(v1.Vertex).GetElement(0);

            SignIsDifferent = Math.Sign(PlaneD) != Math.Sign(Normal.DotProduct(farPoint).GetElement(0) - PlaneD);
        }

        public Triangle(in Triangle baseTriangle, in UVVertex v1, in UVVertex v2, in UVVertex v3)
        {
            V1 = v1;
            V2 = v2;
            V3 = v3;
            Texture = baseTriangle.Texture;
            BlendMode = baseTriangle.BlendMode;
            Diffuse = baseTriangle.Diffuse;
            Ambient = baseTriangle.Ambient;
            Mirror = baseTriangle.Mirror;
            Specular = baseTriangle.Specular;
            Metal = baseTriangle.Metal;
            Id = baseTriangle.Id;
            FarPoint = baseTriangle.FarPoint;
            InvertMatrix = baseTriangle.InvertMatrix;

            Normal = Avx.Subtract(v2.Vertex, v1.Vertex).CrossProduct(Avx.Subtract(v3.Vertex, v1.Vertex)).Normalize();
            var invertedNormal = InvertMatrix.Transform(Vector256.Create(0.0, 0.0, -1.0, 0.0));
            InvertNormal = -Avx.Subtract(invertedNormal, Vector256.Create(0.0, 0.0, 0.0, invertedNormal.GetElement(3))).Normalize().AsVector3();
            PlaneD = -Normal.DotProduct(v1.Vertex).GetElement(0);

            SignIsDifferent = Math.Sign(PlaneD) != Math.Sign(Normal.DotProduct(baseTriangle.FarPoint).GetElement(0) - PlaneD);
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
    }
}
