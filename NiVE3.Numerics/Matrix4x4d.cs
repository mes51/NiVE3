using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Shared.Extension;

namespace NiVE3.Numerics
{
    // copy from Matrix4x4
    public struct Matrix4x4d
    {
        const double InvertEpsilon = 1E-308;

        static readonly Matrix4x4d _Identity = new Matrix4x4d(
            1.0, 0.0, 0.0, 0.0,
            0.0, 1.0, 0.0, 0.0,
            0.0, 0.0, 1.0, 0.0,
            0.0, 0.0, 0.0, 1.0
        );
        public static Matrix4x4d Identity => _Identity;

        static readonly Matrix4x4d _Zero = new Matrix4x4d(0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0);
        public static Matrix4x4d Zero => _Zero;

        public double M11;
        public double M12;
        public double M13;
        public double M14;
        public double M21;
        public double M22;
        public double M23;
        public double M24;
        public double M31;
        public double M32;
        public double M33;
        public double M34;
        public double M41;
        public double M42;
        public double M43;
        public double M44;

        public Matrix4x4d(
            double m11, double m12, double m13, double m14,
            double m21, double m22, double m23, double m24,
            double m31, double m32, double m33, double m34,
            double m41, double m42, double m43, double m44
        )
        {
            M11 = m11;
            M12 = m12;
            M13 = m13;
            M14 = m14;
            M21 = m21;
            M22 = m22;
            M23 = m23;
            M24 = m24;
            M31 = m31;
            M32 = m32;
            M33 = m33;
            M34 = m34;
            M41 = m41;
            M42 = m42;
            M43 = m43;
            M44 = m44;
        }

