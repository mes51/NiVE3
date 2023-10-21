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
using System.Reflection.Metadata;

namespace NiVE3.Plugin.Struct
{
    // copy from Matrix4x4
    public struct Matrix4x4d
    {
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

        public Matrix4x4d Translate(double x, double y, double z)
        {
            return this * CreateTranslate(x, y, z);
        }

        public Matrix4x4d Scale(double x, double y, double z)
        {
            return this * CreateScale(x, y, z);
        }

        public Matrix4x4d RotateX(double angle)
        {
            return this * CreateRotateX(angle);
        }

        public Matrix4x4d RotateY(double angle)
        {
            return this * CreateRotateY(angle);
        }

        public Matrix4x4d RotateZ(double angle)
        {
            return this * CreateRotateZ(angle);
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
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

        public override int GetHashCode()
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

        public override string ToString()
        {
            return $@"{{
    {{ M11: {M11}, M12: {M12}, M13: {M13}, M14: {M14} }}
    {{ M21: {M21}, M22: {M22}, M23: {M23}, M24: {M24} }}
    {{ M31: {M31}, M32: {M32}, M33: {M33}, M34: {M34} }}
    {{ M41: {M41}, M42: {M42}, M43: {M43}, M44: {M44} }}
}}";
        }

        public static Matrix4x4d CreateLookAt(in Vector256<double> pos, in Vector256<double> target, in Vector256<double> up)
        {
            var z = Avx.Subtract(pos, target).Normalize();
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

            result.M41 = -x.DotProduct(pos).GetElement(0);
            result.M42 = -y.DotProduct(pos).GetElement(0);
            result.M43 = -z.DotProduct(pos).GetElement(0);

            return result;
        }

        public static Matrix4x4d CreatePerspectiveFieldOfView(double fov, double aspect, double near, double far)
        {
            var result = Zero;
            var farRange = double.IsPositiveInfinity(far) ? -1.0 : far / (near - far);
            var scale = 1.0 / Math.Tan(fov * 0.5);

            result.M11 = scale / aspect;
            result.M22 = scale;
            result.M33 = farRange;
            result.M34 = -1.0;
            result.M43 = near * farRange;

            return result;
        }

        public static Matrix4x4d CreateTranslate(double x, double y, double z)
        {
            var result = Identity;

            result.M41 = x;
            result.M42 = y;
            result.M43 = z;

            return result;
        }

        public static Matrix4x4d CreateScale(double x, double y, double z)
        {
            var result = Identity;

            result.M11 = x;
            result.M22 = y;
            result.M33 = z;

            return result;
        }

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

