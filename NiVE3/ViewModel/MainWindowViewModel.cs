using DryIoc;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.ViewModel
{
    internal class MainWindowViewModel : BindableBase
    {
        public static string RegionName = "MainWindow";

        IContainer Container { get; }

        IRegionManager Region { get; }

        public MainWindowViewModel(IContainer container, IRegionManager region)
        {
            Container = container;
            Region = region;
        }
    }
}
