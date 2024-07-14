using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;
using ComputeSharp;
using NiVE3.Numerics;

namespace NiVE3.PresetPlugin.Internal.Extension
{
    static class PrimitiveConvertExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Float4 AsFloat4(this in Vector128<float> v)
        {
            return Unsafe.BitCast<Vector128<float>, Float4>(v);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Float4 AsFloat4(this in Vector4 v)
        {
            return Unsafe.BitCast<Vector4, Float4>(v);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Float3 AsFloat3(this in Vector3 v)
        {
            return Unsafe.BitCast<Vector3, Float3>(v);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Float3x3 ToFloat3x3(this in Matrix3x3 matrix)
        {
            return new Float3x3(
                matrix.M11, matrix.M21, matrix.M31,
                matrix.M12, matrix.M22, matrix.M32,
                matrix.M13, matrix.M23, matrix.M33
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Float4x4 ToFloat4x4(this in Matrix4x4 matrix)
        {
            //return new Float4x4(
            //    matrix.M11, matrix.M21, matrix.M31, matrix.M41,
            //    matrix.M12, matrix.M22, matrix.M32, matrix.M42,
            //    matrix.M13, matrix.M23, matrix.M33, matrix.M43,
            //    matrix.M14, matrix.M24, matrix.M34, matrix.M44
            //);

            return (Float4x4)matrix;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Float4x4 ToFloat4x4(this in Matrix4x4d matrix)
        {
            return ((Matrix4x4)matrix).ToFloat4x4();
        }
    }
}
