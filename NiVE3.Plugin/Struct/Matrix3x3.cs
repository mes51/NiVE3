using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using ILGPU.IR.Values;
using NiVE3.Shared.Extension;

namespace NiVE3.Plugin.Struct
{
    /// <summary>
    /// 3x3の行列
    /// </summary>
    public struct Matrix3x3 : IEquatable<Matrix3x3>
    {
        static readonly Matrix3x3 _Identity = new Matrix3x3(
            1.0F, 0.0F, 0.0F,
            0.0F, 1.0F, 0.0F,
            0.0F, 0.0F, 1.0F
        );
        public static Matrix3x3 Identity => _Identity;

        static readonly Matrix3x3 _Zero = new Matrix3x3();
        public static Matrix3x3 Zero => _Zero;

        Vector3 X;
        Vector3 Y;
        Vector3 Z;

        public float M11 => X.X;
        public float M12 => X.Y;
        public float M13 => X.Z;
        public float M21 => Y.X;
        public float M22 => Y.Y;
        public float M23 => Y.Z;
        public float M31 => Z.X;
        public float M32 => Z.Y;
        public float M33 => Z.Z;

        public Matrix3x3(float m11, float m12, float m13, float m21, float m22, float m23) : this(m11, m12, m13, m21, m22, m23, 0.0F, 0.0F, 1.0F) { }

        public Matrix3x3(
            float m11, float m12, float m13,
            float m21, float m22, float m23,
            float m31, float m32, float m33
        )
        {
            X = new Vector3(m11, m12, m13);
            Y = new Vector3(m21, m22, m23);
            Z = new Vector3(m31, m32, m33);
        }

        public bool IsIdentity => Equals(Identity);

        public bool Equals(Matrix3x3 other)
        {
            return X == other.X && Y == other.Y && Z == other.Z;
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is Matrix3x3 m)
            {
                return Equals(m);
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();

            hashCode.Add(X);
            hashCode.Add(Y);
            hashCode.Add(Z);

            return hashCode.ToHashCode();
        }

