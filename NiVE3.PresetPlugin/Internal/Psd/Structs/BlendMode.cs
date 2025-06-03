using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.PresetPlugin.Internal.Psd.Structs
{
    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    readonly struct BlendMode
    {
        // NOTE: 8BIMの逆順
        const uint ValidSignature = 943868237;

        readonly uint Signature;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 4)]
        readonly string BlendModeKey;

        public bool IsValidSignature => Signature == ValidSignature;

        public BlendModeType BlendModeType => BlendModeKey switch
        {
            "pass" => BlendModeType.PassThrough,
            "norm" => BlendModeType.Normal,
            "diss" => BlendModeType.Dissolve,
            "dark" => BlendModeType.Darken,
            "mul " => BlendModeType.Multiply,
            "idiv" => BlendModeType.ColorBurn,
            "lbrn" => BlendModeType.LinearBurn,
            "dkCl" => BlendModeType.DarkerColor,
            "lite" => BlendModeType.Lighten,
            "scrn" => BlendModeType.Screen,
            "div " => BlendModeType.ColorDodge,
            "lddg" => BlendModeType.LinearDodge,
            "lgCl" => BlendModeType.LighterColor,
            "over" => BlendModeType.Overlay,
            "sLit" => BlendModeType.SoftLight,
            "hLit" => BlendModeType.HardLight,
            "vLit" => BlendModeType.VividLight,
            "lLit" => BlendModeType.LinearLight,
            "pLit" => BlendModeType.PinLight,
            "hMix" => BlendModeType.HardMix,
            "diff" => BlendModeType.Difference,
            "smud" => BlendModeType.Exclusion,
            "fsub" => BlendModeType.Subtract,
            "fdiv" => BlendModeType.Divide,
            "hue " => BlendModeType.Hue,
            "sat " => BlendModeType.Saturation,
            "colr" => BlendModeType.Color,
            "lum " => BlendModeType.Luminosity,
            _ => throw new ArgumentException(nameof(BlendModeKey))
        };
    }

    enum BlendModeType
    {
        PassThrough,
        Normal,
        Dissolve,
        Darken,
        Multiply,
        ColorBurn,
        LinearBurn,
        DarkerColor,
        Lighten,
        Screen,
        ColorDodge,
        LinearDodge,
        LighterColor,
        Overlay,
        SoftLight,
        HardLight,
        VividLight,
        LinearLight,
        PinLight,
        HardMix,
        Difference,
        Exclusion,
        Subtract,
        Divide,
        Hue,
        Saturation,
        Color,
        Luminosity
    }
}
