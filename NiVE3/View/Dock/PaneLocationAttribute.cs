using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.View.Dock
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    sealed class PaneLocationAttribute : Attribute
    {
        public PaneLocation Layout { get; }

        public PaneLocationAttribute(PaneLocation layout)
        {
            Layout = layout;
        }
    }

    public enum PaneLocation
    {
        Document,
        Left,
        Top,
        Right,
        Bottom
    }
}
