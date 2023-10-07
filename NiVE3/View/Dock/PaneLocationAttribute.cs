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

        public double Size { get; set; }

        public PaneLocationAttribute(PaneLocation layout)
        {
            Layout = layout;
        }
    }

    [Flags]
    public enum PaneLocation
    {
        Document = 0,

        Vertical   = 0b0100000,
        Horizontal = 0b1000000,
        CenterArea = 0b0000001,
        TopArea    = 0b0000010,
        BottomArea = 0b0000100,
        LeftArea   = 0b0001000,
        RightArea  = 0b0010000,

        Top = Vertical | TopArea,
        Bottom = Vertical | BottomArea,
        LeftTop = Horizontal | LeftArea | TopArea,
        LeftCenter = Horizontal | LeftArea | CenterArea,
        LeftBottom = Horizontal | LeftArea | BottomArea,
        RightTop = Horizontal | RightArea | TopArea,
        RightCenter = Horizontal | RightArea | CenterArea,
        RightBottom = Horizontal | RightArea | BottomArea
    }
}
