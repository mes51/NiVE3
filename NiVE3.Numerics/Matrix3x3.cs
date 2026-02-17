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
using NiVE3.Shared.Extension;

namespace NiVE3.Numerics
{
    /// <summary>
    /// 3x3の行列
    /// </summary>
    // TODO: 行列の向き直す?
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

        public readonly float M11 => X.X;
        public readonly float M12 => X.Y;
        public readonly float M13 => X.Z;
        public readonly float M21 => Y.X;
        public readonly float M22 => Y.Y;
        public readonly float M23 => Y.Z;
        public readonly float M31 => Z.X;
        public readonly float M32 => Z.Y;
        public readonly float M33 => Z.Z;

        public Matrix3x3(float m11, float m12, float m21, float m22, float m31, float m32) : this(m11, m12, 0.0F, m21, m22, 0.0F, m31, m32, 1.0F) { }

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

        public readonly bool IsIdentity => Equals(Identity);

        public readonly bool Equals(Matrix3x3 other)
        {
            return X == other.X && Y == other.Y && Z == other.Z;
        }

        public override readonly bool Equals([NotNullWhen(true)] object? obj)
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

        public override readonly int GetHashCode()
        {
            var hashCode = new HashCode();

            hashCode.Add(X);
            hashCode.Add(Y);
            hashCode.Add(Z);

            return hashCode.ToHashCode();
        }

        public override readonly string ToString()
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Matrix3x3 Translate(float x, float y)
        {
            return this * CreateTranslate(x, y);
        }

        /// <summary>
        /// 回転します
        /// </summary>
        /// <param name="angle">回転角度</param>
        /// <returns>計算後の行列</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Matrix3x3 Rotate(float angle)
        {
            return this * CreateRotate(angle);
        }

        /// <summary>
        /// スケーリングします
        /// </summary>
        /// <param name="w">Xの比</param>
        /// <param name="h">Yの比</param>
        /// <returns>計算後の行列</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Matrix3x3 Scale(float w, float h)
        {
            return this * CreateScale(w, h);
        }

        /// <summary>
        /// 一次変換を行います
        /// </summary>
        /// <param name="x">変換する点のX座標</param>
        /// <param name="y">変換する点のY座標</param>
        /// <returns>変換後の点</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly (float x, float y) Transform(float x, float y)
        {
            var result = x * X + y * Y + Z;
            return (result.X, result.Y);
        }

