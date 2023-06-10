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

        /// <summary>
        /// DataTemplate使用時、DataContextが更新されない場合がある時用
        /// SEE: https://stackoverflow.com/a/65750003
        /// TODO: ラッパー側のContentPresenterのスタイルが邪魔になったときにStyleプロパティを用意する
        /// </summary>
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

        DataTemplate WrapTemplate(DataTemplate inner)
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
