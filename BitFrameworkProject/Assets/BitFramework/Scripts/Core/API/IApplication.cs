using BitFramework.Container;
using BitFramework.EventDispatcher;

namespace BitFramework.Core
{
    public interface IApplication : IContainer
    {
        /// <summary>
        /// 该值指示在主线程上
        /// </summary>
        bool IsMainThread { get; }

        DebugLevel DebugLevel { get; set; }

        /// <summary>
        /// 获取事件派发器
        /// </summary>
        IEventDispatcher GetDispatcher();

        /// <summary>
        /// 向应用程序注册服务提供者
        /// </summary>
        /// <param name="provider">服务提供者</param>
        /// <param name="force">True便是强制注册</param>
        void Register(IServiceProvider provider, bool force = false);

        /// <summary>
        /// 检查提供的服务提供者是否已经注册
        /// </summary>
        bool IsRegistered(IServiceProvider provider);

        /// <summary>
        /// 获取运行时id
        /// </summary>
        long GetRuntimeId();

        /// <summary>
        /// 终止IApplication
        /// </summary>
        void Terminate();
    }
}