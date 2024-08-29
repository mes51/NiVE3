using Microsoft.Xaml.Behaviors;
using NiVE3.Wpf.Behavior;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace NiVE3.Wpf.Attach
{
    class WindowGestureBehaviorAttachProperty
    {
        public static readonly DependencyProperty AttachProperty = DependencyProperty.RegisterAttached(
            "Attach",
            typeof(bool),
            typeof(WindowGestureBehaviorAttachProperty),
            new PropertyMetadata(false, AttachChanged)
        );

        public static bool GetAttach(DependencyObject obj)
        {
            return (bool)obj.GetValue(AttachProperty);
        }

        public static void SetAttach(DependencyObject obj, bool value)
        {
            obj.SetValue(AttachProperty, value);
        }

        static void AttachChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is not Window)
            {
                return;
            }

            var behaviors = Microsoft.Xaml.Behaviors.Interaction.GetBehaviors(sender);
            var behavior = behaviors.FirstOrDefault(b => b is WindowGestureBehavior);
            if (behavior != null)
            {
                behaviors.Remove(behavior);
            }

            if (e.NewValue is bool enable && enable)
            {
                behaviors.Add(behavior ?? new WindowGestureBehavior());
            }
        }
    }
}
