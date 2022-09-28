using BitFramework.Core;
using UnityEngine;
using BitFramework.Runtime;

public static class Bootstraps
{
    public static IBootstrap[] GetBootstraps(Component component)
    {
        return new IBootstrap[]
        {
            new BootstrapProviderRegister(component, Providers.ServiceProviders),
            // TODO: 其他引导程序
        };
    }
}