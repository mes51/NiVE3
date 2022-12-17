using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
using NiVE3.Config;
using NiVE3.Wpf.Input;
using NiVE3.Wpf.Behavior;
using System.Windows.Data;

namespace NiVE3.Wpf.Attach
{
    class ShortcutKeyBindingProperty
    {
        public static readonly DependencyProperty InputBindingProperty = DependencyProperty.RegisterAttached(
            "InputBinding",
            typeof(bool),
            typeof(ShortcutKeyBindingProperty),
            new PropertyMetadata(false, InputBindingChanged)
        );

        public static bool GetInputBinding(DependencyObject obj)
        {
            return (bool)obj.GetValue(InputBindingProperty);
        }

        public static void SetInputBinding(DependencyObject obj, bool value)
        {
            obj.SetValue(InputBindingProperty, value);
        }

        static void InputBindingChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is UIElement uiElement)
            {
                var oldInputBindings = uiElement.InputBindings.OfType<GestureBindableKeyBinding>().ToArray();
                foreach (var keyBinding in oldInputBindings)
                {
                    BindingOperations.ClearAllBindings(keyBinding);
                    uiElement.InputBindings.Remove(keyBinding);
                }

                if (e.NewValue is bool enable && enable)
                {
                    var setting = ShortcutKeySetting.Setting;
                    foreach (var (key, dp) in ShortcutKeySetting.DependencyProperties)
                    {
                        var gesture = (InputGesture)setting.GetValue(dp);
                        var keyBinding = new GestureBindableKeyBinding(WindowGestureBehavior.GestureCommand, gesture)
                        {
                            CommandParameter = key
                        };
                        var binding = new Binding(key) { Source = setting };
                        BindingOperations.SetBinding(keyBinding, GestureBindableKeyBinding.BindableGestureProperty, binding);

                        uiElement.InputBindings.Add(keyBinding);
                    }
                }
            }
        }
    }
}
