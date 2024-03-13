using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace NiVE3.View.Resource
{
    // NOTE: VisualTree上に存在できるようにFrameworkElementとする
    // TODO: 他にイベントを作る以外でReadOnlyなDependencyPropertyをBindする方法が無いか探す
    internal class ReadOnlyPropertyWire : FrameworkElement
    {
        public static readonly DependencyProperty RecieverProperty = DependencyProperty.Register(
            nameof(Reciever),
            typeof(object),
            typeof(ReadOnlyPropertyWire),
            new PropertyMetadata(null, RecieverPropertyChanged)
        );

        public static readonly DependencyProperty SenderProperty = DependencyProperty.Register(
            nameof(Sender),
            typeof(object),
            typeof(ReadOnlyPropertyWire),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault)
        );

        public object? Sender
        {
            get { return GetValue(SenderProperty); }
            set { SetValue(SenderProperty, value); }
        }

        public object? Reciever
        {
            get { return GetValue(RecieverProperty); }
            set { SetValue(RecieverProperty, value); }
        }

        static ReadOnlyPropertyWire()
        {
            VisibilityProperty.OverrideMetadata(typeof(ReadOnlyPropertyWire), new FrameworkPropertyMetadata(Visibility.Collapsed, VisibilityChanged));
        }

        static void RecieverPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ReadOnlyPropertyWire ro)
            {
                ro.Sender = ro.Reciever;
            }
        }

        static void VisibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ReadOnlyPropertyWire ro && ro.Visibility != Visibility.Collapsed)
            {
                // Visibilityを変えて表示しようとしたらエラーとする
                throw new InvalidOperationException();
            }
        }
    }
}
