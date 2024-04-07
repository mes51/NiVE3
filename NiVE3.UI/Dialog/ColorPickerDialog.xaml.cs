using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;
using NiVE3.UI.Internal;
using NiVE3.UI.Resources;
using NiVE3.Shared.Extension;
using NiVE3.UI.Command;
using System.Numerics;

namespace NiVE3.UI.Dialog
{
    /// <summary>
    /// ColorPickerDialog.xaml の相互作用ロジック
    /// </summary>
    public partial class ColorPickerDialog : Window
    {
        public static readonly DependencyProperty OKButtonTextProperty = DependencyProperty.Register(
            nameof(OKButtonText),
            typeof(string),
            typeof(ColorPickerDialog),
            new FrameworkPropertyMetadata("OK")
        );

        public static readonly DependencyProperty CancelButtonTextProperty = DependencyProperty.Register(
            nameof(CancelButtonText),
            typeof(string),
            typeof(ColorPickerDialog),
            new FrameworkPropertyMetadata("キャンセル")
        );

        public static readonly DependencyProperty ColorProperty = DependencyProperty.Register(
            nameof(Color),
            typeof(Color),
            typeof(ColorPickerDialog),
            new FrameworkPropertyMetadata(Colors.Black)
        );

        public static readonly DependencyProperty VectorColorProperty = DependencyProperty.Register(
            nameof(VectorColor),
            typeof(Vector4),
            typeof(ColorPickerDialog),
            new FrameworkPropertyMetadata(Vector4.UnitW)
        );

        private static readonly DependencyProperty OldColorProperty = DependencyProperty.Register(
            nameof(OldColor),
            typeof(Color),
            typeof(ColorPickerDialog),
            new FrameworkPropertyMetadata(Colors.Black)
        );

        public Vector4 VectorColor
        {
            get { return (Vector4)GetValue(VectorColorProperty); }
            set { SetValue(VectorColorProperty, value); }
        }

        public Color Color
        {
            get { return (Color)GetValue(ColorProperty); }
            set { SetValue(ColorProperty, value); }
        }

        public string CancelButtonText
        {
            get { return (string)GetValue(CancelButtonTextProperty); }
            set { SetValue(CancelButtonTextProperty, value); }
        }

        public string OKButtonText
        {
            get { return (string)GetValue(OKButtonTextProperty); }
            set { SetValue(OKButtonTextProperty, value); }
        }

        private Color OldColor
        {
            get { return (Color)GetValue(OldColorProperty); }
            set { SetValue(OldColorProperty, value); }
        }

        public ICommand OKCommand { get; }

        public ColorPickerDialog(Vector4 oldColor) : this(oldColor.ToColor()) { }

        public ColorPickerDialog(Color oldColor)
        {
            OKCommand = new ActionCommand(() =>
            {
                DialogResult = true;
                Close();
            });

            InitializeComponent();
            Color = oldColor;
            OldColor = oldColor;
        }
    }
}
