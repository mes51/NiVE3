using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using NiVE3.ViewModel;

namespace NiVE3.View.Primitive
{
    public class PaneViewBase : UserControl
    {
        public PaneViewBase()
        {
            DataContextChanged += PaneViewBase_DataContextChanged;
        }

        private void PaneViewBase_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is PaneViewModelBase oldViewModel)
            {
                oldViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            }
            if (e.NewValue is PaneViewModelBase newViewModel)
            {
                newViewModel.PropertyChanged += ViewModel_PropertyChanged;
            }
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(PaneViewModelBase.IsActive) || DataContext is not PaneViewModelBase viewModel || (viewModel?.IsActive ?? true))
            {
                return;
            }

            ClearLogicalFocusInsidePane();

            // キーボードフォーカスが自分の中に残っている場合だけウィンドウへ移す。
            // 既に他のパネルへ移っている場合は、そのパネルのフォーカスを奪わない。
            if (Keyboard.FocusedElement is DependencyObject focusedElement && ReferenceEquals(FindOwnerPane(focusedElement), this))
            {
                Window.GetWindow(this)?.Focus();
            }
        }

        /// <summary>
        /// このパネルを含むフォーカススコープを外側へ順にたどり、論理フォーカスがこのパネルの中を指しているものを外す。
        /// キーボードフォーカスが別のスコープへ移っても、外側のスコープ (ウィンドウ) の論理フォーカスは古いまま残り、
        /// メニューを閉じたときなどにそこへフォーカスが戻されてしまうため。
        /// </summary>
        void ClearLogicalFocusInsidePane()
        {
            DependencyObject? current = this;
            while (current != null)
            {
                var scope = FocusManager.GetFocusScope(current);
                if (scope == null)
                {
                    break;
                }
                if (FocusManager.GetFocusedElement(scope) is DependencyObject focused && ReferenceEquals(FindOwnerPane(focused), this))
                {
                    FocusManager.SetFocusedElement(scope, null);
                }
                // GetFocusScope は要素自身がスコープならそれを返すため、親から探し直して外側のスコープへ進む
                current = GetParent(scope);
            }
        }

        static PaneViewBase? FindOwnerPane(DependencyObject element)
        {
            var current = element;
            while (current != null)
            {
                if (current is PaneViewBase pane)
                {
                    return pane;
                }
                current = GetParent(current);
            }
            return null;
        }

        static DependencyObject? GetParent(DependencyObject element)
        {
            // Visual でない要素 (Run など) やポップアップ内の要素は論理ツリーで親をたどる
            var parent = element is Visual or Visual3D ? VisualTreeHelper.GetParent(element) : null;
            return parent ?? LogicalTreeHelper.GetParent(element);
        }
    }
}
