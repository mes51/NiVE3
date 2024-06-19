using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows;

namespace NiVE3.UI.Resources
{
    [ContentProperty(nameof(Templates))]
    public class DataTemplateCollection : DataTemplateSelector
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

        /// <summary>
        /// DataTemplate使用時、DataContextが更新されない場合がある時用
        /// </summary>
        // SEE: https://stackoverflow.com/a/65750003
        // TODO: ラッパー側のContentPresenterのスタイルが邪魔になったときにStyleプロパティを用意する
        public bool AlwaysWrapTemplate { get; set; }

        static DataTemplateCollection()
        {
            EmptyTemplate = new DataTemplate
            {
                VisualTree = new FrameworkElementFactory(typeof(FrameworkElement))
            };
        }

        public override DataTemplate SelectTemplate(object? item, DependencyObject container)
        {
            DataTemplate? template = null;
            if (item != null)
            {
                template = FindTemplate(item.GetType());
            }

            if (template == null && DefaultIsEmpty)
            {
                return EmptyTemplate;
            }

            if (template == null && HasNullTypeTemplate)
            {
                template = Templates.FirstOrDefault(t => t.DataType == null);
            }

            template ??= base.SelectTemplate(item, container);
            if (AlwaysWrapTemplate)
            {
                return WrapTemplate(template);
            }
            else
            {
                return template;
            }
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

            return null;
        }

        static DataTemplate WrapTemplate(DataTemplate inner)
        {
            var wrapperContentPresenterFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            wrapperContentPresenterFactory.SetValue(ContentPresenter.ContentTemplateProperty, inner);
            return new DataTemplate
            {
                VisualTree = wrapperContentPresenterFactory
            };
        }
    }
}
