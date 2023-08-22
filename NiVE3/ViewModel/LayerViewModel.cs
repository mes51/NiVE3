using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Model;
using NiVE3.SourceGenerator.ViewModelWireGenerator;
using Prism.Mvvm;

namespace NiVE3.ViewModel
{
    [ViewModelWireable(nameof(WiringModel), WithInitializeProperty = true)]
    partial class LayerViewModel : BindableBase
    {
        private string name = "";
        [NeedWire(nameof(LayerModel))]
        public string Name
        {
            get { return name; }
            set { SetProperty(ref name, value); }
        }

        LayerModel LayerModel { get; }

        public LayerViewModel(LayerModel layerModel)
        {
            LayerModel = layerModel;

            WiringModel();
        }


        partial void WiringModel();
    }
}
