using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Numerics;

namespace NiVE3.PresetPlugin.Internal.Drawing.Primitive3D
{
    public readonly struct UVVertex
    {
        public readonly Vector256<double> Vertex;

        public readonly double U;

        public readonly double V;

        public UVVertex()
        {
            Vertex = Vector256<double>.Zero;
            U = 0.0F;
            V = 0.0F;
        }

        public UVVertex(in Vector256<double> vertex, double u, double v)
        {
            Vertex = vertex;
            U = u;
            V = v;
        }

        public UVVertex Transform(Matrix4x4d matrix)
        {
            return new UVVertex(matrix.Transform(Vertex), U, V);
        }

        public static UVVertex operator *(in UVVertex v, double s)
        {
            return new UVVertex(Avx.Multiply(v.Vertex, Vector256.Create(s)), v.U * s, v.V * s);
        }

        public static UVVertex operator /(in UVVertex v, double s)
        {
            return new UVVertex(Avx.Divide(v.Vertex, Vector256.Create(s)), v.U / s, v.V / s);
        }
    }
}