        public unsafe static bool Invert(Matrix4x4d matrix, out Matrix4x4d result)
        {
            // Load the matrix values into rows
            var row1 = Avx.LoadVector256(&matrix.M11);
            var row2 = Avx.LoadVector256(&matrix.M21);
            var row3 = Avx.LoadVector256(&matrix.M31);
            var row4 = Avx.LoadVector256(&matrix.M41);

            // Transpose the matrix
            var vTemp1 = row1.Shuffle4x64(row2, 0x44); //_MM_SHUFFLE(1, 0, 1, 0)
            var vTemp3 = row1.Shuffle4x64(row2, 0xEE); //_MM_SHUFFLE(3, 2, 3, 2)
            var vTemp2 = row3.Shuffle4x64(row4, 0x44); //_MM_SHUFFLE(1, 0, 1, 0)
            var vTemp4 = row3.Shuffle4x64(row4, 0xEE); //_MM_SHUFFLE(3, 2, 3, 2)

            row1 = vTemp1.Shuffle4x64(vTemp2, 0x88); //_MM_SHUFFLE(2, 0, 2, 0)
            row2 = vTemp1.Shuffle4x64(vTemp2, 0xDD); //_MM_SHUFFLE(3, 1, 3, 1)
            row3 = vTemp3.Shuffle4x64(vTemp4, 0x88); //_MM_SHUFFLE(2, 0, 2, 0)
            row4 = vTemp3.Shuffle4x64(vTemp4, 0xDD); //_MM_SHUFFLE(3, 1, 3, 1)

            var V00 = Permute(row3, 0x50);           //_MM_SHUFFLE(1, 1, 0, 0)
            var V10 = Permute(row4, 0xEE);           //_MM_SHUFFLE(3, 2, 3, 2)
            var V01 = Permute(row1, 0x50);           //_MM_SHUFFLE(1, 1, 0, 0)
            var V11 = Permute(row2, 0xEE);           //_MM_SHUFFLE(3, 2, 3, 2)
            var V02 = row3.Shuffle4x64(row1, 0x88);  //_MM_SHUFFLE(2, 0, 2, 0)
            var V12 = row4.Shuffle4x64(row2, 0xDD);  //_MM_SHUFFLE(3, 1, 3, 1)

            var D0 = Avx.Multiply(V00, V10);
            var D1 = Avx.Multiply(V01, V11);
            var D2 = Avx.Multiply(V02, V12);

            V00 = Permute(row3, 0xEE);           //_MM_SHUFFLE(3, 2, 3, 2)
            V10 = Permute(row4, 0x50);           //_MM_SHUFFLE(1, 1, 0, 0)
            V01 = Permute(row1, 0xEE);           //_MM_SHUFFLE(3, 2, 3, 2)
            V11 = Permute(row2, 0x50);           //_MM_SHUFFLE(1, 1, 0, 0)
            V02 = row3.Shuffle4x64(row1, 0xDD);  //_MM_SHUFFLE(3, 1, 3, 1)
            V12 = row4.Shuffle4x64(row2, 0x88);  //_MM_SHUFFLE(2, 0, 2, 0)

            // Note:  We use this expansion pattern instead of Fused Multiply Add
            // in order to support older hardware
            D0 = Avx.Subtract(D0, Avx.Multiply(V00, V10));
            D1 = Avx.Subtract(D1, Avx.Multiply(V01, V11));
            D2 = Avx.Subtract(D2, Avx.Multiply(V02, V12));

            // V11 = D0Y,D0W,D2Y,D2Y
            V11 = D0.Shuffle4x64(D2, 0x5D);   //_MM_SHUFFLE(1, 1, 3, 1)
            V00 = Permute(row2, 0x49);        //_MM_SHUFFLE(1, 0, 2, 1)
            V10 = V11.Shuffle4x64(D0, 0x32);  //_MM_SHUFFLE(0, 3, 0, 2)
            V01 = Permute(row1, 0x12);        //_MM_SHUFFLE(0, 1, 0, 2)
            V11 = V11.Shuffle4x64(D0, 0x99);  //_MM_SHUFFLE(2, 1, 2, 1)

            // V13 = D1Y,D1W,D2W,D2W
            var V13 = D1.Shuffle4x64(D2, 0xFD);               //_MM_SHUFFLE(3, 3, 3, 1)
            V02 = Permute(row4, 0x49);                        //_MM_SHUFFLE(1, 0, 2, 1)
            V12 = V13.Shuffle4x64(D1, 0x32);                  //_MM_SHUFFLE(0, 3, 0, 2)
            var V03 = Permute(row3, 0x12);                    //_MM_SHUFFLE(0, 1, 0, 2)
            V13 = V13.Shuffle4x64(D1, 0x99);                  //_MM_SHUFFLE(2, 1, 2, 1)

            var C0 = Avx.Multiply(V00, V10);
            var C2 = Avx.Multiply(V01, V11);
            var C4 = Avx.Multiply(V02, V12);
            var C6 = Avx.Multiply(V03, V13);

            // V11 = D0X,D0Y,D2X,D2X
            V11 = D0.Shuffle4x64(D2, 0x4);     //_MM_SHUFFLE(0, 0, 1, 0)
            V00 = Permute(row2, 0x9e);         //_MM_SHUFFLE(2, 1, 3, 2)
            V10 = D0.Shuffle4x64(V11, 0x93);   //_MM_SHUFFLE(2, 1, 0, 3)
            V01 = Permute(row1, 0x7b);         //_MM_SHUFFLE(1, 3, 2, 3)
            V11 = D0.Shuffle4x64(V11, 0x26);   //_MM_SHUFFLE(0, 2, 1, 2)

            // V13 = D1X,D1Y,D2Z,D2Z
            V13 = D1.Shuffle4x64(D2, 0xa4);    //_MM_SHUFFLE(2, 2, 1, 0)
            V02 = Permute(row4, 0x9e);         //_MM_SHUFFLE(2, 1, 3, 2)
            V12 = D1.Shuffle4x64(V13, 0x93);   //_MM_SHUFFLE(2, 1, 0, 3)
            V03 = Permute(row3, 0x7b);         //_MM_SHUFFLE(1, 3, 2, 3)
            V13 = D1.Shuffle4x64(V13, 0x26);   //_MM_SHUFFLE(0, 2, 1, 2)

            C0 = Avx.Subtract(C0, Avx.Multiply(V00, V10));
            C2 = Avx.Subtract(C2, Avx.Multiply(V01, V11));
            C4 = Avx.Subtract(C4, Avx.Multiply(V02, V12));
            C6 = Avx.Subtract(C6, Avx.Multiply(V03, V13));

            V00 = Permute(row2, 0x33); //_MM_SHUFFLE(0, 3, 0, 3)

            // V10 = D0Z,D0Z,D2X,D2Y
            V10 = D0.Shuffle4x64(D2, 0x4A);  //_MM_SHUFFLE(1, 0, 2, 2)
            V10 = Permute(V10, 0x2C);        //_MM_SHUFFLE(0, 2, 3, 0)
            V01 = Permute(row1, 0x8D);       //_MM_SHUFFLE(2, 0, 3, 1)

            // V11 = D0X,D0W,D2X,D2Y
            V11 = D0.Shuffle4x64(D2, 0x4C);  //_MM_SHUFFLE(1, 0, 3, 0)
            V11 = Permute(V11, 0x93);        //_MM_SHUFFLE(2, 1, 0, 3)
            V02 = Permute(row4, 0x33);       //_MM_SHUFFLE(0, 3, 0, 3)

            // V12 = D1Z,D1Z,D2Z,D2W
            V12 = D1.Shuffle4x64(D2, 0xEA);  //_MM_SHUFFLE(3, 2, 2, 2)
            V12 = Permute(V12, 0x2C);        //_MM_SHUFFLE(0, 2, 3, 0)
            V03 = Permute(row3, 0x8D);       //_MM_SHUFFLE(2, 0, 3, 1)

            // V13 = D1X,D1W,D2Z,D2W
            V13 = D1.Shuffle4x64(D2, 0xEC);  //_MM_SHUFFLE(3, 2, 3, 0)
            V13 = Permute(V13, 0x93);        //_MM_SHUFFLE(2, 1, 0, 3)

            V00 = Avx.Multiply(V00, V10);
            V01 = Avx.Multiply(V01, V11);
            V02 = Avx.Multiply(V02, V12);
            V03 = Avx.Multiply(V03, V13);

            var C1 = Avx.Subtract(C0, V00);
            C0 = Avx.Add(C0, V00);
            var C3 = Avx.Add(C2, V01);
            C2 = Avx.Subtract(C2, V01);
            var C5 = Avx.Subtract(C4, V02);
            C4 = Avx.Add(C4, V02);
            var C7 = Avx.Add(C6, V03);
            C6 = Avx.Subtract(C6, V03);

            C0 = C0.Shuffle4x64(C1, 0xD8); //_MM_SHUFFLE(3, 1, 2, 0)
            C2 = C2.Shuffle4x64(C3, 0xD8); //_MM_SHUFFLE(3, 1, 2, 0)
            C4 = C4.Shuffle4x64(C5, 0xD8); //_MM_SHUFFLE(3, 1, 2, 0)
            C6 = C6.Shuffle4x64(C7, 0xD8); //_MM_SHUFFLE(3, 1, 2, 0)

            C0 = Permute(C0, 0xD8); //_MM_SHUFFLE(3, 1, 2, 0)
            C2 = Permute(C2, 0xD8); //_MM_SHUFFLE(3, 1, 2, 0)
            C4 = Permute(C4, 0xD8); //_MM_SHUFFLE(3, 1, 2, 0)
            C6 = Permute(C6, 0xD8); //_MM_SHUFFLE(3, 1, 2, 0)

            // Get the determinant
            vTemp2 = row1;
            var det = C0.DotProduct(vTemp2).GetElement(0);

            // Check determinate is not zero
            if (Math.Abs(det) < double.Epsilon)
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
            vTemp = Avx.Divide(ones, vTemp);

            row1 = Avx.Multiply(C0, vTemp);
            row2 = Avx.Multiply(C2, vTemp);
            row3 = Avx.Multiply(C4, vTemp);
            row4 = Avx.Multiply(C6, vTemp);

            Unsafe.SkipInit(out result);
            ref Vector256<double> vResult = ref Unsafe.As<Matrix4x4d, Vector256<double>>(ref result);

            vResult = row1;
            Unsafe.Add(ref vResult, 1) = row2;
            Unsafe.Add(ref vResult, 2) = row3;
            Unsafe.Add(ref vResult, 3) = row4;

            return true;
        }

