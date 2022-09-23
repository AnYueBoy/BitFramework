namespace BitFramework.Core
{
    /// <summary>
    /// 框架启动进程类型
    /// </summary>
    public enum StartProcess
    {
        Construct,

        Bootstrap,

        Bootstrapping,

        Bootstraped,

        Init,

        Initing,

        Inited,

        Running,

        Terminating,

        Terminated
    }
}