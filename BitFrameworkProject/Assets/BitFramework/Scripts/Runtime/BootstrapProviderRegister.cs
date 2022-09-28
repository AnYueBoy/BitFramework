using System.Collections.Generic;
using BitFramework.Core;
using UComponent = UnityEngine.Component;

namespace BitFramework.Runtime
{
    /// <summary>
    /// 注册Unity组件的服务提供者以及非Unity组件的服务提供者
    /// </summary>
    public sealed class BootstrapProviderRegister : IBootstrap
    {
        private readonly IServiceProvider[] providers;
        private readonly UComponent component;

        public BootstrapProviderRegister(UComponent component, IServiceProvider[] serviceProviders)
        {
            providers = serviceProviders;
            this.component = component;
        }

        public void Bootstrap()
        {
            LoadUnityComponentProvider();
            RegisterProviders(providers);
        }

        private void LoadUnityComponentProvider()
        {
            if (!component)
            {
                return;
            }

            RegisterProviders(component.GetComponentsInChildren<IServiceProvider>());
        }

        private static void RegisterProviders(IEnumerable<IServiceProvider> providers)
        {
            foreach (var provider in providers)
            {
                if (provider == null)
                {
                    continue;
                }

                if (!App.IsRegistered(provider))
                {
                    App.Register(provider);
                }
            }
        }
    }
}