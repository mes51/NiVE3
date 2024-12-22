using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Internal.Util;
using NiVE3.Plugin.ValueObject;
using NiVE3.Shared.Extension;

namespace NiVE3.Plugin.Property.Types
{
    public class ColorPropertyType : IPropertyType
    {
        static readonly byte[] TransparentHashBase = [..Enumerable.Repeat((byte)0, Marshal.SizeOf<Vector4>())];

        public static readonly ColorPropertyType Instance = new ColorPropertyType();

        public InterpolationType SupportedInterpolationTypes => InterpolationType.None | InterpolationType.Linear | InterpolationType.CatmullRom;

        public bool IsSupportedExpression => true;

        private ColorPropertyType() { }

        public object? Interpolate(IReadOnlyList<KeyFrame> keyFrames, Time time)
        {
            var baseKeyFrameIndex = keyFrames.FindLastIndex(k => k.Time <= time);
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
                    return Interpolation.Linear((Vector4)keyFrame1.Value!, (Vector4)keyFrame2.Value!, keyFrame1.Time, keyFrame2.Time, (double)time);
                case InterpolationType.CatmullRom:
                    {
                        var keyFrame0 = baseKeyFrameIndex > 0 ? keyFrames[baseKeyFrameIndex - 1] : keyFrame1;
                        var keyFrame3 = baseKeyFrameIndex <= keyFrames.Count - 3 ? keyFrames[baseKeyFrameIndex + 2] : keyFrame2;
                        return Interpolation.CatmullRom((Vector4)keyFrame0.Value!, (Vector4)keyFrame1.Value!, (Vector4)keyFrame2.Value!, (Vector4)keyFrame3.Value!, keyFrame1.Time, keyFrame2.Time, (double)time);
                    }
                default:
                    return keyFrame1.Value;
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

        public bool TryConvertFromExpressionValue(object? expressionValue, object? rawValue, out object? value)
        {
            if (expressionValue is object[] colorElements)
            {
                if (colorElements.Length < 3)
                {
                    value = null;
                    return false;
                }

                try
                {
                    var b = Convert.ToSingle(colorElements[0]);
                    var g = Convert.ToSingle(colorElements[1]);
                    var r = Convert.ToSingle(colorElements[2]);

                    if (colorElements.Length > 3)
                    {
                        var a = Convert.ToSingle(colorElements[3]);
                        value = new Vector4(b, g, r, a);
                    }
                    else
                    {
                        value = new Vector4(b, g, r, 1.0F);
                    }
                    return true;
                }
                catch { } // そのまま最後に流れ落ちる
            }

            value = null;
            return false;
        }

        public object? ConvertToExpressionValue(object? value)
        {
            var color = (Vector4)(value ?? Vector4.Zero);

            return new object[] { color.X, color.Y, color.Z, color.W };
        }
    }

    file record SerializedColor(float B, float G, float R, float A);
}
