using System;
using BitFramework.EventDispatcher;

namespace BitFramework.Core
{
    /// <summary>
    /// 表示应用程序事件
    /// </summary>
    public class BitApplicationEventArgs : EventParam 
    {
        public IApplication BitApplication { get; }

        public BitApplicationEventArgs(IApplication bitApplication)
        {
            BitApplication = bitApplication;
        }
    }
}