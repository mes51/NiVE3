using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.View.Command
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    sealed class CommandHandlingAttribute : Attribute
    {
        public string TargetGesture { get; }

        public string CommandName { get; }

        public bool IsGlobal { get; set; }

        public CommandHandlingAttribute(string commandName, string targetGesture)
        {
            CommandName = commandName;
            TargetGesture = targetGesture;
        }
    }
}
