namespace BitFramework.Core
{
    public class RegisterProviderEventArgs : BitApplicationEventArgs
    {
        private readonly IServiceProvider provider;
        public bool IsSkip { get; private set; }

        public bool IsPropagationStopped => IsSkip;

        public RegisterProviderEventArgs(IServiceProvider provider, IApplication bitApplication) : base(bitApplication)
        {
            IsSkip = false;
            this.provider = provider;
        }

        /// <summary>
        /// 获取将注册的服务提供者.
        /// </summary>
        public IServiceProvider GetServiceProvider()
        {
            return provider;
        }

        /// <summary>
        /// 跳过注册服务提供者
        /// </summary>
        public void Skip()
        {
            IsSkip = true;
        }
    }
}