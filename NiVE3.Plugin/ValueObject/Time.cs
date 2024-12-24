using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using NiVE3.Plugin.Property;

namespace NiVE3.Plugin.ValueObject
{
    /// <summary>
    /// 時間を表す構造体
    /// </summary>
    public readonly struct Time : INumber<Time>, IMinMaxValue<Time>, IComparable<double>, IEquatable<double>
    {
        const int TimeDigit = 10;

        const int FrameRateDigit = 2;

        static int INumberBase<Time>.Radix => 2;

        static Time IMultiplicativeIdentity<Time, Time>.MultiplicativeIdentity => One;

        static Time IAdditiveIdentity<Time, Time>.AdditiveIdentity => Zero;

        public static Time One => new Time(1.0);

        public static Time Zero => new Time(0.0);

        public static Time MaxValue => new Time(double.MaxValue);

        public static Time MinValue => new Time(double.MinValue);

        public static Time NaN => new Time(double.NaN);

        public static Time PositiveInfinity => new Time(double.PositiveInfinity);

        public static Time NegativeInfinity => new Time(double.NegativeInfinity);

        /// <summary>
        /// フレームによらない実時間。フレーム数とフレームレートによって時間を表現する場合はNaN
        /// </summary>
        public readonly double RealTime;

        /// <summary>
        /// フレーム数。実時間によって時間を表現する場合は0
        /// </summary>
        public readonly long Frame;

        /// <summary>
        /// フレームレート。実時間によって時間を表現する場合は0
        /// </summary>
        public readonly double FrameRate;

        /// <summary>
        /// 時間をフレーム数とフレームレートで表しているかどうか
        /// </summary>
        public readonly bool IsFrameTime;

        /// <summary>
        /// フレームレートが整数であるかどうか。非整数の場合、同じフレームレート同士以外での計算は全て実時間で計算されます
        /// </summary>
        public readonly bool FrameRateIsInteger;

        public Time(long frame, double frameRate)
        {
            if (double.IsNaN(frameRate) || double.IsInfinity(frameRate) || frameRate <= 0.0)
            {
                throw new ArgumentException(null, nameof(frameRate));
            }

            RealTime = double.NaN;
            Frame = frame;
            FrameRate = frameRate;
            IsFrameTime = true;
            FrameRateIsInteger = IsIntegerFrame(frameRate);
        }

        internal Time(double realTime)
        {
            RealTime = realTime;
            IsFrameTime = false;
            FrameRateIsInteger = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Time RoundToFrameRate(double newFrameRate)
        {
            if (IsFrameTime && FrameRate == newFrameRate)
            {
                return this;
            }
            else
            {
                var time = IsFrameTime ? ToDoubleNonRounded(this) : RealTime;
                return new Time((int)Math.Round(time * newFrameRate), newFrameRate);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Time FloorToFrameRate(double newFrameRate)
        {
            if (IsFrameTime && FrameRate == newFrameRate)
            {
                return this;
            }
            else
            {
                var time = IsFrameTime ? ToDoubleNonRounded(this) : RealTime;
                return new Time((int)Math.Floor(time * newFrameRate), newFrameRate);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Time CeilingToFrameRate(double newFrameRate)
        {
            if (IsFrameTime && FrameRate == newFrameRate)
            {
                return this;
            }
            else
            {
                var time = IsFrameTime ? ToDoubleNonRounded(this) : RealTime;
                return new Time((int)Math.Ceiling(time * newFrameRate), newFrameRate);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is Time time)
            {
                return Equals(time);
            }
            else if (obj is double realTime)
            {
                return Equals(realTime);
            }
            else
            {
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Time other)
        {
            if (IsFrameTime)
            {
                if (!other.IsFrameTime)
                {
                    return (double)this == other.RealTime;
                }
                else
                {
                    return FrameRate == other.FrameRate ? Frame == other.Frame : (double)this == (double)other;
                }
            }
            else
            {
                return (double)this == (double)other;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(double other)
        {
            return (double)this == other;
        }

        public override int GetHashCode()
        {
            return ((double)this).GetHashCode();
        }

        public override string ToString()
        {
            if (IsFrameTime)
            {
                return $"<Frame: {Frame}, FrameRate: {FrameRate}>";
            }
            else
            {
                return $"<RealTime: {RealTime}>";
            }
        }

        public int CompareTo(object? obj)
        {
            if (obj is Time time)
            {
                return CompareTo(time);
            }
            else if (obj is double realTime)
            {
                return CompareTo(realTime);
            }
            else if (obj == null)
            {
                return 1;
            }
            else
            {
                throw new ArgumentException("obj is must be time or double");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(Time other)
        {
            if (FrameRate == other.FrameRate)
            {
                if (IsFrameTime)
                {
                    return Frame.CompareTo(other.Frame);
                }
                else
                {
                    return RealTime.CompareTo(other.RealTime);
                }
            }
            else
            {
                return ((double)this).CompareTo((double)other);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(double other)
        {
            return ((double)this).CompareTo(other);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Time FromTime(double time, double frameRate = 0.0)
        {
            if (double.IsNaN(frameRate) || double.IsInfinity(frameRate) || frameRate < 0.0)
            {
                throw new ArgumentException(null, nameof(frameRate));
            }
            if (frameRate == 0.0)
            {
                return new Time(RoundTimeDigit(time));
            }

            var frame = RoundFrameDigit(time * frameRate);
            if (IsIntegerFrame(frame))
            {
                return new Time((int)frame, frameRate);
            }
            else
            {
                return new Time(RoundTimeDigit(time));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static long LeastCommonMultiple(long a, long b)
        {
            return a * b / GratestCommonDivisor(a, b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static long GratestCommonDivisor(long a, long b)
        {
            a = Math.Abs(a);
            b = Math.Abs(b);
            if (a == 0 || b == 0)
            {
                return a + b;
            }
            var tzcA = BitOperations.TrailingZeroCount(a);
            var tzcB = BitOperations.TrailingZeroCount(b);
            a >>= tzcA;
            b >>= tzcB;
            while (a != b)
            {
                if (a > b)
                {
                    a -= b;
                    a >>= BitOperations.TrailingZeroCount(a);
                }
                else
                {
                    b -= a;
                    b >>= BitOperations.TrailingZeroCount(b);
                }
            }

            return a << Math.Min(tzcA, tzcB);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static double RoundTimeDigit(double time)
        {
            return Math.Round(time, TimeDigit);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static double RoundFrameDigit(double frame)
        {
            return Math.Round(frame, FrameRateDigit);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static double ToDoubleNonRounded(in Time value)
        {
            return value.Frame / value.FrameRate;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool IsIntegerFrame(double frameRate)
        {
            return RoundFrameDigit(frameRate - (int)frameRate) == 0.0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Time Abs(Time value)
        {
            return value.IsFrameTime ? new Time(Math.Abs(value.Frame), value.FrameRate) : new Time(Math.Abs(value.RealTime));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCanonical(Time value)
        {
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsComplexNumber(Time value)
        {
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEvenInteger(Time value)
        {
            return double.IsEvenInteger((double)value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsFinite(Time value)
        {
            return value.IsFrameTime || double.IsFinite(value.RealTime);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsImaginaryNumber(Time value)
        {
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInfinity(Time value)
        {
            return !value.IsFrameTime && double.IsInfinity(value.RealTime);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInteger(Time value)
        {
            return double.IsInteger((double)value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNaN(Time value)
        {
            return !value.IsFrameTime && double.IsNaN(value.RealTime);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNegative(Time value)
        {
            return (value.IsFrameTime && value.Frame < 1) || double.IsNegative(value.RealTime);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNegativeInfinity(Time value)
        {
            return !value.IsFrameTime && double.IsNegativeInfinity(value.RealTime);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNormal(Time value)
        {
            return double.IsNormal((double)value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsOddInteger(Time value)
        {
            return double.IsOddInteger((double)value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPositive(Time value)
        {
            return (value.IsFrameTime && value.Frame >= 0) || double.IsPositive(value.RealTime);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPositiveInfinity(Time value)
        {
            return !value.IsFrameTime && double.IsPositiveInfinity(value.RealTime);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsRealNumber(Time value)
        {
            return !IsNaN(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsSubnormal(Time value)
        {
            return double.IsSubnormal((double)value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<Time>.IsZero(Time value)
        {
            return value == Zero;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Time MaxMagnitude(Time x, Time y)
        {
            // from Math class implementation
            var absX = Abs(x);
            var absY = Abs(y);

            if (absX > absY || IsNaN(absX))
            {
                return x;
            }
            else if (absX == absY)
            {
                return IsNegative(x) ? y : x;
            }
            else
            {
                return y;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Time MaxMagnitudeNumber(Time x, Time y)
        {
            // from double implementation
            var absX = Abs(x);
            var absY = Abs(y);

            if (absX > absY || IsNaN(absY))
            {
                return x;
            }
            else if (absX == absY)
            {
                return IsNegative(x) ? y : x;
            }
            else
            {
                return y;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Time MinMagnitude(Time x, Time y)
        {
            // from Math class implementation
            var absX = Abs(x);
            var absY = Abs(y);

            if (absX < absY || IsNaN(absX))
            {
                return x;
            }
            else if (absX == absY)
            {
                return IsNegative(x) ? x : y;
            }
            else
            {
                return y;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Time MinMagnitudeNumber(Time x, Time y)
        {
            // from double implementation
            var absX = Abs(x);
            var absY = Abs(y);

            if (absX < absY || IsNaN(absY))
            {
                return x;
            }
            else if (absX == absY)
            {
                return IsNegative(x) ? x : y;
            }
            else
            {
                return y;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Time Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider)
        {
            var time = double.Parse(s, style, provider);
            return new Time(time);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Time Parse(string s, NumberStyles style, IFormatProvider? provider)
        {
            var time = double.Parse(s, style, provider);
            return new Time(time);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, [MaybeNullWhen(false)] out Time result)
        {
            if (double.TryParse(s, style, provider, out var time))
            {
                result = new Time(time);
                return true;
            }
            else
            {
                result = Zero;
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, [MaybeNullWhen(false)] out Time result)
        {
            if (double.TryParse(s, style, provider, out var time))
            {
                result = new Time(time);
                return true;
            }
            else
            {
                result = Zero;
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
        {
            charsWritten = 0;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ToString(string? format, IFormatProvider? formatProvider)
        {
            return ((double)this).ToString(format, formatProvider);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Time Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
        {
            var time = double.Parse(s, provider);
            return new Time(time);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out Time result)
        {
            if (double.TryParse(s, provider, out var time))
            {
                result = new Time(time);
                return true;
            }
            else
            {
                result = Zero;
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Time Parse(string s, IFormatProvider? provider)
        {
            var time = double.Parse(s, provider);
            return new Time(time);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out Time result)
        {
            if (double.TryParse(s, provider, out var time))
            {
                result = new Time(time);
                return true;
            }
            else
            {
                result = Zero;
                return false;
            }
        }

        static bool INumberBase<Time>.TryConvertFromChecked<TOther>(TOther value, out Time result)
        {
            if (TOther.TryConvertToChecked(value, out double time))
            {
                result = new Time(time);
                return true;
            }
            else
            {
                result = Zero;
                return false;
            }
        }

        static bool INumberBase<Time>.TryConvertFromSaturating<TOther>(TOther value, out Time result)
        {
            if (TOther.TryConvertToSaturating(value, out double time))
            {
                result = new Time(time);
                return true;
            }
            else
            {
                result = Zero;
                return false;
            }
        }

        static bool INumberBase<Time>.TryConvertFromTruncating<TOther>(TOther value, out Time result)
        {
            if (TOther.TryConvertToTruncating(value, out double time))
            {
                result = new Time(time);
                return true;
            }
            else
            {
                result = Zero;
                return false;
            }
        }

        static bool INumberBase<Time>.TryConvertToChecked<TOther>(Time value, [MaybeNullWhen(false)] out TOther result)
        {
            return TOther.TryConvertFromChecked((double)value, out result);
        }

        static bool INumberBase<Time>.TryConvertToSaturating<TOther>(Time value, [MaybeNullWhen(false)] out TOther result)
        {
            return TOther.TryConvertFromSaturating((double)value, out result);
        }

        static bool INumberBase<Time>.TryConvertToTruncating<TOther>(Time value, [MaybeNullWhen(false)] out TOther result)
        {
            return TOther.TryConvertFromTruncating((double)value, out result);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Time MaxAndMin(Time value, Time min, Time max)
        {
            if (min > max)
            {
                (min, max) = (max, min);
            }
            return Min(Max(value, min), max);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Time Clamp(in Time value, in Time min, in Time max)
        {
            if (min > max)
            {
                throw new ArgumentException("min is greater than max", nameof(min));
            }

            return Min(Max(value, min), max);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Time CopySign(in Time value, in Time sign)
        {
            if (IsNegative(value) != IsNegative(sign))
            {
                return -value;
            }
            else
            {
                return value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Time Max(in Time x, in Time y)
        {
            if (x != y)
            {
                if (!IsNaN(x))
                {
                    return x > y ? x : y;
                }
                else
                {
                    return x;
                }
            }
            else
            {
                return x;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Time MaxNumber(in Time x, in Time y)
        {
            if (x != y)
            {
                if (!IsNaN(y))
                {
                    return x > y ? x : y;
                }
                else
                {
                    return x;
                }
            }
            else
            {
                return x;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Time Min (in Time x, in Time y)
        {
            if (x != y)
            {
                if (!IsNaN(x))
                {
                    return x < y ? x : y;
                }
                else
                {
                    return x;
                }
            }
            else
            {
                return x;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Time MinNumber(in Time x, in Time y)
        {
            if (x != y)
            {
                if (!IsNaN(y))
                {
                    return x < y ? x : y;
                }
                else
                {
                    return x;
                }
            }
            else
            {
                return x;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Sign(in Time value)
        {
            return IsNegative(value) ? -1 : 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Time operator --(Time value)
        {
            if (value.FrameRateIsInteger)
            {
                return new Time(value.Frame - (int)value.FrameRate, value.FrameRate);
            }
            else if (value.IsFrameTime)
            {
                return new Time(RoundTimeDigit(value.Frame / value.FrameRate - 1.0));
            }
            else
            {
                return new Time(RoundTimeDigit(value.RealTime - 1.0));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Time operator ++(Time value)
        {
            if (value.FrameRateIsInteger)
            {
                return new Time(value.Frame + (int)value.FrameRate, value.FrameRate);
            }
            else if (value.IsFrameTime)
            {
                return new Time(RoundTimeDigit(value.Frame / value.FrameRate + 1.0));
            }
            else
            {
                return new Time(RoundTimeDigit(value.RealTime + 1.0));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Time operator -(Time value)
        {
            return value.IsFrameTime ? new Time(-value.Frame, value.FrameRate) : new Time(-value.RealTime);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Time operator +(Time value)
        {
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Time operator +(Time a, Time b)
        {
            if (IsNaN(a) || IsNaN(b))
            {
                return NaN;
            }
            else if (IsInfinity(a) || IsInfinity(b))
            {
                return new Time((double)a + (double)b);
            }

            if (a.FrameRate == b.FrameRate)
            {
                if (a.IsFrameTime)
                {
                    return new Time(a.Frame + b.Frame, a.FrameRate);
                }
                else
                {
                    return new Time(RoundTimeDigit(a.RealTime + b.RealTime));
                }
            }
            else if (a.IsFrameTime && b.IsFrameTime)
            {
                if (a.FrameRateIsInteger && b.FrameRateIsInteger)
                {
                    var frameRateA = (int)a.FrameRate;
                    var frameRateB = (int)b.FrameRate;
                    var lcdFrameRate = LeastCommonMultiple(frameRateA, frameRateB);
                    var rateA = lcdFrameRate / frameRateA;
                    var rateB = lcdFrameRate / frameRateB;
                    var newFrame = a.Frame * rateA + b.Frame * rateB;

                    if (newFrame % rateA == 0)
                    {
                        return new Time(newFrame / rateA, a.FrameRate);
                    }
                    else
                    {
                        var denom = GratestCommonDivisor(newFrame, lcdFrameRate);
                        if (denom > 1)
                        {
                            newFrame /= denom;
                            lcdFrameRate /= denom;
                        }
                        return new Time(newFrame, lcdFrameRate);
                    }
                }
                else
                {
                    return new Time(RoundTimeDigit(ToDoubleNonRounded(a) + ToDoubleNonRounded(b)));
                }
            }
            else
            {
                var newTime = (a.IsFrameTime, b.IsFrameTime) switch
                {
                    (false, _) => a.RealTime + ToDoubleNonRounded(b),
                    (_, false) => ToDoubleNonRounded(a) + b.RealTime,
                    _ => ToDoubleNonRounded(a) + ToDoubleNonRounded(b)
                };
                if (a.FrameRateIsInteger)
                {
                    var newFrame = RoundFrameDigit(newTime * a.FrameRate);
                    if (IsIntegerFrame(newFrame))
                    {
                        return new Time((int)newFrame, a.FrameRate);
                    }
                }
                else if (b.FrameRateIsInteger)
                {
                    var newFrame = RoundFrameDigit(newTime * b.FrameRate);
                    if (IsIntegerFrame(newFrame))
                    {
                        return new Time((int)newFrame, b.FrameRate);
                    }
                }

                return new Time(RoundTimeDigit(newTime));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Time operator +(in Time a, double realTime)
        {
            if (IsNaN(a) || double.IsNaN(realTime))
            {
                return NaN;
            }
            else if (IsInfinity(a) || double.IsInfinity(realTime))
            {
                return new Time((double)a + realTime);
            }

            if (a.IsFrameTime)
            {
                if (realTime == 0.0)
                {
                    return a;
                }

                var frame = realTime * a.FrameRate;
                if (IsIntegerFrame(frame))
                {
                    return new Time(a.Frame + (int)frame, a.FrameRate);
                }
                else
                {
                    return new Time(RoundTimeDigit(a.Frame / a.FrameRate + realTime));
                }
            }
            else
            {
                return new Time(RoundTimeDigit(a.RealTime + realTime));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Time operator +(double realTime, in Time a)
        {
            return a + realTime;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Time operator -(Time a, Time b)
        {
            if (IsNaN(a) || IsNaN(b))
            {
                return NaN;
            }
            else if (IsInfinity(a) || IsInfinity(b))
            {
                return new Time((double)a - (double)b);
            }

            if (a.FrameRate == b.FrameRate)
            {
                if (a.IsFrameTime)
                {
                    return new Time(a.Frame - b.Frame, a.FrameRate);
                }
                else
                {
                    return new Time(RoundTimeDigit(a.RealTime - b.RealTime));
                }
            }
            else if (a.IsFrameTime && b.IsFrameTime)
            {
                if (a.FrameRateIsInteger && b.FrameRateIsInteger)
                {
                    var frameRateA = (int)a.FrameRate;
                    var frameRateB = (int)b.FrameRate;
                    var lcdFrameRate = LeastCommonMultiple(frameRateA, frameRateB);
                    var rateA = lcdFrameRate / frameRateA;
                    var rateB = lcdFrameRate / frameRateB;
                    var newFrame = a.Frame * rateA - b.Frame * rateB;

                    if (newFrame % rateA == 0)
                    {
                        return new Time(newFrame / rateA, a.FrameRate);
                    }
                    else
                    {
                        var denom = GratestCommonDivisor(newFrame, lcdFrameRate);
                        if (denom > 1)
                        {
                            newFrame /= denom;
                            lcdFrameRate /= denom;
                        }
                        return new Time(newFrame, lcdFrameRate);
                    }
                }
                else
                {
                    return new Time(RoundTimeDigit(ToDoubleNonRounded(a) - ToDoubleNonRounded(b)));
                }
            }
            else
            {
                var newTime = (a.IsFrameTime, b.IsFrameTime) switch
                {
                    (false, _) => a.RealTime - ToDoubleNonRounded(b),
                    (_, false) => ToDoubleNonRounded(a) - b.RealTime,
                    _ => ToDoubleNonRounded(a) - ToDoubleNonRounded(b)
                };
                if (a.FrameRateIsInteger)
                {
                    var newFrame = RoundFrameDigit(newTime * a.FrameRate);
                    if (IsIntegerFrame(newFrame))
                    {
                        return new Time((int)newFrame, a.FrameRate);
                    }
                }
                else if (b.FrameRateIsInteger)
                {
                    var newFrame = RoundFrameDigit(newTime * b.FrameRate);
                    if (IsIntegerFrame(newFrame))
                    {
                        return new Time((int)newFrame, b.FrameRate);
                    }
                }

                return new Time(RoundTimeDigit(newTime));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Time operator -(in Time a, double realTime)
        {
            if (IsNaN(a) || double.IsNaN(realTime))
            {
                return NaN;
            }
            else if (IsInfinity(a) || double.IsInfinity(realTime))
            {
                return new Time((double)a - realTime);
            }

            if (a.IsFrameTime)
            {
                if (realTime == 0.0)
                {
                    return a;
                }

                var frame = realTime * a.FrameRate;
                if (IsIntegerFrame(frame))
                {
                    return new Time(a.Frame - (int)frame, a.FrameRate);
                }
                else
                {
                    return new Time(RoundTimeDigit(a.Frame / a.FrameRate - realTime));
                }
            }
            else
            {
                return new Time(RoundTimeDigit(a.RealTime - realTime));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Time operator -(double realTime, in Time a)
        {
            if (a.IsFrameTime)
            {
                if (realTime == 0.0)
                {
                    return a;
                }

                var frame = realTime * a.FrameRate;
                if (IsIntegerFrame(frame))
                {
                    return new Time((int)frame - a.Frame, a.FrameRate);
                }
                else
                {
                    return new Time(RoundTimeDigit(realTime - a.Frame / a.FrameRate));
                }
            }
            else
            {
                return new Time(RoundTimeDigit(realTime - a.RealTime));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Time operator *(Time a, Time b)
        {
            if (IsNaN(a) || IsNaN(b))
            {
                return NaN;
            }
            else if (IsInfinity(a) || IsInfinity(b))
            {
                return new Time((double)a * (double)b);
            }
            else if (b == One)
            {
                return a;
            }
            else if (a == One)
            {
                return b;
            }
            else if (a == Zero || b == Zero)
            {
                return Zero;
            }

            if (a.FrameRate == b.FrameRate)
            {
                if (a.IsFrameTime)
                {
                    return new Time(a.Frame * b.Frame, a.FrameRate);
                }
                else
                {
                    return new Time(RoundTimeDigit(a.RealTime * b.RealTime));
                }
            }
            else if (a.IsFrameTime && b.IsFrameTime)
            {
                if (a.FrameRateIsInteger && b.FrameRateIsInteger)
                {
                    var frameRateA = (int)a.FrameRate;
                    var frameRateB = (int)b.FrameRate;
                    var lcdFrameRate = LeastCommonMultiple(frameRateA, frameRateB);
                    var rateB = lcdFrameRate / frameRateB;
                    var newFrame = a.Frame * b.Frame * rateB;

                    return new Time(newFrame, a.FrameRate);
                }
                else
                {
                    return new Time(RoundTimeDigit(ToDoubleNonRounded(a) * ToDoubleNonRounded(b)));
                }
            }
            else
            {
                var newTime = (a.IsFrameTime, b.IsFrameTime) switch
                {
                    (false, _) => a.RealTime * ToDoubleNonRounded(b),
                    (_, false) => ToDoubleNonRounded(a) * b.RealTime,
                    _ => ToDoubleNonRounded(a) * ToDoubleNonRounded(b)
                };
                if (a.FrameRateIsInteger)
                {
                    var newFrame = RoundFrameDigit(newTime * a.FrameRate);
                    if (IsIntegerFrame(newFrame))
                    {
                        return new Time((int)newFrame, a.FrameRate);
                    }
                }
                else if (b.FrameRateIsInteger)
                {
                    var newFrame = RoundFrameDigit(newTime * b.FrameRate);
                    if (IsIntegerFrame(newFrame))
                    {
                        return new Time((int)newFrame, b.FrameRate);
                    }
                }

                return new Time(RoundTimeDigit(newTime));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Time operator *(in Time a, double realTime)
        {
            if (IsNaN(a) || double.IsNaN(realTime))
            {
                return NaN;
            }
            else if (IsInfinity(a) || double.IsInfinity(realTime))
            {
                return new Time((double)a * realTime);
            }
            else if (realTime == 1.0)
            {
                return a;
            }
            else if (realTime == 0.0 || a == Zero)
            {
                return Zero;
            }

            if (a.IsFrameTime)
            {
                var newTime = (a.Frame / a.FrameRate * realTime);
                var newFrame = RoundFrameDigit(newTime * a.FrameRate);
                if (IsIntegerFrame(newFrame))
                {
                    return new Time((int)newFrame, a.FrameRate);
                }
                else
                {
                    return new Time(RoundTimeDigit(newTime));
                }
            }
            else
            {
                return new Time(RoundTimeDigit(a.RealTime * realTime));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Time operator *(double realTime, in Time a)
        {
            return a * realTime;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Time operator *(in Time a, int realTime)
        {
            if (realTime == 1)
            {
                return a;
            }
            else if (realTime == 0 || a == Zero)
            {
                return Zero;
            }

            if (a.IsFrameTime)
            {
                return new Time(a.Frame * realTime, a.FrameRate);
            }
            else
            {
                return new Time(RoundTimeDigit(a.RealTime * realTime));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Time operator *(int realTime, in Time a)
        {
            return a * realTime;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Time operator /(Time a, Time b)
        {
            if (IsNaN(a) || IsNaN(b))
            {
                return NaN;
            }
            else if (IsInfinity(a) || IsInfinity(b))
            {
                return new Time((double)a / (double)b);
            }
            else if (b == One)
            {
                return a;
            }
            else if (b == Zero)
            {
                return new Time((double)a / 0.0);
            }

            if (a.FrameRate == b.FrameRate)
            {
                if (a.IsFrameTime)
                {
                    var newFrame = Math.DivRem(a.Frame, b.Frame, out var reminder);
                    if (reminder == 0)
                    {
                        return new Time(newFrame, a.FrameRate);
                    }
                    else
                    {
                        return new Time(RoundTimeDigit(a.Frame / (double)b.Frame));
                    }
                }
                else
                {
                    return new Time(RoundTimeDigit(a.RealTime / b.RealTime));
                }
            }
            else if (a.IsFrameTime && b.IsFrameTime)
            {
                if (a.FrameRateIsInteger && b.FrameRateIsInteger)
                {
                    var frameRateA = (int)a.FrameRate;
                    var frameRateB = (int)b.FrameRate;
                    var lcdFrameRate = LeastCommonMultiple(frameRateA, frameRateB);
                    var rateA = lcdFrameRate / frameRateA;
                    var rateB = lcdFrameRate / frameRateB;
                    var newFrame = Math.DivRem(a.Frame * rateA, b.Frame * rateB, out var reminder);

                    if (reminder == 0)
                    {
                        if (newFrame % rateA == 0)
                        {
                            return new Time(newFrame / rateA, a.FrameRate);
                        }
                        else
                        {
                            var denom = GratestCommonDivisor(newFrame, lcdFrameRate);
                            if (denom > 1)
                            {
                                newFrame /= denom;
                                lcdFrameRate /= denom;
                            }
                            return new Time(newFrame, lcdFrameRate);
                        }
                    }
                }

                return new Time(RoundTimeDigit(a.Frame / a.FrameRate / (b.Frame / b.FrameRate)));
            }
            else
            {
                var newTime = (a.IsFrameTime, b.IsFrameTime) switch
                {
                    (false, _) => a.RealTime / ToDoubleNonRounded(b),
                    (_, false) => ToDoubleNonRounded(a) / b.RealTime,
                    _ => ToDoubleNonRounded(a) / ToDoubleNonRounded(b)
                };
                if (a.FrameRateIsInteger)
                {
                    var newFrame = RoundFrameDigit(newTime * a.FrameRate);
                    if (IsIntegerFrame(newFrame))
                    {
                        return new Time((int)newFrame, a.FrameRate);
                    }
                }
                else if (b.FrameRateIsInteger)
                {
                    var newFrame = RoundFrameDigit(newTime * b.FrameRate);
                    if (IsIntegerFrame(newFrame))
                    {
                        return new Time((int)newFrame, b.FrameRate);
                    }
                }

                return new Time(RoundTimeDigit(newTime));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Time operator /(in Time a, double realTime)
        {
            if (IsNaN(a) || double.IsNaN(realTime))
            {
                return NaN;
            }
            else if (IsInfinity(a) || double.IsInfinity(realTime))
            {
                return new Time((double)a / realTime);
            }
            else if (realTime == 1.0)
            {
                return a;
            }
            else if (realTime == 0.0)
            {
                return new Time((double)a / realTime);
            }

            if (a.IsFrameTime)
            {
                var newTime = ToDoubleNonRounded(a) / realTime;
                var newFrame = RoundFrameDigit(newTime * a.FrameRate);
                if (IsIntegerFrame(newFrame))
                {
                    return new Time((int)newFrame, a.FrameRate);
                }
                else
                {
                    return new Time(RoundTimeDigit(newTime));
                }
            }
            else
            {
                return new Time(RoundTimeDigit(a.RealTime / realTime));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Time operator /(double realTime, in Time a)
        {
            return ((Time)realTime) / a;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Time operator %(Time a, Time b)
        {
            if (IsNaN(a) || IsNaN(b) || IsInfinity(a) || b == Zero)
            {
                return NaN;
            }
            else if (IsInfinity(b))
            {
                return a;
            }
            else if(b == One || b == -One)
            {
                return Zero;
            }

            if (a.FrameRate == b.FrameRate)
            {
                if (a.IsFrameTime)
                {
                    return new Time(a.Frame % b.Frame, a.FrameRate);
                }
                else
                {
                    return new Time(a.RealTime % b.RealTime);
                }
            }
            else if (a.IsFrameTime && b.IsFrameTime)
            {
                if (a.FrameRateIsInteger && b.FrameRateIsInteger)
                {
                    var frameRateA = (int)a.FrameRate;
                    var frameRateB = (int)b.FrameRate;
                    var lcdFrameRate = LeastCommonMultiple(frameRateA, frameRateB);
                    var rateA = lcdFrameRate / frameRateA;
                    var rateB = lcdFrameRate / frameRateB;
                    Math.DivRem(a.Frame * rateA, b.Frame * rateB, out var reminder);

                    if (reminder % rateA == 0)
                    {
                        return new Time(reminder / rateA, a.FrameRate);
                    }
                    else
                    {
                        var denom = GratestCommonDivisor(reminder, lcdFrameRate);
                        if (denom > 1)
                        {
                            reminder /= denom;
                            lcdFrameRate /= denom;
                        }
                        return new Time(reminder, lcdFrameRate);
                    }
                }
                else
                {
                    return new Time(RoundTimeDigit((a.Frame / a.FrameRate) % (b.Frame / b.FrameRate)));
                }
            }
            else
            {
                var newTime = (a.IsFrameTime, b.IsFrameTime) switch
                {
                    (false, _) => a.RealTime % ToDoubleNonRounded(b),
                    (_, false) => ToDoubleNonRounded(a) % b.RealTime,
                    _ => ToDoubleNonRounded(a) % ToDoubleNonRounded(b)
                };
                if (a.FrameRateIsInteger)
                {
                    var newFrame = RoundFrameDigit(newTime * a.FrameRate);
                    if (IsIntegerFrame(newFrame))
                    {
                        return new Time((int)newFrame, a.FrameRate);
                    }
                }
                else if (b.FrameRateIsInteger)
                {
                    var newFrame = RoundFrameDigit(newTime * b.FrameRate);
                    if (IsIntegerFrame(newFrame))
                    {
                        return new Time((int)newFrame, b.FrameRate);
                    }
                }

                return new Time(RoundTimeDigit(newTime));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Time operator %(in Time a, double realTime)
        {
            if (IsNaN(a) || double.IsNaN(realTime) || IsInfinity(a) || realTime == 0.0)
            {
                return NaN;
            }
            else if (double.IsInfinity(realTime))
            {
                return a;
            }
            else if (realTime == 1.0 || realTime == -1.0)
            {
                return Zero;
            }

            if (a.IsFrameTime)
            {
                var newTime = ToDoubleNonRounded(a) % realTime;
                var newFrame = RoundFrameDigit(newTime * a.FrameRate);
                if (IsIntegerFrame(newFrame))
                {
                    return new Time((int)newFrame, a.FrameRate);
                }
                else
                {
                    return new Time(RoundTimeDigit(newTime));
                }
            }
            else
            {
                return new Time(RoundTimeDigit(a.RealTime % realTime));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Time operator %(double realTime, in Time a)
        {
            return ((Time)realTime) % a;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <(Time a, Time b)
        {
            if (a.FrameRate == b.FrameRate)
            {
                if (a.IsFrameTime)
                {
                    return a.Frame < b.Frame;
                }
                else
                {
                    return a.RealTime < b.RealTime;
                }
            }
            else
            {
                return (a.IsFrameTime, b.IsFrameTime) switch
                {
                    (true, true) => RoundFrameDigit(a.Frame / a.FrameRate) < RoundFrameDigit(b.Frame / b.FrameRate),
                    (true, false) => RoundFrameDigit(a.Frame / a.FrameRate) < b.RealTime,
                    (false, true) => a.RealTime < RoundFrameDigit(b.Frame / b.FrameRate),
                    _ => a.RealTime < b.RealTime
                };
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <=(Time a, Time b)
        {
            if (a.FrameRate == b.FrameRate)
            {
                if (a.IsFrameTime)
                {
                    return a.Frame <= b.Frame;
                }
                else
                {
                    return a.RealTime <= b.RealTime;
                }
            }
            else
            {
                return (a.IsFrameTime, b.IsFrameTime) switch
                {
                    (true, true) => RoundFrameDigit(a.Frame / a.FrameRate) <= RoundFrameDigit(b.Frame / b.FrameRate),
                    (true, false) => RoundFrameDigit(a.Frame / a.FrameRate) <= b.RealTime,
                    (false, true) => a.RealTime <= RoundFrameDigit(b.Frame / b.FrameRate),
                    _ => a.RealTime <= b.RealTime
                };
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(Time a, Time b)
        {
            if (a.FrameRate == b.FrameRate)
            {
                if (a.IsFrameTime)
                {
                    return a.Frame > b.Frame;
                }
                else
                {
                    return a.RealTime > b.RealTime;
                }
            }
            else
            {
                return (a.IsFrameTime, b.IsFrameTime) switch
                {
                    (true, true) => RoundFrameDigit(a.Frame / a.FrameRate) > RoundFrameDigit(b.Frame / b.FrameRate),
                    (true, false) => RoundFrameDigit(a.Frame / a.FrameRate) > b.RealTime,
                    (false, true) => a.RealTime > RoundFrameDigit(b.Frame / b.FrameRate),
                    _ => a.RealTime > b.RealTime
                };
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >=(Time a, Time b)
        {
            if (a.FrameRate == b.FrameRate)
            {
                if (a.IsFrameTime)
                {
                    return a.Frame >= b.Frame;
                }
                else
                {
                    return a.RealTime >= b.RealTime;
                }
            }
            else
            {
                return (a.IsFrameTime, b.IsFrameTime) switch
                {
                    (true, true) => RoundFrameDigit(a.Frame / a.FrameRate) >= RoundFrameDigit(b.Frame / b.FrameRate),
                    (true, false) => RoundFrameDigit(a.Frame / a.FrameRate) >= b.RealTime,
                    (false, true) => a.RealTime >= RoundFrameDigit(b.Frame / b.FrameRate),
                    _ => a.RealTime >= b.RealTime
                };
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Time left, Time right)
        {
            return left.Equals(right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Time left, Time right)
        {
            return !left.Equals(right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator double(in Time a)
        {
            return a.IsFrameTime ? RoundTimeDigit(a.Frame / a.FrameRate) : a.RealTime;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Time(double realTime)
        {
            return new Time(RoundTimeDigit(realTime));
        }

        // NOTE: 以下移行のための一時的な実装
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(Time a, double b)
        {
            return (double)a > b;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >=(Time a, double b)
        {
            return (double)a >= b;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <(Time a, double b)
        {
            return (double)a < b;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <=(Time a, double b)
        {
            return (double)a <= b;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Time a, double b)
        {
            return a.Equals(b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Time a, double b)
        {
            return !a.Equals(b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(double a, Time b)
        {
            return a > (double)b;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >=(double a, Time b)
        {
            return a >= (double)b;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <(double a, Time b)
        {
            return a < (double)b;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <=(double a, Time b)
        {
            return a <= (double)b;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(double a, Time b)
        {
            return a == (double)b;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(double a, Time b)
        {
            return a != (double)b;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Time Clamp(in Time value, in Time min, double max)
        {
            return Clamp(value, min, (Time)max);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Time Clamp(in Time value, double min, in Time max)
        {
            return Clamp(value, (Time)min, max);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Time Clamp(in Time value, double min, double max)
        {
            return Clamp(value, (Time)min, (Time)max);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Time Clamp(double value, in Time min, double max)
        {
            return Clamp((Time)value, min, (Time)max);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Time Clamp(double value, double min, in Time max)
        {
            return Clamp((Time)value, (Time)min, max);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Time Clamp(double value, double min, double max)
        {
            return Clamp((Time)value, (Time)min, (Time)max);
        }
    }
}