        public override string ToString()
        {
            return $@"{{
    {{ M11: {M11}, M12: {M12}, M13: {M13} }}
    {{ M21: {M21}, M22: {M22}, M23: {M23} }}
    {{ M31: 
            {M31}, M32: {M32}, M33: {M33} }}
}}
            ";
        }

        /// <summary>
        /// 平行移動します
        /// </summary>
        /// <param name="x">Xの移動距離</param>
        /// <param name="y">Yの移動距離</param>
        /// <returns>計算後の行列</returns>
        public Matrix3x3 Translate(float x, float y)
        {
            return this * new Matrix3x3(1.0F, 0.0F, x, 0.0F, 1.0F, y);
        }

        /// <summary>
        /// 回転します
        /// </summary>
        /// <param name="angle">回転角度</param>
        /// <returns>計算後の行列</returns>
        public Matrix3x3 Rotate(float angle)
        {
            var rad = (float)(Math.PI / 180.0 * angle);
            var cos = MathF.Cos(rad);
            var sin = MathF.Sin(rad);

            return this * new Matrix3x3(cos, -sin, 0, sin, cos, 0.0F);
        }

        /// <summary>
        /// 指定した点で回転します
        /// </summary>
        /// <param name="x">点のX座標</param>
        /// <param name="y">点のY座標</param>
        /// <param name="angle">回転角度</param>
        /// <returns>計算後の行列</returns>
        public Matrix3x3 RotateAt(float x, float y, float angle)
        {
            var rad = (float)(Math.PI / 180.0 * angle);
            var cos = MathF.Cos(rad);
            var sin = MathF.Sin(rad);

            return this * new Matrix3x3(cos, -sin, x - x * cos + y * sin, sin, cos, y - x * sin - y * cos);
        }

        /// <summary>
        /// スケーリングします
        /// </summary>
        /// <param name="w">Xの比</param>
        /// <param name="h">Yの比</param>
        /// <returns>計算後の行列</returns>
        public Matrix3x3 Scale(float w, float h)
        {
            return this * new Matrix3x3(w, 0.0F, 0.0F, 0.0F, h, 0.0F);
        }

        /// <summary>
        /// 一次変換を行います
        /// </summary>
        /// <param name="x">変換する点のX座標</param>
        /// <param name="y">変換する点のY座標</param>
        /// <returns>変換後の点</returns>
        public (float x, float y) Transform(float x, float y)
        {
            var p = new Vector3(x, y, 1.0F);
            return ((p * X).HorizontalAdd(), (p * Y).HorizontalAdd());
        }

        /// <summary>
        /// 一次変換を行います
        /// </summary>
        /// <param name="v">変換する点</param>
        /// <returns>変換後の点</returns>
        public Vector2 Transform(Vector2 v)
        {
            var p = new Vector3(v, 1.0F);
            return new Vector2((p * X).HorizontalAdd(), (p * Y).HorizontalAdd());
        }

        float Determinant()
        {
            return (M11 * M22 * M33 + M12 * M23 * M31 + M13 * M21 * M32) - (M13 * M22 * M31 + M32 * M23 * M11 + M33 * M21 * M12);
        }

        /// <summary>
        /// 逆行列を求めます
        /// </summary>
        /// <param name="matrix">元の行列</param>
        /// <param name="result">求めた逆行列</param>
        /// <returns>逆行列が存在するかどうか</returns>
        public static bool Invert(Matrix3x3 matrix, out Matrix3x3 result)
        {
            var det = matrix.Determinant();

            result = new Matrix3x3();
            if (det != 0.0F)
            {
                result.X = new Vector3(
                    matrix.M22 * matrix.M33 - matrix.M23 * matrix.M32,
                    - (matrix.M12 * matrix.M33 - matrix.M13 * matrix.M32),
                    matrix.M12 * matrix.M23 - matrix.M13 * matrix.M22
                ) / det;
                result.Y = new Vector3(
                    -(matrix.M21 * matrix.M33 - matrix.M23 * matrix.M31),
                    matrix.M11 * matrix.M33 - matrix.M13 * matrix.M31,
                    - (matrix.M11 * matrix.M23 - matrix.M13 * matrix.M21)
                ) / det;
                result.Z = new Vector3(
                    matrix.M21 * matrix.M32 - matrix.M22 * matrix.M31,
                    -(matrix.M11 * matrix.M32 - matrix.M12 * matrix.M31),
                    matrix.M11 * matrix.M22 - matrix.M12 * matrix.M21
                ) / det;

                return true;
            }
            else
            {
                return false;
            }
        }

        public static Matrix3x3 AffineTransform(Vector2 anchorPoint, Vector2 scale, float angle, Vector2 position)
        {
            return Identity.Translate(position.X, position.Y)
                .Rotate(angle)
                .Scale(scale.X, scale.Y)
                .Translate(-anchorPoint.X, -anchorPoint.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static bool operator ==(Matrix3x3 a, Matrix3x3 b)
        {
            return a.Equals(b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static bool operator !=(Matrix3x3 a, Matrix3x3 b)
        {
            return !a.Equals(b);
        }

        public unsafe static Matrix3x3 operator +(Matrix3x3 a, Matrix3x3 b)
        {
            a.X += b.X;
            a.Y += b.Y;
            a.Z += b.Z;
            return a;
        }

        public unsafe static Matrix3x3 operator -(Matrix3x3 a, Matrix3x3 b)
        {
            a.X -= b.X;
            a.Y -= b.Y;
            a.Z -= b.Z;
            return a;
        }

        public unsafe static Matrix3x3 operator -(Matrix3x3 a)
        {
            a.X = -a.X;
            a.Y = -a.Y;
            a.Z = -a.Z;
            return a;
        }

        public unsafe static Matrix3x3 operator *(Matrix3x3 a, Matrix3x3 b)
        {
            a.X = a.X.X * b.X + a.X.Y * b.Y + a.X.Z * b.Z;
            a.Y = a.Y.X * b.X + a.Y.Y * b.Y + a.Y.Z * b.Z;
            a.Z = a.Z.X * b.X + a.Z.Y * b.Y + a.Z.Z * b.Z;

            return a;
        }

        public unsafe static Matrix3x3 operator *(Matrix3x3 a, float s)
        {
            a.X *= s;
            a.Y *= s;
            a.Z *= s;
            return a;
        }
    }
}
