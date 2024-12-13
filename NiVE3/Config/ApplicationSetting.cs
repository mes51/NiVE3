using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Media;
using NiVE3.Data.Json.Converter;
using NiVE3.Extension;
using NiVE3.Util;

namespace NiVE3.Config
{
    class ApplicationSetting
    {
        static readonly string FilePath = Path.Combine(Paths.ConfigDirectory, "application.json");

        public static ApplicationSetting Setting { get; }

        static ApplicationSetting()
        {
            Setting = new ApplicationSetting();
            Setting.Load();
        }

        public event EventHandler? UpdateSetting;

        [SerializableSetting]
        public bool IsDarkMode { get; set; }

        [SerializableSetting]
        public string SolidFolderName { get; set; } = "Solid";

        [SerializableSetting]
        public bool ForceUseCpu { get; set; }

        [SerializableSetting]
        public string UseGpuLuid { get; set; } = "";

        [SerializableSetting]
        [JsonConverter(typeof(ColorJsonConverter))]
        public Color DefaultImageLayerTag { get; set; } = Color.FromRgb(237, 66, 97);

        [SerializableSetting]
        [JsonConverter(typeof(ColorJsonConverter))]
        public Color DefaultAudioLayerTag { get; set; } = Color.FromRgb(66, 98, 237);

        [SerializableSetting]
        [JsonConverter(typeof(ColorJsonConverter))]
        public Color DefaultVideoLayerTag { get; set; } = Color.FromRgb(92, 107, 173);

        [SerializableSetting]
        [JsonConverter(typeof(ColorJsonConverter))]
        public Color DefaultShapeLayerTag { get; set; } = Color.FromRgb(66, 237, 97);

        [SerializableSetting]
        [JsonConverter(typeof(ColorJsonConverter))]
        public Color DefaultCameraLayerTag { get; set; } = Color.FromRgb(237, 194, 66);

        [SerializableSetting]
        [JsonConverter(typeof(ColorJsonConverter))]
        public Color DefaultLightLayerTag { get; set; } = Color.FromRgb(152, 93, 104);

        [SerializableSetting]
        [JsonConverter(typeof(ColorJsonConverter))]
        public Color DefaultNullObjectLayerTag { get; set; } = Color.FromRgb(86, 90, 110);

        [SerializableSetting]
        [JsonConverter(typeof(ColorJsonConverter))]
        public Color DefaultTextLayerTag { get; set; } = Color.FromRgb(93, 152, 104);

        [SerializableSetting]
        [JsonConverter(typeof(ColorJsonConverter))]
        public Color DefaultCompositionLayerTag { get; set; } = Color.FromRgb(152, 137, 93);

        [SerializableSetting]
        public int ExpressionTimeout { get; set; } = 10;

        [SerializableSetting]
        public int ImageCacheLimit { get; set; } = Math.Min(SystemInfo.MaxCacheLimitMiB - Math.Min((int)Math.Floor(SystemInfo.MaxCacheLimitMiB * 0.25), 4 * 1024), 16 * 1024);

        [SerializableSetting]
        public int RamPreviewCacheLimit { get; set; } = Math.Min((int)Math.Floor(SystemInfo.MaxCacheLimitMiB * 0.25), 4 * 1024);

        [SerializableSetting]
        public bool IsCompressCache { get; set; } = false;

        [SerializableSetting]
        public bool UseAutoSave { get; set; } = true;

        [SerializableSetting]
        public int AutoSaveInterval { get; set; } = 10;

        [SerializableSetting]
        public int AutoSaveCount { get; set; } = 10;

        public void RaiseUpdateSetting()
        {
            UpdateSetting?.Invoke(this, EventArgs.Empty);
        }

        public void Save()
        {
            var json = JsonSerializer.Serialize(this);
            File.WriteAllText(FilePath, json);
        }

        void Load()
        {
            if (!File.Exists(FilePath))
            {
                return;
            }

            try
            {
                var applicationSettingData = JsonSerializer.Deserialize<ApplicationSetting>(File.ReadAllText(FilePath));
                if (applicationSettingData == null)
                {
                    return;
                }

                foreach (var p in typeof(ApplicationSetting).GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(p => p.IsApplied<SerializableSettingAttribute>()))
                {
                    p.SetValue(this, p.GetValue(applicationSettingData));
                }

                // NOTE: 万一SystemInfo.MaxCacheLimitMiBがConst.MinImageCacheSizeMiB以下になったときのためにClampは使用しない
                ImageCacheLimit = Math.Max(Math.Min(SystemInfo.MaxCacheLimitMiB, ImageCacheLimit), Const.MinImageCacheSizeMiB);
                RamPreviewCacheLimit = Math.Max(Math.Min(SystemInfo.MaxCacheLimitMiB - ImageCacheLimit, RamPreviewCacheLimit), Const.MinImageCacheSizeMiB);

                AutoSaveInterval = Math.Clamp(AutoSaveInterval, Const.MinAutoSaveInterval, Const.MaxAutoSaveInterval);
                AutoSaveCount = Math.Clamp(AutoSaveCount, Const.MinAutoSaveCount, Const.MaxAutoSaveCount);

                RaiseUpdateSetting();
            }
            catch { }
        }
    }

    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    file class SerializableSettingAttribute : Attribute { }
}
