using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Interfaces;

namespace NiVE3.PresetPlugin.Extension
{
    static class PropertyObjectExtensions
    {
        public static T GetValue<T>(this IPropertyObject[] properties, string id, double time, T defaultValue) where T : notnull
        {
            return (T)(properties.First(p => p.Id == id).GetValue(time) ?? defaultValue);
        }

        public static T? GetValue<T>(this IPropertyObject[] properties, string id, double time)
        {
            return (T?)properties.First(p => p.Id == id).GetValue(time);
        }

        public static T GetValue<T>(this IPropertyObject property, double time, T defaultValue) where T : notnull
        {
            return (T)(property.GetValue(time) ?? defaultValue);
        }
    }
}
