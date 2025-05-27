using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.PresetPlugin.Effect.Util
{
    public enum ChannelType : int
    {
        RGB,
        R,
        G,
        B,
        A
    }

    public enum WithHSLChannelType : int
    {
        RGB,
        R,
        G,
        B,
        A,
        Hue,
        Saturation
    }

    public enum LuminanceAndSingleChannelType
    {
        R,
        G,
        B,
        A,
        Luminance
    }

    enum CompositeOrder : int
    {
        Front,
        Back
    }

    enum SourceLayerPositionType
    {
        Center,
        Stretch,
        Loop
    }
}
