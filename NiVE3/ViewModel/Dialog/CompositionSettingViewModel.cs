using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using NiVE3.Model;
using NiVE3.View.Resource;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;

namespace NiVE3.ViewModel.Dialog
{
    class CompositionSettingViewModel : BindableBase, IDialogAware
    {
        public const string SelectedRendererType = nameof(SelectedRendererType);

        public const string SelectedToneMapperType = nameof(SelectedToneMapperType);

        // TODO: 要調整
        const int FrameTimeDigit = 7;

        private string name = "";
        public string Name
        {
            get { return name; }
            set { SetProperty(ref name, value); }
        }

        private int width = 1920;
        public int Width
        {
            get { return width; }
            set { SetProperty(ref width, value); }
        }

        private int height = 1080;
        public int Height
        {
            get { return height; }
            set { SetProperty(ref height, value); }
        }

        private double frameRate = 30.0;
        public double FrameRate
        {
            get { return frameRate; }
            set { SetProperty(ref frameRate, value); }
        }

        private double frameDuration = 1.0 / 30.0;
        public double FrameDuration
        {
            get { return frameDuration; }
            set { SetProperty(ref frameDuration, value); }
        }

        private double duration = 10.0;
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

        private bool applyToneMappingWhenNested;
        public bool ApplyToneMappingWhenNested
        {
            get { return applyToneMappingWhenNested; }
            set { SetProperty(ref applyToneMappingWhenNested, value); }
        }

        private int shutterAngle = 180;
        public int ShutterAngle
        {
            get { return shutterAngle; }
            set { SetProperty(ref shutterAngle, value); }
        }

        private int shutterPhase = 180;
        public int ShutterPhase
        {
            get { return shutterPhase; }
            set { SetProperty(ref shutterPhase, value); }
        }

        private int motionBlurSampleCount = 16;
        public int MotionBlurSampleCount
        {
            get { return motionBlurSampleCount; }
            set { SetProperty(ref motionBlurSampleCount, value); }
        }

        private int selectedRenderer;
        public int SelectedRenderer
        {
            get { return selectedRenderer; }
            set { SetProperty(ref selectedRenderer, value); }
        }

        private int selectedToneMapper;
        public int SelectedToneMapper
        {
            get { return selectedToneMapper; }
            set { SetProperty(ref selectedToneMapper, value); }
        }

        public string[] Renderers { get; }

        public string[] ToneMappers { get; }

        public string Title => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.CompositionSettingView_Title);

        public event Action<IDialogResult>? RequestClose;

        public ICommand OKCommand { get; }

        public ICommand CancelCommand { get; }

        public ICommand CreatePresetCommand { get; }

        public ICommand DeletePresetCommand { get; }

        Type[] RendererTypes { get; }

        Type[] ToneMapperTypes { get; }

        public CompositionSettingViewModel(RendererListModel rendererListModel, ToneMapperListModel toneMapperListModel)
        {
            Renderers = [..rendererListModel.RendererMetadata.Select(r => r.Name)];
            RendererTypes = [..rendererListModel.RendererMetadata.Select(r => r.PluginType)];
            ToneMappers = [..toneMapperListModel.ToneMapperMetadata.Select(t => t.Name)];
            ToneMapperTypes = [..toneMapperListModel.ToneMapperMetadata.Select(t => t.PluginType)];

            OKCommand = new DelegateCommand(() =>
            {
                var result = new DialogParameters
                {
                    { nameof(Name), Name },
                    { nameof(Width), Width },
                    { nameof(Height), Height },
                    { nameof(FrameRate), FrameRate },
                    { nameof(Duration), Duration },
                    { nameof(IsRetentionFrameRate), IsRetentionFrameRate },
                    { nameof(ApplyToneMappingWhenNested), ApplyToneMappingWhenNested },
                    { nameof(ShutterAngle), ShutterAngle },
                    { nameof(ShutterPhase), ShutterPhase },
                    { nameof(MotionBlurSampleCount), MotionBlurSampleCount },
                    { SelectedRendererType, RendererTypes[SelectedRenderer] },
                    { SelectedToneMapperType, ToneMapperTypes[SelectedToneMapper] }
                };

                RequestClose?.Invoke(new DialogResult(ButtonResult.OK, result));
            });

            CancelCommand = new DelegateCommand(() => RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel, null)));

            CreatePresetCommand = new DelegateCommand(() => { });

            DeletePresetCommand = new DelegateCommand(() => { });

            PropertyChanged += CompositionSettingViewModel_PropertyChanged;
        }

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            Name = parameters.GetValue<string>(nameof(Name));
        }

        public void OnDialogClosed() { }

        private void CompositionSettingViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FrameRate))
            {
                FrameDuration = 1.0 / FrameRate;
            }
            else if (e.PropertyName == nameof(FrameDuration) && FrameRate != Math.Round(1.0 / FrameDuration, FrameTimeDigit))
            {
                FrameRate = Math.Round(1.0 / FrameDuration, FrameTimeDigit);
            }
        }
    }
}
