using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using BitFramework.Exception;
using BitFramework.Util;
using SException = System.Exception;

namespace BitFramework.Container
{
    public class Container : IContainer
    {
        /// <summary>
        /// 禁止字符
        /// </summary>
        private static readonly char[] ServiceBanChars = {'@', ':', '$'};

        // 服务-绑定数据映射
        // 为何不直接使用Type作为key，因为框架中提供了别名逻辑，通过别名反映射到Type Name后，通过反射获取对应的Type
        private readonly Dictionary<string, BindData> bindings;

        // 服务-单例映射
        private readonly Dictionary<string, object> instances;

        // 单例-服务反向映射
        private readonly Dictionary<object, string> instancesReverse;

        // 别名-服务映射
        private readonly Dictionary<string, string> aliases;

        // 服务-别名列表反映射
        private readonly Dictionary<string, List<string>> aliasesReverse;

        // 服务-tag列表映射
        private readonly Dictionary<string, List<string>> tags;

        // 全局解析的回调列表
        private readonly List<Action<IBindData, object>> resolving;

        // 全局解析后的回调列表
        private readonly List<Action<IBindData, object>> afterResolving;

        // 全局释放的回调列表
        private readonly List<Action<IBindData, object>> release;

        // 服务的扩展闭包
        private readonly Dictionary<string, List<Func<object, IContainer, object>>> extenders;

        // 将字符转换成服务类型
        private readonly SortSet<Func<string, Type>, int> findType;

        // 已找到的类型缓存
        private readonly Dictionary<string, Type> findTypeCache;

        // 已解析的服务的哈希集
        private readonly HashSet<string> resolved;

        // 单例服务构建时间列表
        private readonly SortSet<string, int> instanceTiming;

        // 所有已注册的回弹回调。
        private readonly Dictionary<string, List<Action<object>>> rebound;

        // 方法的ioc容器
        private readonly MethodContainer methodContainer;

        // 表示跳过的对象以跳过某些依赖项注入
        private readonly object skipped;

        // 容器是否正在刷新
        private bool flushing;

        // 唯一Id用于标记全局生成顺序
        private int instanceId;

        /// <summary>
        /// 获取当前正在生成的具体化的堆栈
        /// </summary>
        protected Stack<string> BuildStack { get; }

        /// <summary>
        /// 获取正在生成的用户参数的堆栈
        /// </summary>
        protected Stack<object[]> UserParamsStack { get; }

        public Container(int prime = 64)
        {
            prime = Math.Max(8, prime);
            tags = new Dictionary<string, List<string>>((int) (prime * 0.25));
            aliases = new Dictionary<string, string>(prime * 4);
            aliasesReverse = new Dictionary<string, List<string>>(prime * 4);
            instances = new Dictionary<string, object>(prime * 4);
            instancesReverse = new Dictionary<object, string>(prime * 4);
            bindings = new Dictionary<string, BindData>(prime * 4);
            resolving = new List<Action<IBindData, object>>((int) (prime * 0.25));
            afterResolving = new List<Action<IBindData, object>>((int) (prime * 0.25));
            release = new List<Action<IBindData, object>>((int) (prime * 0.25));
            extenders = new Dictionary<string, List<Func<object, IContainer, object>>>((int) (prime * 0.25));
            resolved = new HashSet<string>();
            findType = new SortSet<Func<string, Type>, int>();
            findTypeCache = new Dictionary<string, Type>(prime * 4);
            rebound = new Dictionary<string, List<Action<object>>>(prime);
            instanceTiming = new SortSet<string, int>();
            BuildStack = new Stack<string>(32);
            UserParamsStack = new Stack<object[]>(32);
            methodContainer = new MethodContainer(this);
            flushing = false;
            instanceId = 0;
        }

        public object this[string service]
        {
            get => Make(service);
            set
            {
                GetBind(service)?.Unbind();
                Bind(service, (container, args) => value, false);
            }
        }

        #region Build

        public object Make(string service, params object[] userParams)
        {
            GuardConstruct(nameof(Make));
            return Resolve(service, userParams);
        }

        protected object Resolve(string service, params object[] userParams)
        {
            Guard.ParameterNotNull(service);

            service = AliasToService(service);
            if (instances.TryGetValue(service, out object instance))
            {
                return instance;
            }

            if (BuildStack.Contains(service))
            {
                throw MakeCircularDependencyException(service);
            }

            BuildStack.Push(service);
            UserParamsStack.Push(userParams);

            try
            {
                var bindData = GetBindFillable(service);

                // 开始构建服务的实例并尝试进行依赖注入
                instance = Build(bindData, userParams);

                // 如果我们为指定的服务定义了扩展程序，那么我们需要依次执行扩展程序，并允许扩展程序修改或覆盖原始服务
                instance = Extend(service, instance);

                instance = bindData.IsStatic
                    ? Instance(bindData.Service, instance)
                    : TriggerOnResolving(bindData, instance);

                resolved.Add(bindData.Service);
                return instance;
            }
            finally
            {
                UserParamsStack.Pop();
                BuildStack.Pop();
            }
        }

        /// <summary>
        /// 构建指定服务
        /// </summary>
        protected virtual object Build(BindData makeServiceBindData, object[] userParams)
        {
            var instance = makeServiceBindData.Concrete != null
                ? makeServiceBindData.Concrete(this, userParams)
                : CreateInstance(makeServiceBindData, SpeculatedServiceType(makeServiceBindData.Service), userParams);
            return Inject(makeServiceBindData, instance);
        }

