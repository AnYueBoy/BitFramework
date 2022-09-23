namespace BitFramework.Core
{
    /// <summary>
    /// 表示引导已启动
    /// </summary>
    public class AfterBootEventArgs : BitApplicationEventArgs
    {
        public AfterBootEventArgs(IApplication bitApplication) : base(bitApplication)
        {
        }
    }
}