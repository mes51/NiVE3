using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Internal.Util;
using NiVE3.Shared.Extension;

namespace NiVE3.Plugin.Property.Types
{
    public class EnumPropertyType : IPropertyType
    {
        const string SerializedKeyAssemblyName = "Assembly";

        const string SerializedKeyType = "Type";

        const string SerializedKeyValue = "Value";

        public static readonly EnumPropertyType Instance = new EnumPropertyType();

        public InterpolationType SupportedInterpolationTypes => InterpolationType.None;

        public bool IsSupportedExpression => false;

        private EnumPropertyType() { }

        public object? Interpolate(IReadOnlyList<KeyFrame> keyFrames, double t)
        {
            var baseKeyFrameIndex = keyFrames.FindLastIndex(k => k.Time <= t);
            if (baseKeyFrameIndex < 0)
            {
                return keyFrames[0].Value;
            }
            else if (baseKeyFrameIndex >= keyFrames.Count - 1)
            {
                return keyFrames[baseKeyFrameIndex].Value;
            }
            return keyFrames[baseKeyFrameIndex].Value;
        }

        public object? SerializeValue(object? value)
        {
            if (value != null)
            {
                return new Dictionary<string, object?>
                {
                    { SerializedKeyAssemblyName, value.GetType().Assembly.FullName },
                    { SerializedKeyType, value.GetType().FullName },
                    { SerializedKeyValue, value.ToString() }
                };
            }
            else
            {
                return null;
            }
        }

        public object? DeserializeValue(object? serializedValue)
        {
            if (serializedValue is IDictionary<string, object> dictionary)
            {
                var assemblyName = (string)dictionary[SerializedKeyAssemblyName];
                var typeName = (string)dictionary[SerializedKeyType];
                var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.FullName == assemblyName);
                var type = assembly?.GetType(typeName) ?? AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.DefinedTypes).FirstOrDefault(t => t.FullName == typeName);
                if (type != null && type.IsEnum)
                {
                    return Enum.Parse(type, (string)dictionary[SerializedKeyValue]);
                }
            }

            return null;
        }

        public Span<byte> ConvertToHashBase(object? value)
        {
            if (value is Enum e)
            {
                return Convert.ToInt32(e).ConvertToSpan();
            }
            else
            {
                return [];
            }
        }

        public bool TryConvertFromExpressionValue(object? expressionValue, object? rawValue, out object? value)
        {
            throw new NotImplementedException();
        }

        public object? ConvertToExpressionValue(object? value)
        {
            if (value is Enum e)
            {
                return Convert.ToInt32(e);
            }
            else
            {
                return null;
            }
        }
    }
}
