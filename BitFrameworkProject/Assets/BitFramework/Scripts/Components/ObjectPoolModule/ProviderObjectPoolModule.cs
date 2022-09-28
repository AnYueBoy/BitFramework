using BitFramework.Core;

namespace BitFramework.Component.ObjectPoolModule
{
    public class ProviderObjectPoolModule : IServiceProvider
    {
        public void Init()
        {
        }

        public void Register()
        {
            App.Singleton<IObjectPool, ObjectPool>();
        }
    }
}