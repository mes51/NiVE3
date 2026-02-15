using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Shared.Extension;

namespace NiVE3.Numerics
{
    public readonly record struct QuaternionD(double X, double Y, double Z, double W)
    {
        public static readonly QuaternionD Identity = new QuaternionD(0, 0, 0, 1.0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public QuaternionD Normalize()
        {
            var v = Unsafe.BitCast<QuaternionD, Vector256<double>>(this);
            return Unsafe.BitCast<Vector256<double>, QuaternionD>(v.Normalize());
        }

        /// <summary>
        /// ノーマライズされたクオータニオンからZYXの順で回転するオイラー角を計算します
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3d ToEular()
        {
            // SEE: https://qiita.com/aa_debdeb/items/abe90a9bd0b4809813da#%E3%82%AF%E3%82%A9%E3%83%BC%E3%82%BF%E3%83%8B%E3%82%AA%E3%83%B3%E3%81%8B%E3%82%89%E3%82%AA%E3%82%A4%E3%83%A9%E3%83%BC%E8%A7%92
            var sy = -(2.0 * X * Z - 2.0 * Y * W);
            var y = Math.Asin(sy);
            if (sy < 0.999999999)
            {
                var x = Math.Atan2(2.0 * Y * Z + 2.0 * X * W, 2.0 * W * W + 2.0 * Z * Z - 1.0);
                var z = Math.Atan2(2.0 * X * Y + 2.0 * Z * W, 2.0 * W * W + 2.0 * X * X - 1.0);
                return new Vector3d(x, y, z);
            }
            else
            {
                var z = Math.Atan2(-(2.0 * X * Y - 2.0 * Z * W), 2.0 * W * W + 2.0 * Y * Y - 1.0);
                return new Vector3d(0.0, y, z);
            }
        }

        /// <summary>
        /// ZYXの順で回転するクオータニオンを作成します
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static QuaternionD FromEuler(double x, double y, double z)
        {
            // SEE: https://qiita.com/aa_debdeb/items/abe90a9bd0b4809813da#%E3%82%AA%E3%82%A4%E3%83%A9%E3%83%BC%E8%A7%92%E3%81%8B%E3%82%89%E3%82%AF%E3%82%A9%E3%83%BC%E3%82%BF%E3%83%8B%E3%82%AA%E3%83%B3
            var sx = Math.Sin(x * 0.5);
            var sy = Math.Sin(y * 0.5);
            var sz = Math.Sin(z * 0.5);
            var cx = Math.Cos(x * 0.5);
            var cy = Math.Cos(y * 0.5);
            var cz = Math.Cos(z * 0.5);

            return new QuaternionD(
                sx * cy * cz - cx * sy * sz,
                sx * cy * sz + cx * sy * cz,
                -sx * sy * cz + cx * cy * sz,
                sx * sy * sz + cx * cy * cz
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static QuaternionD FromRotationMatrix(in Matrix4x4d matrix)
        {
            // SEE: https://github.com/dotnet/runtime/blob/35d27803b83bbd46d3cfe42aa58f46f585c0c6ae/src/libraries/System.Private.CoreLib/src/System/Numerics/Quaternion.cs#L228
            var trace = matrix.M11 + matrix.M22 + matrix.M33;

            var v = Vector256<double>.Zero;

            if (trace > 0.0)
            {
                var s = Math.Sqrt(trace + 1.0);
                var sv = Vector256.Create(0.5, 0.5, 0.5, s * 0.5) / Vector256.Create(s, s, s, 1.0);
                v = Vector256.Create(matrix.M23 - matrix.M32, matrix.M31 - matrix.M13, matrix.M12 - matrix.M21, 1.0) * sv;
            }
            else if (matrix.M11 >= matrix.M22 && matrix.M11 >= matrix.M33)
            {
                var s = Math.Sqrt(1.0 + matrix.M11 - matrix.M22 - matrix.M33);
                var sv = Vector256.Create(0.5 * s, 0.5, 0.5, 0.5) / Vector256.Create(1.0, s, s, s);
                v = Vector256.Create(1.0, matrix.M12 + matrix.M21, matrix.M13 + matrix.M31, matrix.M23 - matrix.M32) * sv;
            }
            else if (matrix.M22 > matrix.M33)
            {
                var s = Math.Sqrt(1.0 + matrix.M22 - matrix.M11 - matrix.M33);
                var sv = Vector256.Create(0.5, 0.5 * s, 0.5, 0.5) / Vector256.Create(s, 1.0, s, s);
                v = Vector256.Create(matrix.M21 + matrix.M12, 1.0, matrix.M32 + matrix.M23, matrix.M31 - matrix.M13) * sv;
            }
            else
            {
                var s = Math.Sqrt(1.0 + matrix.M33 - matrix.M11 - matrix.M22);
                var sv = Vector256.Create(0.5, 0.5, 0.5 * s, 0.5) / Vector256.Create(s, s, 1.0, s);
                v = Vector256.Create(matrix.M31 + matrix.M13, matrix.M32 + matrix.M23, 1.0, matrix.M12 - matrix.M21) * sv;
            }

            return Unsafe.BitCast<Vector256<double>, QuaternionD>(v);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator QuaternionD(in Quaternion q)
        {
            var v = Vector128.LoadUnsafe(in q.X);
            return Unsafe.BitCast<Vector256<double>, QuaternionD>(Vector256.WidenLower(v.ToVector256()));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Quaternion(in QuaternionD q)
        {
            var v = Unsafe.BitCast<QuaternionD, Vector256<double>>(q);
            return Unsafe.BitCast<Vector128<float>, Quaternion>(Vector128.Narrow(v.GetLower(), v.GetUpper()));
        }
    }
}
