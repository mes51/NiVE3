using DryIoc;
using Microsoft.Win32;
using NiVE3.Config;
using NiVE3.Model;
using NiVE3.View.Command;
using NiVE3.View.Dialog;
using NiVE3.View.Resource;
using NiVE3.ViewModel.Dialog;
using NiVE3.SourceGenerator.ViewModelWireGenerator;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using NiVE3.Wpf.Interaction.Trigger;
using System.Threading;
using NiVE3.Model.UI;
using System.Collections.ObjectModel;
using NiVE3.UI.Command;

namespace NiVE3.ViewModel
{
    [CommandHandling(nameof(NewProjectCommand), nameof(ShortcutKeySetting.NewProjectGesture))]
    [CommandHandling(nameof(OpenProjectCommand), nameof(ShortcutKeySetting.OpenProjectGesture))]
    [CommandHandling(nameof(SaveProjectCommand), nameof(ShortcutKeySetting.SaveProjectGesture))]
    [CommandHandling(nameof(SaveProjectAsNewNameCommand), nameof(ShortcutKeySetting.SaveProjectAsNewNameGesture))]
    [CommandHandling(nameof(ExitCommand), nameof(ShortcutKeySetting.ExitGesture))]
    [CommandHandling(nameof(NewCompositionCommand), nameof(ShortcutKeySetting.NewCompositionGesture))]
    [ViewModelWireable(nameof(WiringModel), WithInitializeProperty = true)]
    partial class MainWindowViewModel : BindableBase
    {
        public static string RegionName = "MainWindow";

        IContainer Container { get; }

        IRegionManager Region { get; }

        IRegion MainRegion => Region.Regions[RegionName];

        private string projectPath = "";
        [NeedWire(nameof(ProjectModel))]
        public string ProjectPath
        {
            get { return projectPath; }
            set { SetProperty(ref projectPath, value); }
        }

        private string projectName = "";
        [NeedWire(nameof(ProjectModel), IsOneWay = true)]
        public string ProjectName
        {
            get { return projectName; }
            set { SetProperty(ref projectName, value); }
        }

        private bool isEdited;
        [NeedWire(nameof(ProjectModel), IsOneWay = true)]
        public bool IsEdited
        {
            get { return isEdited; }
            set { SetProperty(ref isEdited, value); }
        }

        private bool isRendering;
        [NeedWire(nameof(ProjectModel), IsOneWay = true)]
        public bool IsRendering
        {
            get { return isRendering; }
            set { SetProperty(ref isRendering, value); }
        }

        private Dictionary<string, List<EffectItem>> groupedEffects = [];
        public Dictionary<string, List<EffectItem>> GroupedEffects
        {
            get { return groupedEffects; }
            set { SetProperty(ref groupedEffects, value); }
        }

        public bool IsForceClosing { get; set; }

        public object[] ViewModels => [..MainRegion.Views];

        public object[] SingletonViewModels => MainRegion.Views.OfType<SingletonePaneViewModelBase>().ToArray();

        public CommandOnlyViewModelBase[] CommandOnlyViewModels => Container.ResolveMany<CommandOnlyViewModelBase>().ToArray();

        public InteractionRequest CloseRequest { get; } = new InteractionRequest();

        public ICommand NewProjectCommand { get; }

        public ICommand OpenProjectCommand { get; }

        public ICommand SaveProjectCommand { get; }

        public ICommand SaveProjectAsNewNameCommand { get; }

        public ICommand ExitCommand { get; }

        public ICommand OpenSettingCommand { get; }

        public ICommand OpenShortcutKeySettingCommand { get; }

        public ICommand NewCompositionCommand { get; }

        public ICommand RemoveViewModelCommand { get; }

        public ICommand SaveProjectBeforeCloseCommand { get; }

        public ICommand StopRenderingBeforeCloseCommand { get; }

        public ICommand AddEffectCommand { get; }

        ProjectModel ProjectModel { get; }

