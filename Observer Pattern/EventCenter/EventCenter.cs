using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CodeComplete.Observer_Pattern.EventCenter
{
    class EventCenter
    {
        private Dictionary<Type, Dictionary<string, Delegate>> eventTable;


        public void SendEvent(string name)
        {
            var action = GetDelegate(name, typeof(Action)) as Action;
            if (action != null)
                action();
        }

        public void SendEvent<T>(string name, T arg1)
        {
            var action = GetDelegate(name, typeof(Action<T>)) as Action<T>;
            if (action != null)
                action(arg1);
        }

        public void SendEvent<T, U>(string name, T arg1, U arg2)
        {
            var action = GetDelegate(name, typeof(Action<T, U>)) as Action<T, U>;
            if (action != null)
                action(arg1, arg2);
        }

        public void SendEvent<T, U, V>(string name, T arg1, U arg2, V arg3)
        {
            var action = GetDelegate(name, typeof(Action<T, U, V>)) as Action<T, U, V>;
            if (action != null)
                action(arg1, arg2, arg3);
        }

        private Delegate GetDelegate(string name, Type type)
        {
            Dictionary<string, Delegate> dictionary;
            Delegate delegate2;
            if (eventTable != null && eventTable.TryGetValue(type, out dictionary) && dictionary.TryGetValue(name, out delegate2))
                return delegate2;
            return null;
        }

        public void RegisterEvent(string name, Action handler)
        {
            RegisterEvent(name, (Delegate)handler);
        }

        public void RegisterEvent<T>(string name, Action<T> handler)
        {
            RegisterEvent(name, handler);
        }

        public void RegisterEvent<T, U>(string name, Action<T, U> handler)
        {
            RegisterEvent(name, handler);
        }

        public void RegisterEvent<T, U, V>(string name, Action<T, U, V> handler)
        {
            RegisterEvent(name, handler);
        }

        private void RegisterEvent(string name, Delegate handler)
        {
            Dictionary<string, Delegate> dictionary;
            Delegate handlers;
            if (eventTable == null)
                eventTable = new Dictionary<Type, Dictionary<string, Delegate>>();
            var type = handler.GetType();
            if (!eventTable.TryGetValue(type, out dictionary))
            {
                dictionary = new Dictionary<string, Delegate>();
                eventTable.Add(type, dictionary);
            }
            if (dictionary.TryGetValue(name, out handlers))
                dictionary[name] = Delegate.Combine(handlers, handler);
            else
                dictionary.Add(name, handler);
        }

        public void UnregisterEvent(string name, Action handler)
        {
            UnregisterEvent(name, (Delegate)handler);
        }

        public void UnregisterEvent<T>(string name, Action<T> handler)
        {
            UnregisterEvent(name, handler);
        }

        public void UnregisterEvent<T, U>(string name, Action<T, U> handler)
        {
            UnregisterEvent(name, handler);
        }

        public void UnregisterEvent<T, U, V>(string name, Action<T, U, V> handler)
        {
            UnregisterEvent(name, handler);
        }

        private void UnregisterEvent(string name, Delegate handler)
        {
            Dictionary<string, Delegate> dictionary;
            Delegate handlers;
            if (eventTable != null && eventTable.TryGetValue(handler.GetType(), out dictionary) &&
                dictionary.TryGetValue(name, out handlers))
                dictionary[name] = Delegate.Remove(handlers, handler);
        }
    }
}
