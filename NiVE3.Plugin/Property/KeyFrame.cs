using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.Plugin.Property
{
    public record KeyFrame(double Time, object Value, Ease EaseIn, Ease EaseOut, InterpolationType InterpolationType)
    {
    }
}