        PlayControllerModel PlayControllerModel { get; }

        ViewStateModel ViewState { get; }

        EffectListStateModel EffectListStateModel { get; }

        EventHubModel EventHubModel { get; }

        IDialogService DialogService { get; }

        public MainWindowViewModel(IContainer container, IRegionManager region, ApplicationModel applicationModel, ProjectModel projectModel, PlayControllerModel playControllerModel, ViewStateModel viewState, EffectListStateModel effectListStateModel, EventHubModel eventHubModel, IDialogService dialogService)
        {
            Container = container;
            Region = region;
            ProjectModel = projectModel;
            PlayControllerModel = playControllerModel;
            ViewState = viewState;
            EffectListStateModel = effectListStateModel;
            EventHubModel = eventHubModel;
            DialogService = dialogService;

            applicationModel.RaiseGPUException += ApplicationModel_RaiseGPUException;

            projectModel.OpenCompositionTimeline += ProjectModel_OpenCompositionTimeline;
            projectModel.CompositionRemoved += ProjectModel_CompositionRemoved;
            projectModel.PreviewModels.CollectionChanged += PreviewModels_CollectionChanged;

            eventHubModel.SelectLayerRequest += EventHubModel_SelectLayerRequest;

            foreach (var e in effectListStateModel.Effects)
            {
                if (!GroupedEffects.ContainsKey(e.Category))
                {
                    GroupedEffects.Add(e.Category, []);
                }
                GroupedEffects[e.Category].Add(e);
            }

            NewProjectCommand = new DelegateCommand(() =>
            {
                if (IsEdited)
                {
                    var title = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.Dialog_NotSaveEditedWhenCloseProject_Title);
                    var text = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.Dialog_NotSaveEditedWhenCloseProject_Text);
                    switch (MessageBox.Show(text, title, MessageBoxButton.YesNoCancel, MessageBoxImage.Warning))
                    {
                        case MessageBoxResult.Yes:
                            if (!SaveProject(false))
                            {
                                return;
                            }
                            break;
                        case MessageBoxResult.No:
                            break;
                        default:
                            return;
                    }
                }

                ProjectModel.ClearToNewProject();
            });

            OpenProjectCommand = new DelegateCommand(() =>
            {
                if (IsEdited)
                {
                    var title = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.Dialog_NotSaveEditedWhenClose_Title);
                    var text = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.Dialog_NotSaveEditedWhenClose_Text);
                    switch (MessageBox.Show(text, title, MessageBoxButton.YesNoCancel, MessageBoxImage.Warning))
                    {
                        case MessageBoxResult.Yes:
                            if (!SaveProject(false))
                            {
                                return;
                            }
                            break;
                        case MessageBoxResult.No:
                            break;
                        default:
                            return;
                    }
                }

                var open = new OpenFileDialog
                {
                    Filter = $"{LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.Dialog_OpenSaveProject_Filter_Project)}(*.nvp3)|*.nvp3"
                };
                if (!(open.ShowDialog() ?? false))
                {
                    return;
                }

