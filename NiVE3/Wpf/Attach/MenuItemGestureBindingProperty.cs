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
using NiVE3.View.Converter;

namespace NiVE3.Wpf.Attach
{
    class MenuItemGestureBindingProperty
    {
        static IValueConverter DisplayKeyConverter = new DelegateConverter<InputGesture, string>(gesture =>
        {
            return gesture switch
            {
                KeyGesture keyGesture => keyGesture.GetDisplayStringForCulture(CultureInfo.CurrentCulture),
                SingleKeyGesture singleKeyGesture => singleKeyGesture.GetDisplayStringForCulture(CultureInfo.CurrentCulture),
                _ => ""
            };
        });

        public static readonly DependencyProperty InputBindingProperty = DependencyProperty.RegisterAttached(
            "InputBinding",
            typeof(InputBinding),
            typeof(MenuItemGestureBindingProperty),
            new PropertyMetadata(null, InputBindingChanged)
        );

        public static readonly DependencyProperty InputGestureProperty = DependencyProperty.RegisterAttached(
            "InputGesture",
            typeof(InputGesture),
            typeof(MenuItemGestureBindingProperty),
            new PropertyMetadata(null, InputGestureChanged)
        );

        public static InputGesture GetInputGesture(DependencyObject obj)
        {
            return (InputGesture)obj.GetValue(InputGestureProperty);
        }

        public static void SetInputGesture(DependencyObject obj, InputGesture value)
        {
            obj.SetValue(InputGestureProperty, value);
        }

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
                    // TODO: BindableGestureを持つinterfaceを定義するかどうか(SourceGeneratorで生成するクラスが依存する事になるのをどうするか)
                    item.SetBinding(MenuItem.InputGestureTextProperty, new Binding { Source = binding, Mode = BindingMode.OneWay, Path = new PropertyPath("BindableGesture"), Converter = DisplayKeyConverter });
                    item.SetBinding(MenuItem.CommandProperty, new Binding(nameof(InputBinding.Command)) { Source = binding, Mode = BindingMode.OneWay });
                    item.SetBinding(MenuItem.CommandParameterProperty, new Binding(nameof(InputBinding.CommandParameter)) { Source = binding, Mode = BindingMode.OneWay });

                    var displayKey = binding.Gesture switch
                    {
                        KeyGesture keyGesture => keyGesture.GetDisplayStringForCulture(CultureInfo.CurrentCulture),
                        SingleKeyGesture singleKeyGesture => singleKeyGesture.GetDisplayStringForCulture(CultureInfo.CurrentCulture),
                        _ => ""
                    };
                    item.SetCurrentValue(MenuItem.InputGestureTextProperty, displayKey);
                }
                else
                {
                    item.InputGestureText = "";
                    item.ClearValue(MenuItem.InputGestureTextProperty);
                    item.ClearValue(MenuItem.CommandProperty);
                    item.ClearValue(MenuItem.CommandParameterProperty);
                }
            }
        }

        static void InputGestureChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            if (obj is MenuItem item)
            {
                if (e.NewValue is InputGesture gesture)
                {
                    switch (gesture)
                    {
                        case KeyGesture keyGesture:
                            item.InputGestureText = keyGesture.GetDisplayStringForCulture(CultureInfo.CurrentCulture);
                            break;
                        case SingleKeyGesture singleKeyGesture:
                            item.InputGestureText = singleKeyGesture.GetDisplayStringForCulture(CultureInfo.CurrentCulture);
                            break;
                    }
                }
                else
                {
                    item.InputGestureText = "";
                }
            }
        }
    }
}