        protected virtual object CreateInstance(Bindable makeServiceBindData, Type makeServiceType, object[] userParams)
        {
            if (IsUnableType(makeServiceType))
            {
                return null;
            }

            // 该函数会选取合适的构造函数并返回符合的参数列表
            userParams = GetConstructorsInjectParams(makeServiceBindData, makeServiceType, userParams);
            try
            {
                // userParams通过合适的构造函数并返回符合的参数列表后通过反射创建实例
                return CreateInstance(makeServiceType, userParams);
            }
            catch (SException e)
            {
                throw MakeBuildFailedException(makeServiceBindData.Service, makeServiceType, e);
            }
        }

        protected virtual object CreateInstance(Type makeServiceType, object[] userParams)
        {
            //如果参数不存在，则在反射时无需写入参数 可获得更好的性能
            if (userParams == null || userParams.Length <= 0)
            {
                return Activator.CreateInstance(makeServiceType);
            }

            return Activator.CreateInstance(makeServiceType, userParams);
        }

        /// <summary>
        /// 通过闭包获取实例
        /// </summary>
        protected virtual bool MakeFromContextualClosure(Func<object> closure, Type needType, out object output)
        {
            output = null;
            if (closure == null)
            {
                return false;
            }

            output = closure();
            return ChangeInstanceType(ref output, needType);
        }

        /// <summary>
        /// 通过服务名城获取实例
        /// </summary>
        protected virtual bool MakeFromContextualService(string service, Type needType, out object output)
        {
            output = null;
            if (!CanMake(service))
            {
                return false;
            }

            output = Make(service);
            return ChangeInstanceType(ref output, needType);
        }

        /// <summary>
        /// 基于上下文获取构建闭包
        /// </summary>
        protected virtual Func<object> GetContextualClosure(Bindable makeServiceBindData, string service,
            string paramName)
        {
            return makeServiceBindData.GetContextualClosure(service) ??
                   makeServiceBindData.GetContextualClosure($"${paramName}");
        }

        /// <summary>
        /// 根据上下文获取构建的服务
        /// </summary>
        protected virtual string GetContextualService(Bindable makeServiceBindData, string service, string paramName)
        {
            return makeServiceBindData.GetContextual(service) ?? makeServiceBindData.GetContextual($"${paramName}") ??
                service;
        }

        /// <summary>
        /// 根据上下文关系解析指定的服务
        /// </summary>
        /// <param name="makeServiceBindData"></param>
        /// <param name="service"></param>
        /// <param name="paramName">依赖项的属性名称参数</param>
        /// <param name="paramType">依赖项的属性类型参数</param>
        /// <param name="output">依赖的实例</param>
        /// <returns>如果生成依赖项实例成功，则为True,否则为false</returns>
        protected virtual bool ResolveFromContextual(Bindable makeServiceBindData, string service, string paramName,
            Type paramType, out object output)
        {
            var closure = GetContextualClosure(makeServiceBindData, service, paramName);
            if (MakeFromContextualClosure(closure, paramType, out output))
            {
                return true;
            }

            var buildService = GetContextualService(makeServiceBindData, service, paramName);
            return MakeFromContextualService(buildService, paramType, out output);
        }

        /// <summary>
        /// 解析引用类型的属性选择器
        /// </summary>
        protected virtual object ResolveAttrClass(Bindable makeServiceBindData, string service, PropertyInfo baseParam)
        {
            if (ResolveFromContextual(makeServiceBindData, service, baseParam.Name, baseParam.PropertyType,
                out object instance))
            {
                return instance;
            }

            // 检索应用于指定成员的指定类型的自定义特性。
            var inject = (InjectAttribute) baseParam.GetCustomAttribute(typeof(InjectAttribute));
            if (inject != null && !inject.Required)
            {
                return skipped;
            }

            throw MakeUnresolvableException(baseParam.Name, baseParam.DeclaringType);
        }

        /// <summary>
        /// 解析基元类型的属性选择器
        /// </summary>
        protected virtual object ResolveAttrPrimitive(Bindable makeServiceBindData, string service,
            PropertyInfo baseParam)
        {
            if (ResolveFromContextual(makeServiceBindData, service, baseParam.Name, baseParam.PropertyType,
                out object instance))
            {
                return instance;
            }

            if (baseParam.PropertyType.IsGenericType &&
                baseParam.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                return null;
            }

            var inject = (InjectAttribute) baseParam.GetCustomAttribute(typeof(InjectAttribute));
            if (inject != null && !inject.Required)
            {
                return skipped;
            }

            throw MakeUnresolvableException(baseParam.Name, baseParam.DeclaringType);
        }

        /// <summary>
        /// 解析引用类型的构造函数
        /// </summary>
        protected virtual object ResolveClass(Bindable makeServiceBindData, string service, ParameterInfo baseParam)
        {
            if (ResolveFromContextual(makeServiceBindData, service, baseParam.Name, baseParam.ParameterType,
                out object instance))
            {
                return instance;
            }

            // 该值指示该参数是否可选
            if (baseParam.IsOptional)
            {
                return baseParam.DefaultValue;
            }

            // baseParam.Member可能为空，并且可能在某些底层开发覆盖ParameterInfo类时发生
            throw MakeUnresolvableException(baseParam.Name, baseParam.Member?.DeclaringType);
        }

