using AvalonDock;
using NiVE3.Module;
using NiVE3.Region;
using NiVE3.Util;
using NiVE3.View.Resource;
using NiVE3.ViewModel;
using NiVE3.Windows;
using Prism.DryIoc;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Mvvm;
using Prism.Navigation.Regions;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace NiVE3
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : PrismApplication
    {
        bool HandledUnhandledException { get; set; }

        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void InitializeModules()
        {
            // show splash

            base.InitializeModules();

            // close splash
        }

        protected override IModuleCatalog CreateModuleCatalog()
        {
            var catalog = new ConfigurationModuleCatalog();
            catalog.AddModule(typeof(MainModule));
            return catalog;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            DispatcherUnhandledException += App_DispatcherUnhandledException;

            base.OnStartup(e);
        }

        protected override void ConfigureRegionAdapterMappings(RegionAdapterMappings regionAdapterMappings)
        {
            base.ConfigureRegionAdapterMappings(regionAdapterMappings);

            regionAdapterMappings.RegisterMapping(typeof(DockingManager), Container.Resolve<DockingManagerRegionAdapter>());
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry) { }

        protected override void ConfigureViewModelLocator()
        {
            base.ConfigureViewModelLocator();

            ViewModelLocationProvider.Register<MainWindow, MainWindowViewModel>();
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                HandleUnhandledException(ex);
            }
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            HandleUnhandledException(e.Exception);
            e.Handled = true;
        }

        void HandleUnhandledException(Exception ex)
        {
            if (HandledUnhandledException)
            {
                return;
            }

            ErrorLog.ExportErrorLog(ex);
            new UnhandledExceptionWindow(ex).ShowDialog();

            var viewModel = Container.Resolve<MainWindowViewModel>();
            if (viewModel.IsEdited)
            {
                var title = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.Dialog_SaveChanceWhenThrownUnhandledException_Title);
                var text = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.Dialog_SaveChanceWhenThrownUnhandledException_Text);
                if (MessageBox.Show(text, title, MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    viewModel.EmergencySaveProject();
                }
            }
            viewModel.IsForceClosing = true;

            HandledUnhandledException = true;

            Current.Shutdown();
        }
    }
}
