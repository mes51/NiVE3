using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Shared.Extension;

namespace NiVE3.Plugin.Property.Types
{
    public class ColorPropertyType : IPropertyType
    {
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

        public bool TryConvertFrom(object otherValue, out object convertedValue)
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
            return value;
        }

        public object? DeserializeValue(object? serializedValue)
        {
            switch (serializedValue)
            {
                case IDictionary<string, object> dictionary:
                    return new Vector4(
                        Convert.ToSingle(dictionary[nameof(Vector4.X)]),
                        Convert.ToSingle(dictionary[nameof(Vector4.Y)]),
                        Convert.ToSingle(dictionary[nameof(Vector4.Z)]),
                        Convert.ToSingle(dictionary[nameof(Vector4.W)])
                    );
                case Vector4 vector4:
                    return vector4;
                default:
                    return null;
            }
        }
    }
}
