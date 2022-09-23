namespace BitFramework.Core
{
    /// <summary>
    /// 表示所有服务提供者的初始化函数已被调用
    /// </summary>
    public class AfterInitEventArgs : BitApplicationEventArgs
    {
        public AfterInitEventArgs(IApplication bitApplication) : base(bitApplication)
        {
        }
    }
}