using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.View.Resource;
using System.Windows;
using System.Runtime.CompilerServices;

namespace NiVE3.Util
{
    static class TimeCalc
    {
        public const double TimeEpsilon = 1E-10;

        const int TimeDigit = 10;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double CalcTimeFromPixel(double x, double uiWidth, double range, double rangeStart, double min = double.MinValue, double max = double.MaxValue)
        {
            var timePerPixel = range / (uiWidth - UIParameters.TimelineRangeThumbTotalWidth);
            return Math.Clamp(rangeStart + x * timePerPixel, min, max);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double CalcTimeFromPixelAligned(double x, double uiWidth, double range, double rangeStart, double frameRate, double min = double.MinValue, double max = double.MaxValue)
        {
            var time = CalcTimeFromPixel(x, uiWidth, range, rangeStart, min, max);
            return (int)Math.Round(time * frameRate) / frameRate;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double AlignRound(double time, double frameRate)
        {
            return (int)Math.Round(time * frameRate) / frameRate;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double AlignFloor(double time, double frameRate)
        {
            return (int)Math.Round(time * frameRate, TimeDigit) / frameRate;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double AlignCeiling(double time, double frameRate)
        {
            return (int)Math.Ceiling(Math.Round(time * frameRate, TimeDigit)) / frameRate;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double RoundTimeDigit(double time)
        {
            return Math.Round(time, TimeDigit);
        }
    }
}
