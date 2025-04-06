using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using NiVE3.Data.Config;
using NiVE3.Util;

namespace NiVE3.Config
{
    class WindowLayoutSetting
    {
        const double DefaultWindowWidth = 1800;

        const double DefaultWindowHeight = 1000.0;

        static readonly string FilePath = Path.Combine(Paths.ConfigDirectory, "layout.json");

        public static WindowLayoutSetting Setting { get; }

        public Point Location { get; set; } = new Point((SystemParameters.PrimaryScreenWidth - DefaultWindowWidth) * 0.5, (SystemParameters.PrimaryScreenHeight - DefaultWindowHeight) * 0.5);

        public Size Size { get; set; } = new Size(DefaultWindowWidth, DefaultWindowHeight);

        public WindowState WindowState { get; set; } = WindowState.Normal;

        public string DockingLayout { get; set; } = "";

        static WindowLayoutSetting()
        {
            Setting = new WindowLayoutSetting();
            Setting.Load();
        }

        private WindowLayoutSetting() { }

        public void Save()
        {
            var data = new WindowLayoutData
            {
                Location = Location,
                Size = Size,
                WindowState = WindowState,
                DockingLayout = DockingLayout
            };

            var json = JsonSerializer.Serialize(data);
            File.WriteAllText(FilePath, json);
        }

        void Load()
        {
            if (!File.Exists(FilePath))
            {
                return;
            }

            var data = JsonSerializer.Deserialize<WindowLayoutData>(File.ReadAllBytes(FilePath));
            if (data == null)
            {
                return;
            }

            Location = data.Location;
            Size = data.Size;
            WindowState = data.WindowState;
            DockingLayout = data.DockingLayout;
        }
    }
}
