using NiVE3.PresetPlugin.Internal.IO;
using NiVE3.PresetPlugin.Internal.Psd.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.PresetPlugin.Internal.Psd.Structs.Layer
{
    class LayerMaskData
    {
        public RectTLBR Bounds { get; }

        public byte DefaultColor { get; }

        public byte Flags { get; }

        public byte UserMaskDensity { get; }

        public double UserMaskFeather { get; }

        public byte VectorMaskDensity { get; }

        public double VectorMaskFeather { get; }

        public byte RealFlags { get; }

        public byte RealUserMaskBackground { get; }

        public RectTLBR EnclosingMaskBounds { get; }

        private LayerMaskData(RectTLBR bounds, byte defaultColor, byte flags) : this(bounds, defaultColor, flags, 0, 0, 0, 0, 0, 0, new RectTLBR()) { }

        private LayerMaskData(RectTLBR bounds, byte defaultColor, byte flags, byte userMaskDensity, double userMaskFeather, byte vectorMaskDensity, double vectorMaskFeather, byte realFlags, byte realUserMaskBackground, RectTLBR enclosingMaskBounds)
        {
            Bounds = bounds;
            DefaultColor = defaultColor;
            Flags = flags;
            UserMaskDensity = userMaskDensity;
            UserMaskFeather = userMaskFeather;
            VectorMaskDensity = vectorMaskDensity;
            VectorMaskFeather = vectorMaskFeather;
            RealFlags = realFlags;
            RealUserMaskBackground = realUserMaskBackground;
            EnclosingMaskBounds = enclosingMaskBounds;
        }

        public static LayerMaskData? Parse(RandomAccessFileReader reader)
        {
            var length = reader.ReadInt32();
            if (length < 1)
            {
                return null;
            }

            var bounds = reader.ReadStruct<RectTLBR>();
            var defaultColor = reader.ReadByte();
            var flags = reader.ReadByte();

            length -= 18;

            if (length == 2)
            {
                reader.Position += 2;
                return new LayerMaskData(bounds, defaultColor, flags);
            }

            var maskParameters = 0;
            var userMaskDensity = (byte)0;
            var userMaskFeather = 0.0;
            var vectorMaskDensity = (byte)0;
            var vectorMaskFeather = 0.0;
            var realFlags = (byte)0;
            var realUserMaskBackground = (byte)0;
            var enclosingMask = new RectTLBR();

            if ((flags & 0b10000) != 0)
            {
                if (length > 0)
                {
                    maskParameters = reader.ReadByte();
                    length--;
                }
                if (length > 0 && (maskParameters & 0b0001) != 0)
                {
                    userMaskDensity = reader.ReadByte();
                    length--;
                }
                if (length > 0 && (maskParameters & 0b0010) != 0)
                {
                    userMaskFeather = reader.ReadDouble();
                    length -= sizeof(double);
                }
                if (length > 0 && (maskParameters & 0b0100) != 0)
                {
                    vectorMaskDensity = reader.ReadByte();
                    length--;
                }
                if (length > 0 && (maskParameters & 0b1000) != 0)
                {
                    vectorMaskFeather = reader.ReadDouble();
                    length -= sizeof(double);
                }
            }

            if (length > 0)
            {
                realFlags = reader.ReadByte();
                length--;
            }
            if (length > 0)
            {
                realUserMaskBackground = reader.ReadByte();
                length--;
            }

            if (length > RectTLBR.Size)
            {
                enclosingMask = reader.ReadStruct<RectTLBR>();
                length -= RectTLBR.Size;
            }

            reader.Position += length;

            return new LayerMaskData(
                bounds,
                defaultColor,
                flags,
                userMaskDensity,
                userMaskFeather,
                vectorMaskDensity,
                vectorMaskFeather,
                realFlags,
                realUserMaskBackground,
                enclosingMask
            );
        }
    }
}