        /// <summary>
        /// 解析基元类型的构造函数
        /// </summary>
        protected virtual object ResolvePrimitive(Bindable makeServiceBindData, string service, ParameterInfo baseParam)
        {
            if (ResolveFromContextual(makeServiceBindData, service, baseParam.Name, baseParam.ParameterType,
                out object instance))
            {
                return instance;
            }

            if (baseParam.IsOptional)
            {
                return baseParam.DefaultValue;
            }

            if (baseParam.ParameterType.IsGenericType &&
                baseParam.ParameterType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                return null;
            }

            throw MakeUnresolvableException(baseParam.Name, baseParam.Member?.DeclaringType);
        }

        #endregion

        #region Dependency Inject

        /// <summary>
        /// 为指定实例进行依赖注入
        /// </summary>
        /// <param name="bindable"></param>
        /// <param name="instance"></param>
        /// <returns></returns>
        private object Inject(Bindable bindable, object instance)
        {
            GuardResolveInstance(instance, bindable.Service);
            AttributeInject(bindable, instance);
            return instance;
        }

        /// <summary>
        /// 属性选择器的依赖项注入
        /// </summary>
        protected virtual void AttributeInject(Bindable makeServiceBindData, object makeServiceInstance)
        {
            if (makeServiceInstance == null)
            {
                return;
            }

            var properties = makeServiceInstance.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var property in properties)
            {
                // property.IsDefined => 如果对此成员应用了 attributeType 属性则返回true 
                if (!property.CanWrite || !property.IsDefined(typeof(InjectAttribute), false))
                {
                    continue;
                }

                var needService = GetPropertyNeedsService(property);

                object instance;
                if (property.PropertyType.IsClass || property.PropertyType.IsInterface)
                {
                    instance = ResolveAttrClass(makeServiceBindData, needService, property);
                }
                else
                {
                    instance = ResolveAttrPrimitive(makeServiceBindData, needService, property);
                }

                // 确定两个实例是否为相同的实例
                if (ReferenceEquals(instance, skipped))
                {
                    continue;
                }

                if (!CanInject(property.PropertyType, instance))
                {
                    throw new UnresolvableException(
                        $"[{makeServiceBindData.Service}]({makeServiceInstance.GetType()}) Attr inject type must be [{property.PropertyType}], " +
                        $"But instance is [{instance?.GetType()}], Make service is [{needService}].");
                }

                // 通过反射设置属性值
                property.SetValue(makeServiceInstance, instance, null);
            }
        }

        /// <summary>
        /// 选取合适的构造函数并获取符合的参数数组
        /// </summary>
        protected virtual object[] GetConstructorsInjectParams(Bindable makerServiceBindData, Type makerServiceType,
            object[] userParams)
        {
            var constructors = makerServiceType.GetConstructors();
            if (constructors.Length <= 0)
            {
                return Array.Empty<object>();
            }

            ExceptionDispatchInfo exceptionDispatchInfo = null;
            foreach (var constructor in constructors)
            {
                try
                {
                    return GetDependencies(makerServiceBindData, constructor.GetParameters(), userParams);
                }
                catch (SException e)
                {
                    if (exceptionDispatchInfo == null)
                    {
                        exceptionDispatchInfo = ExceptionDispatchInfo.Capture(e);
                    }
                }
            }

            exceptionDispatchInfo?.Throw();
            throw new AssertException("Exception dispatch info is null.");
        }

        /// <summary>
        /// 获取要解析的实例的依赖参数的参数列表
        /// </summary>
        protected internal virtual object[] GetDependencies(Bindable makeServiceBindData, ParameterInfo[] baseParams,
            object[] userParams)
        {
            if (baseParams.Length <= 0)
            {
                return Array.Empty<object>();
            }

            var results = new object[baseParams.Length];

            // 获取用于筛选参数的参数匹配器
            var matcher = GetParamsMather(ref userParams);
            for (int i = 0; i < baseParams.Length; i++)
            {
                var baseParam = baseParams[i];

                // 第一种策略 
                // 参数匹配器是第一个执行的，因为它们的匹配精度是最精确的
                var param = matcher?.Invoke(baseParam);

                // 第二种策略
                // 当容器发现开发人员使用object或object[]作为依赖参数类型时，我们尝试压缩注入的用户参数。
                param = param ?? GetCompactInjectUserParams(baseParam, ref userParams);

                // 第三种策略
                // 从用户参数中选择适当的参数，并按相对顺序注入它们
                param = param ?? GetDependenciesFromUserParams(baseParam, ref userParams);

                string needService = null;
                if (param == null)
                {
                    // 最后策略
                    // 尝试通过依赖注入容器生成所需的参数
                    needService = TypeConvertToService(baseParam.ParameterType);

                    if (baseParam.ParameterType.IsClass || baseParam.ParameterType.IsInterface)
                    {
                        param = ResolveClass(makeServiceBindData, needService, baseParam);
                    }
                    else
                    {
                        param = ResolvePrimitive(makeServiceBindData, needService, baseParam);
                    }
                }

                if (!CanInject(baseParam.ParameterType, param))
                {
                    var error =
                        $"[{makeServiceBindData.Service}] Params inject type must be [{baseParam.ParameterType}] ," +
                        $" But instance is [{param?.GetType()}]";
                    if (needService == null)
                    {
                        error += " Inject params from user incoming parameters";
                    }
                    else
                    {
                        error += $" Make service is [{needService}]";
                    }

                    throw new UnresolvableException(error);
                }

                results[i] = param;
            }

            return results;
        }

        /// <summary>
        /// 获取参数匹配器
        /// </summary>
        /// <param name="userParams">用户参数的数组</param>
        /// <returns>返回参数匹配器，如果为空，则没有匹配器</returns>
        protected virtual Func<ParameterInfo, object> GetParamsMather(ref object[] userParams)
        {
            if (userParams == null || userParams.Length <= 0)
            {
                return null;
            }

            var tables = GetParamsTypeInUserParams(ref userParams);
            return tables.Length <= 0 ? null : MakeParamsMatcher(tables);
        }