        /// <summary>
        /// 一次変換を行います
        /// </summary>
        /// <param name="v">変換する点</param>
        /// <returns>変換後の点</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Vector2 Transform(Vector2 v)
        {
            return (v.X * X + v.Y * Y + Z).AsVector128().AsVector2();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly float Determinant()
        {
            return M11 * M22 * M33 + M12 * M23 * M31 + M13 * M21 * M32 - (M13 * M22 * M31 + M32 * M23 * M11 + M33 * M21 * M12);
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
                    -(matrix.M12 * matrix.M33 - matrix.M13 * matrix.M32),
                    matrix.M12 * matrix.M23 - matrix.M13 * matrix.M22
                ) / det;
                result.Y = new Vector3(
                    -(matrix.M21 * matrix.M33 - matrix.M23 * matrix.M31),
                    matrix.M11 * matrix.M33 - matrix.M13 * matrix.M31,
                    -(matrix.M11 * matrix.M23 - matrix.M13 * matrix.M21)
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3x3 AffineTransform(Vector2 anchorPoint, Vector2 scale, float angle, Vector2 translate)
        {
            return Identity.Translate(-anchorPoint.X, -anchorPoint.Y)
                .Scale(scale.X, scale.Y)
                .Rotate(angle)
                .Translate(translate.X, translate.Y);
        }

        /// <summary>
        /// 平行移動します
        /// </summary>
        /// <param name="x">Xの移動距離</param>
        /// <param name="y">Yの移動距離</param>
        /// <returns>生成された行列</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3x3 CreateTranslate(float x, float y)
        {
            return new Matrix3x3(1.0F, 0.0F, 0.0F, 1.0F, x, y);
        }

        /// <summary>
        /// 回転します
        /// </summary>
        /// <param name="angle">回転角度</param>
        /// <returns>生成された行列</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3x3 CreateRotate(float angle)
        {
            var rad = (float)(Math.PI / 180.0 * angle);
            var cos = MathF.Cos(rad);
            var sin = MathF.Sin(rad);

            return new Matrix3x3(cos, sin, -sin, cos, 0.0F, 0.0F);
        }

        /// <summary>
        /// 指定した点で回転します
        /// </summary>
        /// <param name="angle">回転角度</param>
        /// <param name="x">中心X座標</param>
        /// <param name="y">中心Y座標</param>
        /// <returns>生成された行列</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3x3 CreateRotateAt(float angle, float x, float y)
        {
            var rad = (float)(Math.PI / 180.0 * angle);
            var cos = MathF.Cos(rad);
            var sin = MathF.Sin(rad);

            return new Matrix3x3(cos, sin,  -sin, cos, x - x * cos + y * sin, y - x * sin - y * cos);
        }

        /// <summary>
        /// スケーリングします
        /// </summary>
        /// <param name="w">Xの比</param>
        /// <param name="h">Yの比</param>
        /// <returns>生成された行列</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3x3 CreateScale(float w, float h)
        {
            return new Matrix3x3(w, 0.0F, 0.0F, h, 0.0F, 0.0F);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(in Matrix3x3 a, in Matrix3x3 b)
        {
            return a.Equals(b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(in Matrix3x3 a, in Matrix3x3 b)
        {
            return !a.Equals(b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3x3 operator +(Matrix3x3 a, in Matrix3x3 b)
        {
            a.X += b.X;
            a.Y += b.Y;
            a.Z += b.Z;
            return a;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3x3 operator -(Matrix3x3 a, in Matrix3x3 b)
        {
            a.X -= b.X;
            a.Y -= b.Y;
            a.Z -= b.Z;
            return a;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3x3 operator -(Matrix3x3 a)
        {
            a.X = -a.X;
            a.Y = -a.Y;
            a.Z = -a.Z;
            return a;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3x3 operator *(Matrix3x3 a, in Matrix3x3 b)
        {
            a.X = a.X.X * b.X + a.X.Y * b.Y + a.X.Z * b.Z;
            a.Y = a.Y.X * b.X + a.Y.Y * b.Y + a.Y.Z * b.Z;
            a.Z = a.Z.X * b.X + a.Z.Y * b.Y + a.Z.Z * b.Z;

            return a;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3x3 operator *(Matrix3x3 a, float s)
        {
            a.X *= s;
            a.Y *= s;
            a.Z *= s;
            return a;
        }

        public static (Vector2 position, Vector2 scale, float angle) Decompose(Matrix3x3 matrix)
        {
            const float ScaleEpsilon = 1E-7F;
            const float ShearThreshold = 1E-4F;

            var position = new Vector2(matrix.M31, matrix.M32);

            matrix.X *= new Vector3(1.0F, 1.0F, 0.0F);
            matrix.Y *= new Vector3(1.0F, 1.0F, 0.0F);
            matrix.Z = new Vector3(0.0F, 0.0F, 1.0F);

            var sx = matrix.X.Length();
            var sy = matrix.Y.Length();

            if (Math.Abs(sx) < ScaleEpsilon || Math.Abs(sy) < ScaleEpsilon)
            {
                return (position, Vector2.Zero, 0.0F);
            }

            var det = matrix.M11 * matrix.M22 - matrix.M12 * matrix.M21;
            if (det < 0.0F)
            {
                sx = -sx;
            }

            var r0 = matrix.X / sx;
            var r1 = matrix.Y / sy;
            var outDiagonal = Math.Abs(Vector3.Dot(r0, r1));
            var diagonalDiff = Math.Abs(r0.LengthSquared() - 1.0F) + Math.Abs(r1.LengthSquared() - 1.0F);
            var hasShear = (outDiagonal + diagonalDiff) > ShearThreshold;

            var scale = new Vector2(sx, sy);
            var rotate = 0.0F;
            if (hasShear)
            {
                rotate = MathF.Atan2(r0.Y - r1.X, r0.X + r1.Y);
                var cos = MathF.Cos(rotate);
                var sin = MathF.Sin(rotate);
                scale = new Vector2(matrix.M11 * cos + matrix.M12 * sin, -matrix.M21 * sin + matrix.M22 * cos);
            }
            else
            {
                rotate = MathF.Atan2(r0.Y, r0.X);
            }


            return (position, scale, rotate / MathF.PI * 180.0F);
        }
    }
}
