using System;
using System.Collections.Generic;

namespace BitFramework.Conatiner
{
    public class Container : IContainer
    {
        // 服务-绑定数据映射
        private readonly Dictionary<string, BindData> bindings;

        // 服务-实例映射
        private readonly Dictionary<string, object> instances;

        // 已被构建的服务列表
        private readonly HashSet<string> madeSet;

        public IBindData Bind(string service, Type concrete, bool isStatic)
        {
            if (string.IsNullOrEmpty(service))
            {
                throw new Exception("service can not be null.");
            }

            if (IsUnableType(concrete))
            {
                throw new Exception($"type {concrete} is unable type.");
            }

            if (bindings.ContainsKey(service))
            {
                throw new Exception($"bind {service} is exist");
            }

            if (instances.ContainsKey(service))
            {
                throw new Exception($"instances {service} is exist.");
            }

            BindData bindData = new BindData();
            bindings.Add(service, bindData);
            if (IsMade(service))
            {
                return bindData;
            }

            if (isStatic)
            {
                // TODO:
            }

            return bindData;
        }

        private object Make(string service, params object[] userParams)
        {
            if (instances.TryGetValue(service, out object instance))
            {
                return instance;
            }

            // TODO:
            return null;
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
    }
}