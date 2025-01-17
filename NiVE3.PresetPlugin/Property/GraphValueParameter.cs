using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Shared.Extension;

namespace NiVE3.PresetPlugin.Property
{
    class GraphValueParameter
    {
        public static readonly GraphValueParameter Identity = new GraphValueParameter([..Enumerable.Repeat(1.0F, ValueCount)]);

        public static readonly GraphValueParameter LinearUp = new GraphValueParameter([..Enumerable.Range(0, ValueCount).Select(i => i / (float)(ValueCount - 1))]);

        public static readonly GraphValueParameter LinearDown = new GraphValueParameter([..Enumerable.Range(0, ValueCount).Select(i => 1.0F - i / (float)(ValueCount - 1))]);

        public static readonly GraphValueParameter Triangle = new GraphValueParameter([..Enumerable.Range(0, ValueCount).Select(i => 1.0F - Math.Abs((i / (float)(ValueCount - 1)) * 2.0F - 1.0F))]);

        public static readonly GraphValueParameter Curve = new GraphValueParameter([..Enumerable.Range(0, ValueCount).Select(i => Math.Clamp(MathF.Sqrt(1.0F - MathF.Pow((i / (float)(ValueCount - 1)) * 2.0F - 1.0F, 2.0F)), 0.0F, 1.0F))]);

        public const int ValueCount = 100;

        public float[] Values { get; }

        public GraphValueParameter(float[] values)
        {
            Values = CoerceValues(values);
        }

        public object Serialize()
        {
            return new Dictionary<string, object>
            {
                { nameof(Values), Values }
            };
        }

        public static GraphValueParameter? Deserialize(object? serializedValue)
        {
            if (serializedValue is not IDictionary<string, object?> dictionary ||
                !dictionary.TryGetValue(nameof(Values), out var values) ||
                values is not float[] v)
            {
                return null;
            }

            return new GraphValueParameter(v);
        }

        static float[] CoerceValues(float[] values)
        {
            if (values.Length != ValueCount)
            {
                return [.. values.Concat(EnumerableExtensions.RepeatInfinity(values[^1])).Take(ValueCount)];
            }
            else
            {
                return values;
            }
        }

        public float Interpolation(float value1, float value2, float t)
        {
            if (t <= 0.0)
            {
                return float.Lerp(value1, value2, Values[0]);
            }
            else if (t >= 1.0)
            {
                return float.Lerp(value1, value2, Values[^1]);
            }

            var current = t * (ValueCount - 1);
            var prevIndex = (int)MathF.Floor(current);
            var nextIndex = (int)MathF.Ceiling(current);

            var interpolationDiff = current - prevIndex;
            return float.Lerp(value1, value2, float.Lerp(Values[prevIndex], Values[nextIndex], interpolationDiff));
        }

        public Vector4 Interpolation(Vector4 value1, Vector4 value2, float t)
        {
            if (t <= 0.0)
            {
                return Vector4.Lerp(value1, value2, Values[0]);
            }
            else if (t >= 1.0)
            {
                return Vector4.Lerp(value1, value2, Values[^1]);
            }

            var current = t * (ValueCount - 1);
            var prevIndex = (int)MathF.Floor(current);
            var nextIndex = (int)MathF.Ceiling(current);

            var interpolationDiff = current - prevIndex;
            return Vector4.Lerp(value1, value2, float.Lerp(Values[prevIndex], Values[nextIndex], interpolationDiff));
        }
    }
}
