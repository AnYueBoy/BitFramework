using System;

namespace BitFramework.Container
{
    /// <summary>
    /// 表示与指定服务相关的关系数据。
    /// </summary>
    public interface IBindData : IBindable<IBindData>
    {
        /// <summary>
        /// 获取服务实例的委托
        /// </summary>
        Func<IContainer, object[], object> Concrete { get; }

        /// <summary>
        /// 获取一个值，该值指示服务为单例（静态）时是否为true。
        /// </summary>
        bool IsStatic { get; }

        /// <summary>
        /// 指定服务的别名
        /// </summary>
        IBindData Alias(string alias);

        /// <summary>
        /// 指定给定服务的Tag
        /// </summary>
        IBindData Tag(string tag);

        IBindData OnResolving(Action<IBindData, object> closure);

        IBindData OnAfterResolving(Action<IBindData, object> closure);

        IBindData OnRelease(Action<IBindData, object> closure);
    }
}