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
            return Attribute.IsDefined(type, typeof(T));
        }

        public static bool IsApplied<T>(this object obj) where T : Attribute
        {
            return Attribute.IsDefined(obj.GetType(), typeof(T));
        }

        public static bool IsApplied<T>(this MemberInfo member) where T : Attribute
        {
            return member.IsDefined(typeof(T));
        }
    }
}
