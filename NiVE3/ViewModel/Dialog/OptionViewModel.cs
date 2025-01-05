using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using ComputeSharp;
using NiVE3.Config;
using NiVE3.Extension;
using NiVE3.UI.Command;
using NiVE3.Util;
using NiVE3.View.Resource;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Mvvm;

namespace NiVE3.ViewModel.Dialog
{
    class OptionViewModel : BindableBase, IDialogAware
    {
        // for SlidableNumberTextBox.Maximum

        public const double MinAutoSaveInterval = Const.MinAutoSaveInterval;

        public const double MaxAutoSaveInterval = Const.MaxAutoSaveInterval;

        public const double MinAutoSaveCount = Const.MinAutoSaveCount;

        public const double MaxAutoSaveCount = Const.MaxAutoSaveCount;

        public const double MinImageCacheSize = Const.MinImageCacheSizeMiB;

        public static readonly double MaxImageCacheLimit = SystemInfo.MaxCacheLimitMiB - Const.MinImageCacheSizeMiB;

        public static Tuple<string, string>[] AvailableGpuDevices;

        static HashSet<string> SettingPropertyNames { get; } = [];

        public string Title => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.OptionView_Title);

        public DialogCloseListener RequestClose { get; }

        public ICommand ApplyCommand { get; }

        public ICommand OKCommand { get; }

        public ICommand CancelCommand { get; }

        private string solidFolderName = "";
        [SettingProperty]
        public string SolidFolderName
        {
            get { return solidFolderName; }
            set { SetProperty(ref solidFolderName, value); }
        }

        private bool forceUseCpu;
        [SettingProperty]
        public bool ForceUseCpu
        {
            get { return forceUseCpu; }
            set { SetProperty(ref forceUseCpu, value); }
        }

        private string useGpuLuid = "";
        [SettingProperty]
        public string UseGpuLuid
        {
            get { return useGpuLuid; }
            set { SetProperty(ref useGpuLuid, value); }
        }

        private int imageCacheLimit;
        [SettingProperty]
        public int ImageCacheLimit
        {
            get { return imageCacheLimit; }
            set { SetProperty(ref imageCacheLimit, value); }
        }

        private int ramPreviewCacheLimit;
        [SettingProperty]
        public int RamPreviewCacheLimit
        {
            get { return ramPreviewCacheLimit; }
            set { SetProperty(ref ramPreviewCacheLimit, value); }
        }

        private bool isCompressCache;
        [SettingProperty]
        public bool IsCompressCache
        {
            get { return isCompressCache; }
            set { SetProperty(ref isCompressCache, value); }
        }

        private bool useGpuCache;
        [SettingProperty]
        public bool UseGpuCache
        {
            get { return useGpuCache; }
            set { SetProperty(ref useGpuCache, value); }
        }

        private double gpuCacheLimitRate;
        [SettingProperty]
        public double GpuCacheLimitRate
        {
            get { return gpuCacheLimitRate; }
            set { SetProperty(ref gpuCacheLimitRate, value); }
        }

        private bool useAutoSave;
        [SettingProperty]
        public bool UseAutoSave
        {
            get { return useAutoSave; }
            set { SetProperty(ref useAutoSave, value); }
        }

        private int autoSaveInterval;
        [SettingProperty]
        public int AutoSaveInterval
        {
            get { return autoSaveInterval; }
            set { SetProperty(ref autoSaveInterval, value); }
        }

        private int autoSaveCount;
        [SettingProperty]
        public int AutoSaveCount
        {
            get { return autoSaveCount; }
            set { SetProperty(ref autoSaveCount, value); }
        }

        private double maxRamPreviewCacheLimit;
        public double MaxRamPreviewCacheLimit
        {
            get { return maxRamPreviewCacheLimit; }
            set { SetProperty(ref maxRamPreviewCacheLimit, value); }
        }

        private long gpuCahceLimit;
        public long GpuCacheLimit
        {
            get { return gpuCahceLimit; }
            set { SetProperty(ref gpuCahceLimit, value); }
        }

        private Tuple<string, string> selectedGpuDevice = Tuple.Create("", "");
        public Tuple<string, string> SelectedGpuDevice
        {
            get { return selectedGpuDevice; }
            set { SetProperty(ref selectedGpuDevice, value); }
        }

        private bool isDirty;
        public bool IsDirty
        {
            get { return isDirty; }
            set { SetProperty(ref isDirty, value); }
        }

