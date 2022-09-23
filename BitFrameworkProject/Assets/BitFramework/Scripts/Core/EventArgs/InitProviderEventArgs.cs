namespace BitFramework.Core
{
    /// <summary>
    /// 表示将初始化的服务提供者
    /// </summary>
    public class InitProviderEventArgs : BitApplicationEventArgs
    {
        private readonly IServiceProvider provider;

        public InitProviderEventArgs(IServiceProvider provider, IApplication bitApplication) : base(bitApplication)
        {
            this.provider = provider;
        }

        /// <summary>
        /// 获取将初始化的服务提供者
        /// </summary>
        public IServiceProvider GetServiceProvider()
        {
            return provider;
        }
    }
}