        public unsafe static Matrix4x4d Transpose(Matrix4x4d matrix)
        {
            var row1 = Avx.LoadVector256(&matrix.M11);
            var row2 = Avx.LoadVector256(&matrix.M21);
            var row3 = Avx.LoadVector256(&matrix.M31);
            var row4 = Avx.LoadVector256(&matrix.M41);

            var l12 = Avx.UnpackLow(row1, row2);
            var l34 = Avx.UnpackLow(row3, row4);
            var h12 = Avx.UnpackHigh(row1, row2);
            var h34 = Avx.UnpackHigh(row3, row4);

            Avx.Store(&matrix.M11, Vector256.Create(Avx.ExtractVector128(l12, 0), Avx.ExtractVector128(l34, 0)));
            Avx.Store(&matrix.M21, Vector256.Create(Avx.ExtractVector128(h12, 0), Avx.ExtractVector128(h34, 0)));
            Avx.Store(&matrix.M31, Vector256.Create(Avx.ExtractVector128(l12, 1), Avx.ExtractVector128(l34, 1)));
            Avx.Store(&matrix.M41, Vector256.Create(Avx.ExtractVector128(h12, 1), Avx.ExtractVector128(h34, 1)));

            return matrix;
        }

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

        public unsafe static bool operator ==(Matrix4x4d a, Matrix4x4d b)
        {
            var e1 = Avx.MoveMask(Avx.CompareNotEqual(Avx.LoadVector256(&a.M11), Avx.LoadVector256(&b.M11)));
            var e2 = Avx.MoveMask(Avx.CompareNotEqual(Avx.LoadVector256(&a.M21), Avx.LoadVector256(&b.M21)));
            var e3 = Avx.MoveMask(Avx.CompareNotEqual(Avx.LoadVector256(&a.M31), Avx.LoadVector256(&b.M31)));
            var e4 = Avx.MoveMask(Avx.CompareNotEqual(Avx.LoadVector256(&a.M41), Avx.LoadVector256(&b.M41)));

            return e1 == 0 && e2 == 0 && e3 == 0 && e4 == 0;
        }

