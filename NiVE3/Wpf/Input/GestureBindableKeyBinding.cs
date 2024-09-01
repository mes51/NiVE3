using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows;

namespace NiVE3.Wpf.Input
{
    class GestureBindableKeyBinding : InputBinding
    {
        public static readonly DependencyProperty BindableGestureProperty = DependencyProperty.Register(
            nameof(BindableGesture),
            typeof(InputGesture),
            typeof(GestureBindableKeyBinding),
            new PropertyMetadata(new KeyGesture(Key.None, ModifierKeys.None), GestureChanged)
        );

        public InputGesture BindableGesture
        {
            get { return (InputGesture)GetValue(BindableGestureProperty); }
            set { SetValue(BindableGestureProperty, value); }
        }

        public override InputGesture Gesture
        {
            get
            {
                return base.Gesture;
            }
            set
            {
                base.Gesture = value;
                SetCurrentValue(BindableGestureProperty, value);
            }
        }

        public GestureBindableKeyBinding() : base() { }

        public GestureBindableKeyBinding(ICommand command, Key key, ModifierKeys modifiers) : this(command, new KeyGesture(key, modifiers)) { }

        public GestureBindableKeyBinding(ICommand command, InputGesture gesture) : base(command, gesture)
        {
            BindableGesture = gesture;
        }

        static void GestureChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is GestureBindableKeyBinding inputBinding && e.NewValue is InputGesture newInput)
            {
                inputBinding.Gesture = newInput;
            }
        }

        protected override Freezable CreateInstanceCore()
        {
            return new GestureBindableKeyBinding();
        }
    }
}
