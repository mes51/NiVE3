using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

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

        public Color DefaultImageLayerTag { get; set; } = Color.FromRgb(237, 66, 97);

        public Color DefaultAudioLayerTag { get; set; } = Color.FromRgb(66, 98, 237);

        public Color DefaultVideoLayerTag { get; set; } = Color.FromRgb(92, 107, 173);

        public Color DefaultShapeLayerTag { get; set; } = Color.FromRgb(66, 237, 97);

        public Color DefaultCameraLayerTag { get; set; } = Color.FromRgb(237, 194, 66);

        public Color DefaultLightLayerTag { get; set; } = Color.FromRgb(152, 93, 104);

        public Color DefaultNullObjectLayerTag { get; set; } = Color.FromRgb(86, 90, 110);

        public Color DefaultTextLayerTag { get; set; } = Color.FromRgb(93, 152, 104);

        public Color DefaultCompositionLayerTag { get; set; } = Color.FromRgb(152, 137, 93);

        public int ExpressionTimeout { get; set; } = 10;

        public void RaiseUpdateSetting()
        {
            UpdateSetting?.Invoke(this, EventArgs.Empty);
        }
    }
}
