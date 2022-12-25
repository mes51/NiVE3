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
        static Dictionary<Type, ILookup<string, Tuple<CommandHandlingAttribute, PropertyInfo>>> Cache { get; } = new Dictionary<Type, ILookup<string, Tuple<CommandHandlingAttribute, PropertyInfo>>>();

        public string TargetGesture { get; }

        public string CommandName { get; }

        public bool IsGlobal { get; set; }

        public CommandHandlingAttribute(string commandName, string targetGesture)
        {
            CommandName = commandName;
            TargetGesture = targetGesture;
        }

        public static ICommand? GetCommand(object target, string targetGesture, bool isGlobal)
        {
            var type = target.GetType();
            if (!Cache.ContainsKey(type))
            {
                Cache.Add(type, CreateCache(type));
            }

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
