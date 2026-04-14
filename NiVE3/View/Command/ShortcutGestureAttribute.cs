using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using NiVE3.Shared.Extension;
using NiVE3.Shared.Util;

namespace NiVE3.View.Command
{
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
    sealed class ShortcutGestureAttribute : Attribute
    {
        static Dictionary<Type, ILookup<string, Tuple<ShortcutGestureAttribute, PropertyInfo>>> Cache { get; } = [];

        public string TargetGesture { get; }

        public bool IsGlobal { get; set; }

        public ShortcutGestureAttribute(string targetGesture)
        {
            TargetGesture = targetGesture;
        }

        public static ICommand? GetCommand(object target, string targetGesture, bool isGlobal)
        {
            var type = target.GetType();
#pragma warning disable CA1854 // リファクタを実行した結果の方が変になるため無視する
            if (!Cache.ContainsKey(type))
            {
                Cache.Add(type, CreateCache(type));
            }
#pragma warning restore CA1854 // 'IDictionary.TryGetValue(TKey, out TValue)' メソッドを優先します

            var commandInfo = Cache[type][targetGesture].FirstOrDefault();
            if (commandInfo != null && (!isGlobal || commandInfo.Item1.IsGlobal))
            {
                return commandInfo.Item2.GetValue(target) as ICommand;
            }
            else
            {
                return null;
            }
        }

        static ILookup<string, Tuple<ShortcutGestureAttribute, PropertyInfo>> CreateCache(Type type)
        {
            var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            return properties.Select(p =>
                {
                    var attr = p.GetCustomAttribute<ShortcutGestureAttribute>();
                    if (attr != null)
                    {
                        return Tuple.Create(attr, p);
                    }
                    else
                    {
                        return null;
                    }
                })
                .NonNull()
                .ToLookup(t => t.Item1.TargetGesture);
        }
    }
}
