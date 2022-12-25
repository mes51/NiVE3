using DryIoc;
using NiVE3.Config;
using NiVE3.View.Command;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NiVE3.ViewModel
{
    [CommandHandling(nameof(MainWindowViewModel.OpenProjectCommand), nameof(ShortcutKeySetting.OpenProjectGesture))]
    [CommandHandling(nameof(MainWindowViewModel.ExitCommand), nameof(ShortcutKeySetting.ExitGesture))]
    class MainWindowViewModel : BindableBase
    {
        public static string RegionName = "MainWindow";

        IContainer Container { get; }

        IRegionManager Region { get; }

        public object[] ViewModels => Region.Regions[RegionName].Views.ToArray();

        public ICommand OpenProjectCommand { get; }

        public ICommand ExitCommand { get; }

        public MainWindowViewModel(IContainer container, IRegionManager region)
        {
            Container = container;
            Region = region;

            OpenProjectCommand = new DelegateCommand(() => System.Diagnostics.Debug.WriteLine("Exec Command: OpenProjectCommand"));

            ExitCommand = new DelegateCommand(() => System.Diagnostics.Debug.WriteLine("Exec Command: ExitCommand"));
        }
    }
}
