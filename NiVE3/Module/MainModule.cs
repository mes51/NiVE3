using DryIoc;
using NiVE3.Model;
using NiVE3.View.Dialog;
using NiVE3.ViewModel;
using NiVE3.ViewModel.Dialog;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace NiVE3.Module
{
    class MainModule : IModule
    {
        IRegionManager RegionManager { get; }

        IRegionViewRegistry ViewRegistry { get; }

        IContainer Container { get; }

        public MainModule(IContainer container, IRegionViewRegistry registry, IRegionManager manager)
        {
            Container = container;
            ViewRegistry = registry;
            RegionManager = manager;
        }

        public void OnInitialized(IContainerProvider containerProvider)
        {
            Application.Current.MainWindow.DataContext = Container.Resolve<MainWindowViewModel>();
            ViewRegistry.RegisterViewWithRegion(MainWindowViewModel.RegionName, typeof(FootageListViewModel));
            ViewRegistry.RegisterViewWithRegion(MainWindowViewModel.RegionName, typeof(EffectListViewModel));
            ViewRegistry.RegisterViewWithRegion(MainWindowViewModel.RegionName, typeof(PlayControllerViewModel));
            ViewRegistry.RegisterViewWithRegion(MainWindowViewModel.RegionName, typeof(HistoryViewModel));
            ViewRegistry.RegisterViewWithRegion(MainWindowViewModel.RegionName, typeof(LayerPropertyControllerViewModel));
            ViewRegistry.RegisterViewWithRegion(MainWindowViewModel.RegionName, typeof(TextPropertyViewModel));
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            Container.Register<AcceleratorModel>(Reuse.Singleton, FactoryMethod.ConstructorWithResolvableArguments);
            Container.Register<HistoryModel>(Reuse.Singleton, FactoryMethod.ConstructorWithResolvableArguments);
            Container.Register<PlayControllerModel>(Reuse.Singleton, FactoryMethod.ConstructorWithResolvableArguments);
            Container.Register<FootageListModel>(Reuse.Singleton, FactoryMethod.ConstructorWithResolvableArguments);
            Container.Register<EffectListModel>(Reuse.Singleton, FactoryMethod.ConstructorWithResolvableArguments);
            Container.Register<RendererListModel>(Reuse.Singleton, FactoryMethod.ConstructorWithResolvableArguments);
            Container.Register<TextPropertyModel>(Reuse.Singleton, FactoryMethod.ConstructorWithResolvableArguments);
            Container.Register<ProjectModel>(Reuse.Singleton, FactoryMethod.ConstructorWithResolvableArguments);
            Container.Register<ViewStateModel>(Reuse.Singleton, FactoryMethod.ConstructorWithResolvableArguments);
            Container.Register<AudioPlayerModel>(Reuse.Singleton, FactoryMethod.ConstructorWithResolvableArguments);

            Container.Register<MainWindowViewModel>(Reuse.Singleton, FactoryMethod.ConstructorWithResolvableArguments);
            Container.Register<HistoryViewModel>(Reuse.Singleton, FactoryMethod.ConstructorWithResolvableArguments);
            Container.Register<FootageListViewModel>(Reuse.Singleton, FactoryMethod.ConstructorWithResolvableArguments);
            Container.Register<EffectListViewModel>(Reuse.Singleton, FactoryMethod.ConstructorWithResolvableArguments);
            Container.Register<PlayControllerViewModel>(Reuse.Singleton, FactoryMethod.ConstructorWithResolvableArguments);
            Container.Register<LayerPropertyControllerViewModel>(Reuse.Singleton, FactoryMethod.ConstructorWithResolvableArguments);
            Container.Register<TextPropertyViewModel>(Reuse.Singleton, FactoryMethod.ConstructorWithResolvableArguments);

            Container.Register<PreviewViewModel>(Reuse.Transient, FactoryMethod.ConstructorWithResolvableArguments);
            Container.Register<TimelineViewModel>(Reuse.Transient, FactoryMethod.ConstructorWithResolvableArguments);

            // MEMO: ViewなしショートカットのみのViewModelの場合
            // Container.RegisterMapping<CommandOnlyViewModelBase, ViewModelType>();

            containerRegistry.RegisterDialog<InputSettingView, InputSettingViewModel>();
            containerRegistry.RegisterDialog<CompositionSettingView, CompositionSettingViewModel>();
        }
    }
}