        public unsafe static bool operator !=(Matrix4x4d a, Matrix4x4d b)
        {
            var e1 = Avx.MoveMask(Avx.CompareEqual(Avx.LoadVector256(&a.M11), Avx.LoadVector256(&b.M11)));
            var e2 = Avx.MoveMask(Avx.CompareEqual(Avx.LoadVector256(&a.M21), Avx.LoadVector256(&b.M21)));
            var e3 = Avx.MoveMask(Avx.CompareEqual(Avx.LoadVector256(&a.M31), Avx.LoadVector256(&b.M31)));
            var e4 = Avx.MoveMask(Avx.CompareEqual(Avx.LoadVector256(&a.M41), Avx.LoadVector256(&b.M41)));

            return e1 == 0 && e2 == 0 && e3 == 0 && e4 == 0;
        }

        public unsafe static Matrix4x4d operator +(Matrix4x4d a, Matrix4x4d b)
        {
            Avx.Store(&a.M11, Avx.Add(Avx.LoadVector256(&a.M11), Avx.LoadVector256(&b.M11)));
            Avx.Store(&a.M21, Avx.Add(Avx.LoadVector256(&a.M21), Avx.LoadVector256(&b.M21)));
            Avx.Store(&a.M31, Avx.Add(Avx.LoadVector256(&a.M31), Avx.LoadVector256(&b.M31)));
            Avx.Store(&a.M41, Avx.Add(Avx.LoadVector256(&a.M41), Avx.LoadVector256(&b.M41)));

            return a;
        }

