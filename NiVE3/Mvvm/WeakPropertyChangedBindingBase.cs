using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.Mvvm
{
    class WeakPropertyChangedBindingBase : INotifyPropertyChanged
    {
        WeakEventPublisher<PropertyChangedEventArgs> PropertyChangedPublisher { get; } = new WeakEventPublisher<PropertyChangedEventArgs>();
        public event PropertyChangedEventHandler? PropertyChanged
        {
            add
            {
                if (value != null)
                {
                    PropertyChangedPublisher.Subscribe(value.Method.CreateDelegate<EventHandler<PropertyChangedEventArgs>>(value.Target));
                }
            }
            remove
            {
                if (value != null)
                {
                    PropertyChangedPublisher.Unsubscribe(value.Method.CreateDelegate<EventHandler<PropertyChangedEventArgs>>(value.Target));
                }
            }
        }

        // NOTE: Prism.Mvvm.BindableBase 合わせ
        protected virtual bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
        {
            if (!EqualityComparer<T>.Default.Equals(storage, value))
            {
                storage = value;
                RaisePropertyChanged(propertyName);
                return true;
            }
            else
            {
                return false;
            }
        }

        protected void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        protected void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChangedPublisher.Publish(this, e);
        }
    }
}
