using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.PresetPlugin.Internal.Psd.Enums
{
    enum ColorMode : short
    {
        Bitmap = 0,
        GrayScale = 1,
        Indexed = 2,
        RGB = 3,
        CMYK = 4,
        MultiChannel = 7,
        Duotone = 8,
        Lab = 9
    }
}
