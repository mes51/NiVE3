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
    /// UseMaskPathPropertyControl.xaml の相互作用ロジック
    /// </summary>
    public partial class UseMaskPathPropertyControl : PropertyControlBase
    {
        public static readonly DependencyProperty MaskCollectionSourceProperty = DependencyProperty.Register(
            nameof(MaskCollectionSource),
            typeof(IEnumerable<IMaskViewModel>),
            typeof(UseMaskPathPropertyControl),
            new FrameworkPropertyMetadata(Array.Empty<IMaskViewModel>())
        );

        public static readonly DependencyProperty SelectedMaskProperty = DependencyProperty.Register(
            nameof(SelectedMask),
            typeof(IMaskViewModel),
            typeof(UseMaskPathPropertyControl),
            new FrameworkPropertyMetadata(null)
        );

        public IMaskViewModel? SelectedMask
        {
            get { return (IMaskViewModel)GetValue(SelectedMaskProperty); }
            set { SetValue(SelectedMaskProperty, value); }
        }

        public IEnumerable<IMaskViewModel> MaskCollectionSource
        {
            get { return (IEnumerable<IMaskViewModel>)GetValue(MaskCollectionSourceProperty); }
            set { SetValue(MaskCollectionSourceProperty, value); }
        }

        ILayerViewModel LayerViewModel { get; }

        public UseMaskPathPropertyControl(ILayerViewModel layerViewModel)
        {
            LayerViewModel = layerViewModel;

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
                if (newViewModel.CurrentTimeRawValue is UseMaskPathTarget target)
                {
                    SetCurrentValue(SelectedMaskProperty, LayerViewModel.MaskViewModels.FirstOrDefault(m => m.MaskId == target.MaskId));
                }
                else
                {
                    SetCurrentValue(SelectedMaskProperty, null);
                }
            }
        }

        private void MaskSelectBox_SelectItemChangedByUser(object sender, RoutedEventArgs e)
        {
            var viewModel = ViewModel;
            if (viewModel == null)
            {
                return;
            }

            viewModel.BeginEditCommand.Execute(null);

            viewModel.CurrentTimeRawValue = SelectedMask != null ? new UseMaskPathTarget(SelectedMask.MaskId) : UseMaskPathTarget.Empty;

            viewModel.EndEditCommand.Execute(null);
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IPropertyViewModel.CurrentTimeRawValue))
            {
                if (ViewModel?.CurrentTimeRawValue is UseMaskPathTarget target)
                {
                    SetCurrentValue(SelectedMaskProperty, LayerViewModel.MaskViewModels.FirstOrDefault(m => m.MaskId == target.MaskId));
                }
                else
                {
                    SetCurrentValue(SelectedMaskProperty, null);
                }
            }
        }
    }
}
