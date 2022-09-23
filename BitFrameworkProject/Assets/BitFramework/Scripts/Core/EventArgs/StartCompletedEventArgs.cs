namespace BitFramework.Core
{
    /// <summary>
    /// 指示框架就绪
    /// </summary>
    public class StartCompletedEventArgs : BitApplicationEventArgs
    {
        public StartCompletedEventArgs(IApplication bitApplication) : base(bitApplication)
        {
        }
    }
}