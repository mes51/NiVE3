using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace NiVE3.UI.Resources
{
    public class CollectionChangeNotifier : Freezable, INotifyPropertyChanged, INotifyCollectionChanged, IEnumerable
    {
        public static readonly DependencyProperty CollectionProperty = DependencyProperty.Register(
            nameof(Collection),
            typeof(INotifyCollectionChanged),
            typeof(CollectionChangeNotifier),
            new PropertyMetadata(null, CollectionChangedHandler)
        );

        public INotifyCollectionChanged? Collection
        {
            get { return (INotifyCollectionChanged)GetValue(CollectionProperty); }
            set { SetValue(CollectionProperty, value); }
        }

        public int Update { get; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public event NotifyCollectionChangedEventHandler? CollectionChanged;

        protected override Freezable CreateInstanceCore()
        {
            return new CollectionChangeNotifier();
        }

        private void Collection_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Collection)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Update)));
            CollectionChanged?.Invoke(this, e);
        }

        static void CollectionChangedHandler(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CollectionChangeNotifier notifier)
            {
                if (e.OldValue is INotifyCollectionChanged oldCollection)
                {
                    oldCollection.CollectionChanged -= notifier.Collection_CollectionChanged;
                }
                if (e.NewValue is INotifyCollectionChanged newCollection)
                {
                    newCollection.CollectionChanged += notifier.Collection_CollectionChanged;
                }
            }
        }

        public IEnumerator GetEnumerator()
        {
            if (Collection is IEnumerable collection)
            {
                return collection.GetEnumerator();
            }
            else
            {
                return Array.Empty<object>().GetEnumerator();
            }
        }
    }
}
