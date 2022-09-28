using System;
using System.Collections.Generic;
using UnityEngine;
using SException = System.Exception;

namespace BitFramework.EventDispatcher
{
    public class EventDispatcher : IEventDispatcher
    {
        private readonly Dictionary<string, IList<EventHandler<EventParam>>> handlerDic;

        public EventDispatcher()
        {
            handlerDic = new Dictionary<string, IList<EventHandler<EventParam>>>();
        }

        public void AddListener(string eventName, EventHandler<EventParam> handler)
        {
            if (string.IsNullOrEmpty(eventName) || handler == null)
            {
                throw new SException("eventName or handler is null.");
            }

            if (!handlerDic.TryGetValue(eventName, out var handlersList))
            {
                handlerDic[eventName] = handlersList = new List<EventHandler<EventParam>>();
            }

            if (handlersList.Contains(handler))
            {
                throw new SException(
                    $"repeat event handler been added. eventName: {eventName}, handler {handler.GetType().Name}");
            }

            handlersList.Add(handler);
        }

        public void RemoveListener(string eventName, EventHandler<EventParam> handler = null)
        {
            if (handler == null)
            {
                handlerDic.Remove(eventName);
                return;
            }

            if (!handlerDic.TryGetValue(eventName, out var handlers))
            {
                throw new SException($"will remove event not exist. eventName: {eventName}");
            }

            if (!handlers.Contains(handler))
            {
                throw new SException($"will remove event handler not exist. handler: {handler.GetType().Name}");
            }

            handlers.Remove(handler);
            if (handlers.Count <= 0)
            {
                handlerDic.Remove(eventName);
            }
        }

        public void Raise(string eventName, object sender, EventParam e = null)
        {
            if (!handlerDic.TryGetValue(eventName, out var handlers))
            {
#if UNITY_EDITOR
                Debug.LogWarning($"{eventName} no listener.");
                return;
#else
                throw new SException($"raise event name not exist. eventName:{eventName}");
#endif
            }

            e = e ?? new EventParam();
            foreach (EventHandler<EventParam> handler in handlers)
            {
                handler.Invoke(sender, e);
            }
        }
    }


    public class EventParam : EventArgs
    {
        private object[] data;

        public EventParam(params object[] data)
        {
            this.data = data;
        }
    }
}