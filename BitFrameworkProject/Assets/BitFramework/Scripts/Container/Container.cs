using System;
using System.Collections.Generic;
using System.Linq;
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
        private static readonly char[] ServiceBanChars = { '@', ':', '$' };

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
            tags = new Dictionary<string, List<string>>((int)(prime * 0.25));
            aliases = new Dictionary<string, string>(prime * 4);
            aliasesReverse = new Dictionary<string, List<string>>(prime * 4);
            instances = new Dictionary<string, object>(prime * 4);
            instancesReverse = new Dictionary<object, string>(prime * 4);
            bindings = new Dictionary<string, BindData>(prime * 4);
            resolving = new List<Action<IBindData, object>>((int)(prime * 0.25));
            afterResolving = new List<Action<IBindData, object>>((int)(prime * 0.25));
            release = new List<Action<IBindData, object>>((int)(prime * 0.25));
            extenders = new Dictionary<string, List<Func<object, IContainer, object>>>((int)(prime * 0.25));
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
            // get=>
            // TODO:
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

                instance = 
            }
            finally
            {
                UserParamsStack.Pop();
                BuildStack.Pop();
            }
        }

        protected virtual object Build(BindData makeServiceBindData, object[] userParams)
        {
            var instance = makeServiceBindData.Concrete != null
                ? makeServiceBindData.Concrete(this, userParams)
                : CreateInstance(makeServiceBindData, SpeculatedServiceType(makeServiceBindData.Service), userParams);
            // TODO:
            return
        }

        protected virtual object CreateInstance(Bindable makeServiceBindData, Type makeServiceType, object[] userParams)
        {
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

        protected virtual object ResolveAttrClass(Bindable makeServiceBindData, string service, PropertyInfo baseParam)
        {
            if (ResolveFromContextual(makeServiceBindData, service, baseParam.Name, baseParam.PropertyType,
                    out object instance))
            {
                return instance;
            }

            // 检索应用于指定成员的指定类型的自定义特性。
            var inject = (InjectAttribute)baseParam.GetCustomAttribute(typeof(InjectAttribute));
            if (inject != null && !inject.Required)
            {
                return skipped;
            }
            
            throw 
        }

        #endregion

        #region Dependency Inject

        private object Inject(Bindable bindable, object instance)
        {
            GuardResolveInstance(instance, bindable.Service);
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
                    instance = Re 
                }
                else
                {
                    instance = 
                }
            }
        }

        #endregion

        #region Other

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
                    catch (SException e)
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
            catch (SException e)
            {
                // ignore. When throw exception then stop inject. 
            }

            return false;
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
            var message = makeService != null
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
        
        protected virtual 

        #endregion
    }
}