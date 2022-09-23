using System;

namespace BitFramework.Core
{
    /// <summary>
    /// 表示应用程序事件
    /// </summary>
    public class BitApplicationEventArgs : EventArgs
    {
        public IApplication BitApplication { get; }

        public BitApplicationEventArgs(IApplication bitApplication)
        {
            BitApplication = bitApplication;
        }
    }
}