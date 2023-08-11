using DryIoc;
using NiVE3.Config;
using NiVE3.Model;
using NiVE3.View.Command;
using NiVE3.View.Dialog;
using NiVE3.ViewModel.Dialog;
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
    [CommandHandling(nameof(ExitCommand), nameof(ShortcutKeySetting.ExitGesture))]
    [CommandHandling(nameof(NewCompositionCommand), nameof(ShortcutKeySetting.NewCompositionGesture))]
    [CommandHandling(nameof(NewPreviewCommand), nameof(ShortcutKeySetting.NewPreviewGesture))]
    class MainWindowViewModel : BindableBase
    {
        public static string RegionName = "MainWindow";

        IContainer Container { get; }

        IRegionManager Region { get; }

        IRegion MainRegion => Region.Regions[RegionName];

        public object[] ViewModels => MainRegion.Views.ToArray();

        public object[] SingletonViewModels => MainRegion.Views.OfType<SingletonePaneViewModelBase>().ToArray();

        public CommandOnlyViewModelBase[] CommandOnlyViewModels => Container.ResolveMany<CommandOnlyViewModelBase>().ToArray();

        public ICommand OpenProjectCommand { get; }

        public ICommand ExitCommand { get; }

        public ICommand NewCompositionCommand { get; }

        public ICommand NewPreviewCommand { get; }

        public ICommand RemoveViewModelCommand { get; }

        ProjectModel ProjectModel { get; }

        IDialogService DialogService { get; }

        public MainWindowViewModel(IContainer container, IRegionManager region, ProjectModel projectModel, IDialogService dialogService)
        {
            Container = container;
            Region = region;
            ProjectModel = projectModel;
            DialogService = dialogService;

            ProjectModel.CompositionModels.CollectionChanged += CompositionModels_CollectionChanged;

            ProjectModel.PreviewModels.CollectionChanged += PreviewModels_CollectionChanged;

            OpenProjectCommand = new DelegateCommand(() => System.Diagnostics.Debug.WriteLine("Exec Command: OpenProjectCommand"));

            ExitCommand = new DelegateCommand(() => System.Diagnostics.Debug.WriteLine("Exec Command: ExitCommand"));

            NewCompositionCommand = new DelegateCommand(() =>
            {
                var param = new DialogParameters();
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

            NewPreviewCommand = new DelegateCommand(() => ProjectModel.CreatePreview());

            RemoveViewModelCommand = new DelegateCommand<BindableBase>(MainRegion.Remove);

            MainRegion.Views.CollectionChanged += ViewModels_CollectionChanged;
        }

        private void CompositionModels_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            foreach (var newComposition in e.NewItems?.OfType<CompositionModel>() ?? Enumerable.Empty<CompositionModel>())
            {
                var viewModel = Container.Resolve<TimelineViewModel>(new object[] { newComposition });
                MainRegion.Add(viewModel);
            }
        }

        private void PreviewModels_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            foreach (var newPreview in e.NewItems?.OfType<PreviewModelBase>() ?? Enumerable.Empty<PreviewModelBase>())
            {
                var viewModel = Container.Resolve<PreviewViewModel>(new object[] { newPreview });
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
                    ProjectModel.RemovePreview(vm.PreviewModel);
                }
            }
        }
    }
}
