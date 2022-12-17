using NiVE3.Event;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace NiVE3.Mvvm
{
    class PropertyPublisher<T> : INotifyPropertyChanged, IGenericWeakEventHandler<PropertyChangedEventArgs>
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public event EventHandler<PropertyChangedEventArgs>? EventRaised;

        private T? _value = default(T);
        public T? Value
        {
            get
            {
                return _value;
            }
            set
            {
                if (!EqualityComparer<T>.Default.Equals(_value, value))
                {
                    _value = value;
                    OnPropertyChanged(nameof(Value));
                }
            }
        }

        public PropertyPublisher(T? defaultValue)
        {
            _value = defaultValue;
        }

        public void ForceUpdateValue(T value)
        {
            _value = value;
            OnPropertyChanged(nameof(Value));
        }

        public PropertySubscriber<T?> Subscribe(Action<T?> handler)
        {
            var subscriber = new PropertySubscriber<T?>(this, handler);
            GenericWeakEventManager<PropertyPublisher<T?>, PropertyChangedEventArgs>.AddListener(this, subscriber);

            return subscriber;
        }

        void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            EventRaised?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    class PropertySubscriber<T> : IWeakEventListener, IDisposable
    {
        public PropertySubscriber(PropertyPublisher<T?> source, Action<T?> handler)
        {
            Source = source;
            Handler = handler;
        }

        public Action<T?> Handler { get; }

        public bool Disposed { get; private set; }

        PropertyPublisher<T?> Source { get; }

        public void Dispose()
        {
            Dispose(true);
        }

        protected void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                GenericWeakEventManager<PropertyPublisher<T?>, PropertyChangedEventArgs>.RemoveListener(Source, this);

                if (disposing)
                {
                    GC.SuppressFinalize(this);
                }
            }

            Disposed = true;
        }

        ~PropertySubscriber()
        {
            Dispose(false);
        }

        public bool ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
        {
            if (managerType == typeof(GenericWeakEventManager<PropertyPublisher<T?>, PropertyChangedEventArgs>))
            {
                Handler(Source.Value);
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
