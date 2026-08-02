using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using NiVE3.OpenFX.Bridge.Property.Properties;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property.Control;

namespace NiVE3.OpenFX.Bridge.Property.Control
{
    /// <summary>
    /// OfxStrChoicePropertyControl.xaml の相互作用ロジック
    /// </summary>
    public partial class OfxStrChoicePropertyControl : PropertyControlBase
    {
        public static readonly DependencyProperty SelectedOptionProperty = DependencyProperty.Register(
            nameof(SelectedOption),
            typeof(object),
            typeof(OfxStrChoicePropertyControl),
            new FrameworkPropertyMetadata(null)
        );

        public object? SelectedOption
        {
            get { return GetValue(SelectedOptionProperty); }
            set { SetValue(SelectedOptionProperty, value); }
        }

        OfxStrChoiceProperty? Property => ViewModel?.Property as OfxStrChoiceProperty;

        public OfxStrChoicePropertyControl()
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
            var index = ViewModel?.CurrentTimeRawValue is string value ? property.OptionValues.ToList().IndexOf(value) : -1;
            SetCurrentValue(SelectedOptionProperty, property.Options[Math.Clamp(index, 0, property.Options.Count - 1)]);
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
            if (index >= 0 && property.OptionValues[index] != (viewModel.CurrentTimeRawValue as string))
            {
                viewModel.BeginEditCommand.Execute(null);
                viewModel.CurrentTimeRawValue = property.OptionValues[index];
                viewModel.EndEditCommand.Execute(null);
            }
        }
    }
}
