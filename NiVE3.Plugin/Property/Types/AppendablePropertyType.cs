using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.Plugin.Property.Types
{
    /// <summary>
    /// AppendableProperty用のPropertyType
    /// </summary>
    internal class AppendablePropertyType : IPropertyType
    {
        public static readonly AppendablePropertyType Instance = new AppendablePropertyType();

        private AppendablePropertyType() { }

        public InterpolationType SupportedInterpolationTypes => throw new NotImplementedException();

        public Span<byte> ConvertToHashBase(object? value)
        {
            throw new NotImplementedException();
        }

        public object? DeserializeValue(object? serializedValue)
        {
            throw new NotImplementedException();
        }

        public object? Interpolate(IReadOnlyList<KeyFrame> keyFrames, double t)
        {
            throw new NotImplementedException();
        }

        public object? SerializeValue(object? value)
        {
            throw new NotImplementedException();
        }
    }
}
