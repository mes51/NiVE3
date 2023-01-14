using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Property.Control;
using NiVE3.Plugin.Property.Types;

namespace NiVE3.Plugin.Property
{
    public abstract class PropertyBase
    {
        public string Name { get; }

        internal PropertyBase(string name)
        {
            Name = name;
        }
    }

    public class Property : PropertyBase
    {
        public IPropertyType PropertyType { get; }

        public Property(string name, IPropertyType propertyType) : base(name)
        {
            PropertyType = propertyType;
        }
    }

    public class PropertyGroup : PropertyBase
    {
        public PropertyBase[] Children { get; }

        public PropertyGroup(string name, PropertyBase[] children) : base(name)
        {
            Children = children;
        }
    }
}
