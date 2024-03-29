using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace NiVE3.UI.Resources
{
    [ContentProperty(nameof(Templates))]
    public class ItemContainerTemplateCollection : ItemContainerTemplateSelector
    {
        static readonly DataTemplate EmptyTemplate;

        public List<DataTemplate> Templates { get; set; } = [];

        /// <summary>
        /// テンプレートが存在しなかったとき、デフォルトではなく空のテンプレートを返すかどうか
        /// </summary>
        public bool DefaultIsEmpty { get; set; }

        /// <summary>
        /// Itemがnullの時のテンプレートがTemplatesに含まれているかどうか
        /// </summary>
        public bool HasNullTypeTemplate { get; set; }

        static ItemContainerTemplateCollection()
        {
            EmptyTemplate = new DataTemplate
            {
                VisualTree = new FrameworkElementFactory(typeof(FrameworkElement))
            };
        }

        public override DataTemplate SelectTemplate(object item, ItemsControl parentItemsControl)
        {
            DataTemplate? template = null;
            if (item != null)
            {
                template = FindTemplate(item.GetType());
            }

            if (template == null && HasNullTypeTemplate)
            {
                template = Templates.FirstOrDefault(t => t.DataType == null);
            }

            if (template == null && DefaultIsEmpty)
            {
                return EmptyTemplate;
            }

            return template ?? base.SelectTemplate(item, parentItemsControl);
        }

        DataTemplate? FindTemplate(Type type)
        {
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

            // all type or null
            return Templates.FirstOrDefault(t => t.DataType == null);
        }
    }
}
