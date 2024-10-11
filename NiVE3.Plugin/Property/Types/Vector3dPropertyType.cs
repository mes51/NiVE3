using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Shared.Extension;
using NiVE3.Numerics;
using System.Runtime.InteropServices;
using NiVE3.Plugin.Internal.Util;
using System.Diagnostics.CodeAnalysis;

namespace NiVE3.Plugin.Property.Types
{
    public class Vector3dPropertyType : IPropertyType
    {
        static readonly byte[] ZeroHashBase = [..Enumerable.Repeat((byte)0, Marshal.SizeOf<Vector3d>())];

        public static Vector3dPropertyType Instance { get; } = new Vector3dPropertyType();

        public InterpolationType SupportedInterpolationTypes => InterpolationType.None | InterpolationType.Linear | InterpolationType.CatmullRom;

        private Vector3dPropertyType() { }

        public object Interpolate(IReadOnlyList<KeyFrame> keyFrames, double t)
        {
            var baseKeyFrameIndex = keyFrames.IndexOfLast(k => k.Time <= t);
            if (baseKeyFrameIndex < 0)
            {
                return keyFrames[0].Value!;
            }
            else if (baseKeyFrameIndex >= keyFrames.Count - 1)
            {
                return keyFrames[baseKeyFrameIndex].Value!;
            }
            var keyFrame1 = keyFrames[baseKeyFrameIndex];
            var keyFrame2 = keyFrames[baseKeyFrameIndex + 1];
            switch (keyFrames[baseKeyFrameIndex].InterpolationType)
            {
                case InterpolationType.Linear:
                    {
                        var v1 = (Vector3d)keyFrame1.Value!;
                        var v2 = (Vector3d)keyFrame2.Value!;
                        return new Vector3d(
                            Interpolation.Linear(v1.X, v2.X, keyFrame1.Time, keyFrame2.Time, t),
                            Interpolation.Linear(v1.Y, v2.Y, keyFrame1.Time, keyFrame2.Time, t),
                            Interpolation.Linear(v1.Z, v2.Z, keyFrame1.Time, keyFrame2.Time, t)
                        );
                    }
                case InterpolationType.CatmullRom:
                    {
                        var keyFrame0 = baseKeyFrameIndex > 0 ? keyFrames[baseKeyFrameIndex - 1] : keyFrame1;
                        var keyFrame3 = baseKeyFrameIndex <= keyFrames.Count - 3 ? keyFrames[baseKeyFrameIndex + 2] : keyFrame2;

                        var v0 = (Vector3d)keyFrame0.Value!;
                        var v1 = (Vector3d)keyFrame1.Value!;
                        var v2 = (Vector3d)keyFrame2.Value!;
                        var v3 = (Vector3d)keyFrame3.Value!;

                        return new Vector3d(
                            Interpolation.CatmullRom(v0.X, v1.X, v2.X, v3.X, keyFrame1.Time, keyFrame2.Time, t),
                            Interpolation.CatmullRom(v0.Y, v1.Y, v2.Y, v3.Y, keyFrame1.Time, keyFrame2.Time, t),
                            Interpolation.CatmullRom(v0.Z, v1.Z, v2.Z, v3.Z, keyFrame1.Time, keyFrame2.Time, t)
                        );
                    }
                default:
                    return keyFrame1.Value!;
            }
        }

        public object? SerializeValue(object? value)
        {
            return value;
        }

        public object? DeserializeValue(object? serializedValue)
        {
            return serializedValue switch
            {
                IDictionary<string, object> dictionary => new Vector3d(
                                        Convert.ToDouble(dictionary[nameof(Vector3d.X)]),
                                        Convert.ToDouble(dictionary[nameof(Vector3d.Y)]),
                                        Convert.ToDouble(dictionary[nameof(Vector3d.Z)])
                                    ),
                Vector3d vector => vector,
                _ => null,
            };
        }

        public Span<byte> ConvertToHashBase(object? value)
        {
            if (value is Vector3d v)
            {
                return v.ConvertToSpan();
            }
            else
            {
                return ZeroHashBase;
            }
        }
    }
}
