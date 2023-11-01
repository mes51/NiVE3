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

        public double Appearance { get; set; } = 1.0;

        public string SolidFolderName { get; set; } = "Solid";

        public bool UseGpu { get; set; } = true;
    }
}