        static OptionViewModel()
        {
            var defaultDevice = GraphicsDevice.GetDefault();
            var devices = new List<Tuple<string, string>>
            {
                Tuple.Create("", LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.OptionView_Performance_UseGpuDevice_Default))
            };
            foreach (var gd in GraphicsDevice.EnumerateDevices())
            {
                var luid = gd.Luid.ToString();
                if (gd.IsHardwareAccelerated)
                {
                    devices.Add(Tuple.Create(luid, gd.Name));
                }
                if (gd != defaultDevice && luid != ApplicationSetting.Setting.UseGpuLuid)
                {
                    gd.Dispose();
                }
            }
            AvailableGpuDevices = [..devices];

            foreach (var p in typeof(OptionViewModel).GetProperties().Where(p => p.IsApplied<SettingPropertyAttribute>()))
            {
                SettingPropertyNames.Add(p.Name);
            }
        }

        public OptionViewModel()
        {
            ApplyCommand = new RequerySuggestedCommand(() => ApplySetting(), () => IsDirty);

            OKCommand = new DelegateCommand(() =>
            {
                ApplySetting();

                RequestClose.Invoke(new DialogResult(ButtonResult.OK));
            });

            CancelCommand = new DelegateCommand(() => RequestClose.Invoke(new DialogResult(ButtonResult.Cancel)));

            PropertyChanged += OptionViewModel_PropertyChanged;
        }

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed() { }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            SolidFolderName = ApplicationSetting.Setting.SolidFolderName;
            ForceUseCpu = ApplicationSetting.Setting.ForceUseCpu;
            UseGpuLuid = ApplicationSetting.Setting.UseGpuLuid;
            ImageCacheLimit = ApplicationSetting.Setting.ImageCacheLimit;
            RamPreviewCacheLimit = ApplicationSetting.Setting.RamPreviewCacheLimit;
            IsCompressCache = ApplicationSetting.Setting.IsCompressCache;
            UseGpuCache = ApplicationSetting.Setting.UseGpuCache;
            GpuCacheLimitRate = ApplicationSetting.Setting.GpuCacheLimitRate;
            UseAutoSave = ApplicationSetting.Setting.UseAutoSave;
            AutoSaveInterval = ApplicationSetting.Setting.AutoSaveInterval;
            AutoSaveCount = ApplicationSetting.Setting.AutoSaveCount;

            SelectedGpuDevice = AvailableGpuDevices.FirstOrDefault(d => d.Item1 == UseGpuLuid, AvailableGpuDevices[0]);

            IsDirty = false;
        }

        void ApplySetting()
        {
            ApplicationSetting.Setting.ForceUseCpu = ForceUseCpu;
            ApplicationSetting.Setting.SolidFolderName = SolidFolderName;
            ApplicationSetting.Setting.UseGpuLuid = UseGpuLuid;
            ApplicationSetting.Setting.ImageCacheLimit = ImageCacheLimit;
            ApplicationSetting.Setting.RamPreviewCacheLimit = RamPreviewCacheLimit;
            ApplicationSetting.Setting.IsCompressCache = IsCompressCache;
            ApplicationSetting.Setting.UseGpuCache = UseGpuCache;
            ApplicationSetting.Setting.GpuCacheLimitRate = GpuCacheLimitRate;
            ApplicationSetting.Setting.UseAutoSave = UseAutoSave;
            ApplicationSetting.Setting.AutoSaveInterval = AutoSaveInterval;
            ApplicationSetting.Setting.AutoSaveCount = AutoSaveCount;

            ApplicationSetting.Setting.RaiseUpdateSetting();
            ApplicationSetting.Setting.Save();

            IsDirty = false;
        }

        private void OptionViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ImageCacheLimit))
            {
                MaxRamPreviewCacheLimit = SystemInfo.MaxCacheLimitMiB - ImageCacheLimit;
            }

            if (SettingPropertyNames.Contains(e.PropertyName ?? ""))
            {
                IsDirty = true;
            }
            else
            {
                switch (e.PropertyName)
                {
                    case nameof(SelectedGpuDevice):
                        UseGpuLuid = SelectedGpuDevice.Item1;
                        break;
                }
            }
        }
    }

    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    file sealed class SettingPropertyAttribute : Attribute
    {
        public SettingPropertyAttribute() { }
    }
}
