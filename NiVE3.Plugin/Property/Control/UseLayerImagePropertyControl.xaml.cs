using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.ValueObject;

namespace NiVE3.Plugin.Property.Control
{
    /// <summary>
    /// UseLayerImagePropertyControl.xaml の相互作用ロジック
    /// </summary>
    public partial class UseLayerImagePropertyControl : PropertyControlBase
    {
        // NOTE: なぜかTypeConverterをSourceTypeにつけてもNREが出てXAML上でリソースとして定義出来ないため、定数として定義する
        public static readonly SourceType ImageSourceType = SourceType.Image | SourceType.Video;

        public static readonly DependencyProperty LayerCollectionSourceProperty = DependencyProperty.Register(
            nameof(LayerCollectionSource),
            typeof(IEnumerable<ILayerViewModel>),
            typeof(UseLayerImagePropertyControl),
            new FrameworkPropertyMetadata(Array.Empty<ILayerViewModel>())
        );

        public static readonly DependencyProperty SelectedLayerProperty = DependencyProperty.Register(
            nameof(SelectedLayer),
            typeof(ILayerViewModel),
            typeof(UseLayerImagePropertyControl),
            new FrameworkPropertyMetadata(null)
        );

        public static readonly DependencyProperty SelectedImageProcessTypeProperty = DependencyProperty.Register(
            nameof(SelectedImageProcessType),
            typeof(LayerImageProcessType),
            typeof(UseLayerImagePropertyControl),
            new FrameworkPropertyMetadata(LayerImageProcessType.Raw)
        );

        public LayerImageProcessType SelectedImageProcessType
        {
            get { return (LayerImageProcessType)GetValue(SelectedImageProcessTypeProperty); }
            set { SetValue(SelectedImageProcessTypeProperty, value); }
        }

        public ILayerViewModel? SelectedLayer
        {
            get { return (ILayerViewModel)GetValue(SelectedLayerProperty); }
            set { SetValue(SelectedLayerProperty, value); }
        }

        public IEnumerable<ILayerViewModel> LayerCollectionSource
        {
            get { return (IEnumerable<ILayerViewModel>)GetValue(LayerCollectionSourceProperty); }
            set { SetValue(LayerCollectionSourceProperty, value); }
        }

        ICompositionViewModel CompositionViewModel { get; }

        public UseLayerImagePropertyControl(ICompositionViewModel compositionViewModel)
        {
            CompositionViewModel = compositionViewModel;

            InitializeComponent();
        }

        private void Root_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (DataContext is not IPropertyViewModel viewModel)
            {
                return;
            }

            if (e.OldValue is IPropertyViewModel oldViewModel)
            {
                oldViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            }
            if (e.NewValue is IPropertyViewModel newViewModel)
            {
                newViewModel.PropertyChanged += ViewModel_PropertyChanged;
                if (newViewModel.CurrentTimeRawValue is UseLayerImageTarget target)
                {
                    SetCurrentValue(SelectedLayerProperty, CompositionViewModel.LayerViewModels.FirstOrDefault(l => l.LayerId == target.LayerId));
                    SetCurrentValue(SelectedImageProcessTypeProperty, target.ImageProcessType);
                }
                else
                {
                    SetCurrentValue(SelectedLayerProperty, null);
                    SetCurrentValue(SelectedImageProcessTypeProperty, LayerImageProcessType.Raw);
                }
            }
        }

        private void LayerSelectBox_SelectItemChangedByUser(object sender, RoutedEventArgs e)
        {
            var viewModel = ViewModel;
            if (viewModel == null)
            {
                return;
            }

            viewModel.BeginEditCommand.Execute(null);

            viewModel.CurrentTimeRawValue = SelectedLayer != null ? new UseLayerImageTarget(SelectedLayer.LayerId, SelectedImageProcessType) : UseLayerImageTarget.Empty;

            viewModel.EndEditCommand.Execute(null);
        }

        private void ImageProcessTypeSelectBox_SelectItemChangedByUser(object sender, RoutedEventArgs e)
        {
            var viewModel = ViewModel;
            if (viewModel == null)
            {
                return;
            }

            viewModel.BeginEditCommand.Execute(null);

            viewModel.CurrentTimeRawValue = SelectedLayer != null ? new UseLayerImageTarget(SelectedLayer.LayerId, SelectedImageProcessType) : UseLayerImageTarget.Empty;

            viewModel.EndEditCommand.Execute(null);
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IPropertyViewModel.CurrentTimeRawValue))
            {
                if (ViewModel?.CurrentTimeRawValue is UseLayerImageTarget target)
                {
                    SetCurrentValue(SelectedLayerProperty, CompositionViewModel.LayerViewModels.FirstOrDefault(l => l.LayerId == target.LayerId));
                    SetCurrentValue(SelectedImageProcessTypeProperty, target.ImageProcessType);
                }
                else
                {
                    SetCurrentValue(SelectedLayerProperty, null);
                    SetCurrentValue(SelectedImageProcessTypeProperty, LayerImageProcessType.Raw);
                }
            }
        }
    }
}
