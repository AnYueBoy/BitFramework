using BitFramework.Component.AssetsModule;
using BitFramework.Component.ObjectPoolModule;
using BitFramework.Core;

public static class Providers
{
    public static IServiceProvider[] ServiceProviders
    {
        get
        {
            return new IServiceProvider[]
            {
                // 项目中自定义的服务提供者
                new ProviderAssetsModule(),
                new ProviderObjectPoolModule(),
            };
        }
    }
}