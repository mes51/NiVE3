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

namespace NiVE3.ViewModel
{
    [CommandHandling(nameof(OpenProjectCommand), nameof(ShortcutKeySetting.OpenProjectGesture))]
    [CommandHandling(nameof(SaveProjectCommand), nameof(ShortcutKeySetting.SaveProjectGesture))]
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

        public object[] ViewModels => MainRegion.Views.ToArray();

        public object[] SingletonViewModels => MainRegion.Views.OfType<SingletonePaneViewModelBase>().ToArray();

        public CommandOnlyViewModelBase[] CommandOnlyViewModels => Container.ResolveMany<CommandOnlyViewModelBase>().ToArray();

        public ICommand OpenProjectCommand { get; }

        public ICommand SaveProjectCommand { get; }

        public ICommand ExitCommand { get; }

        public ICommand NewCompositionCommand { get; }

        public ICommand RemoveViewModelCommand { get; }

        ProjectModel ProjectModel { get; }

        IDialogService DialogService { get; }

        PlayControllerViewModel PlayControllerViewModel { get; }

        public MainWindowViewModel(IContainer container, IRegionManager region, ProjectModel projectModel, IDialogService dialogService, PlayControllerViewModel playControlViewModel)
        {
            Container = container;
            Region = region;
            ProjectModel = projectModel;
            DialogService = dialogService;
            PlayControllerViewModel = playControlViewModel;

            ProjectModel.OpenCompositionTimeline += ProjectModel_OpenCompositionTimeline;
            ProjectModel.CompositionRemoved += ProjectModel_CompositionRemoved;
            ProjectModel.PreviewModels.CollectionChanged += PreviewModels_CollectionChanged;

            OpenProjectCommand = new DelegateCommand(() => System.Diagnostics.Debug.WriteLine("Exec Command: OpenProjectCommand"));

            SaveProjectCommand = new DelegateCommand(() =>
            {
                if (string.IsNullOrEmpty(ProjectModel.ProjectPath))
                {
                    var save = new SaveFileDialog();
                    save.Filter = $"{LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.Dialog_OpenSaveProject_Filter_Project)}(*.nvp3)|*.nvp3";
                    if (!(save.ShowDialog() ?? false))
                    {
                        return;
                    }
                    ProjectPath = save.FileName;
                }

                ProjectModel.SaveProject();
            });

            ExitCommand = new DelegateCommand(() => System.Diagnostics.Debug.WriteLine("Exec Command: ExitCommand"));

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
                        result.Parameters.GetValue<int>(nameof(CompositionSettingViewModel.ShutterAngle)),
                        result.Parameters.GetValue<int>(nameof(CompositionSettingViewModel.ShutterPhase)),
                        result.Parameters.GetValue<int>(nameof(CompositionSettingViewModel.MotionBlurSampleCount)),
                        result.Parameters.GetValue<Type>(CompositionSettingViewModel.SelectedRendererType)
                    );
                }
            });

            RemoveViewModelCommand = new DelegateCommand<BindableBase>(MainRegion.Remove);

            PlayControllerViewModel.ChangeFrameRequest += PlayControllerViewModel_ChangeFrameRequest;

            MainRegion.Views.CollectionChanged += ViewModels_CollectionChanged;

            var timelineViewModel = Container.Resolve<TimelineViewModel>();
            timelineViewModel.CurrentTimeChangeByUser += TimelineViewModel_CurrentTimeChangeByUser;
            MainRegion.Add(timelineViewModel);

            WiringModel();
        }

        partial void WiringModel();

        private void ProjectModel_OpenCompositionTimeline(object? sender, CompositionEventArgs e)
        {
            var timelineViewModel = ViewModels.OfType<TimelineViewModel>().FirstOrDefault(vm => vm.CompositionModel == null || vm.CompositionModel == e.Composition);
            if (timelineViewModel == null)
            {
                timelineViewModel = Container.Resolve<TimelineViewModel>();
                timelineViewModel.CurrentTimeChangeByUser += TimelineViewModel_CurrentTimeChangeByUser;
                MainRegion.Add(timelineViewModel);
            }

            timelineViewModel.CompositionModel = e.Composition;
            timelineViewModel.IsSelected = true;
            timelineViewModel.IsActive = true;
            MainRegion.Activate(timelineViewModel);
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
            foreach (var newPreview in e.NewItems?.OfType<PreviewModelBase>() ?? Enumerable.Empty<PreviewModelBase>())
            {
                var viewModel = Container.Resolve<PreviewViewModel>(new object[] { newPreview });
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
                    ProjectModel.RemovePreview(vm.PreviewModel);
                }
                if (!ViewModels.OfType<PreviewViewModel>().Any())
                {
                    PlayControllerViewModel.CanPreview = false;
                }

                foreach (var vm in removedPane.OfType<TimelineViewModel>())
                {
                    vm.CurrentTimeChangeByUser -= TimelineViewModel_CurrentTimeChangeByUser;
                }
            }
        }

        private void PreviewViewModel_PaneSelected(object? sender, EventArgs e)
        {
            if (sender is PreviewViewModel vm)
            {
                PlayControllerViewModel.CanPreview = vm.SourceType.HasFlag(Plugin.Interfaces.SourceType.Video);
                PlayControllerViewModel.WorkareaBegin = vm.WorkareaBegin;
                PlayControllerViewModel.WorkareaEnd = vm.WorkareaEnd;
                PlayControllerViewModel.Duration = vm.Duration;
                PlayControllerViewModel.FrameRate = vm.FrameRate;
                PlayControllerViewModel.CurrentTime = vm.CurrentTime;
            }
        }

        private void PreviewViewModel_SourceChanged(object? sender, EventArgs e)
        {
            if (sender is PreviewViewModel vm && vm.IsSelected)
            {
                PlayControllerViewModel.CanPreview = vm.SourceType.HasFlag(Plugin.Interfaces.SourceType.Video);
                PlayControllerViewModel.WorkareaBegin = vm.WorkareaBegin;
                PlayControllerViewModel.WorkareaEnd = vm.WorkareaEnd;
                PlayControllerViewModel.Duration = vm.Duration;
                PlayControllerViewModel.FrameRate = vm.FrameRate;
                PlayControllerViewModel.CurrentTime = vm.CurrentTime;
            }
        }

        private void PreviewViewModel_WorkareaChanged(object? sender, EventArgs e)
        {
            if (sender is PreviewViewModel vm && vm.IsSelected)
            {
                PlayControllerViewModel.WorkareaBegin = vm.WorkareaBegin;
                PlayControllerViewModel.WorkareaEnd = vm.WorkareaEnd;
                PlayControllerViewModel.Duration = vm.Duration;
            }
        }

        private void PreviewViewModel_CurrentTimeChangeByUser(object? sender, EventArgs e)
        {
            if (sender is PreviewViewModel vm && vm.IsSelected)
            {
                PlayControllerViewModel.StopCommand.Execute(null);
                PlayControllerViewModel.CurrentTime = vm.CurrentTime;
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
                PlayControllerViewModel.StopCommand.Execute(null);
                previewModel.CurrentTime = vm.CurrentTime;
                PlayControllerViewModel.CurrentTime = vm.CurrentTime;
            }
        }

        private void PlayControllerViewModel_ChangeFrameRequest(object? sender, EventArgs e)
        {
            var vm = ViewModels.OfType<PreviewViewModel>().FirstOrDefault(v => v.IsSelected);
            if (vm != null)
            {
                vm.CurrentTime = PlayControllerViewModel.CurrentTime;
            }
        }
    }
}
