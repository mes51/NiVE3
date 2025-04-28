using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.Mvvm
{
    class WeakEventPublisher<T> where T : EventArgs
    {
        List<WeakEventHandler> Handlers { get; } = [];

        public void Subscribe(EventHandler<T> handler)
        {
            Handlers.Add(new WeakEventHandler(handler));
        }

        public void Unsubscribe(EventHandler<T> handler)
        {
            foreach (var h in Handlers.ToArray())
            {
                if (!h.IsAlive || h.IsSameHandler(handler))
                {
                    Handlers.Remove(h);
                }
            }
        }

        public void Publish(object? sender, T args)
        {
            foreach (var h in Handlers.ToArray())
            {
                h.TryGetHandler()?.Invoke(sender, args);
            }
        }

        private class WeakEventHandler
        {
            public bool IsAlive => Receiver.IsAlive;

            WeakReference Receiver { get; }

            MethodInfo Method { get; }

            public WeakEventHandler(EventHandler<T> handler)
            {
                Receiver = new WeakReference(handler.Target);
                Method = handler.Method;
            }

            public EventHandler<T>? TryGetHandler()
            {
                if (Method.IsStatic)
                {
                    return Method.CreateDelegate<EventHandler<T>>();
                }
                else if (Receiver.Target is object target)
                {
                    return Method.CreateDelegate<EventHandler<T>>(target);
                }
                else
                {
                    return null;
                }
            }

            public bool IsSameHandler(EventHandler<T> handler)
            {
                return Receiver.Target == handler.Target && Method == handler.Method;
            }
        }
    }
}
