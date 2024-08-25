using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.Config
{
    class ApplicationSetting
    {
        public static ApplicationSetting Setting { get; }

        static ApplicationSetting()
        {
            Setting = new ApplicationSetting();
        }

        private ApplicationSetting() { }

        public event EventHandler? UpdateSetting;

        public bool IsDarkMode { get; set; }

        public string SolidFolderName { get; set; } = "Solid";

        public bool ForceUseCpu { get; set; }

        public string UseGpuLuid { get; set; } = "";

        public void RaiseUpdateSetting()
        {
            UpdateSetting?.Invoke(this, EventArgs.Empty);
        }
    }
}
