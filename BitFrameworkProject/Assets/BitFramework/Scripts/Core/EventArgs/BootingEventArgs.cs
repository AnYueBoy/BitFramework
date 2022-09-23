namespace BitFramework.Core
{
    public class BootingEventArgs : BitApplicationEventArgs
    {
        private readonly IBootstrap bootstrap;

        /// <summary>
        /// 此值表示该引导程序是否跳过引导
        /// </summary>
        public bool IsSkip { get; private set; }

        public bool IsPropagationStopped => IsSkip;

        public BootingEventArgs(IBootstrap bootstrap, IApplication bitApplication) : base(bitApplication)
        {
            IsSkip = false;
            this.bootstrap = bootstrap;
        }

        /// <summary>
        /// 获取正在引导的引导程序
        /// </summary>
        public IBootstrap GetBootstrap()
        {
            return bootstrap;
        }

        public void Skip()
        {
            IsSkip = true;
        }
    }
}