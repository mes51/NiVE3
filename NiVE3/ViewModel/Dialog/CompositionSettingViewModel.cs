using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using NiVE3.View.Resource;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;

namespace NiVE3.ViewModel.Dialog
{
    class CompositionSettingViewModel : BindableBase, IDialogAware
    {
        private string name = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.CompositionSettingView_DefaultName);
        public string Name
        {
            get { return name; }
            set { SetProperty(ref name, value); }
        }

        private int width;
        public int Width
        {
            get { return width; }
            set { SetProperty(ref width, value); }
        }

        private int height;
        public int Height
        {
            get { return height; }
            set { SetProperty(ref height, value); }
        }

        private double frameRate;
        public double FrameRate
        {
            get { return frameRate; }
            set { SetProperty(ref frameRate, value); }
        }

        private double duration;
        public double Duration
        {
            get { return duration; }
            set { SetProperty(ref duration, value); }
        }

        private bool isRetentionFrameRate;
        public bool IsRetentionFrameRate
        {
            get { return isRetentionFrameRate; }
            set { SetProperty(ref isRetentionFrameRate, value); }
        }

        private int shutterAngle;
        public int ShutterAngle
        {
            get { return shutterAngle; }
            set { SetProperty(ref shutterAngle, value); }
        }

        private int shutterPhase;
        public int ShutterPhase
        {
            get { return shutterPhase; }
            set { SetProperty(ref shutterPhase, value); }
        }

        private int motionBlurSampleCount;
        public int MotionBlurSampleCount
        {
            get { return motionBlurSampleCount; }
            set { SetProperty(ref motionBlurSampleCount, value); }
        }

        public string Title => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.CompositionSettingView_Title);

        public event Action<IDialogResult>? RequestClose;

        public ICommand OKCommand { get; }

        public ICommand CancelCommand { get; }

        public ICommand CreatePresetCommand { get; }

        public ICommand DeletePresetCommand { get; }

        public CompositionSettingViewModel()
        {
            OKCommand = new DelegateCommand(() => RequestClose?.Invoke(new DialogResult(ButtonResult.OK, null)));

            CancelCommand = new DelegateCommand(() => RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel, null)));

            CreatePresetCommand = new DelegateCommand(() => { });

            DeletePresetCommand = new DelegateCommand(() => { });
        }

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed() { }

        public void OnDialogOpened(IDialogParameters parameters)
        {
        }
    }
}
