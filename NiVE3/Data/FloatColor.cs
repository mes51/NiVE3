using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace NiVE3.Data
{
    readonly record struct FloatColor(float R, float G, float B, float A)
    {
        public Color ToByteColor()
        {
            return Color.FromArgb(
                (byte)Math.Clamp(A * 255.0F, 0.0F, 255.0F),
                (byte)Math.Clamp(R * 255.0F, 0.0F, 255.0F),
                (byte)Math.Clamp(G * 255.0F, 0.0F, 255.0F),
                (byte)Math.Clamp(B * 255.0F, 0.0F, 255.0F)
            );
        }

        public static FloatColor FromColor(Color color)
        {
            return new FloatColor(color.R / 255.0F, color.G / 255.0F, color.B / 255.0F, color.A / 255.0F);
        }

        public static implicit operator Vector128<float>(FloatColor color)
        {
            return Vector128.Create(color.B, color.G, color.R, color.A);
        }
    }
}
