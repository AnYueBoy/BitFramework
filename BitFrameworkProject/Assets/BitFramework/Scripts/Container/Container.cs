using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace BitFramework.Conatiner
{
    public class Container : IContainer
    {
        // 服务-绑定数据映射
        // 为何不直接使用Type作为key，因为框架中提供了别名逻辑，通过别名反映射到Type Name后，通过反射获取对应的Type
        private readonly Dictionary<string, BindData> bindings;

        // 服务-实例映射
        private readonly Dictionary<string, object> instances;

        // 已被构建的服务列表
        private readonly HashSet<string> madeSet;

        // 正在构建的服务栈
        private Stack<string> buildStack { get; }

        // 正在构建的服务参数栈
        private Stack<object[]> userParamsStack { get; }

        public IBindData Bind(string service, Type concrete, bool isStatic)
        {
            if (IsUnableType(concrete))
            {
                throw new Exception($"type {concrete} is unable type.");
            }

            return Bind(service, (userParams) => CreateInstance(GetBindData(service), concrete, userParams), isStatic);
        }

        public IBindData Bind(string service, Func<object[], object> concrete, bool isStatic)
        {
            if (string.IsNullOrEmpty(service))
            {
                throw new Exception("service can not be null.");
            }

            if (bindings.ContainsKey(service))
            {
                throw new Exception($"bind {service} is exist");
            }

            if (instances.ContainsKey(service))
            {
                throw new Exception($"instances {service} is exist.");
            }

            // TODO:
            BindData bindData = new BindData(service, concrete, isStatic);
            bindings.Add(service, bindData);
            if (IsMade(service))
            {
                return bindData;
            }

            if (isStatic)
            {
                Make(service);
            }
            else
            {
                // TODO: 重绑定
            }

            return bindData;
        }

        private object Make(string service, params object[] userParams)
        {
            if (instances.TryGetValue(service, out object instance))
            {
                return instance;
            }

            if (buildStack.Contains(service))
            {
                throw new Exception($"circle dependency {service}");
            }

            buildStack.Push(service);
            userParamsStack.Push(userParams);

            try
            {
                BindData bindData = GetBindData(service);
                // instance = 
            }
            finally
            {
            }

            {
                buildStack.Pop();
                userParamsStack.Pop();
            }

            // TODO:
            return null;
        }

        private object Build(BindData bindData, object[] userParams)
        {
            if (bindData.Concrete != null)
            {
                return bindData.Concrete.Invoke(userParams);
            }

            // TODO: FIXME: 
            return null;
        }

        private object CreateInstance(BindData bindData, Type type, object[] userParams)
        {
            if (IsUnableType(type))
            {
                throw new Exception($"create instance type is not unable type. service name :{bindData.Service}");
            }

            // TODO: FIXME:
            return null;
        }

        private object[] GetConstructorsInjectParams(BindData bindData, Type type, object[] userParams)
        {
            // 获取此类的所有构造函数
            var constructors = type.GetConstructors();
            if (constructors.Length <= 0)
            {
                return Array.Empty<object>();
            }

            // 创建异常捕获
            ExceptionDispatchInfo exceptionDispatchInfo = null;
            foreach (var constructor in constructors)
            {
                try
                {
                    // TODO:
                    return null;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }

            // FIXME
            return null;
        }

        private object[] GetDependencies(BindData bindData, ParameterInfo[] baseParams, object[] userParams)
        {
            if (baseParams.Length <= 0)
            {
                return Array.Empty<object>();
            }

            var results = new object[baseParams.Length];

            //TODO: 
            // FIXME:
            return null;
        }

        private Func<ParameterInfo, object> GetParamsMatcher(ref object[] userParams)
        {
            if (userParams == null || userParams.Length <= 0)
            {
                return null;
            }

            var tables = GetParamsTypeInUserParams(ref userParams);
            return tables.Length <= 0 ? null : MakeParamsMatcher(tables);
        }

        /// <summary>
        /// 过滤获取UserParams中参数继承IParams的部分
        /// </summary>
        /// <param name="userParams"></param>
        /// <returns></returns>
        private IParams[] GetParamsTypeInUserParams(ref object[] userParams)
        {
            IList<IParams> elements = new List<IParams>();
            foreach (var userParam in userParams)
            {
                if (userParam is IParams param)
                {
                    elements.Add(param);
                }
            }

            return elements.ToArray();
        }

        private Func<ParameterInfo, object> MakeParamsMatcher(IParams[] tables)
        {
            //## 默认匹配器策略将匹配具有参数表的参数名称,并返回第一个有效参数值

            //  ParameterInfo 发现参数的属性并提供对参数元数据的访问权限。
            return (parameterInfo) =>
            {
                // 将tables参数参数名称获得的对象转为参数类型的实例
                foreach (var table in tables)
                {
                    if (!table.TryGetValue(parameterInfo.Name, out object result))
                    {
                        continue;
                    }

                    if (ChangeType(ref result, parameterInfo.ParameterType))
                    {
                        return result;
                    }
                }

                return null;
            };
        }

        private bool ChangeType(ref object result, Type conversionType)
        {
            try
            {
                // 确定指定的对象是否是当前 Type 的实例.
                if (result == null || conversionType.IsInstanceOfType(result))
                {
                    return true;
                }

                // 是否是基本类型 && 用反射判断某个方法是否运用了自定义Attribute
                if (IsBasicType(result.GetType()) && conversionType.IsDefined(typeof(VariantAttribute), false))
                {
                    try
                    {
                        result = Make(conversionType.ToString(), result);
                        return true;
                    }
#pragma warning disable CS0168
                    catch (Exception e)
#pragma warning restore CS0168
                    {
                        // 当抛异常时注入停止
                    }
                }

                // IConvertible接口：定义特定的方法，这些方法将实现 引用或值类型的值 转换为具有等效值的 公共语言运行库类型。
                // C#中一个很好用的函数是Convert.ChangeType，它允许用户将某个类型转换成其他类型。但是如何你需要转换的对象不是继承自IConvertible接口，那么系统会抛出异常，转换就失败了。
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
                if (result is IConvertible && typeof(IConvertible).IsAssignableFrom(conversionType))
                {
                    //返回一个指定类型的对象，该对象的值等效于指定的对象。
                    result = Convert.ChangeType(result, conversionType);
                    return true;
                }
            }
#pragma warning disable CS0168
            catch (Exception e)
#pragma warning restore CS0168
            {
                // 当抛异常时注入停止
            }

            return false;
        }

        private BindData GetBindData(string service)
        {
            return service != null && bindings.TryGetValue(service, out var bindData)
                ? bindData
                : new BindData(service, null, false);
        }

        private bool IsMade(string service)
        {
            return madeSet.Contains(service) || instances.ContainsKey(service);
        }

        private bool IsUnableType(Type type)
        {
            return type == null || type.IsAbstract || type.IsInterface || type.IsArray || type.IsEnum ||
                   (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>));
        }

        private bool IsBasicType(Type type)
        {
            //获取一个值，通过该值指示 Type 是否为基元类型之一。
            // 基元类型有 Boolean, Byte, SByte, Int16, UInt16, Int32, UInt32, Int64, UInt64, IntPtr, UIntPtr, Char, Double, and Single.
            return type == null || type.IsPrimitive || type == typeof(string);
        }
    }
}