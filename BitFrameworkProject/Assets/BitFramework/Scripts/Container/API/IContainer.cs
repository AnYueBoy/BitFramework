using System;
using System.Reflection;

namespace BitFramework.Container
{
    /// <summary>
    /// IContainer 是所有IOC容器类的实现接口
    /// </summary>
    public interface IContainer
    {
        /// <summary>
        /// 从容器中解析出给定的类型
        /// </summary>
        /// <param name="service">服务的名称或者别名</param>
        /// <returns>服务实例。如果服务无法解析将抛出异常</returns>
        object this[string service] { get; set; }

        /// <summary>
        /// 获得给定服务的绑定数据
        /// </summary>
        /// <param name="service">服务的名称或者别名</param>
        /// <returns>若没有绑定数据将返回null</returns>
        IBindData GetBind(string service);

        /// <summary>
        /// 给定服务是否已绑定
        /// </summary>
        /// <param name="service">服务的名称或者别名</param>
        /// <returns>服务已绑定则返回True</returns>
        bool HasBind(string service);

        /// <summary>
        /// 容器中是否存在现有实例
        /// </summary>
        /// <param name="service">服务的名称或者别名</param>
        /// <returns>如果实例存在则返回True</returns>
        bool HasInstance(string service);

        /// <summary>
        /// 服务是否已被解析
        /// </summary>
        /// <param name="service">服务的名称或者别名</param>
        /// <returns>如果服务被解析则返回True</returns>
        bool IsResolved(string service);

        /// <summary>
        /// 给定的服务是否可以构建
        /// </summary>
        /// <param name="service">服务的名称或者别名</param>
        /// <returns>给定的服务可以构建则返回True</returns>
        bool CanMake(string service);

        /// <summary>
        /// 给定服务是否为单例绑定。如果服务不存在，则为false
        /// </summary>
        /// <param name="service">服务的名称或者别名</param>
        /// <returns>如果服务是单例绑定，则为True</returns>
        bool IsStatic(string service);

        /// <summary>
        /// 给定名称是否为别名。
        /// </summary>
        /// <param name="name">给定的名称</param>
        /// <returns>如果给定的名称是别名则返回True</returns>
        bool IsAlias(string name);

        /// <summary>
        /// 向容器中注册绑定
        /// </summary>
        /// <param name="service">服务名称</param>
        /// <param name="concrete">服务类型</param>
        /// <param name="isStatic">服务是否为单例绑定</param>
        /// <returns>绑定数据</returns>
        IBindData Bind(string service, Type concrete, bool isStatic);

        /// <summary>
        /// 向容器中注册绑定
        /// </summary>
        /// <param name="service">服务名称</param>
        /// <param name="concrete">返回实例的闭包</param>
        /// <param name="isStatic">服务是否为单例绑定</param>
        /// <returns>绑定数据</returns>
        IBindData Bind(string service, Func<IContainer, object[], object> concrete, bool isStatic);

        /// <summary>
        /// 如果服务不存在，则向容器注册绑定。
        /// </summary>
        /// <param name="service">服务名称</param>
        /// <param name="concrete">返回实例的闭包</param>
        /// <param name="isStatic">服务是否为单例绑定</param>
        /// <param name="bindData">绑定数据</param>
        /// <returns>向服务容器中注册成功则返回True</returns>
        bool BindIf(string service, Func<IContainer, object[], object> concrete, bool isStatic, out IBindData bindData);

        /// <summary>
        /// 如果服务不存在，则向容器注册绑定。
        /// </summary>
        /// <param name="service">服务名称</param>
        /// <param name="concrete">服务类型</param>
        /// <param name="isStatic">服务是否为单例绑定</param>
        /// <param name="bindData">绑定数据</param>
        /// <returns>向服务容器中注册成功则返回True</returns>
        bool BindIf(string service, Type concrete, bool isStatic, out IBindData bindData);

        /// <summary>
        /// 向容器中注册方法
        /// </summary>
        /// <param name="method">方法名称</param>
        /// <param name="target">调用目标</param>
        /// <param name="called">要调用的方法信息</param>
        /// <returns>方法绑定数据</returns>
        IMethodBind BindMethod(string method, object target, MethodInfo called);

        /// <summary>
        /// 从服务容器中解绑方法
        /// </summary>
        /// <param name="target">
        /// The target.
        /// <para><code>string</code>将作为方法名称</para>
        /// <para><code>IMethodBind</code>将被视为给定的方法</para>
        /// <para>其他对象将作为调用目标</para>
        /// </param>
        void UnbindMethod(object target);

