using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows;
using NiVE3.View.Converter;
using System.ComponentModel;

namespace NiVE3.View.Primitive
{
    class TreeListExpandableGridViewColumn : GridViewColumn, INotifyPropertyChanged
    {
        internal const string ExpanderButtonName = "Expander";

        public static readonly DependencyProperty ExpanderStyleProperty = DependencyProperty.Register(
            nameof(ExpanderStyle),
            typeof(Style),
            typeof(TreeListExpandableGridViewColumn),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure, UpdateTemplate)
        );

        public static readonly DependencyProperty CellContentTemplateProperty = DependencyProperty.Register(
            nameof(CellContentTemplate),
            typeof(DataTemplate),
            typeof(TreeListExpandableGridViewColumn),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure, UpdateTemplate)
        );

        BindingBase? lowPriorityDisplayMemberBinding;
        public BindingBase? LowPriorityDisplayMemberBinding
        {
            get => lowPriorityDisplayMemberBinding;
            set
            {
                if (!EqualityComparer<BindingBase>.Default.Equals(value, lowPriorityDisplayMemberBinding))
                {
                    lowPriorityDisplayMemberBinding = value;
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(LowPriorityDisplayMemberBinding)));
                    CellTemplate = CreateCellTemplate(this);
                }
            }
        }

        public DataTemplate CellContentTemplate
        {
            get { return (DataTemplate)GetValue(CellContentTemplateProperty); }
            set { SetValue(CellContentTemplateProperty, value); }
        }

        public Style ExpanderStyle
        {
            get { return (Style)GetValue(ExpanderStyleProperty); }
            set { SetValue(ExpanderStyleProperty, value); }
        }

        public TreeListExpandableGridViewColumn()
        {
            CellTemplate = CreateCellTemplate(this);
        }

        static DataTemplate CreateCellTemplate(TreeListExpandableGridViewColumn self)
        {
            var container = new FrameworkElementFactory(typeof(Grid), "ContainerFactory");

            var containerColumn1 = new FrameworkElementFactory(typeof(ColumnDefinition));
            containerColumn1.SetValue(ColumnDefinition.WidthProperty, new GridLength(1, GridUnitType.Auto));
            var containerColumn2 = new FrameworkElementFactory(typeof(ColumnDefinition));
            containerColumn2.SetValue(ColumnDefinition.WidthProperty, new GridLength(1, GridUnitType.Star));
            container.AppendChild(containerColumn1);
            container.AppendChild(containerColumn2);

            var expander = new FrameworkElementFactory(typeof(ToggleButton), ExpanderButtonName);
            expander.SetValue(Grid.ColumnProperty, 0);
            expander.SetValue(ButtonBase.ClickModeProperty, ClickMode.Press);
            expander.SetBinding(FrameworkElement.StyleProperty, new Binding(nameof(ExpanderStyle)) { Source = self });
            expander.SetBinding(FrameworkElement.MarginProperty, new Binding(nameof(TreeListViewItem.Indent)) { RelativeSource = new RelativeSource { AncestorType = typeof(TreeListViewItem) }, Converter = new DoubleToThicknessConverter(), ConverterParameter = ThicknessConvertFace.Left });
            expander.SetBinding(ToggleButton.IsCheckedProperty, new Binding(nameof(TreeListViewItem.IsExpanded)) { RelativeSource = new RelativeSource { AncestorType = typeof(TreeListViewItem) } });
            container.AppendChild(expander);

            if (self.CellContentTemplate != null)
            {
                var content = new FrameworkElementFactory(typeof(ContentPresenter), "Content");
                content.SetValue(Grid.ColumnProperty, 1);
                content.SetBinding(ContentPresenter.ContentTemplateProperty, new Binding(nameof(CellContentTemplate)) { Source = self });
                content.SetBinding(ContentPresenter.ContentProperty, new Binding());
                container.AppendChild(content);
            }
            else if (self.LowPriorityDisplayMemberBinding != null)
            {
                var text = new FrameworkElementFactory(typeof(TextBlock));
                text.SetValue(Grid.ColumnProperty, 1);
                text.SetBinding(TextBlock.TextProperty, self.LowPriorityDisplayMemberBinding);
                text.SetBinding(FrameworkElement.DataContextProperty, new Binding());
                container.AppendChild(text);
            }

            var expanderVisibility = new DataTrigger
            {
                Binding = new Binding(nameof(ItemsControl.HasItems)) { RelativeSource = new RelativeSource { AncestorType = typeof(TreeListViewItem) } },
                Value = false
            };
            expanderVisibility.Setters.Add(new Setter(UIElement.VisibilityProperty, Visibility.Hidden, expander.Name));

            var result = new DataTemplate();
            result.VisualTree = container;
            result.Triggers.Add(expanderVisibility);

            return result;
        }

        static void UpdateTemplate(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TreeListExpandableGridViewColumn column)
            {
                column.CellTemplate = CreateCellTemplate(column);
            }
        }
    }
}
