using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows;
using NiVE3.Wpf.Input;

namespace NiVE3.Wpf.Attach
{
    class MenuItemGestureBindingProperty
    {
        public static readonly DependencyProperty InputBindingProperty = DependencyProperty.RegisterAttached(
            "InputBinding",
            typeof(InputBinding),
            typeof(MenuItemGestureBindingProperty),
            new PropertyMetadata(null, InputBindingChanged)
        );

        public static InputBinding GetInputBinding(DependencyObject obj)
        {
            return (InputBinding)obj.GetValue(InputBindingProperty);
        }

        public static void SetInputBinding(DependencyObject obj, InputBinding value)
        {
            obj.SetValue(InputBindingProperty, value);
        }

        static void InputBindingChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            if (obj is MenuItem item)
            {
                if (e.NewValue is InputBinding binding)
                {
                    switch (binding.Gesture)
                    {
                        case KeyGesture keyGesture:
                            item.InputGestureText = keyGesture.GetDisplayStringForCulture(CultureInfo.CurrentCulture);
                            break;
                        case SingleKeyGesture singleKeyGesture:
                            item.InputGestureText = singleKeyGesture.GetDisplayStringForCulture(CultureInfo.CurrentCulture);
                            break;
                    }
                    item.SetBinding(MenuItem.CommandProperty, new Binding(nameof(InputBinding.Command)) { Source = binding, Mode = BindingMode.OneWay });
                    item.SetBinding(MenuItem.CommandParameterProperty, new Binding(nameof(InputBinding.CommandParameter)) { Source = binding, Mode = BindingMode.OneWay });
                }
                else
                {
                    item.InputGestureText = "";
                    item.ClearValue(MenuItem.CommandProperty);
                    item.ClearValue(MenuItem.CommandParameterProperty);
                }
            }
        }
    }
}