        /// <summary>
        /// 生成默认参数IParams匹配器
        /// </summary>
        private Func<ParameterInfo, object> MakeParamsMatcher(IParams[] tables)
        {
            // 默认匹配器策略 将参数名与参数表的参数名相匹配
            // 第一个有效参数值将作为返回值返回
            return parameterInfo =>
            {
                foreach (var table in tables)
                {
                    if (!table.TryGetValue(parameterInfo.Name, out object result))
                    {
                        continue;
                    }

                    if (ChangeInstanceType(ref result, parameterInfo.ParameterType))
                    {
                        return result;
                    }
                }

                return null;
            };
        }

        /// <summary>
        /// 从userParams中获取IParams类型的参数列表
        /// </summary>
        private IParams[] GetParamsTypeInUserParams(ref object[] userParams)
        {
            // 过滤出 参数列表中类型为IParams的参数
            var elements = Arr.Filter(userParams, value => value is IParams);
            var results = new IParams[elements.Length];
            for (int i = 0; i < elements.Length; i++)
            {
                results[i] = (IParams) elements[i];
            }

            // 将过滤出的IParams类型的参数转换为数组
            return results;
        }

        /// <summary>
        /// 获取通过压缩注入的参数
        /// 通过反射对参数与用户输入参数进行策略分析，并返回合适的参数
        /// </summary>
        protected virtual object GetCompactInjectUserParams(ParameterInfo baseParam, ref object[] userParams)
        {
            if (!CheckCompactInjectUserParams(baseParam, userParams))
            {
                return null;
            }

            try
            {
                // 如果参数类型为object 且用户参数为一个时，则返回用户参数列表的第一个即可
                if (baseParam.ParameterType == typeof(object) && userParams != null && userParams.Length == 1)
                {
                    return userParams[0];
                }

                return userParams;
            }
            finally
            {
                userParams = null;
            }
        }

        /// <summary>
        /// 从用户参数获取依赖项。
        /// </summary>
        /// <param name="baseParam">依赖类型</param>
        /// <param name="userParams">用户参数列表</param>
        /// <returns>与依赖项类型匹配的实例</returns>
        protected virtual object GetDependenciesFromUserParams(ParameterInfo baseParam, ref object[] userParams)
        {
            if (userParams == null)
            {
                return null;
            }

            GuardUserParamsCount(userParams.Length);

            for (int i = 0; i < userParams.Length; i++)
            {
                var userParam = userParams[i];
                if (!ChangeInstanceType(ref userParam, baseParam.ParameterType))
                {
                    continue;
                }

                // 移除用户参数列表中符合条件的项
                Arr.RemoveAt(ref userParams, i);
                // 返回符合条件的参数
                return userParam;
            }

            return null;
        }

        #endregion

        #region Extend

        private object Extend(string service, object instance)
        {
            if (extenders.TryGetValue(service, out List<Func<object, IContainer, object>> list))
            {
                foreach (var extender in list)
                {
                    instance = extender(instance, this);
                }
            }

            if (!extenders.TryGetValue(string.Empty, out list))
            {
                return instance;
            }

            foreach (var extender in list)
            {
                instance = extender(instance, this);
            }

            return this;
        }

        public void Extend(string service, Func<object, IContainer, object> closure)
        {
            Guard.Requires<ArgumentNullException>(closure != null);
            GuardFlushing();

            service = string.IsNullOrEmpty(service) ? string.Empty : AliasToService(service);

            if (!string.IsNullOrEmpty(service) && instances.TryGetValue(service, out object instance))
            {
                // 如果实例已经存在，则应用扩展
                // 扩展将不再添加到永久扩展列表中
                var old = instance;
                instances[service] = instance = closure(instance, this);

                if (!old.Equals(instance))
                {
                    instancesReverse.Remove(old);
                    instancesReverse.Add(instance, service);
                }

                TriggerOnRebound(service, instance);
                return;
            }

            // 服务尚未解析创建出instance，则将扩展放入扩展列表中，待服务解析时应用扩展
            if (!extenders.TryGetValue(service, out List<Func<object, IContainer, object>> extender))
            {
                extenders[service] = extender = new List<Func<object, IContainer, object>>();
            }

            extender.Add(closure);

            if (!string.IsNullOrEmpty(service) && IsResolved(service))
            {
                TriggerOnRebound(service);
            }
        }

        /// <summary>
        /// 移除指定服务的所有扩展
        /// </summary>
        public void ClearExtenders(string service)
        {
            GuardFlushing();
            service = AliasToService(service);
            extenders.Remove(service);

            if (!IsResolved(service))
            {
                return;
            }

            Release(service);
            TriggerOnRebound(service);
        }

        #endregion

        #region Instance

        public object Instance(string service, object instance)
        {
            Guard.ParameterNotNull(service, nameof(service));
            GuardFlushing();
            GuardServiceName(service);

            service = AliasToService(service);

            var bindData = GetBind(service);
            if (bindData != null)
            {
                if (!bindData.IsStatic)
                {
                    throw new LogicException($"Service [{service}] is not Singleton(Static) Bind.");
                }
            }
            else
            {
                bindData = MakeEmptyBindData(service);
            }

            instance = TriggerOnResolving((BindData) bindData, instance);

            if (instance != null && instancesReverse.TryGetValue(instance, out string realService) &&
                realService != service)
            {
                throw new LogicException($"The instance has been registered as a singleton in {realService}");
            }

