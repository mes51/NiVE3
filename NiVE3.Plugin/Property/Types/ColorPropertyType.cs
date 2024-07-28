using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Internal.Util;
using NiVE3.Shared.Extension;

namespace NiVE3.Plugin.Property.Types
{
    public class ColorPropertyType : IPropertyType
    {
        static readonly byte[] TransparentHashBase = [..Enumerable.Repeat((byte)0, Marshal.SizeOf<Vector4>())];

        public static readonly ColorPropertyType Instance = new ColorPropertyType();

        public InterpolationType SupportedInterpolationTypes => InterpolationType.None | InterpolationType.Linear | InterpolationType.CatmullRom;

        private ColorPropertyType() { }

        public object? Interpolate(IReadOnlyList<KeyFrame> keyFrames, double t)
        {
            var baseKeyFrameIndex = keyFrames.IndexOfLast(k => k.Time <= t);
            if (baseKeyFrameIndex < 0)
            {
                return keyFrames[0].Value;
            }
            else if (baseKeyFrameIndex >= keyFrames.Count - 1)
            {
                return keyFrames[baseKeyFrameIndex].Value;
            }
            var keyFrame1 = keyFrames[baseKeyFrameIndex];
            var keyFrame2 = keyFrames[baseKeyFrameIndex + 1];
            switch (keyFrames[baseKeyFrameIndex].InterpolationType)
            {
                case InterpolationType.Linear:
                    return Interpolation.Linear((Vector4)keyFrame1.Value!, (Vector4)keyFrame2.Value!, keyFrame1.Time, keyFrame2.Time, t);
                case InterpolationType.CatmullRom:
                    {
                        var keyFrame0 = baseKeyFrameIndex > 0 ? keyFrames[baseKeyFrameIndex - 1] : keyFrame1;
                        var keyFrame3 = baseKeyFrameIndex <= keyFrames.Count - 3 ? keyFrames[baseKeyFrameIndex + 2] : keyFrame2;
                        return Interpolation.CatmullRom((Vector4)keyFrame0.Value!, (Vector4)keyFrame1.Value!, (Vector4)keyFrame2.Value!, (Vector4)keyFrame3.Value!, keyFrame1.Time, keyFrame2.Time, t);
                    }
                default:
                    return keyFrame1.Value;
            }
        }

        public bool TryConvertFrom(object otherValue, [NotNullWhen(true)] out object? convertedValue)
        {
            switch (otherValue)
            {
                case Vector4:
                    convertedValue = otherValue;
                    return true;
                case Array array:
                    var elements = array.Cast<object>()
                        .Select(
                            e =>
                            {
                                return e switch
                                {
                                    byte v => v,
                                    sbyte v => v,
                                    short v => v,
                                    ushort v => v,
                                    int v => v,
                                    uint v => v,
                                    long v => v,
                                    ulong v => v,
                                    Int128 v => (float)v,
                                    UInt128 v => (float)v,
                                    Half v => (float)v,
                                    float v => v,
                                    double v => (float)v,
                                    decimal v => (float)v,
                                    _ => 0.0F,
                                };
                            })
                        .ToArray();
                    if (elements.Length > 3)
                    {
                        convertedValue = new Vector4(elements[0], elements[1], elements[2], elements[3]);
                        return true;
                    }
                    else
                    {
                        convertedValue = new Vector4();
                        return false;
                    }
                default:
                    convertedValue = new Vector4();
                    return false;
            }
        }

        public object? SerializeValue(object? value)
        {
            if (value is not Vector4 v)
            {
                return null;
            }

            return new SerializedColor(v.X, v.Y, v.Z, v.W);
        }

        public object? DeserializeValue(object? serializedValue)
        {
            if (serializedValue is SerializedColor serializedColor)
            {
                return new Vector4(serializedColor.B, serializedColor.G, serializedColor.R, serializedColor.A);
            }
            else if (serializedValue is IDictionary<string, object> dictionary)
            {
                return new Vector4(
                    Convert.ToSingle(dictionary[nameof(SerializedColor.B)]),
                    Convert.ToSingle(dictionary[nameof(SerializedColor.G)]),
                    Convert.ToSingle(dictionary[nameof(SerializedColor.R)]),
                    Convert.ToSingle(dictionary[nameof(SerializedColor.A)])
                );
            }
            else
            {
                return null;
            }
        }

        public Span<byte> ConvertToHashBase(object? value)
        {
            if (value is Vector4 color)
            {
                return color.ConvertToSpan();
            }
            else
            {
                return TransparentHashBase;
            }
        }
    }

    file record SerializedColor(float B, float G, float R, float A);
}
