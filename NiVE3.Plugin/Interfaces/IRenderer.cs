using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.Plugin.Interfaces
{
    public interface IRenderer : IDisposable
    {
    }

    /// <summary>
    /// 画像の補間画質。レベルが高いほど高画質
    /// </summary>
    public enum ImageInterpolationQuality
    {
        Level1,
        Level2
    }

    public enum BlendMode
    {
        Normal,
        Replace,
        Add,
        Subtract,
        Multiply,
        Screen,
        Overlay,
        HardLight,
        SoftLight,
        VividLight,
        LinearLight,
        PinLight,
        ColorDodge,
        LinearDodge,
        ColorBurn,
        LinearBurn,
        Darken,
        Lighten,
        Difference,
        Exclusion,
        Hue,
        Saturation,
        Color,
        Luminance
    }
}
