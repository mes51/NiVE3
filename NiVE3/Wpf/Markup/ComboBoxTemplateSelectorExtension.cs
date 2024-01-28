using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.View.Resource;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows;

namespace NiVE3.Wpf.Markup
{
    // https://stackoverflow.com/a/33421573
    class ComboBoxTemplateSelectorExtension : MarkupExtension
    {
        public DataTemplate? SelectedItemTemplate { get; set; }
        public DataTemplateSelector? SelectedItemTemplateSelector { get; set; }
        public DataTemplate? DropdownItemsTemplate { get; set; }
        public DataTemplateSelector? DropdownItemsTemplateSelector { get; set; }

        public override object ProvideValue(IServiceProvider serviceProvider)
            => new ComboBoxTemplateSelector()
            {
                SelectedItemTemplate = SelectedItemTemplate,
                SelectedItemTemplateSelector = SelectedItemTemplateSelector,
                DropdownItemsTemplate = DropdownItemsTemplate,
                DropdownItemsTemplateSelector = DropdownItemsTemplateSelector
            };
    }
}
