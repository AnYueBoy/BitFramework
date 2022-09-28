using System;
using BitFramework.Core;
using IServiceProvider = BitFramework.Core.IServiceProvider;

namespace BitFramework.Component.AssetsModule
{
    public class ProviderAssetsModule : IServiceProvider
    {
        public void Init()
        {
        }

        public void Register()
        {
            App.Singleton<IAssetsManager, AssetsManager>();
        }
    }
}