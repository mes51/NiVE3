using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace NiVE3.View.Resource
{
    class BindingProxy : Freezable
    {
        public static readonly DependencyProperty DataProperty = DependencyProperty.Register(
            nameof(Data),
            typeof(object),
            typeof(BindingProxy),
            new PropertyMetadata(null)
        );

        public object? Data
        {
            get { return GetValue(DataProperty); }
            set { SetValue(DataProperty, value); }
        }

        protected override Freezable CreateInstanceCore()
        {
            return new BindingProxy();
        }
    }
}
