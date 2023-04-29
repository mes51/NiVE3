using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace NiVE3.View.Resource
{
    [ContentProperty(nameof(Templates))]
    class DataTemplateCollection : DataTemplateSelector
    {
        static readonly DataTemplate EmptyTemplate;

        public List<DataTemplate> Templates { get; set; } = new List<DataTemplate>();

        public bool DefaultIsEmpty { get; set; }

        static DataTemplateCollection()
        {
            EmptyTemplate = new DataTemplate
            {
                VisualTree = new FrameworkElementFactory(typeof(FrameworkElement))
            };
        }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var type = item.GetType();

            // concrete type
            foreach (var template in Templates)
            {
                if (template.DataType is Type dt && dt == type)
                {
                    return template;
                }
            }

            // assignable type
            foreach (var template in Templates)
            {
                if (template.DataType is Type dt && type.IsAssignableTo(dt))
                {
                    return template;
                }
            }

            if (DefaultIsEmpty)
            {
                return EmptyTemplate;
            }
            else
            {
                return base.SelectTemplate(item, container);
            }
        }
    }
}
