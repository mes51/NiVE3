using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.View.Resource;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Input;
using NiVE3.ViewModel;

namespace NiVE3.View.Part
{
    // NOTE: 子要素を持つコントロールはXAMLで作るとBindingが上手くいかなくなる
    // SEE: https://shuntaro3.hatenablog.com/entry/2018/10/01/002603
    class LayerItemExpander : ContentControl
    {
        public static readonly DependencyProperty IsExpandedProperty = DependencyProperty.Register(
            nameof(IsExpanded),
            typeof(bool),
            typeof(LayerItemExpander),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault)
        );

        public static readonly DependencyProperty HeaderTextProperty = DependencyProperty.Register(
            nameof(HeaderText),
            typeof(string),
            typeof(LayerItemExpander),
            new FrameworkPropertyMetadata("")
        );

        public static readonly DependencyProperty IndentLevelProperty = DependencyProperty.Register(
            nameof(IndentLevel),
            typeof(int),
            typeof(LayerItemExpander),
            new FrameworkPropertyMetadata(0, IndentParameterChanged)
        );

        public static readonly DependencyProperty IsAVSwitchColumnVisibleProperty = DependencyProperty.Register(
            nameof(IsAVSwitchColumnVisible),
            typeof(bool),
            typeof(LayerItemExpander),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure, IndentParameterChanged)
        );

        public static readonly DependencyProperty IsTagColumnVisibleProperty = DependencyProperty.Register(
            nameof(IsTagColumnVisible),
            typeof(bool),
            typeof(LayerItemExpander),
            new FrameworkPropertyMetadata(true, NameAreaWidthChanged)
        );

        public static readonly DependencyProperty NameAreaWidthProperty = DependencyProperty.Register(
            nameof(NameAreaWidth),
            typeof(double),
            typeof(LayerItemExpander),
            new FrameworkPropertyMetadata(0.0, NameAreaWidthChanged)
        );

        public static readonly DependencyProperty SwitchesProperty = DependencyProperty.Register(
            nameof(Switches),
            typeof(UIElement),
            typeof(LayerItemExpander),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public static readonly DependencyProperty NameDragSourceIgnoreProperty = DependencyProperty.Register(
            nameof(NameDragSourceIgnore),
            typeof(bool),
            typeof(LayerItemExpander),
            new FrameworkPropertyMetadata(true)
        );

        public static readonly DependencyProperty IsHighlightHeaderProperty = DependencyProperty.Register(
            nameof(IsHighlightHeader),
            typeof(bool),
            typeof(LayerItemExpander),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public static readonly DependencyProperty ParentHasExpanderArrowProperty = DependencyProperty.Register(
            nameof(ParentHasExpanderArrow),
            typeof(bool),
            typeof(LayerItemExpander),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        private static readonly DependencyProperty IndentMarginLeftProperty = DependencyProperty.Register(
            nameof(IndentMarginLeft),
            typeof(GridLength),
            typeof(LayerItemExpander),
            new FrameworkPropertyMetadata(new GridLength(UIParameters.AVSwitchWidthWithHalfSplitter), FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        private static readonly DependencyProperty CalculatedNameAreaWidthProperty = DependencyProperty.Register(
            nameof(CalculatedNameAreaWidth),
            typeof(GridLength),
            typeof(LayerItemExpander),
            new FrameworkPropertyMetadata(new GridLength(0.0), FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public static readonly DependencyProperty ControlAreaWidthProperty = DependencyProperty.Register(
            nameof(ControlAreaWidth),
            typeof(GridLength),
            typeof(LayerItemExpander),
            new FrameworkPropertyMetadata(new GridLength(0.0), FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public static readonly DependencyProperty HeaderSubItemProperty = DependencyProperty.Register(
            nameof(HeaderSubItem),
            typeof(UIElement),
            typeof(LayerItemExpander),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public static readonly DependencyProperty IsNameEditingProperty = DependencyProperty.Register(
            nameof(IsNameEditing),
            typeof(bool),
            typeof(LayerItemExpander),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public bool IsNameEditing
        {
            get { return (bool)GetValue(IsNameEditingProperty); }
            set { SetValue(IsNameEditingProperty, value); }
        }

        public UIElement? HeaderSubItem
        {
            get { return (UIElement)GetValue(HeaderSubItemProperty); }
            set { SetValue(HeaderSubItemProperty, value); }
        }

        public GridLength ControlAreaWidth
        {
            get { return (GridLength)GetValue(ControlAreaWidthProperty); }
            set { SetValue(ControlAreaWidthProperty, value); }
        }

        public bool IsHighlightHeader
        {
            get { return (bool)GetValue(IsHighlightHeaderProperty); }
            set { SetValue(IsHighlightHeaderProperty, value); }
        }

        public bool NameDragSourceIgnore
        {
            get { return (bool)GetValue(NameDragSourceIgnoreProperty); }
            set { SetValue(NameDragSourceIgnoreProperty, value); }
        }

        public UIElement? Switches
        {
            get { return (UIElement)GetValue(SwitchesProperty); }
            set { SetValue(SwitchesProperty, value); }
        }

        public double NameAreaWidth
        {
            get { return (double)GetValue(NameAreaWidthProperty); }
            set { SetValue(NameAreaWidthProperty, value); }
        }

        public bool IsTagColumnVisible
        {
            get { return (bool)GetValue(IsTagColumnVisibleProperty); }
            set { SetValue(IsTagColumnVisibleProperty, value); }
        }

        public bool IsAVSwitchColumnVisible
        {
            get { return (bool)GetValue(IsAVSwitchColumnVisibleProperty); }
            set { SetValue(IsAVSwitchColumnVisibleProperty, value); }
        }

        public int IndentLevel
        {
            get { return (int)GetValue(IndentLevelProperty); }
            set { SetValue(IndentLevelProperty, value); }
        }

        public string HeaderText
        {
            get { return (string)GetValue(HeaderTextProperty); }
            set { SetValue(HeaderTextProperty, value); }
        }

        public bool IsExpanded
        {
            get { return (bool)GetValue(IsExpandedProperty); }
            set { SetValue(IsExpandedProperty, value); }
        }

        public bool ParentHasExpanderArrow
        {
            get { return (bool)GetValue(ParentHasExpanderArrowProperty); }
            set { SetValue(ParentHasExpanderArrowProperty, value); }
        }

        private GridLength IndentMarginLeft
        {
            get { return (GridLength)GetValue(IndentMarginLeftProperty); }
            set { SetValue(IndentMarginLeftProperty, value); }
        }

        private GridLength CalculatedNameAreaWidth
        {
            get { return (GridLength)GetValue(CalculatedNameAreaWidthProperty); }
            set { SetValue(CalculatedNameAreaWidthProperty, value); }
        }

        TextBox NameTextBox
        {
            get
            {
                var result = GetTemplateChild(nameof(NameTextBox)) as TextBox;
                if (result == null)
                {
                    throw new InvalidOperationException(nameof(NameTextBox) + " is not found");
                }
                return result;
            }
        }

        INameEditableViewModel? NameEditableViewModel => DataContext as INameEditableViewModel;

        static LayerItemExpander()
        {
            HorizontalContentAlignmentProperty.OverrideMetadata(typeof(LayerItemExpander), new FrameworkPropertyMetadata(HorizontalAlignment.Stretch, FrameworkPropertyMetadataOptions.Inherits));
            IsTabStopProperty.OverrideMetadata(typeof(LayerItemExpander), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));
            FocusVisualStyleProperty.OverrideMetadata(typeof(LayerItemExpander), new FrameworkPropertyMetadata(null, (d, e) => System.Diagnostics.Debug.WriteLine("{0}, {1}", d, e)));
            //FocusableProperty.OverrideMetadata(typeof(LayerItemExpander), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            var nameTextBox = NameTextBox;
            nameTextBox.IsVisibleChanged += NameTextBox_IsVisibleChanged;
            nameTextBox.PreviewKeyDown += NameTextBox_PreviewKeyDown;
            Mouse.AddPreviewMouseDownOutsideCapturedElementHandler(nameTextBox, NameTextBox_PreviewMouseDownOutsideCapturedElement);
            Mouse.AddLostMouseCaptureHandler(nameTextBox, NameTextBox_LostMouseCapture);
        }

        static bool IsClickSameControl(FrameworkElement fe, MouseButtonEventArgs e)
        {
            return new Rect(0.0, 0.0, fe.ActualWidth, fe.ActualHeight).Contains(e.GetPosition(fe));
        }

        private void NameTextBox_LostMouseCapture(object sender, MouseEventArgs e)
        {
            // NOTE: なぜかCaptureMouse後にマウスキャプチャが外れるため再度キャプチャする
            // TODO: キャプチャが外れる原因の調査
            if (NameEditableViewModel?.IsNameEditing ?? false)
            {
                NameTextBox.CaptureMouse();
            }
        }

        private void NameTextBox_PreviewMouseDownOutsideCapturedElement(object sender, MouseButtonEventArgs e)
        {
            var viewModel = NameEditableViewModel;
            if (!IsClickSameControl(NameTextBox, e) && viewModel != null && viewModel.EndEditNameCommand.CanExecute(true))
            {
                viewModel.EndEditNameCommand.Execute(true);
                NameTextBox.ReleaseMouseCapture();
                e.Handled = true;
            }
        }

        private void NameTextBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            var viewModel = NameEditableViewModel;
            if (viewModel == null)
            {
                return;
            }

            if ((e.Key == Key.Tab || (e.Key == Key.Enter && e.ImeProcessedKey == Key.None)) && viewModel.EndEditNameCommand.CanExecute(true))
            {
                viewModel.EndEditNameCommand.Execute(true);
                NameTextBox.ReleaseMouseCapture();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape && viewModel.EndEditNameCommand.CanExecute(false))
            {
                viewModel.EndEditNameCommand.Execute(false);
                NameTextBox.ReleaseMouseCapture();
                e.Handled = true;
            }
        }

        private void NameTextBox_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (NameEditableViewModel?.IsNameEditing ?? false)
            {
                NameTextBox.Focus();
                NameTextBox.SelectAll();
                NameTextBox.CaptureMouse();
            }
        }

        static void IndentParameterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is LayerItemExpander expander)
            {
                var indent = UIParameters.ArrowWidth * expander.IndentLevel;
                if (expander.IsAVSwitchColumnVisible)
                {
                    indent += UIParameters.AVSwitchWidthWithHalfSplitter;
                }
                expander.IndentMarginLeft = new GridLength(indent);

                var nameAreaWidth = expander.NameAreaWidth - UIParameters.ArrowWidth * (expander.IndentLevel + 1) + UIParameters.ArrowWidth;
                if (!expander.ParentHasExpanderArrow)
                {
                    nameAreaWidth -= UIParameters.ArrowWidth;
                }
                if (expander.IsTagColumnVisible)
                {
                    nameAreaWidth += UIParameters.TagAreaWidth;
                }
                expander.CalculatedNameAreaWidth = new GridLength(Math.Max(nameAreaWidth, 0.0));
            }
        }

        static void NameAreaWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is LayerItemExpander expander)
            {
                var nameAreaWidth = expander.NameAreaWidth - UIParameters.ArrowWidth * (expander.IndentLevel + 1) + UIParameters.ArrowWidth;
                if (!expander.ParentHasExpanderArrow)
                {
                    nameAreaWidth -= UIParameters.ArrowWidth;
                }
                if (expander.IsTagColumnVisible)
                {
                    nameAreaWidth += UIParameters.TagAreaWidth;
                }
                expander.CalculatedNameAreaWidth = new GridLength(Math.Max(nameAreaWidth, 0.0));
            }
        }
    }
}
