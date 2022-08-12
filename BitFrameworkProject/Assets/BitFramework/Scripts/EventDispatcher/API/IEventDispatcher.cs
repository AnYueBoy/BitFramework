using System;

namespace BitFramework.EventDispatcher
{
    public interface IEventDispatcher
    {
        void AddListener(string eventName, EventHandler<EventParam> handler);

        void RemoveListener(string eventName, EventHandler<EventParam> handler = null);

        void Raise(string eventName, object sender, EventParam e = null);
    }
}