            var isResolved = IsResolved(service);
            Release(service);

            instances.Add(service, instance);

            if (instance != null)
            {
                instancesReverse.Add(instance, service);
            }

            if (!instanceTiming.Contains(service))
            {
                instanceTiming.Add(service, instanceId++);
            }

            if (isResolved)
            {
                TriggerOnRebound(service, instance);
            }

            return instance;
        }

        #endregion

        #region ChangeInstanceType

        protected virtual bool ChangeInstanceType(ref object result, Type targetType)
        {
            try
            {
                // 确定指定的对象是否是当前 Type 的实例.
                if (result == null || targetType.IsInstanceOfType(result))
                {
                    return true;
                }

                if (IsBasicType(result.GetType()) && targetType.IsDefined(typeof(VariantAttribute), false))
                {
                    try
                    {
                        result = Make(TypeConvertToService(targetType), result);
                        return true;
                    }
#pragma warning disable CS0168
                    catch (SException e)
#pragma warning restore CS0168
                    {
                        // ignore. When throw exception then stop inject. 
                    }
                }

                // IConvertible接口：定义特定的方法，这些方法将实现 引用或值类型的值 转换为具有等效值的 公共语言运行库类型。
                // C#中一个很好用的函数是Convert.ChangeType，它允许用户将某个类型转换成其他类型。但是如果你需要转换的对象不是继承自IConvertible接口，那么系统会抛出异常，转换就失败了。
                // 公共语言运行库类型包括： Boolean、SByte、Byte、Int16、UInt16、Int32、UInt32、Int64、UInt64、Single、Double、Decimal、DateTime、Char 和 String。
                //这些类型都继承了IConvertible接口。
                // ###################
                //  IsAssignableFrom 确定指定类型 c 的实例是否能分配给当前类型的变量。public virtual bool IsAssignableFrom (Type? c);
                // 如果满足下列任一条件，则为 true：
                // c 和当前实例表示相同类型。
                // c 是从当前实例直接或间接派生的。 如果继承于当前实例，则 c 是从当前实例直接派生的；如果继承于从当前实例继承的接连一个或多个类，则 c 是从当前实例间接派生的。
                // 当前实例是 c 实现的一个接口。
                // c 是一个泛型类型参数，并且当前实例表示 c 的约束之一。
                // c 表示一个值类型，并且当前实例表示 Nullable<c>（在 Visual Basic 中为 Nullable(Of c)）。
                // 如果不满足上述任何一个条件或者 c 为 false，则为 null。
                if (result is IConvertible && typeof(IConvertible).IsAssignableFrom(targetType))
                {
                    result = Convert.ChangeType(result, targetType);
                    return true;
                }
            }
#pragma warning disable CS0168
            catch (SException e)
#pragma warning restore CS0168
            {
                // ignore. When throw exception then stop inject. 
            }

            return false;
        }

        #endregion

        #region Add FindType

        public IContainer OnFindType(Func<string, Type> func, int priority = int.MaxValue)
        {
            Guard.Requires<ArgumentException>(func != null);
            GuardFlushing();
            findType.Add(func, priority);
            return this;
        }

        #endregion

        #region Bindable Data

        /// <summary>
        /// 生成空绑定数据
        /// </summary>
        protected virtual BindData MakeEmptyBindData(string service)
        {
            return new BindData(this, service, null, false);
        }

        /// <summary>
        /// 获取服务绑定数据，如果数据为空，则填写数据
        /// </summary>
        protected BindData GetBindFillable(string service)
        {
            return service != null && bindings.TryGetValue(service, out BindData bindData)
                ? bindData
                : MakeEmptyBindData(service);
        }

        public IBindData GetBind(string service)
        {
            if (string.IsNullOrEmpty(service))
            {
                return null;
            }

            service = AliasToService(service);
            return bindings.TryGetValue(service, out BindData bindData) ? bindData : null;
        }

        #endregion

        #region Bind And UnBind

        public IBindData Bind(string service, Func<IContainer, object[], object> concrete, bool isStatic)
        {
            Guard.ParameterNotNull(service);
            Guard.ParameterNotNull(concrete);

            GuardServiceName(service);
            GuardFlushing();

            service = FormatService(service);

            if (bindings.ContainsKey(service))
            {
                throw new LogicException($"Bind [{service}] already exists.");
            }

            if (instances.ContainsKey(service))
            {
                throw new LogicException($"Instances [{service}] is already exists.");
            }

            if (aliases.ContainsKey(service))
            {
                throw new LogicException($"Alias [{service}] is already exists.");
            }

            var bindData = new BindData(this, service, concrete, isStatic);
            bindings.Add(service, bindData);

            if (!IsResolved(service))
            {
                return bindData;
            }

            if (isStatic)
            {
                // 如果为静态则直接解析该服务
                Make(service);
            }
            else
            {
                TriggerOnRebound(service);
            }

            return bindData;
        }

        public IBindData Bind(string service, Type concrete, bool isStatic)
        {
            Guard.Requires<ArgumentNullException>(concrete != null, $"Parameter {nameof(concrete)} can not be null.");

            if (IsUnableType(concrete))
            {
                throw new LogicException(
                    $"Type \"{concrete}\" can not bind. please check if there is a list of types that cannot be built.");
            }

            service = FormatService(service);
            return Bind(service, WrapperTypeBuilder(service, concrete), isStatic);
        }

