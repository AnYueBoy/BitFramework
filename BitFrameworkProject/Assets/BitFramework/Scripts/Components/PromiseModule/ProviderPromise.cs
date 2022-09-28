using BitFramework.Core;

namespace BitFramework.PromiseModule
{
    public class ProviderPromise : IServiceProvider
    {
        public void Init()
        {
        }

        public void Register()
        {
            App.Singleton<IPromiseTimer, PromiseTimer>();
        }
    }
}