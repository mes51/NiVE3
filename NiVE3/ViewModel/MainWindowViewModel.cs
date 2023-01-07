using DryIoc;
using NiVE3.Config;
using NiVE3.Model;
using NiVE3.View.Command;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NiVE3.ViewModel
{
    [CommandHandling(nameof(MainWindowViewModel.OpenProjectCommand), nameof(ShortcutKeySetting.OpenProjectGesture))]
    [CommandHandling(nameof(MainWindowViewModel.ExitCommand), nameof(ShortcutKeySetting.ExitGesture))]
    [CommandHandling(nameof(MainWindowViewModel.NewCompositionCommand), nameof(ShortcutKeySetting.NewCompositionGesture))]
    class MainWindowViewModel : BindableBase
    {
        public static string RegionName = "MainWindow";

        IContainer Container { get; }

        IRegionManager Region { get; }

        IRegion MainRegion => Region.Regions[RegionName];

        public object[] ViewModels => MainRegion.Views.ToArray();

        public ICommand OpenProjectCommand { get; }

        public ICommand ExitCommand { get; }

        public ICommand NewCompositionCommand { get; }

        public ICommand RemoveViewModelCommand { get; }

        ProjectModel ProjectModel { get; }

        public MainWindowViewModel(IContainer container, IRegionManager region, ProjectModel projectModel)
        {
            Container = container;
            Region = region;
            ProjectModel = projectModel;

            ProjectModel.CompositionModels.CollectionChanged += CompositionModels_CollectionChanged;

            OpenProjectCommand = new DelegateCommand(() => System.Diagnostics.Debug.WriteLine("Exec Command: OpenProjectCommand"));

            ExitCommand = new DelegateCommand(() => System.Diagnostics.Debug.WriteLine("Exec Command: ExitCommand"));

            NewCompositionCommand = new DelegateCommand(() => ProjectModel.CreateComposition());

            RemoveViewModelCommand = new DelegateCommand<BindableBase>(vm => MainRegion.Remove(vm));
        }

        private void CompositionModels_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            foreach (var newComposition in e.NewItems?.OfType<CompositionModel>() ?? Enumerable.Empty<CompositionModel>())
            {
                var viewModel = Container.Resolve<TimelineViewModel>(new object[] { newComposition });
                MainRegion.Add(viewModel);
            }
        }
    }
}
