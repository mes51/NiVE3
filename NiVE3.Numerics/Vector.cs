using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using NiVE3.Shared.Extension;

namespace NiVE3.Numerics
{
    public readonly struct Vector2d : IEquatable<Vector2d>
    {
        public static readonly Vector2d Zero = new Vector2d();

        public static readonly Vector2d One = new Vector2d(1.0, 1.0);

        public static readonly Vector2d MaxValue = new Vector2d(double.MaxValue);

        public static readonly Vector2d MinValue = new Vector2d(double.MinValue);

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
        public Vector128<double> AsVector128()
        {
            return Vector128.LoadUnsafe(in X);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double Length()
        {
            return Math.Sqrt(AsVector128().LengthSquared());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2d Normalize()
        {
            return (Vector2d)AsVector128().Normalize();
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString()
        {
            return $"< X = {X}, Y = {Y} >";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Vector2d other)
        {
            return X == other.X && Y == other.Y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2d Min(in Vector2d a, in Vector2d b)
        {
            return (Vector2d)Vector128.Min(a.AsVector128(), b.AsVector128());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2d Max(in Vector2d a, in Vector2d b)
        {
            return (Vector2d)Vector128.Max(a.AsVector128(), b.AsVector128());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2d Clamp(in Vector2d v, in Vector2d min, in Vector2d max)
        {
            return new Vector2d(Math.Clamp(v.X, min.X, max.X), Math.Clamp(v.Y, min.Y, max.Y));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2d MinWithoutNaN(in Vector2d a, in Vector2d b)
        {
            return new Vector2d(
                double.IsNaN(a.X) && double.IsNaN(b.X) ? 0.0 : Math.Min(a.X, b.X),
                double.IsNaN(a.Y) && double.IsNaN(b.Y) ? 0.0 : Math.Min(a.Y, b.Y)
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2d MaxWithoutNaN(in Vector2d a, in Vector2d b)
        {
            return new Vector2d(
                double.IsNaN(a.X) && double.IsNaN(b.X) ? 0.0 : Math.Max(a.X, b.X),
                double.IsNaN(a.Y) && double.IsNaN(b.Y) ? 0.0 : Math.Max(a.Y, b.Y)
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2d ClampWithoutNaN(in Vector2d v, in Vector2d min, in Vector2d max)
        {
            return new Vector2d(
                double.IsNaN(v.X) ? 0.0 : Math.Clamp(v.X, min.X, max.X),
                double.IsNaN(v.Y) ? 0.0 : Math.Clamp(v.Y, min.Y, max.Y)
            );
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
        public static bool operator ==(Vector2d a, Vector2d b)
        {
            return a.Equals(b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Vector2d a, Vector2d b)
        {
            return !a.Equals(b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Vector128<double>(Vector2d v)
        {
            return Vector128.LoadUnsafe(in v.X);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Vector256<double>(Vector2d v)
        {
            return Vector256.Create(v.X, v.Y, 0.0, 0.0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Vector2d(Vector128<double> v)
        {
            return Unsafe.BitCast<Vector128<double>, Vector2d>(v);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator System.Windows.Vector(Vector2d v)
        {
            return new System.Windows.Vector(v.X, v.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Vector2d(System.Windows.Vector v)
        {
            return new Vector2d(v.X, v.Y);
        }
    }

    public readonly struct Vector3d : IEquatable<Vector3d>
    {
        public static readonly Vector3d Zero = new Vector3d();

        public static readonly Vector3d One = new Vector3d(1.0);

        public static readonly Vector3d MaxValue = new Vector3d(double.MaxValue);

        public static readonly Vector3d MinValue = new Vector3d(double.MinValue);

        [JsonInclude]
        public readonly double X;

        [JsonInclude]
        public readonly double Y;

        [JsonInclude]
        public readonly double Z;

        public Vector3d(double value) : this(value, value, value) { }

        public Vector3d(Vector2d v, double z) : this(v.X, v.Y, z) { }

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
        public Vector256<double> ToHomogeneousCoord()
        {
            return Vector256.Create(X, Y, Z, 1.0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double Length()
        {
            return Math.Sqrt(AsVector256().LengthSquared());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3d Normalize()
        {
            return (Vector3d)AsVector256().Normalize();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsNaN()
        {
            return double.IsNaN(X) || double.IsNaN(Y) || double.IsNaN(Z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsInfinty()
        {
            return double.IsInfinity(X) || double.IsInfinity(Y) || double.IsInfinity(Z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y, Z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString()
        {
            return $"< X = {X}, Y = {Y}, Z = {Z} >";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Vector3d other)
        {
            return X == other.X && Y == other.Y && Z == other.Z;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3d Min(in Vector3d a, in Vector3d b)
        {
            return (Vector3d)Vector256.Min(a.AsVector256(), b.AsVector256());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3d Max(in Vector3d a, in Vector3d b)
        {
            return (Vector3d)Vector256.Max(a.AsVector256(), b.AsVector256());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3d Clamp(in Vector3d v, in Vector3d min, in Vector3d max)
        {
            return new Vector3d(Math.Clamp(v.X, min.X, max.X), Math.Clamp(v.Y, min.Y, max.Y), Math.Clamp(v.Z, min.Z, max.Z));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3d MinWithoutNaN(in Vector3d a, in Vector3d b)
        {
            return new Vector3d(
                double.IsNaN(a.X) && double.IsNaN(b.X) ? 0.0 : Math.Min(a.X, b.X),
                double.IsNaN(a.Y) && double.IsNaN(b.Y) ? 0.0 : Math.Min(a.Y, b.Y),
                double.IsNaN(a.Z) && double.IsNaN(b.Z) ? 0.0 : Math.Min(a.Z, b.Z)
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3d MaxWithoutNaN(in Vector3d a, in Vector3d b)
        {
            return new Vector3d(
                double.IsNaN(a.X) && double.IsNaN(b.X) ? 0.0 : Math.Max(a.X, b.X),
                double.IsNaN(a.Y) && double.IsNaN(b.Y) ? 0.0 : Math.Max(a.Y, b.Y),
                double.IsNaN(a.Z) && double.IsNaN(b.Z) ? 0.0 : Math.Max(a.Z, b.Z)
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3d ClampWithoutNaN(in Vector3d v, in Vector3d min, in Vector3d max)
        {
            return new Vector3d(
                double.IsNaN(v.X) ? 0.0 : Math.Clamp(v.X, min.X, max.X),
                double.IsNaN(v.Y) ? 0.0 : Math.Clamp(v.Y, min.Y, max.Y),
                double.IsNaN(v.Z) ? 0.0 : Math.Clamp(v.Z, min.Z, max.Z)
            );
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
        public static bool operator ==(Vector3d a, Vector3d b)
        {
            return a.Equals(b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Vector3d a, Vector3d b)
        {
            return !a.Equals(b);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Vector2d(Vector3d v)
        {
            return new Vector2d(v.X, v.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Vector3d(Vector2d v)
        {
            return new Vector3d(v.X, v.Y, 0.0);
        }
    }
}
