namespace BitFramework.Core
{
    /// <summary>
    /// 表示框架将终止
    /// </summary>
    public class AfterTerminateEventArgs : BitApplicationEventArgs
    {
        public AfterTerminateEventArgs(IApplication bitApplication) : base(bitApplication)
        {
        }
    }
}