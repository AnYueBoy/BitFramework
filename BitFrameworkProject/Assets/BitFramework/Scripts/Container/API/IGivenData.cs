using System;

namespace BitFramework.Container
{
    /// <summary>
    /// 指示上下文的给定关系
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IGivenData<T> where T : IBindable
    {
        /// <summary>
        /// 提供指定的服务
        /// </summary>
        /// <param name="service">服务名称或者别名</param>
        /// <returns>IBindData的实例</returns>
        T Given(string service);

        /// <inheritdoc cref="Given(string)"/> 
        T Given<TService>();

        /// <inheritdoc cref="Given(string)"/>
        /// <param name="closure">闭包返回给定的实例</param>
        T Given(Func<object> closure);
    }
}