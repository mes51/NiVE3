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

        // Orientation_Index_Position
        Vertical    = 0b01_00_00000,
        Horizontal  = 0b10_00_00000,
        CenterArea  = 0b00_00_00001,
        TopArea     = 0b00_00_00010,
        BottomArea  = 0b00_00_00100,
        LeftArea    = 0b00_00_01000,
        RightArea   = 0b00_00_10000,
        FirstPanel  = 0b00_00_00000,
        SecondPanel = 0b00_01_00000,

        Top = Vertical | TopArea,
        Bottom = Vertical | BottomArea,
        Left1Top = Horizontal | LeftArea | TopArea,
        Left1Center = Horizontal | LeftArea | CenterArea,
        Left1Bottom = Horizontal | LeftArea | BottomArea,
        Left2Top = Horizontal | LeftArea | TopArea | SecondPanel,
        Left2Center = Horizontal | LeftArea | CenterArea | SecondPanel,
        Left2Bottom = Horizontal | LeftArea | BottomArea | SecondPanel,
        Right1Top = Horizontal | RightArea | TopArea,
        Right1Center = Horizontal | RightArea | CenterArea,
        Right1Bottom = Horizontal | RightArea | BottomArea,
        Right2Top = Horizontal | RightArea | TopArea | SecondPanel,
        Right2Center = Horizontal | RightArea | CenterArea | SecondPanel,
        Right2Bottom = Horizontal | RightArea | BottomArea | SecondPanel
    }
}
