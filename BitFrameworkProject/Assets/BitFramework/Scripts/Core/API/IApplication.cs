using BitFramework.Container;

namespace BitFramework.Core
{
    public interface IApplication : IContainer
    {
        /// <summary>
        /// 该值指示在主线程上
        /// </summary>
        bool IsMainThread { get; }
    }
}