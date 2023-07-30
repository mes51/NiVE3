using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace NiVE3.View.Resource
{
    class ComboBoxDisplayChanger : DataTemplateSelector
    {
        public DataTemplate? DisplayTemplate { get; set; }

        public DataTemplate? ItemsTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            DataTemplate? result = null;
            if (container is ContentPresenter presenter)
            {
                if (presenter.TemplatedParent is ComboBox)
                {
                    result = DisplayTemplate;
                }
                else
                {
                    result = ItemsTemplate;
                }
            }
            return result ?? base.SelectTemplate(item, container);
        }
    }
}
