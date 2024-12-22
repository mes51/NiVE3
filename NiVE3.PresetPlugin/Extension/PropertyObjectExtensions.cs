using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.ValueObject;

namespace NiVE3.PresetPlugin.Extension
{
    static class PropertyObjectExtensions
    {
        public static T GetValue<T>(this IReadOnlyCollection<IPropertyObject> properties, string id, Time time, T defaultValue) where T : notnull
        {
            return (T)(properties.First(p => p.Id == id).GetValue(time) ?? defaultValue);
        }

        public static T? GetValue<T>(this IReadOnlyCollection<IPropertyObject> properties, string id, Time time)
        {
            return (T?)properties.First(p => p.Id == id).GetValue(time);
        }

        public static T GetValue<T>(this IPropertyObject property, Time time, T defaultValue) where T : notnull
        {
            return (T)(property.GetValue(time) ?? defaultValue);
        }
    }
}
