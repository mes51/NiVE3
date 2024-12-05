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
        public static Tuple<string, string>[] AvailableGpuDevices;

        public static readonly double MaxImageCacheLimit = SystemInfo.MaxImageCacheLimitMiB; // for SlidableNumberTextBox.Maximum

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

            SelectedGpuDevice = AvailableGpuDevices.FirstOrDefault(d => d.Item1 == UseGpuLuid, AvailableGpuDevices[0]);

            IsDirty = false;
        }

        void ApplySetting()
        {
            ApplicationSetting.Setting.ForceUseCpu = ForceUseCpu;
            ApplicationSetting.Setting.SolidFolderName = SolidFolderName;
            ApplicationSetting.Setting.UseGpuLuid = UseGpuLuid;
            ApplicationSetting.Setting.ImageCacheLimit = ImageCacheLimit;

            ApplicationSetting.Setting.RaiseUpdateSetting();
            ApplicationSetting.Setting.Save();

            IsDirty = false;
        }

        private void OptionViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
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
