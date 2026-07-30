using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.ValueObject;
using NiVE3.Shared.Extension;

namespace NiVE3.Plugin.Property.Types
{
    /// <summary>
    /// 実行時に決まる選択肢一覧からインデックス (int) で選択するプロパティの型
    /// </summary>
    public class SelectBoxPropertyType : IPropertyType
    {
        static readonly byte[] ZeroHashBase = [.. Enumerable.Repeat((byte)0, sizeof(int))];

        public static readonly SelectBoxPropertyType Instance = new SelectBoxPropertyType();

        public InterpolationType SupportedInterpolationTypes => InterpolationType.None;

        public bool IsSupportedExpression => true;

        public bool IsSupportedGraphEditor => false;

        private SelectBoxPropertyType() { }

        public object? Interpolate(IReadOnlyList<KeyFrame> keyFrames, Time time)
        {
            var baseKeyFrameIndex = keyFrames.FindLastIndex(k => k.Time <= time);
            if (baseKeyFrameIndex < 0)
            {
                return keyFrames[0].Value;
            }
            return keyFrames[Math.Min(baseKeyFrameIndex, keyFrames.Count - 1)].Value;
        }

        public object? SerializeValue(object? value)
        {
            return value;
        }

        public object? DeserializeValue(object? serializedValue)
        {
            return serializedValue == null ? 0 : Convert.ToInt32(serializedValue);
        }

        public Span<byte> ConvertToHashBase(object? value)
        {
            return value is int v ? BitConverter.GetBytes(v) : ZeroHashBase;
        }

        public bool TryConvertFromExpressionValue(object? expressionValue, object? rawValue, out object? value)
        {
            switch (expressionValue)
            {
                case int v:
                    value = v;
                    return true;
                case long v:
                    value = (int)v;
                    return true;
                case double v:
                    value = (int)Math.Round(v);
                    return true;
                default:
                    value = 0;
                    return false;
            }
        }

        public object? ConvertToExpressionValue(object? value)
        {
            return value;
        }
    }
}
