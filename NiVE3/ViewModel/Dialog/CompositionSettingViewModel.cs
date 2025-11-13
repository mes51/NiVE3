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
using NiVE3.ToneMapper;
using NiVE3.Util;
using NiVE3.View.Dialog;
using NiVE3.View.Resource;
using NiVE3.SourceGenerator.ReactivePropertyGenerator;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Mvvm;

namespace NiVE3.ViewModel.Dialog
{
    [UseReactiveProperty]
    partial class CompositionSettingViewModel : BindableBase, IDialogAware
    {
        public const string SelectedRendererPluginId = nameof(SelectedRendererPluginId);

        public const string SelectedToneMapperPluginId = nameof(SelectedToneMapperPluginId);

        public const string RendererSettingViewData = nameof(RendererSettingViewData);

        public const string ToneMapperSettingViewData = nameof(ToneMapperSettingViewData);

        [ReactiveProperty]
        public partial string Name { get; set; } = "";

        [ReactiveProperty]
        public partial int Width { get; set; } = 1920;

        [ReactiveProperty]
        public partial int Height { get; set; } = 1080;

        [ReactiveProperty]
        public partial double FrameRate { get; set; } = 30.0;

        [ReactiveProperty]
        public partial Time FrameDuration { get; set; } = new Time(1, 30.0);

        [ReactiveProperty]
        public partial Time Duration { get; set; } = new Time(300, 30.0);

        [ReactiveProperty]
        public partial bool IsRetentionFrameRate { get; set; }

        [ReactiveProperty]
        public partial bool ApplyToneMappingWhenNested { get; set; }

        [ReactiveProperty]
        public partial int ShutterAngle { get; set; } = 180;

        [ReactiveProperty]
        public partial int ShutterPhase { get; set; } = -90;
        
        [ReactiveProperty]
        public partial int MotionBlurSampleCount { get; set; } = 16;

        [ReactiveProperty]
        public partial int SelectedRenderer { get; set; }

        [ReactiveProperty]
        public partial int SelectedToneMapper { get; set; }

        [ReactiveProperty]
        public partial object? RendererSetting { get; set; }

        [ReactiveProperty]
        public partial object? ToneMapperSetting { get; set; }

        [ReactiveProperty]
        public partial ObservableCollection<CompositionPresetData> Presets { get; set; } = [];

        [ReactiveProperty]
        public partial CompositionPresetData? SelectedPreset { get; set; }

        public string[] Renderers { get; }

        public string[] ToneMappers { get; }

        public bool[] RendererHasSettingViews { get; }

        public bool[] ToneMapperHasSettingViews { get; }

        public string Title => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.CompositionSettingView_Title);

        public DialogCloseListener RequestClose { get; }

        public ICommand OKCommand { get; }

        public ICommand CancelCommand { get; }

        public ICommand CreatePresetCommand { get; }

        public ICommand DeletePresetCommand { get; }

        public ICommand OpenRendererSettingCommand { get; }

        public ICommand OpenToneMapperSettingCommand { get; }

        Guid[] RendererTypes { get; }

        Guid[] ToneMapperTypes { get; }

        IDialogService DialogService { get; }

        bool IsChangingPreset { get; set; }

        object? RendererSettingViewDataContext { get; set; }

        object? ToneMapperSettingViewDataContext { get; set; }

        bool RendererSettingChanged { get; set; }

        bool ToneMapperSettingChanged { get; set; }

