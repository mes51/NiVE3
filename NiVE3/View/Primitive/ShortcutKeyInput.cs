using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NiVE3.Extension;
using System.Windows.Media;
using NiVE3.UI.Resources;

namespace NiVE3.View.Primitive
{
    class ShortcutKeyInput : Control
    {
        public static readonly DependencyProperty PlaceholderTextProperty = DependencyProperty.Register(
            nameof(PlaceholderText),
            typeof(string),
            typeof(ShortcutKeyInput),
            new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public static readonly RoutedEvent ShortcutKeyInputCompletedEvent = EventManager.RegisterRoutedEvent(
            nameof(ShortcutKeyInputCompleted), RoutingStrategy.Direct, typeof(EventHandler<RoutedEventArgs>), typeof(ShortcutKeyInput)
        );

        public string PlaceholderText
        {
            get { return (string)GetValue(PlaceholderTextProperty); }
            set { SetValue(PlaceholderTextProperty, value); }
        }

        public ModifierKeys Modifier { get; private set; }

        public Key Key { get; private set; }

        public bool IsCompleted { get; private set; }

        ModifierKeys CurrentModifier =>
            ((PressedModifierKeys.Contains(Key.LeftShift) || PressedModifierKeys.Contains(Key.RightShift)) ? ModifierKeys.Shift : ModifierKeys.None) |
            ((PressedModifierKeys.Contains(Key.LeftCtrl) || PressedModifierKeys.Contains(Key.RightCtrl)) ? ModifierKeys.Control : ModifierKeys.None) |
            ((PressedModifierKeys.Contains(Key.LeftAlt) || PressedModifierKeys.Contains(Key.RightAlt) || PressedModifierKeys.Contains(Key.System)) ? ModifierKeys.Alt : ModifierKeys.None);

        List<Key> PressedModifierKeys { get; } = [];

        List<Key> PressedKeys { get; } = [];

        public event EventHandler<RoutedEventArgs> ShortcutKeyInputCompleted
        {
            add { AddHandler(ShortcutKeyInputCompletedEvent, value); }
            remove { RemoveHandler(ShortcutKeyInputCompletedEvent, value); }
        }

        public ShortcutKeyInput()
        {
            SetResourceReference(ForegroundProperty, nameof(AppearanceResourceDictionary.TextBrush));
        }

        public void Clear()
        {
            Modifier = ModifierKeys.None;
            Key = Key.None;
            IsCompleted = false;
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            e.Handled = true;

            if (e.Key != Key.System)
            {
                ProcessInputKey(e.Key);
            }
            else
            {
                ProcessInputKey(e.SystemKey);
            }
        }

        protected override void OnPreviewKeyUp(KeyEventArgs e)
        {
            e.Handled = true;
            PressedModifierKeys.Remove(e.Key);
            PressedKeys.Remove(e.Key);
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);
            PressedModifierKeys.Clear();
            PressedKeys.Clear();
        }

        void ProcessInputKey(Key key)
        {
            if (PressedModifierKeys.Contains(key) || PressedKeys.Contains(key))
            {
                return;
            }
            if (key == Key.LeftShift || key == Key.RightShift || key == Key.LeftCtrl || key == Key.RightCtrl || key == Key.LeftAlt || key == Key.RightAlt || key == Key.System)
            {
                PressedModifierKeys.Add(key);
            }
            else
            {
                PressedKeys.Add(key);
            }

            if (PressedKeys.Count > 0)
            {
                Modifier = CurrentModifier;
                Key = PressedKeys.First();
                IsCompleted = true;
                OnShortcutKeyInputCompleted();
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            var formattedText = this.CreateFormattedText(PlaceholderText, Foreground);
            drawingContext.DrawText(formattedText, new Point(0.0, (ActualHeight - formattedText.Height) * 0.5));
        }

        void OnShortcutKeyInputCompleted()
        {
            RaiseEvent(new RoutedEventArgs(ShortcutKeyInputCompletedEvent));
        }
    }
}
