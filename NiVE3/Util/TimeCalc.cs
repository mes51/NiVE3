using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.View.Resource;
using System.Windows;
using System.Runtime.CompilerServices;
using NiVE3.Plugin.ValueObject;

namespace NiVE3.Util
{
    static class TimeCalc
    {
        public const double TimeEpsilon = 1E-10;

        public const int TimeDigit = 10;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Time CalcTimeFromPixel(double x, double uiWidth, in Time range, in Time rangeStart)
        {
            return CalcTimeFromPixel(x, uiWidth, range, rangeStart, Time.MinValue, Time.MaxValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Time CalcTimeFromPixel(double x, double uiWidth, in Time range, in Time rangeStart, in Time min)
        {
            return CalcTimeFromPixel(x, uiWidth, range, rangeStart, min, Time.MaxValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Time CalcTimeFromPixel(double x, double uiWidth, in Time range, in Time rangeStart, in Time min, in Time max)
        {
            var timePerPixel = (double)range / (uiWidth - UIParameters.TimelineRangeThumbTotalWidth);
            return Time.Clamp(rangeStart + x * timePerPixel, min, max);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Time CalcTimeFromPixelAligned(double x, double uiWidth, in Time range, in Time rangeStart, double frameRate)
        {
            return CalcTimeFromPixelAligned(x, uiWidth, range, rangeStart, frameRate, Time.MinValue, Time.MaxValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Time CalcTimeFromPixelAligned(double x, double uiWidth, in Time range, in Time rangeStart, double frameRate, in Time min)
        {
            return CalcTimeFromPixelAligned(x, uiWidth, range, rangeStart, frameRate, min, Time.MaxValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Time CalcTimeFromPixelAligned(double x, double uiWidth, in Time range, in Time rangeStart, double frameRate, in Time min, in Time max)
        {
            var time = CalcTimeFromPixel(x, uiWidth, range, rangeStart, min, max);
            return time.RoundToFrameRate(frameRate);
        }
    }
}
