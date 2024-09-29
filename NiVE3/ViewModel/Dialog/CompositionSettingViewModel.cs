using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using NiVE3.Data.Config;
using NiVE3.Model;
using NiVE3.Plugin.ValueObject;
using NiVE3.Util;
using NiVE3.View.Dialog;
using NiVE3.View.Resource;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;

namespace NiVE3.ViewModel.Dialog
{
    class CompositionSettingViewModel : BindableBase, IDialogAware
    {
        public const string SelectedRendererPluginId = nameof(SelectedRendererPluginId);

        public const string SelectedToneMapperPluginId = nameof(SelectedToneMapperPluginId);

        public const string RendererSettingViewData = nameof(RendererSettingViewData);

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

        private int shutterPhase = -90;
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

        private object? rendererSetting;
        public object? RendererSetting
        {
            get { return rendererSetting; }
            set { SetProperty(ref rendererSetting, value); }
        }

        private ObservableCollection<CompositionPresetData> presets = [];
        public ObservableCollection<CompositionPresetData> Presets
        {
            get { return presets; }
            set { SetProperty(ref presets, value); }
        }

        private CompositionPresetData? selectedPreset;
        public CompositionPresetData? SelectedPreset
        {
            get { return selectedPreset; }
            set { SetProperty(ref selectedPreset, value); }
        }

        public string[] Renderers { get; }

        public string[] ToneMappers { get; }

        public bool[] RendererHasSettingViews { get; }

        public string Title => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.CompositionSettingView_Title);

        public event Action<IDialogResult>? RequestClose;

        public ICommand OKCommand { get; }

        public ICommand CancelCommand { get; }

        public ICommand CreatePresetCommand { get; }

        public ICommand DeletePresetCommand { get; }

        public ICommand OpenRendererSettingCommand { get; }

        Guid[] RendererTypes { get; }

        Guid[] ToneMapperTypes { get; }

        IDialogService DialogService { get; }

        bool IsChangingPreset { get; set; }

        object? RendererSettingViewDataContext { get; set; }

        bool RendererSettingChanged { get; set; }

        public CompositionSettingViewModel(RendererListModel rendererListModel, ToneMapperListModel toneMapperListModel, IDialogService dialogService)
        {
            Renderers = [..rendererListModel.RendererMetadata.Select(r => r.Name)];
            RendererTypes = [..rendererListModel.RendererMetadata.Select(r => Guid.Parse(r.RendererUuid))];
            RendererHasSettingViews = [..rendererListModel.RendererMetadata.Select(r => r.HasSettingView)];
            ToneMappers = [..toneMapperListModel.ToneMapperMetadata.Select(t => t.Name)];
            ToneMapperTypes = [..toneMapperListModel.ToneMapperMetadata.Select(t => Guid.Parse(t.ToneMapperUuid))];
            DialogService = dialogService;

            if (File.Exists(Paths.CompositionPresetFilePath))
            {
                try
                {
                    var loadedPresets = JsonSerializer.Deserialize<CompositionPresetData[]>(File.ReadAllText(Paths.CompositionPresetFilePath));
                    if (loadedPresets != null)
                    {
                        foreach (var preset in loadedPresets)
                        {
                            Presets.Add(preset);
                        }
                    }
                    else
                    {
                        Presets.Add(CompositionPresetData.DefaultCompositionSetting);
                    }
                }
                catch
                {
                    Presets.Add(CompositionPresetData.DefaultCompositionSetting);
                }
            }
            else
            {
                Presets.Add(CompositionPresetData.DefaultCompositionSetting);
            }

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
                    { SelectedRendererPluginId, RendererTypes[SelectedRenderer] },
                    { SelectedToneMapperPluginId, ToneMapperTypes[SelectedToneMapper] },
                };
                if (RendererSettingChanged)
                {
                    result.Add(RendererSettingViewData, RendererSettingViewDataContext);
                }