        public CompositionSettingViewModel(RendererListModel rendererListModel, ToneMapperListModel toneMapperListModel, IDialogService dialogService)
        {
            Renderers = [..rendererListModel.RendererMetadata.Select(r => r.Name)];
            RendererTypes = [..rendererListModel.RendererMetadata.Select(r => Guid.Parse(r.RendererUuid))];
            RendererHasSettingViews = [..rendererListModel.RendererMetadata.Select(r => r.HasSettingView)];
            // NOTE: NoOpToneMapperを先頭に持ってくる
            var toneMapperMetadata = toneMapperListModel.ToneMapperMetadata.OrderBy(m => m.ToneMapperUuid == NoOpToneMapper.ID ? "" : m.ToneMapperUuid).ToArray();
            ToneMappers = [..toneMapperMetadata.Select(t => t.Name)];
            ToneMapperTypes = [..toneMapperMetadata.Select(t => Guid.Parse(t.ToneMapperUuid))];
            ToneMapperHasSettingViews = [..toneMapperMetadata.Select(t => t.HasSettingView)];
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
                if (RendererSettingChanged && RendererSettingViewDataContext != null)
                {
                    result.Add(RendererSettingViewData, RendererSettingViewDataContext);
                }
                if (ToneMapperSettingChanged && ToneMapperSettingViewDataContext != null)
                {
                    result.Add(ToneMapperSettingViewData, ToneMapperSettingViewDataContext);
                }

                RequestClose.Invoke(new DialogResult(ButtonResult.OK) { Parameters = result });
            });

            CancelCommand = new DelegateCommand(() => RequestClose.Invoke(new DialogResult(ButtonResult.Cancel)));

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

            OpenToneMapperSettingCommand = new DelegateCommand(() =>
            {
                using var toneMapper = toneMapperListModel.CreateToneMapper(ToneMapperTypes[SelectedToneMapper]);
                toneMapper.Value.LoadSetting(ToneMapperSetting);

                var view = toneMapper.Value.GetToneMapperSetting();
                if (view == null)
                {
                    return;
                }

                var param = new DialogParameters
                {
                    { PluginSettingViewModel.TitleLanguageResourceName, LanguageResourceDictionary.ToneMapperSettingView_Title },
                    { nameof(PluginSettingViewModel.SettingView), view }
                };
                IDialogResult? result = null;
                DialogService.ShowDialog(nameof(PluginSettingView), param, r => result = r);
                if (result?.Result == ButtonResult.OK && toneMapper.Value.ApplySetting(view.DataContext))
                {
                    ToneMapperSetting = toneMapper.Value.SaveSetting();
                    ToneMapperSettingViewDataContext = view.DataContext;
                    ToneMapperSettingChanged = true;
                }
            }, () => ToneMapperHasSettingViews[SelectedToneMapper]).ObservesProperty(() => SelectedToneMapper);

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
                parameters.TryGetValue<Time>(nameof(Duration), out var duration) &&
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
                if (parameters.TryGetValue(nameof(RendererSetting), out object? rendererSetting))
                {
                    RendererSetting = rendererSetting;
                }
                if (parameters.TryGetValue(nameof(ToneMapperSetting), out object? toneMapperSetting))
                {
                    ToneMapperSetting = toneMapperSetting;
                }
            }
        }

        public void OnDialogClosed() { }

        private void CompositionSettingViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(FrameRate):
                    FrameDuration = new Time(1, FrameRate);
                    break;
                case nameof(FrameDuration) when FrameRate != FrameDuration.FrameRate:
                    if (!FrameDuration.IsFrameTime)
                    {
                        FrameDuration = new Time(1, Math.Round(1.0 / (double)FrameDuration, 2));
                        return;
                    }
                    FrameRate = FrameDuration.FrameRate;
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
                case nameof(SelectedToneMapper):
                    ToneMapperSetting = null;
                    ToneMapperSettingViewDataContext = null;
                    ToneMapperSettingChanged = false;
                    break;
            }

            if (!IsChangingPreset &&
                e.PropertyName != nameof(FrameDuration) &&
                e.PropertyName != nameof(Duration) &&
                e.PropertyName != nameof(SelectedRenderer) &&
                e.PropertyName != nameof(SelectedToneMapper) &&
                e.PropertyName != nameof(RendererSetting) &&
                e.PropertyName != nameof(ToneMapperSetting) &&
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