                ProjectModel.LoadProject(open.FileName);
            });

            SaveProjectCommand = new RequerySuggestedCommand(() => SaveProject(false), () => IsEdited);

            SaveProjectAsNewNameCommand = new DelegateCommand(() => SaveProject(true));

            ExitCommand = new DelegateCommand(() => CloseRequest.Raise());

            OpenSettingCommand = new DelegateCommand(() =>
            {
                DialogService.ShowDialog(nameof(OptionView));
            });

            OpenShortcutKeySettingCommand = new DelegateCommand(() =>
            {
                DialogService.ShowDialog(nameof(ShortcutKeySettingView));
            });

            NewCompositionCommand = new DelegateCommand(() =>
            {
                var format = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.CompositionSettingView_DefaultName);
                var compNumber = 1;
                while (true)
                {
                    var name = string.Format(format, compNumber);
                    if (ProjectModel.CompositionModels.All(c => c.Name != name))
                    {
                        break;
                    }
                    compNumber++;
                }
                var param = new DialogParameters
                {
                    { nameof(CompositionSettingViewModel.Name), string.Format(format, compNumber) }
                };
                IDialogResult? result = null;
                DialogService.ShowDialog(nameof(CompositionSettingView), param, r => result = r);
                if (result != null && result.Result == ButtonResult.OK)
                {
                    ProjectModel.CreateComposition(
                        result.Parameters.GetValue<string>(nameof(CompositionSettingViewModel.Name)),
                        result.Parameters.GetValue<int>(nameof(CompositionSettingViewModel.Width)),
                        result.Parameters.GetValue<int>(nameof(CompositionSettingViewModel.Height)),
                        result.Parameters.GetValue<double>(nameof(CompositionSettingViewModel.FrameRate)),
                        result.Parameters.GetValue<double>(nameof(CompositionSettingViewModel.Duration)),
                        result.Parameters.GetValue<bool>(nameof(CompositionSettingViewModel.IsRetentionFrameRate)),
                        result.Parameters.GetValue<bool>(nameof(CompositionSettingViewModel.ApplyToneMappingWhenNested)),
                        result.Parameters.GetValue<int>(nameof(CompositionSettingViewModel.ShutterAngle)),
                        result.Parameters.GetValue<int>(nameof(CompositionSettingViewModel.ShutterPhase)),
                        result.Parameters.GetValue<int>(nameof(CompositionSettingViewModel.MotionBlurSampleCount)),
                        result.Parameters.GetValue<Guid>(CompositionSettingViewModel.SelectedRendererPluginId),
                        result.Parameters.GetValue<Guid>(CompositionSettingViewModel.SelectedToneMapperPluginId)
                    );
                }
            });

            RemoveViewModelCommand = new DelegateCommand<BindableBase>(MainRegion.Remove);

            SaveProjectBeforeCloseCommand = new DelegateCommand(() =>
            {
                var title = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.Dialog_NotSaveEditedWhenClose_Title);
                var text = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.Dialog_NotSaveEditedWhenClose_Text);
                switch (MessageBox.Show(text, title, MessageBoxButton.YesNoCancel, MessageBoxImage.Warning))
                {
                    case MessageBoxResult.Yes:
                        SaveProject(false);
                        break;
                    case MessageBoxResult.No:
                        IsForceClosing = true;
                        break;
                }
            });

            StopRenderingBeforeCloseCommand = new DelegateCommand(() =>
            {
                var title = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.Dialog_StopRenderingWhenClose_Title);
                var text = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.Dialog_StopRenderingWhenClose_Text);
                if (MessageBox.Show(text, title, MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.OK)
                {
                    ProjectModel.AbortRendering();
                    while (IsRendering)
                    {
                        Thread.Sleep(10);
                        
                        // NOTE: MAGIC https://hilapon.hatenadiary.org/entry/20130225/1361779314
                        Application.Current.Dispatcher.Invoke((Action)(() => { }), System.Windows.Threading.DispatcherPriority.Background);
                    }
                }
                else
                {
                    return;
                }

                if (IsEdited)
                {
                    SaveProjectBeforeCloseCommand.Execute(null);
                }
            });

            AddEffectCommand = new RequerySuggestedCommand<EffectItem>(effectItem =>
            {
                EventHubModel.NotifyAddEffectToSelectedLayers(ViewState.CurrentEditingCompositionId ?? Guid.Empty, null, [effectItem.PluginId]);
            }, _ => ViewState.CurrentEditingCompositionId.HasValue && ViewState.LastSelectedLayerId.HasValue);

            playControllerModel.ChangeFrameRequest += PlayControllerModel_ChangeFrameRequest;

            MainRegion.Views.CollectionChanged += ViewModels_CollectionChanged;

            var timelineViewModel = Container.Resolve<TimelineViewModel>();
            timelineViewModel.CurrentTimeChangeByUser += TimelineViewModel_CurrentTimeChangeByUser;
            timelineViewModel.PaneSelected += TimelineViewModel_PaneSelected;
            MainRegion.Add(timelineViewModel);

            WiringModel();
        }

        partial void WiringModel();

        bool SaveProject(bool asNewName)
        {
            if (string.IsNullOrEmpty(ProjectModel.ProjectPath) || asNewName)
            {
                var save = new SaveFileDialog
                {
                    Filter = $"{LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.Dialog_OpenSaveProject_Filter_Project)}(*.nvp3)|*.nvp3"
                };
                if (!(save.ShowDialog() ?? false))
                {
                    return false;
                }
                ProjectPath = save.FileName;
            }

            ProjectModel.SaveProject();

            return true;
        }

        private void ApplicationModel_RaiseGPUException(object? sender, EventArgs e)
        {
            if (!ProjectModel.IsRendering)
            {
                var title = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.Dialog_RaiseGPUException_Title);
                var text = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.Dialog_RaiseGPUException_Text);
                MessageBox.Show(text, title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ProjectModel_OpenCompositionTimeline(object? sender, CompositionEventArgs e)
        {
            var timelineViewModel = ViewModels.OfType<TimelineViewModel>().FirstOrDefault(vm => vm.CompositionModel == null || vm.CompositionModel == e.Composition);
            if (timelineViewModel == null)
            {
                timelineViewModel = Container.Resolve<TimelineViewModel>();
                timelineViewModel.CurrentTimeChangeByUser += TimelineViewModel_CurrentTimeChangeByUser;
                timelineViewModel.PaneSelected += TimelineViewModel_PaneSelected;
                MainRegion.Add(timelineViewModel);
            }

            timelineViewModel.CompositionModel = e.Composition;
            timelineViewModel.OpenPane();
        }

        private void ProjectModel_CompositionRemoved(object? sender, CompositionEventArgs e)
        {
            var viewModels = ViewModels.OfType<TimelineViewModel>().ToArray();
            var viewModel = ViewModels.OfType<TimelineViewModel>().FirstOrDefault(vm => vm.CompositionModel == e.Composition);
            if (viewModel != null)
            {
                if (viewModels.Length > 1)
                {
                    MainRegion.Remove(viewModel);
                }
                else
                {
                    viewModel.CompositionModel = null;
                }
            }
        }

        private void PreviewModels_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            foreach (var newPreview in e.NewItems?.OfType<PreviewModelBase>() ?? [])
            {
                var viewModel = Container.Resolve<PreviewViewModel>([newPreview]);
                viewModel.PaneSelected += PreviewViewModel_PaneSelected;
                viewModel.SourceChanged += PreviewViewModel_SourceChanged;
                viewModel.WorkareaChanged += PreviewViewModel_WorkareaChanged;
                viewModel.CurrentTimeChangeByUser += PreviewViewModel_CurrentTimeChangeByUser;
                MainRegion.Add(viewModel);
            }
        }

        private void ViewModels_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            RaisePropertyChanged(nameof(ViewModels));
            if ((e.OldItems?.Cast<PaneViewModelBase>()?.Any(v => v is SingletonePaneViewModelBase) ?? false) || (e.NewItems?.Cast<PaneViewModelBase>()?.Any(v => v is SingletonePaneViewModelBase) ?? false))
            {
                RaisePropertyChanged(nameof(SingletonViewModels));
            }
            else if (e.OldItems?.Cast<PaneViewModelBase>() is IEnumerable<PaneViewModelBase> removedPane)
            {
                foreach (var vm in removedPane.OfType<PreviewViewModel>())
                {
                    vm.PaneSelected -= PreviewViewModel_PaneSelected;
                    vm.SourceChanged -= PreviewViewModel_SourceChanged;
                    vm.WorkareaChanged -= PreviewViewModel_WorkareaChanged;
                    vm.CurrentTimeChangeByUser -= PreviewViewModel_CurrentTimeChangeByUser;
                    vm.Unbind();
                    ProjectModel.RemovePreview(vm.PreviewModel);
                }
                if (!ViewModels.OfType<PreviewViewModel>().Any())
                {
                    PlayControllerModel.Duration = 0.0;
                }

                foreach (var vm in removedPane.OfType<TimelineViewModel>())
                {
                    vm.CurrentTimeChangeByUser -= TimelineViewModel_CurrentTimeChangeByUser;
                    vm.PaneSelected -= TimelineViewModel_PaneSelected;
                }
            }
        }

        private void PreviewViewModel_PaneSelected(object? sender, EventArgs e)
        {
            if (sender is PreviewViewModel vm)
            {
                PlayControllerModel.WorkareaBegin = vm.WorkareaBegin;
                PlayControllerModel.WorkareaEnd = vm.WorkareaEnd;
                PlayControllerModel.Duration = vm.Duration;
                PlayControllerModel.FrameRate = vm.FrameRate;
                PlayControllerModel.CurrentTime = vm.CurrentTime;
            }
        }

        private void PreviewViewModel_SourceChanged(object? sender, EventArgs e)
        {
            if (sender is PreviewViewModel vm && vm.IsSelected)
            {
                PlayControllerModel.WorkareaBegin = vm.WorkareaBegin;
                PlayControllerModel.WorkareaEnd = vm.WorkareaEnd;
                PlayControllerModel.Duration = vm.Duration;
                PlayControllerModel.FrameRate = vm.FrameRate;
                PlayControllerModel.CurrentTime = vm.CurrentTime;
            }
        }

        private void PreviewViewModel_WorkareaChanged(object? sender, EventArgs e)
        {
            if (sender is PreviewViewModel vm && vm.IsSelected)
            {
                PlayControllerModel.WorkareaBegin = vm.WorkareaBegin;
                PlayControllerModel.WorkareaEnd = vm.WorkareaEnd;
                PlayControllerModel.Duration = vm.Duration;
            }
        }

        private void PreviewViewModel_CurrentTimeChangeByUser(object? sender, EventArgs e)
        {
            if (sender is PreviewViewModel vm && vm.IsSelected)
            {
                PlayControllerModel.Stop();
                PlayControllerModel.CurrentTime = vm.CurrentTime;
            }
        }

        private void TimelineViewModel_PaneSelected(object? sender, EventArgs e)
        {
            if (sender is TimelineViewModel vm && vm.CompositionModel != null)
            {
                ProjectModel.ShowCompositionPreview(vm.CompositionModel);
            }
        }

        private void TimelineViewModel_CurrentTimeChangeByUser(object? sender, EventArgs e)
        {
            if (sender is not TimelineViewModel vm)
            {
                return;
            }

            var previewModel = ViewModels.OfType<PreviewViewModel>().FirstOrDefault(p => p.IsSelected && p.PreviewModel is CompositionPreviewModel cpm && cpm.Composition == vm.CompositionModel);
            if (previewModel != null)
            {
                PlayControllerModel.Stop();
                previewModel.CurrentTime = vm.CurrentTime;
                PlayControllerModel.CurrentTime = vm.CurrentTime;
            }
        }

        private void PlayControllerModel_ChangeFrameRequest(object? sender, EventArgs e)
        {
            var vm = ViewModels.OfType<PreviewViewModel>().FirstOrDefault(v => v.IsSelected);
            if (vm != null)
            {
                vm.CurrentTime = PlayControllerModel.CurrentTime;
            }
        }

        private void EventHubModel_SelectLayerRequest(object? sender, SelectLayerEvent e)
        {
            var viewModel = ViewModels.OfType<TimelineViewModel>().FirstOrDefault(t => t.CompositionModel != null && t.CompositionId == e.CompositionId);
            if (viewModel != null)
            {
                viewModel.OpenPane();
                viewModel.SelectLayer(e.LayerId, Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl) || Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift));
            }
        }
    }
}
