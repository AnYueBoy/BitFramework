namespace BitFramework.Core
{
    /// <summary>
    /// 表示框架将终止
    /// </summary>
    public class BeforeTerminateEventArgs : BitApplicationEventArgs
    {
        public BeforeTerminateEventArgs(IApplication bitApplication) : base(bitApplication)
        {
        }
    }
}