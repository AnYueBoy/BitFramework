namespace BitFramework.Core
{
    /// <summary>
    /// 表示将调用服务提供者的初始化函数
    /// </summary>
    public class BeforeInitEventArgs : BitApplicationEventArgs
    {
        public BeforeInitEventArgs(IApplication bitApplication) : base(bitApplication)
        {
        }
    }
}