        public readonly bool IsIdentity => M11 == 1.0 && M22 == 1.0 && M33 == 1.0 && M44 == 1.0 &&
            M12 == 0.0 && M13 == 0.0 && M14 == 0.0 &&
            M21 == 0.0 && M23 == 0.0 && M24 == 0.0 &&
            M31 == 0.0 && M32 == 0.0 && M34 == 0.0 &&
            M41 == 0.0 && M42 == 0.0 && M43 == 0.0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Matrix4x4d Translate(double x, double y, double z)
        {
            return this * CreateTranslate(x, y, z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Matrix4x4d Scale(double x, double y, double z)
        {
            return this * CreateScale(x, y, z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Matrix4x4d RotateX(double angle)
        {
            return this * CreateRotateX(angle);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Matrix4x4d RotateY(double angle)
        {
            return this * CreateRotateY(angle);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Matrix4x4d RotateZ(double angle)
        {
            return this * CreateRotateZ(angle);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly double Determinant()
        {
            return M14 * M23 * M32 * M41 - M13 * M24 * M32 * M41 - M14 * M22 * M33 * M41 + M12 * M24 * M33 * M41 +
                   M13 * M22 * M34 * M41 - M12 * M23 * M34 * M41 - M14 * M23 * M31 * M42 + M13 * M24 * M31 * M42 +
                   M14 * M21 * M33 * M42 - M11 * M24 * M33 * M42 - M13 * M21 * M34 * M42 + M11 * M23 * M34 * M42 +
                   M14 * M22 * M31 * M43 - M12 * M24 * M31 * M43 - M14 * M21 * M32 * M43 + M11 * M24 * M32 * M43 +
                   M12 * M21 * M34 * M43 - M11 * M22 * M34 * M43 - M13 * M22 * M31 * M44 + M12 * M23 * M31 * M44 +
                   M13 * M21 * M32 * M44 - M11 * M23 * M32 * M44 - M12 * M21 * M33 * M44 + M11 * M22 * M33 * M44;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly double FrobeniusNorm()
        {
            var m1 = Vector256.LoadUnsafe(in M11);
            var m2 = Vector256.LoadUnsafe(in M21);
            var m3 = Vector256.LoadUnsafe(in M31);
            var m4 = Vector256.LoadUnsafe(in M41);

            var sum = Vector256.Dot(m1, m1) + Vector256.Dot(m2, m2) + Vector256.Dot(m3, m3) + Vector256.Dot(m4, m4);

            return sum > 0.0 ? Math.Sqrt(sum) : 0.0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override readonly bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is Matrix4x4d m)
            {
                return m == this;
            }
            else
            {
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override readonly int GetHashCode()
        {
            var hashCode = new HashCode();

            hashCode.Add(M11);
            hashCode.Add(M12);
            hashCode.Add(M13);
            hashCode.Add(M14);

            hashCode.Add(M21);
            hashCode.Add(M22);
            hashCode.Add(M23);
            hashCode.Add(M24);

            hashCode.Add(M31);
            hashCode.Add(M32);
            hashCode.Add(M33);
            hashCode.Add(M34);

            hashCode.Add(M41);
            hashCode.Add(M42);
            hashCode.Add(M43);
            hashCode.Add(M44);

            return hashCode.ToHashCode();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override readonly string ToString()
        {
            return $@"{{
    {{ M11: {M11}, M12: {M12}, M13: {M13}, M14: {M14} }}
    {{ M21: {M21}, M22: {M22}, M23: {M23}, M24: {M24} }}
    {{ M31: {M31}, M32: {M32}, M33: {M33}, M34: {M34} }}
    {{ M41: {M41}, M42: {M42}, M43: {M43}, M44: {M44} }}
}}";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix4x4d CreateLookAt(in Vector256<double> pos, in Vector256<double> target, in Vector256<double> up)
        {
            var posInvZ = pos * Vector256.Create(1.0, 1.0, -1.0, 1.0);
            var targetInvZ = target * Vector256.Create(1.0, 1.0, -1.0, 1.0);

            var z = (posInvZ - targetInvZ).Normalize();
            var x = up.CrossProduct(z).Normalize();
            var y = z.CrossProduct(x);

            var result = Identity;
            result.M11 = x.GetElement(0);
            result.M12 = y.GetElement(0);
            result.M13 = z.GetElement(0);
            result.M21 = x.GetElement(1);
            result.M22 = y.GetElement(1);
            result.M23 = z.GetElement(1);
            result.M31 = x.GetElement(2);
            result.M32 = y.GetElement(2);
            result.M33 = z.GetElement(2);

            result.M41 = Vector256.Dot(-x, posInvZ);
            result.M42 = Vector256.Dot(-y, posInvZ);
            result.M43 = Vector256.Dot(-z, posInvZ);

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix4x4d CreatePerspectiveFieldOfView(double fov, double aspect, double near, double far)
        {
            var result = Zero;
            var farRange = (double.IsPositiveInfinity(far) || near == far) ? 1.0 : far / (far - near);
            var scale = 1.0 / Math.Tan(fov * 0.5);

            result.M11 = scale / aspect;
            result.M22 = scale;
            result.M33 = farRange;
            result.M34 = 1.0;
            result.M43 = farRange * -near;

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix4x4d CreateOrthographic(double left, double right, double top, double bottom, double near, double far)
        {
            var result = Zero;

            var width = 1.0 / (right - left);
            var height = 1.0 / (bottom - top);
            var farRange = (double.IsPositiveInfinity(far) || near == far) ? 1.0 : far / (far - near);

            result.M11 = width * 2.0;
            result.M22 = height * 2.0;
            result.M33 = farRange;
            result.M41 = (left + right) * -width;
            result.M42 = (top + bottom) * -height;
            result.M43 = farRange * -near;
            result.M44 = 1.0;

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix4x4d CreateTranslate(double x, double y, double z)
        {
            var result = Identity;

            result.M41 = x;
            result.M42 = y;
            result.M43 = z;

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix4x4d CreateScale(double x, double y, double z)
        {
            var result = Identity;

            result.M11 = x;
            result.M22 = y;
            result.M33 = z;

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix4x4d CreateRotateX(double angle)
        {
            var radian = (float)(Math.PI / 180.0 * angle);
            var sin = Math.Sin(radian);
            var cos = Math.Cos(radian);
            var result = Identity;

            result.M22 = cos;
            result.M23 = sin;
            result.M32 = -sin;
            result.M33 = cos;

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix4x4d CreateRotateY(double angle)
        {
            var radian = (float)(Math.PI / 180.0 * angle);
            var sin = Math.Sin(radian);
            var cos = Math.Cos(radian);
            var result = Identity;

            result.M11 = cos;
            result.M13 = -sin;
            result.M31 = sin;
            result.M33 = cos;

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix4x4d CreateRotateZ(double angle)
        {
            var radian = (float)(Math.PI / 180.0 * angle);
            var sin = Math.Sin(radian);
            var cos = Math.Cos(radian);
            var result = Identity;

            result.M11 = cos;
            result.M12 = sin;
            result.M21 = -sin;
            result.M22 = cos;

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix4x4d CreateRotateZYX(double angleX, double angleY, double angleZ)
        {
            return Identity.RotateZ(angleZ)
                .RotateY(angleY)
                .RotateX(angleX);
        }

        public static bool Invert(Matrix4x4d matrix, out Matrix4x4d result)
        {
            // Load the matrix values into rows
            var row1 = Vector256.LoadUnsafe(ref matrix.M11);
            var row2 = Vector256.LoadUnsafe(ref matrix.M21);
            var row3 = Vector256.LoadUnsafe(ref matrix.M31);
            var row4 = Vector256.LoadUnsafe(ref matrix.M41);

            // Transpose the matrix
            var vTemp1 = row1.Shuffle4x64(row2, 0x44); //_MM_SHUFFLE(1, 0, 1, 0)
            var vTemp3 = row1.Shuffle4x64(row2, 0xEE); //_MM_SHUFFLE(3, 2, 3, 2)
            var vTemp2 = row3.Shuffle4x64(row4, 0x44); //_MM_SHUFFLE(1, 0, 1, 0)
            var vTemp4 = row3.Shuffle4x64(row4, 0xEE); //_MM_SHUFFLE(3, 2, 3, 2)

            row1 = vTemp1.Shuffle4x64(vTemp2, 0x88); //_MM_SHUFFLE(2, 0, 2, 0)
            row2 = vTemp1.Shuffle4x64(vTemp2, 0xDD); //_MM_SHUFFLE(3, 1, 3, 1)
            row3 = vTemp3.Shuffle4x64(vTemp4, 0x88); //_MM_SHUFFLE(2, 0, 2, 0)
            row4 = vTemp3.Shuffle4x64(vTemp4, 0xDD); //_MM_SHUFFLE(3, 1, 3, 1)

            var V00 = row3.Permute4x64(0x50);           //_MM_SHUFFLE(1, 1, 0, 0)
            var V10 = row4.Permute4x64(0xEE);           //_MM_SHUFFLE(3, 2, 3, 2)
            var V01 = row1.Permute4x64(0x50);           //_MM_SHUFFLE(1, 1, 0, 0)
            var V11 = row2.Permute4x64(0xEE);           //_MM_SHUFFLE(3, 2, 3, 2)
            var V02 = row3.Shuffle4x64(row1, 0x88);  //_MM_SHUFFLE(2, 0, 2, 0)
            var V12 = row4.Shuffle4x64(row2, 0xDD);  //_MM_SHUFFLE(3, 1, 3, 1)

            var D0 = V00 * V10;
            var D1 = V01 * V11;
            var D2 = V02 * V12;

            V00 = row3.Permute4x64(0xEE);           //_MM_SHUFFLE(3, 2, 3, 2)
            V10 = row4.Permute4x64(0x50);           //_MM_SHUFFLE(1, 1, 0, 0)
            V01 = row1.Permute4x64(0xEE);           //_MM_SHUFFLE(3, 2, 3, 2)
            V11 = row2.Permute4x64(0x50);           //_MM_SHUFFLE(1, 1, 0, 0)
            V02 = row3.Shuffle4x64(row1, 0xDD);  //_MM_SHUFFLE(3, 1, 3, 1)
            V12 = row4.Shuffle4x64(row2, 0x88);  //_MM_SHUFFLE(2, 0, 2, 0)

            // Note:  We use this expansion pattern instead of Fused Multiply Add
            // in order to support older hardware
            D0 -= (V00 * V10);
            D1 -= (V01 * V11);
            D2 -= (V02 * V12);

            // V11 = D0Y,D0W,D2Y,D2Y
            V11 = D0.Shuffle4x64(D2, 0x5D);   //_MM_SHUFFLE(1, 1, 3, 1)
            V00 = row2.Permute4x64(0x49);        //_MM_SHUFFLE(1, 0, 2, 1)
            V10 = V11.Shuffle4x64(D0, 0x32);  //_MM_SHUFFLE(0, 3, 0, 2)
            V01 = row1.Permute4x64(0x12);        //_MM_SHUFFLE(0, 1, 0, 2)
            V11 = V11.Shuffle4x64(D0, 0x99);  //_MM_SHUFFLE(2, 1, 2, 1)

            // V13 = D1Y,D1W,D2W,D2W
            var V13 = D1.Shuffle4x64(D2, 0xFD);               //_MM_SHUFFLE(3, 3, 3, 1)
            V02 = row4.Permute4x64(0x49);                        //_MM_SHUFFLE(1, 0, 2, 1)
            V12 = V13.Shuffle4x64(D1, 0x32);                  //_MM_SHUFFLE(0, 3, 0, 2)
            var V03 = row3.Permute4x64(0x12);                    //_MM_SHUFFLE(0, 1, 0, 2)
            V13 = V13.Shuffle4x64(D1, 0x99);                  //_MM_SHUFFLE(2, 1, 2, 1)

            var C0 = V00 * V10;
            var C2 = V01 * V11;
            var C4 = V02 * V12;
            var C6 = V03 * V13;

            // V11 = D0X,D0Y,D2X,D2X
            V11 = D0.Shuffle4x64(D2, 0x4);     //_MM_SHUFFLE(0, 0, 1, 0)
            V00 = row2.Permute4x64(0x9e);         //_MM_SHUFFLE(2, 1, 3, 2)
            V10 = D0.Shuffle4x64(V11, 0x93);   //_MM_SHUFFLE(2, 1, 0, 3)
            V01 = row1.Permute4x64(0x7b);         //_MM_SHUFFLE(1, 3, 2, 3)
            V11 = D0.Shuffle4x64(V11, 0x26);   //_MM_SHUFFLE(0, 2, 1, 2)

            // V13 = D1X,D1Y,D2Z,D2Z
            V13 = D1.Shuffle4x64(D2, 0xa4);    //_MM_SHUFFLE(2, 2, 1, 0)
            V02 = row4.Permute4x64(0x9e);         //_MM_SHUFFLE(2, 1, 3, 2)
            V12 = D1.Shuffle4x64(V13, 0x93);   //_MM_SHUFFLE(2, 1, 0, 3)
            V03 = row3.Permute4x64(0x7b);         //_MM_SHUFFLE(1, 3, 2, 3)
            V13 = D1.Shuffle4x64(V13, 0x26);   //_MM_SHUFFLE(0, 2, 1, 2)

            C0 -= (V00 * V10);
            C2 -= (V01 * V11);
            C4 -= (V02 * V12);
            C6 -= (V03 * V13);

            V00 = row2.Permute4x64(0x33); //_MM_SHUFFLE(0, 3, 0, 3)

            // V10 = D0Z,D0Z,D2X,D2Y
            V10 = D0.Shuffle4x64(D2, 0x4A);  //_MM_SHUFFLE(1, 0, 2, 2)
            V10 = V10.Permute4x64(0x2C);        //_MM_SHUFFLE(0, 2, 3, 0)
            V01 = row1.Permute4x64(0x8D);       //_MM_SHUFFLE(2, 0, 3, 1)

            // V11 = D0X,D0W,D2X,D2Y
            V11 = D0.Shuffle4x64(D2, 0x4C);  //_MM_SHUFFLE(1, 0, 3, 0)
            V11 = V11.Permute4x64(0x93);        //_MM_SHUFFLE(2, 1, 0, 3)
            V02 = row4.Permute4x64(0x33);       //_MM_SHUFFLE(0, 3, 0, 3)

            // V12 = D1Z,D1Z,D2Z,D2W
            V12 = D1.Shuffle4x64(D2, 0xEA);  //_MM_SHUFFLE(3, 2, 2, 2)
            V12 = V12.Permute4x64(0x2C);        //_MM_SHUFFLE(0, 2, 3, 0)
            V03 = row3.Permute4x64(0x8D);       //_MM_SHUFFLE(2, 0, 3, 1)

            // V13 = D1X,D1W,D2Z,D2W
            V13 = D1.Shuffle4x64(D2, 0xEC);  //_MM_SHUFFLE(3, 2, 3, 0)
            V13 = V13.Permute4x64(0x93);        //_MM_SHUFFLE(2, 1, 0, 3)

            V00 *= V10;
            V01 *= V11;
            V02 *= V12;
            V03 *= V13;

            var C1 = C0 - V00;
            C0 += V00;
            var C3 = C2 + V01;
            C2 -= V01;
            var C5 = C4 - V02;
            C4 += V02;
            var C7 = C6 + V03;
            C6 -= V03;

            C0 = C0.Shuffle4x64(C1, 0xD8); //_MM_SHUFFLE(3, 1, 2, 0)
            C2 = C2.Shuffle4x64(C3, 0xD8); //_MM_SHUFFLE(3, 1, 2, 0)
            C4 = C4.Shuffle4x64(C5, 0xD8); //_MM_SHUFFLE(3, 1, 2, 0)
            C6 = C6.Shuffle4x64(C7, 0xD8); //_MM_SHUFFLE(3, 1, 2, 0)

            C0 = C0.Permute4x64(0xD8); //_MM_SHUFFLE(3, 1, 2, 0)
            C2 = C2.Permute4x64(0xD8); //_MM_SHUFFLE(3, 1, 2, 0)
            C4 = C4.Permute4x64(0xD8); //_MM_SHUFFLE(3, 1, 2, 0)
            C6 = C6.Permute4x64(0xD8); //_MM_SHUFFLE(3, 1, 2, 0)

            // Get the determinant
            vTemp2 = row1;
            var det = Vector256.Dot(C0, vTemp2);

            // Check determinate is not zero
            if (Math.Abs(det) < InvertEpsilon)
            {
                result = new Matrix4x4d(double.NaN, double.NaN, double.NaN, double.NaN,
                            double.NaN, double.NaN, double.NaN, double.NaN,
                            double.NaN, double.NaN, double.NaN, double.NaN,
                            double.NaN, double.NaN, double.NaN, double.NaN);
                return false;
            }

            // Create Vector256<double> copy of the determinant and invert them.
            var ones = Vector256.Create(1.0);
            var vTemp = Vector256.Create(det);
            vTemp = ones / vTemp;

            row1 = C0 * vTemp;
            row2 = C2 * vTemp;
            row3 = C4 * vTemp;
            row4 = C6 * vTemp;

            Unsafe.SkipInit(out result);
            ref var vResult = ref Unsafe.As<Matrix4x4d, Vector256<double>>(ref result);

            vResult = row1;
            Unsafe.Add(ref vResult, 1) = row2;
            Unsafe.Add(ref vResult, 2) = row3;
            Unsafe.Add(ref vResult, 3) = row4;

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix4x4d Transpose(Matrix4x4d matrix)
        {
            var row1 = Vector256.LoadUnsafe(ref matrix.M11);
            var row2 = Vector256.LoadUnsafe(ref matrix.M21);
            var row3 = Vector256.LoadUnsafe(ref matrix.M31);
            var row4 = Vector256.LoadUnsafe(ref matrix.M41);

            var l12 = Avx.UnpackLow(row1, row2);
            var l34 = Avx.UnpackLow(row3, row4);
            var h12 = Avx.UnpackHigh(row1, row2);
            var h34 = Avx.UnpackHigh(row3, row4);

            Vector256.Create(Avx.ExtractVector128(l12, 0), Avx.ExtractVector128(l34, 0)).StoreUnsafe(ref matrix.M11);
            Vector256.Create(Avx.ExtractVector128(h12, 0), Avx.ExtractVector128(h34, 0)).StoreUnsafe(ref matrix.M21);
            Vector256.Create(Avx.ExtractVector128(l12, 1), Avx.ExtractVector128(l34, 1)).StoreUnsafe(ref matrix.M31);
            Vector256.Create(Avx.ExtractVector128(h12, 1), Avx.ExtractVector128(h34, 1)).StoreUnsafe(ref matrix.M41);

            return matrix;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix4x4d AffineTransform(in Vector3d anchorPoint, in Vector3d scale, in Vector3d direction, double angleX, double angleY, double angleZ, in Vector3d translate)
        {
            return Identity.Translate(-anchorPoint.X, -anchorPoint.Y, -anchorPoint.Z)
                .Scale(scale.X, scale.Y, scale.Z)
                .RotateZ(direction.Z)
                .RotateY(direction.Y)
                .RotateX(direction.X)
                .RotateZ(angleZ)
                .RotateY(angleY)
                .RotateX(angleX)
                .Translate(translate.X, translate.Y, translate.Z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix4x4d FromQuaternion(in QuaternionD q)
        {
            // SEE: https://github.com/dotnet/runtime/blob/35d27803b83bbd46d3cfe42aa58f46f585c0c6ae/src/libraries/System.Private.CoreLib/src/System/Numerics/Matrix4x4.Impl.cs#L295

            var xx = q.X * q.X;
            var yy = q.Y * q.Y;
            var zz = q.Z * q.Z;

            var xy = q.X * q.Y;
            var xz = q.X * q.Z;
            var yz = q.Y * q.Z;
            var wx = q.W * q.X;
            var wy = q.W * q.Y;
            var wz = q.W * q.Z;

            var result = Identity;

            result.M11 = 1.0 - 2.0 * (yy + zz);
            result.M12 = 2.0 * (xy + wz);
            result.M13 = 2.0 * (xz - wy);

            result.M21 = 2.0 * (xy - wz);
            result.M22 = 1.0 - 2.0 * (zz + xx);
            result.M23 = 2.0 * (yz + wx);

            result.M31 = 2.0 * (xz + wy);
            result.M32 = 2.0 * (yz - wx);
            result.M33 = 1.0 - 2.0 * (yy + xx);

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix4x4d Compose(Vector3d position, Vector3d scale, QuaternionD rotation)
        {
            // SEE: https://github.com/mrdoob/three.js/blob/a692ffc9d5519df3062fce89457a900cc9a5dde5/src/math/Matrix4.js#L696

            var result = FromQuaternion(rotation);

            (Vector256.LoadUnsafe(in result.M11) * scale.X).StoreUnsafe(ref result.M11);
            (Vector256.LoadUnsafe(in result.M21) * scale.Y).StoreUnsafe(ref result.M21);
            (Vector256.LoadUnsafe(in result.M31) * scale.Z).StoreUnsafe(ref result.M31);
            position.ToHomogeneousCoord().StoreUnsafe(ref result.M41);

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (Vector3d position, Vector3d scale, QuaternionD rotate) Decompose(Matrix4x4d matrix)
        {
            var position = new Vector3d(matrix.M41, matrix.M42, matrix.M43);

            matrix.M41 = 0.0;
            matrix.M42 = 0.0;
            matrix.M43 = 0.0;

            /*
            var sx = new Vector3d(matrix.M11, matrix.M12, matrix.M13).Length();
            var sy = new Vector3d(matrix.M21, matrix.M22, matrix.M23).Length();
            var sz = new Vector3d(matrix.M31, matrix.M32, matrix.M33).Length();
            var scale = new Vector3d(sx, sy, sz);

            matrix.M11 /= sx;
            matrix.M12 /= sx;
            matrix.M13 /= sx;
            matrix.M21 /= sy;
            matrix.M22 /= sy;
            matrix.M23 /= sy;
            matrix.M31 /= sz;
            matrix.M32 /= sz;
            matrix.M33 /= sz;

            var rotate = QuaternionD.FromRotationMatrix(matrix);
            //*/

            var (scaleMatrix, rotateMatrix) = PolarDecompose(matrix);

            var scale = new Vector3d(scaleMatrix.M11, scaleMatrix.M22, scaleMatrix.M33);
            var rotate = QuaternionD.FromRotationMatrix(rotateMatrix);
            //*/
            return (position, scale, rotate);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(in Matrix4x4d a, in Matrix4x4d b)
        {
            return Vector256.LoadUnsafe(in a.M11) == Vector256.LoadUnsafe(in b.M11) &&
                Vector256.LoadUnsafe(in a.M21) == Vector256.LoadUnsafe(in b.M21) &&
                Vector256.LoadUnsafe(in a.M31) == Vector256.LoadUnsafe(in b.M31) &&
                Vector256.LoadUnsafe(in a.M41) == Vector256.LoadUnsafe(in b.M41);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(in Matrix4x4d a, in Matrix4x4d b)
        {
            return Vector256.LoadUnsafe(in a.M11) != Vector256.LoadUnsafe(in b.M11) &&
                Vector256.LoadUnsafe(in a.M21) != Vector256.LoadUnsafe(in b.M21) &&
                Vector256.LoadUnsafe(in a.M31) != Vector256.LoadUnsafe(in b.M31) &&
                Vector256.LoadUnsafe(in a.M41) != Vector256.LoadUnsafe(in b.M41);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix4x4d operator +(Matrix4x4d a, in Matrix4x4d b)
        {
            (Vector256.LoadUnsafe(in a.M11) + Vector256.LoadUnsafe(in b.M11)).StoreUnsafe(ref a.M11);
            (Vector256.LoadUnsafe(in a.M21) + Vector256.LoadUnsafe(in b.M21)).StoreUnsafe(ref a.M21);
            (Vector256.LoadUnsafe(in a.M31) + Vector256.LoadUnsafe(in b.M31)).StoreUnsafe(ref a.M31);
            (Vector256.LoadUnsafe(in a.M41) + Vector256.LoadUnsafe(in b.M41)).StoreUnsafe(ref a.M41);

            return a;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix4x4d operator -(Matrix4x4d a, in Matrix4x4d b)
        {
            (Vector256.LoadUnsafe(in a.M11) - Vector256.LoadUnsafe(in b.M11)).StoreUnsafe(ref a.M11);
            (Vector256.LoadUnsafe(in a.M21) - Vector256.LoadUnsafe(in b.M21)).StoreUnsafe(ref a.M21);
            (Vector256.LoadUnsafe(in a.M31) - Vector256.LoadUnsafe(in b.M31)).StoreUnsafe(ref a.M31);
            (Vector256.LoadUnsafe(in a.M41) - Vector256.LoadUnsafe(in b.M41)).StoreUnsafe(ref a.M41);

            return a;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix4x4d operator -(Matrix4x4d a)
        {
            (-Vector256.LoadUnsafe(in a.M11)).StoreUnsafe(ref a.M11);
            (-Vector256.LoadUnsafe(in a.M21)).StoreUnsafe(ref a.M21);
            (-Vector256.LoadUnsafe(in a.M31)).StoreUnsafe(ref a.M31);
            (-Vector256.LoadUnsafe(in a.M41)).StoreUnsafe(ref a.M41);

            return a;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix4x4d operator *(Matrix4x4d a, in Matrix4x4d b)
        {
            ((Vector256.Create(a.M11) * Vector256.LoadUnsafe(in b.M11) + Vector256.Create(a.M12) * Vector256.LoadUnsafe(in b.M21)) + (Vector256.Create(a.M13) * Vector256.LoadUnsafe(in b.M31) + Vector256.Create(a.M14) * Vector256.LoadUnsafe(in b.M41))).StoreUnsafe(ref a.M11);
            ((Vector256.Create(a.M21) * Vector256.LoadUnsafe(in b.M11) + Vector256.Create(a.M22) * Vector256.LoadUnsafe(in b.M21)) + (Vector256.Create(a.M23) * Vector256.LoadUnsafe(in b.M31) + Vector256.Create(a.M24) * Vector256.LoadUnsafe(in b.M41))).StoreUnsafe(ref a.M21);
            ((Vector256.Create(a.M31) * Vector256.LoadUnsafe(in b.M11) + Vector256.Create(a.M32) * Vector256.LoadUnsafe(in b.M21)) + (Vector256.Create(a.M33) * Vector256.LoadUnsafe(in b.M31) + Vector256.Create(a.M34) * Vector256.LoadUnsafe(in b.M41))).StoreUnsafe(ref a.M31);
            ((Vector256.Create(a.M41) * Vector256.LoadUnsafe(in b.M11) + Vector256.Create(a.M42) * Vector256.LoadUnsafe(in b.M21)) + (Vector256.Create(a.M43) * Vector256.LoadUnsafe(in b.M31) + Vector256.Create(a.M44) * Vector256.LoadUnsafe(in b.M41))).StoreUnsafe(ref a.M41);

            return a;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix4x4d operator *(Matrix4x4d a, double s)
        {
            (Vector256.LoadUnsafe(in a.M11) * s).StoreUnsafe(ref a.M11);
            (Vector256.LoadUnsafe(in a.M21) * s).StoreUnsafe(ref a.M21);
            (Vector256.LoadUnsafe(in a.M31) * s).StoreUnsafe(ref a.M31);
            (Vector256.LoadUnsafe(in a.M41) * s).StoreUnsafe(ref a.M41);

            return a;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Matrix4x4d(in Matrix4x4 m)
        {
            Unsafe.SkipInit<Matrix4x4d>(out var result);

            Avx.ConvertToVector256Double(Vector128.LoadUnsafe(in m.M11)).StoreUnsafe(ref result.M11);
            Avx.ConvertToVector256Double(Vector128.LoadUnsafe(in m.M21)).StoreUnsafe(ref result.M21);
            Avx.ConvertToVector256Double(Vector128.LoadUnsafe(in m.M31)).StoreUnsafe(ref result.M31);
            Avx.ConvertToVector256Double(Vector128.LoadUnsafe(in m.M41)).StoreUnsafe(ref result.M41);

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Matrix4x4(in Matrix4x4d m)
        {
            Unsafe.SkipInit<Matrix4x4>(out var result);

            Avx.ConvertToVector128Single(Vector256.LoadUnsafe(in m.M11)).StoreUnsafe(ref result.M11);
            Avx.ConvertToVector128Single(Vector256.LoadUnsafe(in m.M21)).StoreUnsafe(ref result.M21);
            Avx.ConvertToVector128Single(Vector256.LoadUnsafe(in m.M31)).StoreUnsafe(ref result.M31);
            Avx.ConvertToVector128Single(Vector256.LoadUnsafe(in m.M41)).StoreUnsafe(ref result.M41);

            return result;
        }

        static (Matrix4x4d scale, Matrix4x4d rotate) PolarDecompose(Matrix4x4d m)
        {
            const int IterationCount = 20;
            const double Epsilon = 1E-7;

            m.M41 = 0.0;
            m.M42 = 0.0;
            m.M43 = 0.0;
            m.M14 = 0.0;
            m.M24 = 0.0;
            m.M34 = 0.0;
            var det = m.Determinant();

            var scaleSign = det < 0.0 ? -1.0 : 1.0;
            if (det < 0.0)
            {
                m *= -1.0;
                m.M44 = 1.0;
            }

            var rotate = m;
            rotate.M44 = 1.0;
            for (var i = 0; i < IterationCount; i++)
            {
                if (!Invert(rotate, out var invertedQ))
                {
                    rotate = Identity;
                    break;
                }

                var invertedQT = Transpose(invertedQ);
                var next = (rotate + invertedQT) * 0.5;

                rotate = next;
                if ((rotate - next).FrobeniusNorm() < Epsilon)
                {
                    break;
                }
            }

            var rotateT = Transpose(rotate);
            var scale = (m * rotateT) * scaleSign;

            return (scale, rotate);
        }
    }

    public static class Matrix4x4dExtension
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<double> Transform(this in Matrix4x4d matrix, in Vector256<double> v)
        {
            var m1 = Vector256.LoadUnsafe(in matrix.M11);
            var m2 = Vector256.LoadUnsafe(in matrix.M21);
            var m3 = Vector256.LoadUnsafe(in matrix.M31);
            var m4 = Vector256.LoadUnsafe(in matrix.M41);

            return (Vector256.Create(v.GetElement(0)) * m1 + Vector256.Create(v.GetElement(1)) * m2) +
                (Vector256.Create(v.GetElement(2)) * m3 + Vector256.Create(v.GetElement(3)) * m4);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3d Transform(this in Matrix4x4d matrix, in Vector3d v)
        {
            return (Vector3d)matrix.Transform(v.ToHomogeneousCoord());
        }
    }
}
