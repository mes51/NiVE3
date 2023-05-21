using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace NiVE3.Data
{
    readonly record struct FloatColor(float R, float G, float B, float A)
    {
        public static FloatColor FromColor(Color color)
        {
            return new FloatColor(color.R / 255.0F, color.G / 255.0F, color.B / 255.0F, color.A / 255.0F);
        }
    }
}
