using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;
using NiVE3.PresetPlugin.Internal.Drawing.Primitive3D;
using System.Runtime.CompilerServices;
using NiVE3.Shared.Extension;
using NiVE3.PresetPlugin.Internal.Util;

namespace NiVE3.PresetPlugin.Internal.Drawing
{
    static class TriangleDivider
    {
        // ビュー空間に持って行った後のカメラよりも後ろの部分のトリミング用ニアクリップ面
        //NOTE: 影用にカメラよりも前にニアクリップ面を持ってきているが、他含め描画上問題が
        //      あったら調整する(現状、カメラのZ -2666.66に対し、レイヤーのZ -2666.57で消える)
        public const float NearZ = 5E-5F;

        public const float Epsilon = 1E-7F;

        public static IEnumerable<T> ClipAndDivide<T>(IEnumerable<T> triangles) where T : TriangleBase<T>
        {
            return ClipAndDivideTriangle(triangles.Where(t => !t.IsInvalidNormal && !t.IsDegenerate()).ToArray())
                // NOTE: ポリゴンの欠けが出たら消す事を検討する
                .Where(t => t.V1.Vertex.GetElement(2) > NearZ - Epsilon && t.V2.Vertex.GetElement(2) > NearZ - Epsilon && t.V3.Vertex.GetElement(2) > NearZ - Epsilon);
        }

        static IEnumerable<T> ClipAndDivideTriangle<T>(T[] triangles) where T : TriangleBase<T>
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

        static IEnumerable<T> DivideTriangles<T>(IEnumerable<T> triangles) where T : TriangleBase<T>
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

            var dSign = Math.Sign(divider.PlaneD);
            var n = divider.Normal;
            var planeD = Vector256.Create(divider.PlaneD, divider.PlaneD, divider.PlaneD, 0.0F);
            var dd1 = (n.DotProduct(triangle.V1.Vertex) + planeD) & Consts.WithoutWMask;
            var dd2 = (n.DotProduct(triangle.V2.Vertex) + planeD) & Consts.WithoutWMask;
            var dd3 = (n.DotProduct(triangle.V3.Vertex) + planeD) & Consts.WithoutWMask;
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

                    var td1 = (t.V1.Vertex.DotProduct(n) + planeD) & Consts.WithoutWMask;
                    var td2 = (t.V2.Vertex.DotProduct(n) + planeD) & Consts.WithoutWMask;
                    var td3 = (t.V3.Vertex.DotProduct(n) + planeD) & Consts.WithoutWMask;

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

            var p12 = p2 - p1;
            var p23 = p3 - p2;
            var p31 = p1 - p3;

            var planeD = p.DotProduct(n);

            var d1 = (p1 - p).DotProduct(n).GetElement(0);
            var d2 = (p2 - p).DotProduct(n).GetElement(0);
            var d3 = (p3 - p).DotProduct(n).GetElement(0);

            if (d1 * d2 <= Epsilon)
            {
                var dt1 = RoundCurrentDirection((planeD - n.DotProduct(p1)) / n.DotProduct(p12), 10);
                var ep1 = new UVVertex(
                    Avx.Blend(Fma.IsSupported ? Fma.MultiplyAdd(p12, dt1, p1) : (p1 + (p12 * dt1)), One, 0b1000),
                    triangle.V1.U + (triangle.V2.U - triangle.V1.U) * dt1.GetElement(0),
                    triangle.V1.V + (triangle.V2.V - triangle.V1.V) * dt1.GetElement(0)
                );

                if (d2 * d3 <= Epsilon)
                {
                    var dt2 = RoundCurrentDirection((planeD - n.DotProduct(p2)) / n.DotProduct(p23), 10);
                    var ep2 = new UVVertex(
                        Avx.Blend(Fma.IsSupported ? Fma.MultiplyAdd(p23, dt2, p2) : (p2 + (p23 * dt2)), One, 0b1000),
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
                    var dt3 = RoundCurrentDirection((planeD - n.DotProduct(p3)) / n.DotProduct(p31), 10);
                    var ep3 = new UVVertex(
                        Avx.Blend(Fma.IsSupported ? Fma.MultiplyAdd(p31, dt3, p3) : (p3 + (p31 * dt3)), One, 0b1000),
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
                var dt2 = RoundCurrentDirection((planeD - n.DotProduct(p2)) / n.DotProduct(p23), 10);
                var ep2 = new UVVertex(
                    Avx.Blend(Fma.IsSupported ? Fma.MultiplyAdd(p23, dt2, p2) : (p2 + (p23 * dt2)), One, 0b1000),
                    triangle.V2.U + (triangle.V3.U - triangle.V2.U) * dt2.GetElement(0),
                    triangle.V2.V + (triangle.V3.V - triangle.V2.V) * dt2.GetElement(0)
                );
                var dt3 = RoundCurrentDirection((planeD - n.DotProduct(p3)) / n.DotProduct(p31), 10);
                var ep3 = new UVVertex(
                    Avx.Blend(Fma.IsSupported ? Fma.MultiplyAdd(p31, dt3, p3) : (p3 + (p31 * dt3)), One, 0b1000),
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector256<double> RoundCurrentDirection(in Vector256<double> v, int decimals)
        {
            var pow = Vector256.Create(Math.Pow(10.0, decimals));
            return Avx.RoundCurrentDirection(v * pow) / pow;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector256<double> MaxByAbs(in Vector256<double> a, in Vector256<double> b)
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
    }
}