                RequestClose?.Invoke(new DialogResult(ButtonResult.OK, result));
            });

            CancelCommand = new DelegateCommand(() => RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel, null)));

            CreatePresetCommand = new DelegateCommand(() =>
            {
                var param = new DialogParameters
                {
                    { nameof(NameSettingViewModel.Title), LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.Dialog_CompositionPresetName_Title) },
                    { nameof(NameSettingViewModel.Label), LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.Dialog_CompositionPresetName_Label) },
                    { nameof(NameSettingViewModel.CanOverwrite), true },
                    { nameof(NameSettingViewModel.RegisteredNames), Presets.Select(p => p.Name).ToArray() }
                };
                IDialogResult? result = null;
                DialogService.ShowDialog(nameof(NameSettingView), param, r => result = r);
                if (result != null && result.Result == ButtonResult.OK)
                {
                    var newPreset = new CompositionPresetData
                    {
                        Name = result.Parameters.GetValue<string>(nameof(NameSettingViewModel.Name)),
                        Width = Width,
                        Height = Height,
                        FrameRate = FrameRate,
                        IsRetentionFrameRate = IsRetentionFrameRate,
                        ApplyToneMappingWhenNested = ApplyToneMappingWhenNested,
                        ShutterAngle = ShutterAngle,
                        ShutterPhase = ShutterPhase,
                        MotionBlurSampleCount = MotionBlurSampleCount
                    };
                    Presets.Add(newPreset);

                    try
                    {
                        var json = JsonSerializer.Serialize(Presets.ToArray());
                        File.WriteAllText(Paths.CompositionPresetFilePath, json);
                        SelectedPreset = newPreset;
                    }
                    catch
                    {
                        // TODO: 保存できなかったダイアログ表示
                        Presets.Remove(newPreset);
                    }
                }
            }, () => SelectedPreset == null).ObservesProperty(() => SelectedPreset);

            DeletePresetCommand = new DelegateCommand(() =>
            {
                if (SelectedPreset == null)
                {
                    return;
                }

                var title = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.Dialog_ConfirmDeleteCompositionPreset_Title);
                var text = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.Dialog_ConfirmDeleteCompositionPreset_Text);
                if (MessageBox.Show(text, title, MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.OK)
                {
                    Presets.Remove(SelectedPreset);
                    SelectedPreset = null;
                }
            }, () => SelectedPreset != null).ObservesProperty(() => SelectedPreset);

            OpenRendererSettingCommand = new DelegateCommand(() =>
            {
                using var renderer = rendererListModel.CreateRenderer(RendererTypes[SelectedRenderer]);
                renderer.Value.LoadSetting(RendererSetting);

                var view = renderer.Value.GetRendererSetting(new Int32Size(Width, Height));
                if (view == null)
                {
                    return;
                }

                var param = new DialogParameters
                {
                    { PluginSettingViewModel.TitleLanguageResourceName, LanguageResourceDictionary.RendererSettingView_Title },
                    { nameof(PluginSettingViewModel.SettingView), view }
                };
                IDialogResult? result = null;
                DialogService.ShowDialog(nameof(PluginSettingView), param, r => result = r);
                if (result?.Result == ButtonResult.OK && renderer.Value.ApplySetting(view.DataContext))
                {
                    RendererSetting = renderer.Value.SaveSetting();
                    RendererSettingViewDataContext = view.DataContext;
                    RendererSettingChanged = true;
                }
            }, () => RendererHasSettingViews[SelectedRenderer]).ObservesProperty(() => SelectedRenderer);

            PropertyChanged += CompositionSettingViewModel_PropertyChanged;
        }

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            Name = parameters.GetValue<string>(nameof(Name));
            if (parameters.TryGetValue<int>(nameof(Width), out var width) &&
                parameters.TryGetValue<int>(nameof(Height), out var height) &&
                parameters.TryGetValue<double>(nameof(FrameRate), out var frameRate) &&
                parameters.TryGetValue<double>(nameof(Duration), out var duration) &&
                parameters.TryGetValue<bool>(nameof(IsRetentionFrameRate), out var isRetentionFrameRate) &&
                parameters.TryGetValue<bool>(nameof(ApplyToneMappingWhenNested), out var applyToneMappingWhenNested) &&
                parameters.TryGetValue<int>(nameof(ShutterAngle), out var shutterAngle) &&
                parameters.TryGetValue<int>(nameof(ShutterPhase), out var shutterPhase) &&
                parameters.TryGetValue<int>(nameof(MotionBlurSampleCount), out var motionBlurSampleCount) &&
                parameters.TryGetValue<Guid>(SelectedRendererPluginId, out var selectedRendererType) &&
                parameters.TryGetValue<Guid>(SelectedToneMapperPluginId, out var selectedToneMapperType)
            )
            {
                Width = width;
                Height = height;
                FrameRate = frameRate;
                Duration = duration;
                IsRetentionFrameRate = isRetentionFrameRate;
                ApplyToneMappingWhenNested = applyToneMappingWhenNested;
                ShutterAngle = shutterAngle;
                ShutterPhase = shutterPhase;
                MotionBlurSampleCount = motionBlurSampleCount;
                SelectedRenderer = Math.Max(Array.IndexOf(RendererTypes, selectedRendererType), 0);
                SelectedToneMapper = Math.Max(Array.IndexOf(ToneMapperTypes, selectedToneMapperType), 0);
                RendererSetting = parameters.GetValue<object?>(nameof(RendererSetting));
            }
        }

        public void OnDialogClosed() { }

        private void CompositionSettingViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(FrameRate):
                    FrameDuration = 1.0 / FrameRate;
                    break;
                case nameof(FrameDuration) when FrameRate != Math.Round(1.0 / FrameDuration, FrameTimeDigit):
                    FrameRate = Math.Round(1.0 / FrameDuration, FrameTimeDigit);
                    break;
                case nameof(SelectedPreset) when SelectedPreset != null:
                    IsChangingPreset = true;
                    Width = SelectedPreset.Width;
                    Height = SelectedPreset.Height;
                    FrameRate = SelectedPreset.FrameRate;
                    IsRetentionFrameRate = SelectedPreset.IsRetentionFrameRate;
                    ApplyToneMappingWhenNested = SelectedPreset.ApplyToneMappingWhenNested;
                    ShutterAngle = SelectedPreset.ShutterAngle;
                    ShutterPhase = SelectedPreset.ShutterPhase;
                    MotionBlurSampleCount = SelectedPreset.MotionBlurSampleCount;
                    IsChangingPreset = false;
                    break;
                case nameof(SelectedRenderer):
                    RendererSetting = null;
                    RendererSettingViewDataContext = null;
                    RendererSettingChanged = false;
                    break;
            }

            if (!IsChangingPreset &&
                e.PropertyName != nameof(FrameDuration) &&
                e.PropertyName != nameof(Duration) &&
                e.PropertyName != nameof(SelectedRenderer) &&
                e.PropertyName != nameof(SelectedToneMapper) &&
                e.PropertyName != nameof(RendererSetting) &&
                e.PropertyName != nameof(SelectedPreset))
            {
                var current = new CompositionPresetData
                {
                    Width = Width,
                    Height = Height,
                    FrameRate = FrameRate,
                    IsRetentionFrameRate = IsRetentionFrameRate,
                    ApplyToneMappingWhenNested = ApplyToneMappingWhenNested,
                    ShutterAngle = ShutterAngle,
                    ShutterPhase = ShutterPhase,
                    MotionBlurSampleCount = MotionBlurSampleCount
                };
                SelectedPreset = Presets.FirstOrDefault(current.IsSame);
            }
        }
    }
}
