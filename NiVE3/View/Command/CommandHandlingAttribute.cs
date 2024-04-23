using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NiVE3.View.Command
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    sealed class CommandHandlingAttribute : Attribute
    {
        static Dictionary<Type, ILookup<string, Tuple<CommandHandlingAttribute, PropertyInfo>>> Cache { get; } = [];

        public string TargetGesture { get; }

        public string CommandName { get; }

        public bool IsGlobal { get; set; }

        public bool IsNonEditCommand { get; set; }

        public CommandHandlingAttribute(string commandName, string targetGesture)
        {
            CommandName = commandName;
            TargetGesture = targetGesture;
        }

        public static ICommand? GetCommand(object target, string targetGesture, bool isGlobal, bool isNonEditOnly)
        {
            var type = target.GetType();
#pragma warning disable CA1854 // リファクタを実行した結果の方が変になるため無視する
            if (!Cache.ContainsKey(type))
            {
                Cache.Add(type, CreateCache(type));
            }
#pragma warning restore CA1854 // 'IDictionary.TryGetValue(TKey, out TValue)' メソッドを優先します

            var commandInfo = Cache[type][targetGesture].FirstOrDefault();
            if (commandInfo != null && (!isGlobal || commandInfo.Item1.IsGlobal) && (!isNonEditOnly || commandInfo.Item1.IsNonEditCommand))
            {
                return commandInfo.Item2.GetValue(target) as ICommand;
            }
            else
            {
                return null;
            }
        }

        static ILookup<string, Tuple<CommandHandlingAttribute, PropertyInfo>> CreateCache(Type type)
        {
            return type.GetCustomAttributes<CommandHandlingAttribute>(false)
                .Select(a =>
                {
                    var commandProperty = type.GetProperty(a.CommandName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (commandProperty == null)
                    {
                        // NOTE: ViewModel内に存在しないコマンドは指定してはいけない
                        throw new InvalidOperationException($"this is bug! {a.CommandName} is not define in {type.Name}");
                    }
                    return Tuple.Create(a, commandProperty);
                })
                .ToLookup(t => t.Item1.TargetGesture);
        }
    }
}