        /// <summary>
        /// 从服务容器中解绑服务
        /// </summary>
        /// <param name="service">服务名称或别名</param>
        void Unbind(string service);

        /// <summary>
        /// 为给定绑定分配一组标记。
        /// </summary>
        /// <param name="tag">标记名称</param>
        /// <param name="services">服务名称或别名的数组。</param>
        void Tag(string tag, params string[] services);

        /// <summary>
        /// 解析给定标记的所有绑定
        /// </summary>
        /// <param name="tag">标记名称</param>
        /// <returns>该标记下的所有服务</returns>
        object[] Tagged(string tag);

        /// <summary>
        /// 将现有实例注册为容器中的共享(单例)实例
        /// </summary>
        /// <param name="service">服务名称或别名</param>
        /// <param name="instance">服务实例</param>
        /// <returns>装饰器处理后的新实例</returns>
        object Instance(string service, object instance);

        /// <summary>
        /// 释放容器中的现有实例
        /// </summary>
        /// <param name="mixed">服务名称或别名或实例</param>
        /// <returns>如果现有实例被释放则返回True</returns>
        bool Release(object mixed);

        /// <summary>
        /// 刷新容器中所有绑定和解析的实例
        /// </summary>
        void Flush();

        /// <summary>
        /// 调用绑定容器中的方法并注入其依赖项。
        /// </summary>
        /// <param name="method">方法名称</param>
        /// <param name="userParams">用户参数</param>
        /// <returns>方法返回值</returns>
        object Invoke(string method, params object[] userParams);

        /// <summary>
        /// 调用给定方法并注入其依赖项。
        /// </summary>
        /// <param name="target">包含其调用方法的实例</param>
        /// <param name="methodInfo">方法名称</param>
        /// <param name="userParams">用户参数</param>
        /// <returns>方法返回值</returns>
        object Call(object target, MethodInfo methodInfo, params object[] userParams);

        /// <summary>
        /// 从容器中解析给定的服务或别名
        /// </summary>
        /// <param name="service">服务名称或别名</param>
        /// <param name="userParams">用户参数</param>
        /// <returns>服务实例。如果服务不能被解析，则抛出异常</returns>
        object Make(string service, params object[] userParams);

        /// <summary>
        /// 将服务别名为其他名称
        /// </summary>
        /// <param name="alias">服务别名</param>
        /// <param name="service">服务名称</param>
        /// <returns>容器实例</returns>
        IContainer Alias(string alias, string service);

        /// <summary>
        /// 在容器中扩展抽象类型
        /// <para>在服务解析期间允许配置或替换服务</para>
        /// </summary>
        /// <param name="service">服务名称或别名，如果应用于全局，则为空</param>
        /// <param name="closure">闭包返回替换的实例</param>
        void Extend(string service, Func<object, IContainer, object> closure);

        /// <summary>
        /// 注册重绑定服务的事件
        /// </summary>
        /// <param name="service">服务名称或别名</param>
        /// <param name="callback">回调</param>
        /// <returns></returns>
        IContainer OnRebound(string service, Action<object> callback);

        /// <summary>
        /// 注册解析回调
        /// </summary>
        /// <param name="closure">回调</param>
        /// <returns>容器实例</returns>
        IContainer OnResolving(Action<IBindData, object> closure);

        /// <summary>
        /// 注册解析后回调
        /// </summary>
        /// <param name="closure">回调</param>
        /// <returns>容器实例</returns>
        IContainer OnAfterResolving(Action<IBindData, object> closure);

        /// <summary>
        /// 注册释放回调
        /// </summary>
        /// <param name="closure">回调</param>
        /// <returns>容器实例</returns>
        IContainer OnRelease(Action<IBindData, object> closure);

        /// <summary>
        /// 在类型查找失败时调用注册时的回调
        /// </summary>
        /// <param name="func">回调函数</param>
        /// <param name="priority">优先级</param>
        /// <returns>容器实例</returns>
        IContainer OnFindType(Func<string, Type> func, int priority = int.MaxValue);

        /// <summary>
        /// 将给定类型转换为服务名称
        /// </summary>
        /// <param name="type">给定类型</param>
        /// <returns>服务名称</returns>
        string TypeConvertToService(Type type);
    }
}