using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.ValueObject;

namespace NiVE3.Plugin.Property.Types
{
    /// <summary>
    /// PropertyGroup用のPropertyType
    /// </summary>
    internal class PropertyGroupType : IPropertyType
    {
        public static readonly IPropertyType Instance = new PropertyGroupType();

        private PropertyGroupType() { }

        public InterpolationType SupportedInterpolationTypes => throw new NotImplementedException();

        public bool IsSupportedExpression => throw new NotImplementedException();

        public Span<byte> ConvertToHashBase(object? value)
        {
            throw new NotImplementedException();
        }

        public object? DeserializeValue(object? serializedValue)
        {
            throw new NotImplementedException();
        }

        public object Interpolate(IReadOnlyList<KeyFrame> keyFrames, Time time)
        {
            throw new NotImplementedException();
        }

        public object? SerializeValue(object? value)
        {
            throw new NotImplementedException();
        }

        public bool TryConvertFromExpressionValue(object? expressionValue, object? rawValue, out object? value)
        {
            throw new NotImplementedException();
        }

        public object? ConvertToExpressionValue(object? value)
        {
            return null;
        }
    }
}
