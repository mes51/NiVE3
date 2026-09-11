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

        /// <summary>
        /// AvalonDock 4 系で保存された XML 形式のドッキングレイアウト (読み込み専用、保存時は空になる)
        /// </summary>
        public string DockingLayout { get; set; } = "";

        /// <summary>
        /// JSON 形式のドッキングレイアウト
        /// </summary>
        public string DockingLayoutJson { get; set; } = "";
    }
}
