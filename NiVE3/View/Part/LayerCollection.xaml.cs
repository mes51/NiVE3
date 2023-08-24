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
using NiVE3.ViewModel;

namespace NiVE3.View.Part
{
    /// <summary>
    /// LayerCollection.xaml の相互作用ロジック
    /// </summary>
    public partial class LayerCollection : ItemsControl, IDragSource
    {
        public static readonly DependencyProperty LayerControlAreaWidthProperty = DependencyProperty.Register(
            nameof(LayerControlAreaWidth),
            typeof(double),
            typeof(LayerCollection),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        internal static readonly DependencyProperty SelectedLayersProperty = DependencyProperty.Register(
            nameof(SelectedLayers),
            typeof(ObservableCollection<LayerViewModel>),
            typeof(LayerCollection),
            new FrameworkPropertyMetadata(new ObservableCollection<LayerViewModel>(), FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public double LayerControlAreaWidth
        {
            get { return (double)GetValue(LayerControlAreaWidthProperty); }
            set { SetValue(LayerControlAreaWidthProperty, value); }
        }

        internal ObservableCollection<LayerViewModel> SelectedLayers
        {
            get { return (ObservableCollection<LayerViewModel>)GetValue(SelectedLayersProperty); }
            set { SetValue(SelectedLayersProperty, value); }
        }

        LayerViewModel? LastSelected { get; set; }

        public LayerCollection()
        {
            InitializeComponent();
        }

        public void StartDrag(IDragInfo dragInfo)
        {
            if (dragInfo.VisualSourceItem is LayerView layer && layer.DataContext is LayerViewModel viewModel)
            {
                if (SelectedLayers.Count > 1 && SelectedLayers.Contains(viewModel))
                {
                    dragInfo.Data = new LayerDragData(SelectedLayers.ToArray(), viewModel);
                }
                else
                {
                    dragInfo.Data = viewModel;
                }
            }

            if (dragInfo.Data != null)
            {
                dragInfo.Effects = DragDropEffects.Move;
            }
            else
            {
                dragInfo.Effects = DragDropEffects.None;
            }
        }

        public bool CanStartDrag(IDragInfo dragInfo)
        {
            return true;
        }

        public void Dropped(IDropInfo dropInfo) { }

        public void DragDropOperationFinished(DragDropEffects operationResult, IDragInfo dragInfo) { }

        public void DragCancelled() { }

        public bool TryCatchOccurredException(Exception exception)
        {
            return false;
        }

        internal void SelectLayer(LayerView layer, bool selectRange, bool selectMultiple)
        {
            var viewModel = layer.DataContext as LayerViewModel;
            if (viewModel == null)
            {
                return;
            }
            var layers = ItemsSource?.OfType<LayerViewModel>()?.ToArray();
            if (layers == null || layers.Length < 1 || !layers.Contains(viewModel))
            {
                return;
            }

            if (selectMultiple)
            {
                if (SelectedLayers.Contains(viewModel))
                {
                    SetSelected(viewModel, false);
                    SelectedLayers.Remove(viewModel);
                }
                else
                {
                    SetSelected(viewModel, true);
                    SelectedLayers.Add(viewModel);
                }
                LastSelected = viewModel;
            }
            else if (selectRange && SelectedLayers.Count > 0)
            {
                var oldSelectedLayers = SelectedLayers.ToArray();
                if (LastSelected == null)
                {
                    LastSelected = layers[0];
                }
                var startIndex = layers.IndexOf(LastSelected);
                var endIndex = layers.IndexOf(viewModel);
                if (startIndex == endIndex)
                {
                    foreach (var l in oldSelectedLayers)
                    {
                        if (l != viewModel)
                        {
                            SetSelected(l, false);
                            SelectedLayers.Remove(l);
                        }
                    }
                    if (!SelectedLayers.Contains(viewModel))
                    {
                        SetSelected(viewModel, true);
                        SelectedLayers.Add(viewModel);
                    }
                    return;
                }
                else if (startIndex > endIndex)
                {
                    var temp = endIndex;
                    endIndex = startIndex;
                    startIndex = temp;
                }

                var targets = layers.Skip(startIndex).Take(endIndex - startIndex + 1).ToArray();
                foreach (var l in oldSelectedLayers.Except(targets))
                {
                    SetSelected(l, false);
                    SelectedLayers.Remove(l);
                }
                foreach (var l in targets.Except(oldSelectedLayers))
                {
                    SetSelected(l, true);
                    SelectedLayers.Add(l);
                }
            }
            else if (!SelectedLayers.Contains(viewModel))
            {
                foreach (var l in layers)
                {
                    if (l != viewModel)
                    {
                        SetSelected(l, false);
                    }
                }
                SelectedLayers.Clear();

                SetSelected(viewModel, true);
                SelectedLayers.Add(viewModel);
                LastSelected = viewModel;
            }
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
                    Path = new PropertyPath(nameof(LayerControlAreaWidth)),
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

        protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
        {
            base.OnItemsSourceChanged(oldValue, newValue);

            SelectedLayers.Clear();
            LastSelected = null;
        }

        void SetSelected(LayerViewModel viewModel, bool selected)
        {
            var item = ItemContainerGenerator.ContainerFromItem(viewModel);
            if (item is LayerView layer)
            {
                layer.IsSelected = selected;
            }
        }
    }

    class LayerDragData
    {
        public LayerViewModel[] SelectedLayers;

        public LayerViewModel DragLayer;

        public LayerDragData(LayerViewModel[] selectedLayers, LayerViewModel dragLayer)
        {
            SelectedLayers = selectedLayers;
            DragLayer = dragLayer;
        }
    }
}
