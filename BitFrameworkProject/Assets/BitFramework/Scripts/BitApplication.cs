using System;
using System.Collections;
using System.Collections.Generic;
using BitFramework.Core;
using BitFramework.EventDispatcher;
using IServiceProvider = BitFramework.Core.IServiceProvider;

namespace BitFramework.Scripts
{
    public class BitApplication : Container.Container, IApplication
    {
        private static string version;
        private readonly IList<IServiceProvider> loadedProviders;
        private readonly int mainThreadId;
        private readonly IDictionary<Type, string> dispatchMapping;
        private bool bootstrapped;
        private bool inited;
        private bool registering;
        private long incrementId;
        private DebugLevel debugLevel;
        private IEventDispatcher dispatcher;
        
        public bool IsMainThread { get; }
        public DebugLevel DebugLevel { get; set; }

        public IEventDispatcher GetDispatcher()
        {
            throw new System.NotImplementedException();
        }

        public void Register(IServiceProvider provider, bool force = false)
        {
            throw new System.NotImplementedException();
        }

        public bool IsRegistered(IServiceProvider provider)
        {
            throw new System.NotImplementedException();
        }

        public long GetRuntimeId()
        {
            throw new System.NotImplementedException();
        }

        public void Terminate()
        {
            throw new System.NotImplementedException();
        }
    }
}