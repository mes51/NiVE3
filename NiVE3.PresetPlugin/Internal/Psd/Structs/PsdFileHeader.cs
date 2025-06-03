using NiVE3.PresetPlugin.Internal.Psd.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.PresetPlugin.Internal.Psd.Structs
{
    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    readonly struct PsdFileHeader
    {
        // NOTE: '8BPS' の逆順
        const uint ValidSignature = 943870035;

        readonly uint Signature;

        public readonly short Version;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public readonly byte[] Reserved;

        public readonly short ColorChannels;

        public readonly int ImageHeight;

        public readonly int ImageWidth;

        public readonly short ColorDepth;

        // NOTE: enumが含まれるとMarshal.SizeOfで例外を吐く
        // SEE: https://github.com/dotnet/runtime/issues/12258
        readonly short ColorModeInt;

        public ColorMode ColorMode => (ColorMode)ColorModeInt;

        public bool IsPsb => Version == 2;

        public bool IsValidSignature => Signature == ValidSignature;
    }
}
