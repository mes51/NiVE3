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
using System.Windows.Navigation;
using System.Windows.Shapes;
using NiVE3.Image.Color;
using NiVE3.Plugin.Internal.Dialog;
using NiVE3.UI.Command;

namespace NiVE3.Plugin.Property.Control
{
    /// <summary>
    /// ColorGradientPropertyControl.xaml の相互作用ロジック
    /// </summary>
    public partial class ColorGradientPropertyControl : PropertyControlBase
    {
        public static readonly DependencyProperty EditButtonTextProperty = DependencyProperty.Register(
            nameof(EditButtonText),
            typeof(string),
            typeof(ColorGradientPropertyControl),
            new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public static readonly DependencyProperty DialogOKButtonTextProperty = DependencyProperty.Register(
            nameof(DialogOKButtonText),
            typeof(string),
            typeof(ColorGradientPropertyControl),
            new FrameworkPropertyMetadata("")
        );

        public static readonly DependencyProperty DialogCancelButtonTextProperty = DependencyProperty.Register(
            nameof(DialogCancelButtonText),
            typeof(string),
            typeof(ColorGradientPropertyControl),
            new FrameworkPropertyMetadata("")
        );

        public static readonly DependencyProperty ShowPreviewOKLabInterpolationProperty = DependencyProperty.Register(
            nameof(ShowPreviewOKLabInterpolation),
            typeof(bool),
            typeof(ColorGradientPropertyControl),
            new FrameworkPropertyMetadata(false)
        );

        public bool ShowPreviewOKLabInterpolation
        {
            get { return (bool)GetValue(ShowPreviewOKLabInterpolationProperty); }
            set { SetValue(ShowPreviewOKLabInterpolationProperty, value); }
        }

        public string DialogCancelButtonText
        {
            get { return (string)GetValue(DialogCancelButtonTextProperty); }
            set { SetValue(DialogCancelButtonTextProperty, value); }
        }

        public string DialogOKButtonText
        {
            get { return (string)GetValue(DialogOKButtonTextProperty); }
            set { SetValue(DialogOKButtonTextProperty, value); }
        }

        public string EditButtonText
        {
            get { return (string)GetValue(EditButtonTextProperty); }
            set { SetValue(EditButtonTextProperty, value); }
        }

        public ICommand EditGradientCommand { get; }

        public ColorGradientPropertyControl()
        {
            EditGradientCommand = new ActionCommand(() =>
            {
                var viewModel = ViewModel;
                if (viewModel == null)
                {
                    return;
                }

                var dialog = new ColorGradientEditDialog((ColorGradient)(ViewModel?.CurrentTimeRawValue ?? ColorGradient.WhiteBlackGradient))
                {
                    Owner = Application.Current.MainWindow,
                    ShowPreviewOKLabInterpolation = ShowPreviewOKLabInterpolation
                };
                if (dialog.ShowDialog() ?? false)
                {
                    viewModel.BeginEditCommand.Execute(null);
                    viewModel.CurrentTimeRawValue = dialog.GetColorGradient();
                    viewModel.EndEditCommand.Execute(null);
                }
            });

            InitializeComponent();
        }
    }
}