        /// <summary>
        /// 包装指定类型
        /// </summary>
        /// <returns>返回一个闭包，调用它来获取服务实例</returns>
        protected virtual Func<IContainer, object[], object> WrapperTypeBuilder(string service, Type concrete)
        {
            return (container, userParams) =>
                ((Container) container).CreateInstance(GetBindFillable(service), concrete, userParams);
        }

        public bool BindIf(string service, Func<IContainer, object[], object> concrete, bool isStatic,
            out IBindData bindData)
        {
            var bind = GetBind(service);
            if (bind == null && (HasInstance(service) || IsAlias(service)))
            {
                bindData = null;
                return false;
            }

            bindData = bind ?? Bind(service, concrete, isStatic);
            return bind == null;
        }

        public bool BindIf(string service, Type concrete, bool isStatic, out IBindData bindData)
        {
            if (!IsUnableType(concrete))
            {
                service = FormatService(service);
                return BindIf(service, WrapperTypeBuilder(service, concrete), isStatic, out bindData);
            }

            bindData = null;
            return false;
        }

        public void Unbind(string service)
        {
            service = AliasToService(service);
            var bind = GetBind(service);
            bind?.Unbind();
        }

        internal void Unbind(IBindable bindable)
        {
            GuardFlushing();
            Release(bindable.Service);
            if (aliasesReverse.TryGetValue(bindable.Service, out List<string> serviceList))
            {
                foreach (var alias in serviceList)
                {
                    aliases.Remove(alias);
                }

                aliasesReverse.Remove(bindable.Service);
            }

            bindings.Remove(bindable.Service);
        }

        #endregion

        #region Method

        public IMethodBind BindMethod(string method, object target, MethodInfo called)
        {
            GuardFlushing();
            GuardMethodName(method);
            return methodContainer.Bind(method, target, called);
        }

        public void UnbindMethod(object target)
        {
            methodContainer.Unbind(target);
        }

        public object Invoke(string method, params object[] userParams)
        {
            GuardConstruct(nameof(Invoke));
            return methodContainer.Invoke(method, userParams);
        }

        public object Call(object target, MethodInfo methodInfo, params object[] userParams)
        {
            Guard.Requires<ArgumentNullException>(methodInfo != null);
            if (!methodInfo.IsStatic)
            {
                Guard.Requires<ArgumentNullException>(target != null);
            }

            GuardConstruct(nameof(Call));

            var parameter = methodInfo.GetParameters();
            var bindData = GetBindFillable(target != null ? TypeConvertToService(target.GetType()) : null);
            userParams = GetDependencies(bindData, parameter, userParams) ?? Array.Empty<object>();
            return methodInfo.Invoke(target, userParams);
        }

        #endregion

        #region Release

        public bool Release(object mixed)
        {
            if (mixed == null)
            {
                return false;
            }

            string service;
            object instance = null;
            if (!(mixed is string))
            {
                service = GetServiceWithInstance(mixed);
            }
            else
            {
                service = AliasToService(mixed.ToString());
                if (!instances.TryGetValue(service, out instance))
                {
                    // 防止将字符串用作服务名称
                    service = GetServiceWithInstance(mixed);
                }
            }

            if (instance == null && (string.IsNullOrEmpty(service) || !instances.TryGetValue(service, out instance)))
            {
                return false;
            }

            var bindData = GetBindFillable(service);
            bindData.TriggerRelease(instance);
            TriggerOnRelease(bindData, instance);

            if (instance != null)
            {
                DisposeInstance(instance);
                instancesReverse.Remove(instance);
            }

            instances.Remove(service);
            if (!HasOnReboundCallbacks(service))
            {
                instanceTiming.Remove(service);
            }

            return true;
        }

