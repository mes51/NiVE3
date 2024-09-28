using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.PresetPlugin.Effect.Distortion
{
    enum DisplacemenMapChannelType
    {
        R,
        G,
        B,
        A,
        Luminance,
        Hue,
        Saturation,
        Lightness,
        On,
        Half,
        Off
    }

    enum DisplacementSourceLayerPositionType
    {
        Center,
        Stretch,
        Loop
    }
}