        public unsafe static Matrix4x4d operator -(Matrix4x4d a, Matrix4x4d b)
        {
            Avx.Store(&a.M11, Avx.Subtract(Avx.LoadVector256(&a.M11), Avx.LoadVector256(&b.M11)));
            Avx.Store(&a.M21, Avx.Subtract(Avx.LoadVector256(&a.M21), Avx.LoadVector256(&b.M21)));
            Avx.Store(&a.M31, Avx.Subtract(Avx.LoadVector256(&a.M31), Avx.LoadVector256(&b.M31)));
            Avx.Store(&a.M41, Avx.Subtract(Avx.LoadVector256(&a.M41), Avx.LoadVector256(&b.M41)));

            return a;
        }

        public unsafe static Matrix4x4d operator -(Matrix4x4d a)
        {
            Avx.Store(&a.M11, Avx.Subtract(Vector256<double>.Zero, Avx.LoadVector256(&a.M11)));
            Avx.Store(&a.M21, Avx.Subtract(Vector256<double>.Zero, Avx.LoadVector256(&a.M21)));
            Avx.Store(&a.M31, Avx.Subtract(Vector256<double>.Zero, Avx.LoadVector256(&a.M31)));
            Avx.Store(&a.M41, Avx.Subtract(Vector256<double>.Zero, Avx.LoadVector256(&a.M41)));

            return a;
        }

