using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace NiVE3.View.Primitive
{
    class PlaceholderTextBox : TextBox
    {
        public static readonly DependencyProperty PlaceholderProperty = DependencyProperty.Register(
            nameof(Placeholder),
            typeof(string),
            typeof(PlaceholderTextBox),
            new FrameworkPropertyMetadata("")
        );

        public static readonly DependencyProperty IsVisiblePlaceholderProperty = DependencyProperty.Register(
            nameof(IsVisiblePlaceholder),
            typeof(bool),
            typeof(PlaceholderTextBox),
            new FrameworkPropertyMetadata(true)
        );

        public static readonly DependencyProperty PlaceholderFontSizeProperty = DependencyProperty.Register(
            nameof(PlaceholderFontSize),
            typeof(double),
            typeof(PlaceholderTextBox),
            new FrameworkPropertyMetadata(10.0)
        );

        public double PlaceholderFontSize
        {
            get { return (double)GetValue(PlaceholderFontSizeProperty); }
            set { SetValue(PlaceholderFontSizeProperty, value); }
        }

        public bool IsVisiblePlaceholder
        {
            get { return (bool)GetValue(IsVisiblePlaceholderProperty); }
            set { SetValue(IsVisiblePlaceholderProperty, value); }
        }

        public string Placeholder
        {
            get { return (string)GetValue(PlaceholderProperty); }
            set { SetValue(PlaceholderProperty, value); }
        }

        public PlaceholderTextBox()
        {
            TextChanged += PlaceholderTextBox_TextChanged;

            BindingOperations.SetBinding(this, PlaceholderFontSizeProperty, new Binding { Source = this, Path = new PropertyPath(FontSizeProperty), Mode = BindingMode.OneWay });
        }

        private void PlaceholderTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SetCurrentValue(IsVisiblePlaceholderProperty, string.IsNullOrEmpty(Text));
        }
    }
}
