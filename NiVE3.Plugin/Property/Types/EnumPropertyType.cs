using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        private EnumPropertyType() { }

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
            return keyFrames[baseKeyFrameIndex].Value;
        }

        public bool TryConvertFrom(object otherValue, out object convertedValue)
        {
            switch (otherValue)
            {
                case byte v:
                    convertedValue = (int)v;
                    return true;
                case sbyte v:
                    convertedValue = (int)v;
                    return true;
                case short v:
                    convertedValue = (int)v;
                    return true;
                case ushort v:
                    convertedValue = (int)v;
                    return true;
                case int v:
                    convertedValue = (int)v;
                    return true;
                case uint v:
                    convertedValue = (int)v;
                    return true;
                case long v:
                    convertedValue = (int)v;
                    return true;
                case ulong v:
                    convertedValue = (int)v;
                    return true;
                default:
                    convertedValue = 0.0;
                    return false;
            }
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
                var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.FullName == assemblyName);
                if (assembly == null)
                {
                    return null;
                }
                var type = assembly.GetType((string)dictionary[SerializedKeyType]);
                if (type != null && type.IsEnum)
                {
                    return Enum.Parse(type, (string)dictionary[SerializedKeyValue]);
                }
            }

            return null;
        }
    }
}
