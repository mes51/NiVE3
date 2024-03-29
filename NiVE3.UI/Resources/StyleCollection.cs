using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace NiVE3.UI.Resources
{
    public class StyleCollection : StyleSelector
    {
        public List<Style> Styles { get; set; } = [];

        public Style? DefaultStyle { get; set; }

        public override Style SelectStyle(object item, DependencyObject container)
        {
            var style = item != null ? FindStyle(item.GetType()) : null;
            return style ?? DefaultStyle ?? base.SelectStyle(item, container);
        }

        Style? FindStyle(Type type)
        {
            // concrete type
            foreach (var style in Styles)
            {
                if (style.TargetType == type)
                {
                    return style;
                }
            }

            // assignable type
            foreach (var style in Styles)
            {
                if (style.TargetType != null && style.TargetType.IsAssignableTo(type))
                {
                    return style;
                }
            }

            // all type or null
            return Styles.FirstOrDefault(s => s.TargetType == null);
        }
    }
}
