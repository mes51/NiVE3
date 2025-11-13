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
using NiVE3.SourceGenerator.ReactivePropertyGenerator;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Mvvm;

namespace NiVE3.ViewModel.Dialog
{
    [UseReactiveProperty]
    partial class OptionViewModel : BindableBase, IDialogAware
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

        [ReactiveProperty]
        [SettingProperty]
        public partial string SolidFolderName { get; set; } = "";

        [ReactiveProperty]
        [SettingProperty]
        public partial bool ForceUseCpu { get; set; }

        [ReactiveProperty]
        [SettingProperty]
        public partial string UseGpuLuid { get; set; } = "";

        [ReactiveProperty]
        [SettingProperty]
        public partial int ImageCacheLimit { get; set; }

        [ReactiveProperty]
        [SettingProperty]
        public partial int RamPreviewCacheLimit { get; set; }

        [ReactiveProperty]
        [SettingProperty]
        public partial bool IsCompressCache { get; set; }

        [ReactiveProperty]
        [SettingProperty]
        public partial bool UseGpuCache { get; set; }

        [ReactiveProperty]
        [SettingProperty]
        public partial double GpuCacheLimitRate { get; set; }

        [ReactiveProperty]
        [SettingProperty]
        public partial bool UseAutoSave { get; set; }

        [ReactiveProperty]
        [SettingProperty]
        public partial int AutoSaveInterval { get; set; }

        [ReactiveProperty]
        [SettingProperty]
        public partial int AutoSaveCount { get; set; }

        [ReactiveProperty]
        [SettingProperty]
        public partial int DisplayFrameRangePropertyInPreview { get; set; }

        [ReactiveProperty]
        public partial double MaxRamPreviewCacheLimit { get; set; }

        [ReactiveProperty]
        public partial long GpuCacheLimit { get; set; }

        [ReactiveProperty]
        public partial Tuple<string, string> SelectedGpuDevice { get; set; } = Tuple.Create("", "");

        [ReactiveProperty]
        public partial bool IsDirty { get; set; }

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
            DisplayFrameRangePropertyInPreview = ApplicationSetting.Setting.DisplayFrameRangePropertyInPreview;

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
            ApplicationSetting.Setting.DisplayFrameRangePropertyInPreview = DisplayFrameRangePropertyInPreview;

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
