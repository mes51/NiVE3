using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace NiVE3.Data.Config
{
    public class WindowLayoutData
    {
        public Point Location { get; set; }

        public Size Size { get; set; }

        public WindowState WindowState { get; set; }

        public string DockingLayout { get; set; } = "";
    }
}
