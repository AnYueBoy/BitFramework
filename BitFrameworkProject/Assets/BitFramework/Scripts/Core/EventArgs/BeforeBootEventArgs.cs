namespace BitFramework.Core
{
    /// <summary>
    /// 它指示引导程序将被引导启动
    /// </summary>
    public class BeforeBootEventArgs : BitApplicationEventArgs
    {
        private IBootstrap[] bootstraps;

        public BeforeBootEventArgs(IBootstrap[] bootstraps, IApplication bitApplication) : base(bitApplication)
        {
            this.bootstraps = bootstraps;
        }

        /// <summary>
        /// 获取将引导的引导程序组
        /// </summary>
        public IBootstrap[] GetBootstraps()
        {
            return bootstraps;
        }

        /// <summary>
        /// 设置引导程序将替换旧的引导列表
        /// </summary>
        public void SetBootstraps(IBootstrap[] bootstraps)
        {
            this.bootstraps = bootstraps;
        }
    }
}