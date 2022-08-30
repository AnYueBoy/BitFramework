namespace BitFramework.Container
{
    /// <summary>
    /// IBindable 是所有可绑定数据的接口
    /// </summary>
    public interface IBindable
    {
        /// <summary>
        /// 服务名称
        /// </summary>
        string Service { get; }

        /// <summary>
        /// 服务所属的Container
        /// </summary>
        IContainer Container { get; }

        /// <summary>
        /// 从Container中解绑服务
        /// 如果服务是单例，那么被构建的单例会自动释放
        /// </summary>
        void Unbind();
    }

    /// <inheritdoc/>
    public interface IBindable<T> : IBindable where T : IBindable
    {
        /// <summary>
        /// 返回需求指定的服务的上下文数据
        /// </summary>
        /// <param name="service">指定的服务名称</param>
        /// <returns>上下文给定的关系</returns>
        IGivenData<T> Needs(string service);

        /// <inheritdoc cref="Needs"/>
        /// <typeparam name="TService">类型转换为服务名称</typeparam>
        IGivenData<T> Needs<TService>();
    }
}