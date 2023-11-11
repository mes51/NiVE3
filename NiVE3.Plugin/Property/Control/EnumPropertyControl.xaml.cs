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

namespace NiVE3.Plugin.Property.Control
{
    /// <summary>
    /// EnumPropertyControl.xaml の相互作用ロジック
    /// </summary>
    public partial class EnumPropertyControl : PropertyControlBase
    {
        public static readonly DependencyProperty SelectBoxWidthProperty = DependencyProperty.Register(
            nameof(SelectBoxWidth),
            typeof(double),
            typeof(EnumPropertyControl),
            new FrameworkPropertyMetadata(75.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public static readonly DependencyProperty SelectedValueProperty = DependencyProperty.Register(
            nameof(SelectedValue),
            typeof(object),
            typeof(EnumPropertyControl),
            new FrameworkPropertyMetadata(0)
        );

        public object SelectedValue
        {
            get { return (object)GetValue(SelectedValueProperty); }
            set { SetValue(SelectedValueProperty, value); }
        }

        public double SelectBoxWidth
        {
            get { return (double)GetValue(SelectBoxWidthProperty); }
            set { SetValue(SelectBoxWidthProperty, value); }
        }

        public EnumPropertyControl()
        {
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
                SetCurrentValue(SelectedValueProperty, newViewModel.CurrentTimeValue);
            }
        }

        private void ContextMenuSelectBox_SelectItemChangedByUser(object sender, RoutedEventArgs e)
        {
            var viewModel = ViewModel;
            if (viewModel == null)
            {
                return;
            }

            viewModel.BeginEditCommand.Execute(null);

            viewModel.CurrentTimeValue = SelectedValue;

            viewModel.EndEditCommand.Execute(null);
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IPropertyViewModel.CurrentTimeValue))
            {
                SetCurrentValue(SelectedValueProperty, ViewModel?.CurrentTimeValue);
            }
        }
    }
}
