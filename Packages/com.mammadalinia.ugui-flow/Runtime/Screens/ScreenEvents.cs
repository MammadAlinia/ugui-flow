using System;
using System.Collections.Generic;

namespace UguiFlow
{
    /// <summary>Synchronous, typed messages shared by one mounted screen on the Unity main thread.</summary>
    public sealed class ScreenEvents
    {
        private readonly Dictionary<Type, List<IDisposable>> subscriptions = new();
        private bool closed;
        internal static ScreenEvents Closed { get; } = new ScreenEvents { closed = true };

        internal ScreenEvents() { }

        public IDisposable Subscribe<T>(UIComponent owner, Action<T> listener)
        {
            if (closed || owner == null || owner.Events != this || listener == null)
                return EmptySubscription.Instance;

            if (!subscriptions.TryGetValue(typeof(T), out var listeners))
            {
                listeners = new List<IDisposable>();
                subscriptions.Add(typeof(T), listeners);
            }

            var subscription = new Subscription<T>(listeners, owner, listener);
            listeners.Add(subscription);
            return subscription;
        }

        public void Publish<T>(T message)
        {
            if (!subscriptions.TryGetValue(typeof(T), out var listeners))
                return;

            // A listener can subscribe, unsubscribe, or close the screen during delivery.
            foreach (var subscription in listeners.ToArray())
                ((Subscription<T>)subscription).Deliver(message);
        }

        internal void Clear()
        {
            closed = true;
            foreach (var listeners in subscriptions.Values)
            {
                for (int i = listeners.Count - 1; i >= 0; i--)
                    listeners[i].Dispose();
            }

            subscriptions.Clear();
        }

        private sealed class Subscription<T> : IDisposable
        {
            private List<IDisposable> listeners;
            private UIComponent owner;
            private Action<T> listener;

            public Subscription(List<IDisposable> listeners, UIComponent owner, Action<T> listener)
            {
                this.listeners = listeners;
                this.owner = owner;
                this.listener = listener;
            }

            internal void Deliver(T message)
            {
                UIComponent recipient = owner;
                if (recipient == null || !recipient.CanReceiveEvents)
                    return;

                try
                {
                    listener?.Invoke(message);
                }
                catch (Exception)
                {
                    if (recipient != null)
                        recipient.ShowError();
                }
            }

            public void Dispose()
            {
                listeners?.Remove(this);
                listeners = null;
                owner = null;
                listener = null;
            }
        }

        private sealed class EmptySubscription : IDisposable
        {
            internal static readonly EmptySubscription Instance = new();
            public void Dispose() { }
        }
    }
}
