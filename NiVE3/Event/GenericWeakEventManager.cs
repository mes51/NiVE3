using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace NiVE3.Event
{
    internal class GenericWeakEventManager<TSource, TEventArgs> : WeakEventManager where TEventArgs : EventArgs where TSource : IGenericWeakEventHandler<TEventArgs>
    {
        static GenericWeakEventManager<TSource, TEventArgs> Manager { get; }

        static GenericWeakEventManager()
        {
            Manager = new GenericWeakEventManager<TSource, TEventArgs>();
            SetCurrentManager(typeof(GenericWeakEventManager<TSource, TEventArgs>), Manager);
        }

        protected override void StartListening(object source)
        {
            if (source is TSource handler)
            {
                handler.EventRaised += GenericWeakEventHandler_EventRaised;
            }
        }

        protected override void StopListening(object source)
        {
            if (source is TSource handler)
            {
                handler.EventRaised -= GenericWeakEventHandler_EventRaised;
            }
        }

        private void GenericWeakEventHandler_EventRaised(object? sender, TEventArgs e)
        {
            DeliverEvent(sender, e);
        }

        public static void AddListener(TSource source, IWeakEventListener listener)
        {
            Manager.ProtectedAddListener(source, listener);
        }

        public static void RemoveListener(TSource source, IWeakEventListener listener)
        {
            Manager.ProtectedRemoveListener(source, listener);
        }

        public static void RemoveAllListener(TSource source)
        {
            Manager.Remove(source);
        }
    }

    internal interface IGenericWeakEventHandler<TEventArgs> where TEventArgs : EventArgs
    {
        event EventHandler<TEventArgs> EventRaised;
    }
}
