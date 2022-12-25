using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows;

namespace NiVE3.View.Dock
{
    [ContentProperty(nameof(Styles))]
    class LayoutItemContainerStyleSelector : StyleSelector
    {
        public List<LayoutItemContainerStyle> Styles { get; set; } = new List<LayoutItemContainerStyle>();

        public override Style SelectStyle(object item, DependencyObject container)
        {
            return Styles.FirstOrDefault(s => s.ViewModelType != null && item.GetType().Equals(s.ViewModelType))?.Style ?? base.SelectStyle(item, container);
        }
    }

    [ContentProperty(nameof(Style))]
    class LayoutItemContainerStyle
    {
        public Type? ViewModelType { get; set; }

        public Style? Style { get; set; }
    }
}
