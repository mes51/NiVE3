using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace NiVE3.Data
{
    readonly record struct FloatColor(float R, float G, float B, float A)
    {
        public static readonly FloatColor White = new FloatColor(1.0F, 1.0F, 1.0F, 1.0F);

        public static readonly FloatColor Black = new FloatColor(0.0F, 0.0F, 0.0F, 1.0F);

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

        public static explicit operator Vector128<float>(FloatColor color)
        {
            return Vector128.Create(color.B, color.G, color.R, color.A);
        }

        public static explicit operator FloatColor(Vector128<float> color)
        {
            return new FloatColor(color.GetElement(2), color.GetElement(1), color.GetElement(0), color.GetElement(3));
        }

        public static explicit operator Vector4(FloatColor color)
        {
            return new Vector4(color.B, color.G, color.R, color.A);
        }

        public static explicit operator FloatColor(Vector4 color)
        {
            return new FloatColor(color.Z, color.Y, color.X, color.W);
        }
    }
}
