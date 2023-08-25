using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using GongSolutions.Wpf.DragDrop;
using ImTools;
using NiVE3.View.Converter;
using NiVE3.View.Primitive;
using NiVE3.ViewModel;

namespace NiVE3.View.Part
{
    /// <summary>
    /// LayerCollection.xaml の相互作用ロジック
    /// </summary>
    internal partial class LayerCollection : NestableItemsCollection<LayerViewModel>
    {
        public LayerCollection()
        {
            InitializeComponent();
        }

        protected override DependencyObject GetContainerForItemOverride()
        {
            return new LayerView();
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is LayerView;
        }

        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);

            if (element is LayerView layer && item is LayerViewModel viewModel)
            {
                layer.DataContext = viewModel;

                var widthBinding = new Binding
                {
                    Path = new PropertyPath(nameof(ControlAreaWidth)),
                    Source = this,
                    Mode = BindingMode.OneWay
                };
                BindingOperations.SetBinding(layer, LayerView.LayerControlAreaWidthProperty, widthBinding);

                var numberBinding = new Binding
                {
                    Path = new PropertyPath(AlternationIndexProperty),
                    Source = layer,
                    Mode = BindingMode.OneWay,
                    Converter = new DelegateConverter<int, int>(v => v + 1)
                };
                BindingOperations.SetBinding(layer, LayerView.LayerNumberProperty, numberBinding);
            }
        }
    }
}
