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
using NiVE3.OpenFX.Bridge.Property.Properties;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property.Control;
using NiVE3.Plugin.Property.Properties;

namespace NiVE3.OpenFX.Bridge.Property.Control
{
    /// <summary>
    /// SelectBoxPropertyControl.xaml の相互作用ロジック
    /// </summary>
    public partial class OfxSelectBoxPropertyControl : PropertyControlBase
    {
        public static readonly DependencyProperty SelectedOptionProperty = DependencyProperty.Register(
            nameof(SelectedOption),
            typeof(object),
            typeof(OfxSelectBoxPropertyControl),
            new FrameworkPropertyMetadata(null)
        );

        public object? SelectedOption
        {
            get { return GetValue(SelectedOptionProperty); }
            set { SetValue(SelectedOptionProperty, value); }
        }

        OfxSelectBoxProperty? Property => ViewModel?.Property as OfxSelectBoxProperty;

        public OfxSelectBoxPropertyControl()
        {
            InitializeComponent();
        }

        private void Root_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is IPropertyViewModel oldViewModel)
            {
                oldViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            }
            if (e.NewValue is IPropertyViewModel newViewModel)
            {
                newViewModel.PropertyChanged += ViewModel_PropertyChanged;
                UpdateSelectedOption();
            }
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IPropertyViewModel.CurrentTimeRawValue))
            {
                UpdateSelectedOption();
            }
        }

        void UpdateSelectedOption()
        {
            var property = Property;
            if (property == null)
            {
                return;
            }
            var index = Math.Clamp(ViewModel?.CurrentTimeRawValue is int v ? v : 0, 0, property.Options.Count - 1);
            SetCurrentValue(SelectedOptionProperty, property.Options[index]);
        }

        private void ContextMenuSelectBox_SelectItemChangedByUser(object sender, RoutedEventArgs e)
        {
            var viewModel = ViewModel;
            var property = Property;
            if (viewModel == null || property == null || SelectedOption is not string option)
            {
                return;
            }

            var index = property.Options.ToList().IndexOf(option);
            if (index >= 0 && index != (viewModel.CurrentTimeRawValue as int? ?? -1))
            {
                viewModel.BeginEditCommand.Execute(null);
                viewModel.CurrentTimeRawValue = index;
                viewModel.EndEditCommand.Execute(null);
            }
        }
    }
}
