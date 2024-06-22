using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.PresetPlugin.Effect.Blur
{
    enum BlurDirection
    {
        HorizontalAndVertical,
        Horizontal,
        Vertical
    }

    enum EdgeRepeatMode
    {
        None,
        Wrap,
        Repeat,
        Mirror
    }
}
