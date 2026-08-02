using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Property.Types;
using NiVE3.Plugin.ValueObject;

namespace NiVE3.OpenFX.Bridge.Property.Types
{
    /// <summary>
    /// 値を持たない、クリック操作のみのプロパティの型
    /// </summary>
    public class OfxButtonPropertyType : IPropertyType
    {
        static readonly byte[] EmptyHashBase = [];

        public static readonly OfxButtonPropertyType Instance = new OfxButtonPropertyType();

        public InterpolationType SupportedInterpolationTypes => InterpolationType.None;

        public bool IsSupportedExpression => false;

        public bool IsSupportedGraphEditor => false;

        private OfxButtonPropertyType() { }

        public object? Interpolate(IReadOnlyList<KeyFrame> keyFrames, Time time)
        {
            return null;
        }

        public object? SerializeValue(object? value)
        {
            return null;
        }

        public object? DeserializeValue(object? serializedValue)
        {
            return null;
        }

        public Span<byte> ConvertToHashBase(object? value)
        {
            return EmptyHashBase;
        }

        public bool TryConvertFromExpressionValue(object? expressionValue, object? rawValue, out object? value)
        {
            throw new NotImplementedException();
        }

        public object? ConvertToExpressionValue(object? value)
        {
            throw new NotImplementedException();
        }
    }
}
