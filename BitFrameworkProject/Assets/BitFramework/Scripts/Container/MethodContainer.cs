using System;
using System.Collections.Generic;
using System.Reflection;
using BitFramework.Exception;
using BitFramework.Util;

namespace BitFramework.Container
{
    /// <summary>
    /// 方法的ioc容器
    /// </summary>
    internal sealed class MethodContainer
    {
        private readonly Dictionary<object, List<string>> targetToMethodsMappings;
        private readonly Dictionary<string, MethodBind> methodMappings;
        private readonly Container container;

        internal MethodContainer(Container container)
        {
            this.container = container;
            targetToMethodsMappings = new Dictionary<object, List<string>>();
            methodMappings = new Dictionary<string, MethodBind>();
        }

        /// <summary>
        /// 向容器注册方法
        /// </summary>
        /// <param name="method">方法名称</param>
        /// <param name="target">调用目标</param>
        /// <param name="methodInfo">要调用的方法信息</param>
        /// <returns>方法绑定数据</returns>
        public IMethodBind Bind(string method, object target, MethodInfo methodInfo)
        {
            Guard.ParameterNotNull(method);
            Guard.ParameterNotNull(methodInfo);

            if (!methodInfo.IsStatic)
            {
                Guard.Requires<ArgumentNullException>(target != null);
            }

            if (methodMappings.ContainsKey(method))
            {
                throw new LogicException($"Method [{method}] is already {nameof(Bind)}");
            }

            var methodBind = new MethodBind(this, container, method, target, methodInfo);
            methodMappings[method] = methodBind;

            if (target == null)
            {
                return methodBind;
            }

            if (!targetToMethodsMappings.TryGetValue(target, out List<string> targetMappings))
            {
                targetToMethodsMappings[target] = targetMappings = new List<string>();
            }

            targetMappings.Add(method);
            return methodBind;
        }

        /// <summary>
        /// 调用绑定容器中的方法并注入其依赖项
        /// </summary>
        /// <param name="method">方法名称</param>
        /// <param name="userParams">用户参数</param>
        /// <returns>方法返回值</returns>
        public object Invoke(string method, params object[] userParams)
        {
            Guard.ParameterNotNull(method);

            if (!methodMappings.TryGetValue(method, out MethodBind methodBind))
            {
                throw new LogicException($"Method [{method}] is not found.");
            }

            var injectParams = container.GetDependencies(methodBind, methodBind.ParameterInfos, userParams) ??
                               Array.Empty<object>();
            return methodBind.MethodInfo.Invoke(methodBind.Target, injectParams);
        }

        /// <summary>
        /// 从容器中取消绑定方法
        /// </summary>
        internal void Unbind(MethodBind methodBind)
        {
            methodMappings.Remove(methodBind.Service);
            if (methodBind.Target == null)
            {
                return;
            }

            if (!targetToMethodsMappings.TryGetValue(methodBind.Target, out List<string> methods))
            {
                return;
            }

            methods.Remove(methodBind.Service);
            if (methods.Count <= 0)
            {
                targetToMethodsMappings.Remove(methodBind.Target);
            }
        }

        public void Unbind(object target)
        {
            Guard.Requires<ArgumentNullException>(target != null);

            if (target is MethodBind methodBind)
            {
                methodBind.Unbind();
                return;
            }

            if (target is string)
            {
                if (!methodMappings.TryGetValue(target.ToString(), out methodBind))
                {
                    return;
                }

                methodBind.Unbind();
                return;
            }

            UnbindWithObject(target);
        }

        /// <summary>
        /// 删除绑定到对象的所有方法
        /// </summary>
        private void UnbindWithObject(object target)
        {
            if (!targetToMethodsMappings.TryGetValue(target, out List<string> methods))
            {
                return;
            }

            foreach (var method in methods)
            {
                Unbind(method);
            }
        }

        /// <summary>
        /// 刷新所有方法绑定的容器
        /// </summary>
        public void Flush()
        {
            targetToMethodsMappings.Clear();
            methodMappings.Clear();
        }
    }
}