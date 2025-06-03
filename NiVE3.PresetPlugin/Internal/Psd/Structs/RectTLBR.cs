using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.PresetPlugin.Internal.Psd.Structs
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct RectTLBR
    {
        public static readonly int Size = Marshal.SizeOf<RectTLBR>();

        public readonly int Top;

        public readonly int Left;

        public readonly int Bottom;

        public readonly int Right;
    }
}