        public unsafe static Matrix4x4d operator *(Matrix4x4d a, Matrix4x4d b)
        {
            Avx.Store(
                &a.M11,
                Avx.Add(
                    Avx.Add(
                        Avx.Multiply(Avx.BroadcastScalarToVector256(&a.M11), Avx.LoadVector256(&b.M11)),
                        Avx.Multiply(Avx.BroadcastScalarToVector256(&a.M12), Avx.LoadVector256(&b.M21))
                    ),
                    Avx.Add(
                        Avx.Multiply(Avx.BroadcastScalarToVector256(&a.M13), Avx.LoadVector256(&b.M31)),
                        Avx.Multiply(Avx.BroadcastScalarToVector256(&a.M14), Avx.LoadVector256(&b.M41))
                    )
                )
            );
            Avx.Store(
                &a.M21,
                Avx.Add(
                    Avx.Add(
                        Avx.Multiply(Avx.BroadcastScalarToVector256(&a.M21), Avx.LoadVector256(&b.M11)),
                        Avx.Multiply(Avx.BroadcastScalarToVector256(&a.M22), Avx.LoadVector256(&b.M21))
                    ),
                    Avx.Add(
                        Avx.Multiply(Avx.BroadcastScalarToVector256(&a.M23), Avx.LoadVector256(&b.M31)),
                        Avx.Multiply(Avx.BroadcastScalarToVector256(&a.M24), Avx.LoadVector256(&b.M41))
                    )
                )
            );
            Avx.Store(
                &a.M31,
                Avx.Add(
                    Avx.Add(
                        Avx.Multiply(Avx.BroadcastScalarToVector256(&a.M31), Avx.LoadVector256(&b.M11)),
                        Avx.Multiply(Avx.BroadcastScalarToVector256(&a.M32), Avx.LoadVector256(&b.M21))
                    ),
                    Avx.Add(
                        Avx.Multiply(Avx.BroadcastScalarToVector256(&a.M33), Avx.LoadVector256(&b.M31)),
                        Avx.Multiply(Avx.BroadcastScalarToVector256(&a.M34), Avx.LoadVector256(&b.M41))
                    )
                )
            );
            Avx.Store(
                &a.M41,
                Avx.Add(
                    Avx.Add(
                        Avx.Multiply(Avx.BroadcastScalarToVector256(&a.M41), Avx.LoadVector256(&b.M11)),
                        Avx.Multiply(Avx.BroadcastScalarToVector256(&a.M42), Avx.LoadVector256(&b.M21))
                    ),
                    Avx.Add(
                        Avx.Multiply(Avx.BroadcastScalarToVector256(&a.M43), Avx.LoadVector256(&b.M31)),
                        Avx.Multiply(Avx.BroadcastScalarToVector256(&a.M44), Avx.LoadVector256(&b.M41))
                    )
                )
            );

            return a;
        }

        public unsafe static Matrix4x4d operator *(Matrix4x4d a, double s)
        {
            var scalar = Vector256.Create(s);
            Avx.Store(&a.M11, Avx.Multiply(Avx.LoadVector256(&a.M11), scalar));
            Avx.Store(&a.M21, Avx.Multiply(Avx.LoadVector256(&a.M21), scalar));
            Avx.Store(&a.M31, Avx.Multiply(Avx.LoadVector256(&a.M31), scalar));
            Avx.Store(&a.M41, Avx.Multiply(Avx.LoadVector256(&a.M41), scalar));

            return a;
        }

        public unsafe static implicit operator Matrix4x4d(Matrix4x4 m)
        {
            var result = Zero;

            Avx.Store(&result.M11, Avx.ConvertToVector256Double(Sse.LoadVector128(&m.M11)));
            Avx.Store(&result.M21, Avx.ConvertToVector256Double(Sse.LoadVector128(&m.M21)));
            Avx.Store(&result.M31, Avx.ConvertToVector256Double(Sse.LoadVector128(&m.M31)));
            Avx.Store(&result.M41, Avx.ConvertToVector256Double(Sse.LoadVector128(&m.M41)));

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector256<double> Permute(Vector256<double> v, byte control)
        {
            if (Avx2.IsSupported)
            {
                return Avx2.Permute4x64(v, control);
            }
            else
            {
                return v.Shuffle4x64(v, control);
            }
        }
    }

    public static class Matrix4x4dExtension
    {
        public unsafe static Vector256<double> Transform(this Matrix4x4d matrix, in Vector256<double> v)
        {
            var m1 = Avx.LoadVector256(&matrix.M11);
            var m2 = Avx.LoadVector256(&matrix.M21);
            var m3 = Avx.LoadVector256(&matrix.M31);
            var m4 = Avx.LoadVector256(&matrix.M41);

            return Avx.Add(
                Avx.Add(
                    Avx.Multiply(Vector256.Create(v.GetElement(0)), m1),
                    Avx.Multiply(Vector256.Create(v.GetElement(1)), m2)
                ),
                Avx.Add(
                    Avx.Multiply(Vector256.Create(v.GetElement(2)), m3),
                    Avx.Multiply(Vector256.Create(v.GetElement(3)), m4)
                )
            );
        }
    }
}
