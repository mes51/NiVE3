using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.Extension
{
    static class AttributeExtensions
    {
        public static bool IsApplied<T>(this Type type) where T : Attribute
        {
            return type.GetCustomAttribute<T>() != null;
        }

        public static bool IsApplied<T>(this object obj) where T : Attribute
        {
            return obj.GetType().GetCustomAttribute<T>() != null;
        }
    }
}
