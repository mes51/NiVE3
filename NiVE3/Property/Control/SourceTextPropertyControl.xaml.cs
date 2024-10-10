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
using NiVE3.Plugin.Property.Control;
using NiVE3.Text;

namespace NiVE3.Property.Control
{
    /// <summary>
    /// SourceTextPropertyControl.xaml の相互作用ロジック
    /// </summary>
    public partial class SourceTextPropertyControl : PropertyControlBase
    {
        public static readonly DependencyProperty SourceTextProperty = DependencyProperty.Register(
            nameof(SourceText),
            typeof(string),
            typeof(SourceTextPropertyControl),
            new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public string SourceText
        {
            get { return (string)GetValue(SourceTextProperty); }
            set { SetValue(SourceTextProperty, value); }
        }

        public SourceTextPropertyControl()
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
                if (newViewModel.CurrentTimeRawValue is StyledText d)
                {
                    SetCurrentValue(SourceTextProperty, d.Text);
                }
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            EditPopup.IsOpen = true;
        }

        private void EditCancelButton_Click(object sender, RoutedEventArgs e)
        {
            EditPopup.IsOpen = false;
            if (ViewModel?.CurrentTimeRawValue is StyledText d)
            {
                SetCurrentValue(SourceTextProperty, d.Text);
            }
        }

        private void EditOKButton_Click(object sender, RoutedEventArgs e)
        {
            EditPopup.IsOpen = false;

            var viewModel = ViewModel;
            if (viewModel == null || viewModel.CurrentTimeRawValue is not StyledText d)
            {
                return;
            }

            viewModel.BeginEditCommand.Execute(null);

            viewModel.CurrentTimeRawValue = d.ChangeText(SourceText);

            viewModel.EndEditCommand.Execute(null);
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IPropertyViewModel.CurrentTimeRawValue) && ViewModel?.CurrentTimeRawValue is StyledText d)
            {
                SetCurrentValue(SourceTextProperty, d.Text);
            }
        }
    }
}
