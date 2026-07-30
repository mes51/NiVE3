using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.OpenFX.Interop
{
    /// <summary>
    /// ofxCore.h の OfxRectD 構造体
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct OfxRectD
    {
        public double X1;
        public double Y1;
        public double X2;
        public double Y2;
    }

    /// <summary>
    /// ofxCore.h の OfxRectI 構造体
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct OfxRectI
    {
        public int X1;
        public int Y1;
        public int X2;
        public int Y2;
    }

    /// <summary>
    /// ofxCore.h の OfxRangeD 構造体
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct OfxRangeD
    {
        public double Min;
        public double Max;
    }
}