        /// <summary>
        /// 通过IDisposable释放指定实例
        /// </summary>
        private void DisposeInstance(object instance)
        {
            if (instance is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        /// <summary>
        /// 获取指定实例的服务名称
        /// </summary>
        protected string GetServiceWithInstance(object instance)
        {
            return instancesReverse.TryGetValue(instance, out string origin) ? origin : null;
        }

        #endregion

        #region Register Event

        public IContainer OnRebound(string service, Action<object> callback)
        {
            Guard.Requires<ArgumentException>(callback != null);
            GuardFlushing();

            service = AliasToService(service);
            if (!IsResolved(service) && !CanMake(service))
            {
                throw new LogicException(
                    $"If you wan use Rebound(Watch), please {nameof(Bind)} or {nameof(Instance)} service first.");
            }

            if (!rebound.TryGetValue(service, out List<Action<object>> list))
            {
                rebound[service] = list = new List<Action<object>>();
            }

            list.Add(callback);
            return this;
        }

        public IContainer OnResolving(Action<IBindData, object> closure)
        {
            AddClosure(closure, resolving);
            return this;
        }

        public IContainer OnAfterResolving(Action<IBindData, object> closure)
        {
            AddClosure(closure, afterResolving);
            return this;
        }

        public IContainer OnRelease(Action<IBindData, object> closure)
        {
            AddClosure(closure, release);
            return this;
        }

        private void AddClosure(Action<IBindData, object> closure, List<Action<IBindData, object>> list)
        {
            Guard.Requires<ArgumentException>(closure != null);
            GuardFlushing();
            list.Add(closure);
        }

        #endregion

        #region Trigger Event

        /// <summary>
        /// 触发指定服务实例的重绑定回调
        /// </summary>
        /// <param name="service">指定的服务名称</param>
        /// <param name="instance">指定的服务实例。如果传入空值，则按服务名称从容器生成</param>
        private void TriggerOnRebound(string service, object instance = null)
        {
            var callbacks = GetOnReboundCallbacks(service);
            if (callbacks == null || callbacks.Count <= 0)
            {
                return;
            }

            var bind = GetBind(service);
            instance = instance ?? Make(service);

            for (int i = 0; i < callbacks.Count; i++)
            {
                callbacks[i](instance);
                if (i + 1 < callbacks.Count && (bind == null || !bind.IsStatic))
                {
                    instance = Make(service);
                }
            }
        }

        /// <summary>
        /// 触发所有的解析中的事件回调
        /// </summary>
        private object TriggerOnResolving(BindData bindData, object instance)
        {
            instance = bindData.TriggerResolving(instance);
            instance = Trigger(bindData, instance, resolving);
            return TriggerOnAfterResolving(bindData, instance);
        }

        /// <summary>
        /// 触发所有的解析后的事件回调
        /// </summary>
        private object TriggerOnAfterResolving(BindData bindData, object instance)
        {
            instance = bindData.TriggerAfterResolving(instance);
            return Trigger(bindData, instance, afterResolving);
        }

        /// <summary>
        /// 触发指定服务实例的释放回调
        /// </summary>
        private void TriggerOnRelease(IBindData bindData, object instance)
        {
            Trigger(bindData, instance, release);
        }

        /// <summary>
        /// 触发指定列表的回调
        /// </summary>
        internal static object Trigger(IBindData bindData, object instance, List<Action<IBindData, object>> list)
        {
            if (list == null)
            {
                return instance;
            }

            foreach (var closure in list)
            {
                closure(bindData, instance);
            }

            return instance;
        }

        private bool HasOnReboundCallbacks(string service)
        {
            var result = GetOnReboundCallbacks(service);
            return result != null && result.Count > 0;
        }

        /// <summary>
        /// 获取指定服务的所有的重绑定回调
        /// </summary>
        private IList<Action<object>> GetOnReboundCallbacks(string service)
        {
            return !rebound.TryGetValue(service, out List<Action<object>> result) ? null : result;
        }

        #endregion

        #region Tag

        public void Tag(string tag, params string[] services)
        {
            Guard.ParameterNotNull(tag);
            GuardFlushing();

            if (!tags.TryGetValue(tag, out List<string> collection))
            {
                tags[tag] = collection = new List<string>();
            }

            foreach (var service in services ?? Array.Empty<string>())
            {
                if (string.IsNullOrEmpty(service))
                {
                    continue;
                }

                collection.Add(service);
            }
        }

        public object[] Tagged(string tag)
        {
            Guard.ParameterNotNull(tag);

            if (!tags.TryGetValue(tag, out List<string> services))
            {
                throw new LogicException($"Tag \"{tag}\" is not exist.");
            }

            return Arr.Map(services, service => Make(service));
        }

        #endregion

        #region Alias

        public IContainer Alias(string alias, string service)
        {
            Guard.ParameterNotNull(alias);
            Guard.ParameterNotNull(service);

            if (alias == service)
            {
                throw new LogicException($"Alias is same as service: \"{alias}\"");
            }

            GuardFlushing();

            alias = FormatService(alias);
            service = AliasToService(service);

            if (aliases.ContainsKey(alias))
            {
                throw new LogicException($"Alias \"{alias}\" is already exists.");
            }

            if (bindings.ContainsKey(alias))
            {
                throw new LogicException($"Alias \"{alias}\" has been used for service name.");
            }

            if (!bindings.ContainsKey(service) && !instances.ContainsKey(service))
            {
                throw new LogicException(
                    $"You must {nameof(Bind)}() or {nameof(Instance)}() service before and you be able to called {nameof(Alias)}().");
            }

            aliases.Add(alias, service);

            if (!aliasesReverse.TryGetValue(service, out List<string> collection))
            {
                aliasesReverse[service] = collection = new List<string>();
            }

            collection.Add(alias);
            return this;
        }

        #endregion

        #region Guard

        protected virtual void GuardConstruct(string method)
        {
        }

        /// <summary>
        /// 确保指定的实例有效
        /// </summary>
        protected virtual void GuardResolveInstance(object instance, string makeService)
        {
            if (instance == null)
            {
                throw MakeBuildFailedException(makeService, SpeculatedServiceType(makeService), null);
            }
        }

        /// <summary>
        /// 确保用户传入的参数数必须小于指定值
        /// </summary>
        protected virtual void GuardUserParamsCount(int count)
        {
            if (count > 255)
            {
                throw new LogicException(
                    $"Too many parameters, must be less or equal than 255 or override the {nameof(GuardUserParamsCount)}");
            }
        }

        protected virtual void GuardServiceName(string service)
        {
            foreach (var c in ServiceBanChars)
            {
                if (service.IndexOf(c) >= 0)
                {
                    throw new LogicException(
                        $"Service name {service}contains disabled characters : {c}. please use Alias replacement");
                }
            }
        }

        protected virtual void GuardMethodName(string method)
        {
        }

        private void GuardFlushing()
        {
            if (flushing)
            {
                throw new LogicException("Container is flushing can not do it.");
            }
        }

        #endregion

        #region Check

        public bool CanMake(string service)
        {
            Guard.ParameterNotNull(service);

            service = AliasToService(service);
            if (HasBind(service) || HasInstance(service))
            {
                return true;
            }

            var type = SpeculatedServiceType(service);
            return !IsBasicType(type) && !IsUnableType(type);
        }

        public bool HasBind(string service)
        {
            return GetBind(service) != null;
        }

        public bool HasInstance(string service)
        {
            Guard.ParameterNotNull(service);
            service = AliasToService(service);
            return instances.ContainsKey(service);
        }

        public bool IsResolved(string service)
        {
            Guard.ParameterNotNull(service);

            service = AliasToService(service);
            return resolved.Contains(service) || instances.ContainsKey(service);
        }

        public bool IsStatic(string service)
        {
            var bind = GetBind(service);
            return bind != null && bind.IsStatic;
        }

        public bool IsAlias(string name)
        {
            name = FormatService(name);
            return aliases.ContainsKey(name);
        }

        /// <summary>
        /// 指定实例是否可注入
        /// </summary>
        protected virtual bool CanInject(Type type, object instance)
        {
            return instance == null || type.IsInstanceOfType(instance);
        }

        /// <summary>
        /// 确定指定的类型是否为容器的默认基本类型
        /// </summary>
        protected virtual bool IsBasicType(Type type)
        {
            // 基元类型有 Boolean, Byte, SByte, Int16, UInt16, Int32, UInt32, Int64, UInt64, IntPtr, UIntPtr, Char, Double, and Single.
            return type == null || type.IsPrimitive || type == typeof(string);
        }

        /// <summary>
        /// 确定指定的类型是否为无法构建的类型
        /// </summary>
        protected virtual bool IsUnableType(Type type)
        {
            return type == null || type.IsAbstract || type.IsInterface || type.IsArray || type.IsEnum ||
                   (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>));
        }

        /// <summary>
        /// 检查用户传入的参数是否可以注入压缩
        /// </summary>
        /// <param name="baseParam">参数信息</param>
        /// <param name="userParams">用户参数数组</param>
        /// <returns>如果参数可以紧缩注入则返回True,否则返回False</returns>
        protected virtual bool CheckCompactInjectUserParams(ParameterInfo baseParam, object[] userParams)
        {
            if (userParams == null || userParams.Length <= 0)
            {
                return false;
            }

            // 参数类型为object[] 或者object时，则参数可进行紧缩注入
            return baseParam.ParameterType == typeof(object[])
                   || baseParam.ParameterType == typeof(object);
        }

        #endregion

        #region Util

        protected virtual string FormatService(string service)
        {
            return service.Trim();
        }

        private string AliasToService(string name)
        {
            name = FormatService(name);
            return aliases.TryGetValue(name, out string alias) ? alias : name;
        }


        /// <summary>
        /// 根据服务名称推断服务类型
        /// </summary>
        /// <param name="service"></param>
        /// <returns></returns>
        protected virtual Type SpeculatedServiceType(string service)
        {
            if (findTypeCache.TryGetValue(service, out Type result))
            {
                return result;
            }

            foreach (var finder in findType)
            {
                var type = finder.Invoke(service);
                if (type != null)
                {
                    return findTypeCache[service] = type;
                }
            }

            return findTypeCache[service] = null;
        }

        protected virtual string GetPropertyNeedsService(PropertyInfo propertyInfo)
        {
            return TypeConvertToService(propertyInfo.PropertyType);
        }

        public string TypeConvertToService(Type type)
        {
            return type.ToString();
        }

        #endregion

        #region Exception

        protected virtual LogicException MakeCircularDependencyException(string service)
        {
            var message = $"Circular dependency detected while for [{service}]";
            message += GetBuildStackDebugMessage();
            return new LogicException(message);
        }

        protected virtual string GetBuildStackDebugMessage()
        {
            var previous = string.Join(", ", BuildStack.ToArray());
            return $" While building stack [{previous}]";
        }

        /// <summary>
        /// 构建解析失败的异常
        /// </summary>
        protected virtual UnresolvableException MakeBuildFailedException(string makeService, Type makeServiceType,
            SException innerException)
        {
            var message = makeServiceType != null
                ? $"Class [{makeServiceType}] build failed. Service is [{makeService}]"
                : $"Service [{makeService}] is not exists.";
            message += GetBuildStackDebugMessage();
            message += GetInnerExceptionMessage(innerException);
            return new UnresolvableException(message);
        }

        /// <summary>
        /// 获取内部异常调试消息
        /// </summary>
        protected virtual string GetInnerExceptionMessage(SException innerException)
        {
            if (innerException == null)
            {
                return String.Empty;
            }

            var stack = new StringBuilder();
            do
            {
                if (stack.Length > 0)
                {
                    stack.Append(", ");
                    stack.Append(innerException);
                }
            } while ((innerException = innerException.InnerException) != null);

            return $" InnerException message stack: [{stack}]";
        }

        /// <summary>
        /// 构建未解析的异常
        /// </summary>
        protected virtual UnresolvableException MakeUnresolvableException(string name, Type declaringClass)
        {
            return new UnresolvableException(
                $"Unresolvable dependency , resolving [{name ?? "Unknown"}] in class [{declaringClass.ToString() ?? "Unknown"}]");
        }

        #endregion

        #region Flush

        public virtual void Flush()
        {
            try
            {
                flushing = true;
                foreach (var service in instanceTiming.GetIterator(false))
                {
                    Release(service);
                }

                Guard.Requires<AssertException>(instances.Count <= 0);

                tags.Clear();
                aliases.Clear();
                aliasesReverse.Clear();
                instances.Clear();
                bindings.Clear();
                resolving.Clear();
                release.Clear();
                extenders.Clear();
                resolved.Clear();
                findType.Clear();
                findTypeCache.Clear();
                BuildStack.Clear();
                UserParamsStack.Clear();
                rebound.Clear();
                methodContainer.Flush();
                instanceTiming.Clear();
                instanceId = 0;
            }
            finally
            {
                flushing = false;
            }
        }

        #endregion
    }
}