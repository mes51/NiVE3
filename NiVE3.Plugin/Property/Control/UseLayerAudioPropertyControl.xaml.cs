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
    /// UseLayerAudioPropertyControl.xaml の相互作用ロジック
    /// </summary>
    public partial class UseLayerAudioPropertyControl : PropertyControlBase
    {
        public static readonly DependencyProperty LayerCollectionSourceProperty = DependencyProperty.Register(
            nameof(LayerCollectionSource),
            typeof(IEnumerable<ILayerViewModel>),
            typeof(UseLayerAudioPropertyControl),
            new FrameworkPropertyMetadata(Array.Empty<ILayerViewModel>())
        );

        public static readonly DependencyProperty SelectedLayerProperty = DependencyProperty.Register(
            nameof(SelectedLayer),
            typeof(ILayerViewModel),
            typeof(UseLayerAudioPropertyControl),
            new FrameworkPropertyMetadata(null)
        );

        public static readonly DependencyProperty SelectedAudioProcessTypeProperty = DependencyProperty.Register(
            nameof(SelectedAudioProcessType),
            typeof(LayerAudioProcessType),
            typeof(UseLayerAudioPropertyControl),
            new FrameworkPropertyMetadata(LayerAudioProcessType.Raw)
        );

        public LayerAudioProcessType SelectedAudioProcessType
        {
            get { return (LayerAudioProcessType)GetValue(SelectedAudioProcessTypeProperty); }
            set { SetValue(SelectedAudioProcessTypeProperty, value); }
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

        public UseLayerAudioPropertyControl(ICompositionViewModel compositionViewModel)
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
                if (newViewModel.CurrentTimeRawValue is UseLayerAudioTarget target)
                {
                    SetCurrentValue(SelectedLayerProperty, CompositionViewModel.LayerViewModels.FirstOrDefault(l => l.LayerId == target.LayerId));
                    SetCurrentValue(SelectedAudioProcessTypeProperty, target.AudioProcessType);
                }
                else
                {
                    SetCurrentValue(SelectedLayerProperty, null);
                    SetCurrentValue(SelectedAudioProcessTypeProperty, LayerAudioProcessType.Raw);
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

            viewModel.CurrentTimeRawValue = SelectedLayer != null ? new UseLayerAudioTarget(SelectedLayer.LayerId, SelectedAudioProcessType) : UseLayerAudioTarget.Empty;

            viewModel.EndEditCommand.Execute(null);
        }

        private void AudioProcessTypeSelectBox_SelectItemChangedByUser(object sender, RoutedEventArgs e)
        {
            var viewModel = ViewModel;
            if (viewModel == null)
            {
                return;
            }

            viewModel.BeginEditCommand.Execute(null);

            viewModel.CurrentTimeRawValue = SelectedLayer != null ? new UseLayerAudioTarget(SelectedLayer.LayerId, SelectedAudioProcessType) : UseLayerAudioTarget.Empty;

            viewModel.EndEditCommand.Execute(null);
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IPropertyViewModel.CurrentTimeRawValue))
            {
                if (ViewModel?.CurrentTimeRawValue is UseLayerAudioTarget target)
                {
                    SetCurrentValue(SelectedLayerProperty, CompositionViewModel.LayerViewModels.FirstOrDefault(l => l.LayerId == target.LayerId));
                    SetCurrentValue(SelectedAudioProcessTypeProperty, target.AudioProcessType);
                }
                else
                {
                    SetCurrentValue(SelectedLayerProperty, null);
                    SetCurrentValue(SelectedAudioProcessTypeProperty, LayerAudioProcessType.Raw);
                }
            }
        }
    }
}
