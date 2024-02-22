using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;

namespace NiVE3.Numerics
{
    public readonly struct Vector2d : IEquatable<Vector2d>
    {
        public static readonly Vector2d Zero = new Vector2d();

        public static readonly Vector2d One = new Vector2d(1.0, 1.0);

        [JsonInclude]
        public readonly double X;

        [JsonInclude]
        public readonly double Y;

        public bool IsZero => X == 0 && Y == 0;

        public Vector2d(double value) : this(value, value) { }

        [JsonConstructor]
        public Vector2d(double x, double y)
        {
            X = x;
            Y = y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3d AsVector3d()
        {
            return new Vector3d(X, Y, 0.0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector256<double> AsVector256()
        {
            return Vector256.Create(X, Y, 0.0, 0.0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsNaN()
        {
            return double.IsNaN(X) || double.IsNaN(Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsInfinty()
        {
            return double.IsInfinity(X) || double.IsInfinity(Y);
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is Vector2d v)
            {
                return Equals(v);
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
            return hashCode.ToHashCode();
        }

        public override string ToString()
        {
            return $"< X = {X}, Y = {Y} >";
        }

        public bool Equals(Vector2d other)
        {
            return X == other.X && Y == other.Y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2d operator +(Vector2d a)
        {
            return a;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2d operator +(Vector2d a, Vector2d b)
        {
            return new Vector2d(a.X + b.X, a.Y + b.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2d operator -(Vector2d a)
        {
            return new Vector2d(-a.X, -a.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2d operator -(Vector2d a, Vector2d b)
        {
            return new Vector2d(a.X - b.X, a.Y - b.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2d operator *(Vector2d a, double s)
        {
            return new Vector2d(a.X * s, a.Y * s);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2d operator *(double s, Vector2d a)
        {
            return new Vector2d(a.X * s, a.Y * s);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2d operator *(Vector2d a, Vector2d b)
        {
            return new Vector2d(a.X * b.X, a.Y * b.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2d operator /(Vector2d a, double s)
        {
            return new Vector2d(a.X / s, a.Y / s);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2d operator /(double s, Vector2d a)
        {
            return new Vector2d(s / a.X, s / a.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2d operator /(Vector2d a, Vector2d b)
        {
            return new Vector2d(a.X / b.X, a.Y / b.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2d operator %(Vector2d a, double s)
        {
            return new Vector2d(a.X % s, a.Y % s);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Vector128<double>(Vector2d v)
        {
            return Vector128.Create(v.X, v.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Vector256<double>(Vector2d v)
        {
            return Vector256.Create(v.X, v.Y, 0.0, 0.0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Vector2d(Vector128<double> v)
        {
            return new Vector2d(v.GetElement(0), v.GetElement(1));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Vector2d(Vector256<double> v)
        {
            return new Vector2d(v.GetElement(0), v.GetElement(1));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Vector2(Vector2d v)
        {
            return new Vector2((float)v.X, (float)v.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Vector2d(Vector2 v)
        {
            return new Vector2d(v.X, v.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Point(Vector2d v)
        {
            return new Point(v.X, v.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Vector2d(Point v)
        {
            return new Vector2d(v.X, v.Y);
        }
    }

    public readonly struct Vector3d : IEquatable<Vector3d>
    {
        [JsonInclude]
        public readonly double X;

        [JsonInclude]
        public readonly double Y;

        [JsonInclude]
        public readonly double Z;

        public Vector3d(double value) : this(value, value, value) { }

        [JsonConstructor]
        public Vector3d(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2d AsVector2d()
        {
            return new Vector2d(X, Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector256<double> AsVector256()
        {
            return Vector256.Create(X, Y, Z, 0.0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double Length()
        {
            return Math.Sqrt(X * X + Y * Y + Z * Z);
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is Vector3d v)
            {
                return Equals(v);
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
            return $"< X = {X}, Y = {Y}, Z = {Z} >";
        }

        public bool Equals(Vector3d other)
        {
            return X == other.X && Y == other.Y && Z == other.Z;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3d operator +(Vector3d a)
        {
            return a;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3d operator +(Vector3d a, Vector3d b)
        {
            return new Vector3d(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3d operator -(Vector3d a)
        {
            return new Vector3d(-a.X, -a.Y, -a.Z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3d operator -(Vector3d a, Vector3d b)
        {
            return new Vector3d(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3d operator *(Vector3d a, double s)
        {
            return new Vector3d(a.X * s, a.Y * s, a.Z * s);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3d operator *(double s, Vector3d a)
        {
            return new Vector3d(a.X * s, a.Y * s, a.Z * s);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3d operator *(Vector3d a, Vector3d b)
        {
            return new Vector3d(a.X * b.X, a.Y * b.Y, a.Z * b.Z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3d operator /(Vector3d a, double s)
        {
            return new Vector3d(a.X / s, a.Y / s, a.Z / s);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3d operator /(double s, Vector3d a)
        {
            return new Vector3d(s / a.X, s / a.Y, s / a.Z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3d operator /(Vector3d a, Vector3d b)
        {
            return new Vector3d(a.X / b.X, a.Y / b.Y, a.Z / b.Z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3d operator %(Vector3d a, double s)
        {
            return new Vector3d(a.X % s, a.Y % s, a.Z % s);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Vector256<double>(Vector3d v)
        {
            return Vector256.Create(v.X, v.Y, v.Z, 0.0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Vector3d(Vector256<double> v)
        {
            return new Vector3d(v.GetElement(0), v.GetElement(1), v.GetElement(2));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Vector3(Vector3d v)
        {
            return new Vector3((float)v.X, (float)v.Y, (float)v.Z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Vector3d(Vector3 v)
        {
            return new Vector3d(v.X, v.Y, v.Z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Vector2(Vector3d v)
        {
            return new Vector2((float)v.X, (float)v.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Vector3d(Vector2 v)
        {
            return new Vector3d(v.X, v.Y, 0.0);
        }
    }
}
