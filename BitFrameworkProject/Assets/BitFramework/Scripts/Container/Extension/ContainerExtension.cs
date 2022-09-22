using System;
using BitFramework.Util;

namespace BitFramework.Container
{
    public static class ContainerExtension
    {
        public static IBindData GetBind<TService>(this IContainer container)
        {
            return container.GetBind(container.TypeConvertToService(typeof(TService)));
        }

        public static bool HasBind<TService>(this IContainer container)
        {
            return container.HasBind(container.TypeConvertToService(typeof(TService)));
        }

        public static bool HasInstance<TService>(this IContainer container)
        {
            return container.HasInstance(container.TypeConvertToService(typeof(TService)));
        }

        public static bool IsResolved<TService>(this IContainer container)
        {
            return container.IsResolved(container.TypeConvertToService(typeof(TService)));
        }

        public static bool CanMake<TService>(this IContainer container)
        {
            return container.CanMake(container.TypeConvertToService(typeof(TService)));
        }

        public static bool IsStatic<TService>(this IContainer container)
        {
            return container.IsStatic(container.TypeConvertToService(typeof(TService)));
        }

        public static bool IsAlias<TService>(this IContainer container)
        {
            return container.IsAlias(container.TypeConvertToService(typeof(TService)));
        }

        public static IContainer Alias<TAlias, TService>(this IContainer container)
        {
            return container.Alias(container.TypeConvertToService(typeof(TAlias)),
                container.TypeConvertToService(typeof(TService)));
        }

        #region Bind

        public static IBindData Bind<TService>(this Container container)
        {
            return container.Bind(container.TypeConvertToService(typeof(TService)), typeof(TService), false);
        }

        public static IBindData Bind<TService, TConcrete>(this Container container)
        {
            return container.Bind(container.TypeConvertToService(typeof(TService)), typeof(TConcrete), false);
        }

        public static IBindData Bind<TService>(this IContainer container, Func<IContainer, object[], object> concrete)
        {
            Guard.Requires<ArgumentNullException>(concrete != null);
            return container.Bind(container.TypeConvertToService(typeof(TService)), concrete, false);
        }

        public static IBindData Bind<TService>(this IContainer container, Func<object[], object> concrete)
        {
            Guard.Requires<ArgumentNullException>(concrete != null);
            return container.Bind(container.TypeConvertToService(typeof(TService)), (c, p) => concrete.Invoke(p),
                false);
        }

        public static IBindData Bind<TService>(this Container container, Func<object> concrete)
        {
            Guard.Requires<ArgumentNullException>(concrete != null);
            return container.Bind(container.TypeConvertToService(typeof(TService)), (c, p) => concrete.Invoke(), false);
        }

        public static IBindData Bind(this IContainer container, string service,
            Func<IContainer, object[], object> concrete)
        {
            Guard.Requires<ArgumentNullException>(concrete != null);
            return container.Bind(service, concrete, false);
        }

        #endregion

        #region Bind If

        public static bool BindIf<TService, TConcrete>(this IContainer container, out IBindData bindData)
        {
            return container.BindIf(container.TypeConvertToService(typeof(TService)), typeof(TConcrete), false,
                out bindData);
        }

        public static bool BindIf<TService>(this IContainer container, out IBindData bindData)
        {
            return container.BindIf(container.TypeConvertToService(typeof(TService)), typeof(TService), false,
                out bindData);
        }

        public static bool BindIf<TService>(this IContainer container, Func<IContainer, object[], object> concrete,
            out IBindData bindData)
        {
            Guard.Requires<ArgumentNullException>(concrete != null);
            return container.BindIf(container.TypeConvertToService(typeof(TService)), concrete, false, out bindData);
        }

        public static bool BindIf<TService>(this IContainer container, Func<object[], object> concrete,
            out IBindData bindData)
        {
            Guard.Requires<ArgumentNullException>(concrete != null);
            return container.BindIf(container.TypeConvertToService(typeof(TService)), (c, p) => concrete.Invoke(p),
                false, out bindData);
        }

        public static bool BindIf<TService>(this IContainer container, Func<object> concrete, out IBindData bindData)
        {
            Guard.Requires<ArgumentNullException>(concrete != null);
            return container.BindIf(container.TypeConvertToService(typeof(TService)), (c, p) => concrete.Invoke(),
                false, out bindData);
        }

        public static bool BindIf(this IContainer container, string service,
            Func<IContainer, object[], object> concrete, out IBindData bindData)
        {
            return container.BindIf(service, concrete, false, out bindData);
        }

        #endregion

        #region Singleton

        public static IBindData Singleton(this IContainer container, string service,
            Func<IContainer, object[], object> concrete)
        {
            return container.Bind(service, concrete, true);
        }

        public static IBindData Singleton<TService, TConcrete>(this IContainer container)
        {
            return container.Bind(container.TypeConvertToService(typeof(TService)), typeof(TConcrete), true);
        }

        public static IBindData Singleton<TService>(this IContainer container)
        {
            return container.Bind(container.TypeConvertToService(typeof(TService)), typeof(TService), true);
        }

        public static IBindData Singleton<TService>(this IContainer container,
            Func<IContainer, object[], object> concrete)
        {
            Guard.Requires<ArgumentNullException>(concrete != null);
            return container.Bind(container.TypeConvertToService(typeof(TService)), concrete, true);
        }

        public static IBindData Singleton<TService>(this IContainer container, Func<object> concrete)
        {
            Guard.Requires<ArgumentNullException>(concrete != null);
            return container.Bind(container.TypeConvertToService(typeof(TService)), (c, p) => concrete.Invoke(), true);
        }

        #endregion

        #region Singleton If

        public static bool SingletonIf<TService, TConcrete>(this IContainer container, out IBindData bindData)
        {
            return container.BindIf(container.TypeConvertToService(typeof(TService)), typeof(TConcrete), true,
                out bindData);
        }

        public static bool SingletonIf<TService>(this IContainer container, out IBindData bindData)
        {
            return container.BindIf(container.TypeConvertToService(typeof(TService)), typeof(TService), true,
                out bindData);
        }

        public static bool SingletonIf<TService>(this IContainer container, Func<IContainer, object[], object> concrete,
            out IBindData bindData)
        {
            return container.BindIf(container.TypeConvertToService(typeof(TService)), concrete, true, out bindData);
        }

        public static bool SingletonIf<TService>(this IContainer container, Func<object> concrete,
            out IBindData bindData)
        {
            Guard.Requires<ArgumentNullException>(concrete != null);
            return container.BindIf(container.TypeConvertToService(typeof(TService)), (c, p) => concrete.Invoke(), true,
                out bindData);
        }

        public static bool SingletonIf<TService>(this IContainer container, Func<object[], object> concrete,
            out IBindData bindData)
        {
            Guard.Requires<ArgumentNullException>(concrete != null);
            return container.BindIf(container.TypeConvertToService(typeof(TService)), (c, p) => concrete.Invoke(p),
                true, out bindData);
        }

        public static bool SingletonIf(this IContainer container, string service,
            Func<IContainer, object[], object> concrete, out IBindData bindData)
        {
            return container.BindIf(service, concrete, true, out bindData);
        }

        #endregion

        #region MethodBind

        public static IMethodBind BindMethod(this IContainer container, string method, object target,
            string call = null)
        {
            Guard.ParameterNotNull(method);
            Guard.ParameterNotNull(target);
            return container.BindMethod(method, target, target.GetType().GetMethod(call ?? Str.Method(method)));
        }

        public static IMethodBind BindMethod(this IContainer container, string method, Func<object> callback)
        {
            Guard.Requires<ArgumentNullException>(method != null);
            Guard.Requires<ArgumentNullException>(callback != null);
            return container.BindMethod(method, callback.Target, callback.Method);
        }

        public static IMethodBind BindMethod<T1>(this IContainer container, string method, Func<T1, object> callback)
        {
            Guard.Requires<ArgumentNullException>(method != null);
            Guard.Requires<ArgumentNullException>(callback != null);
            return container.BindMethod(method, callback.Target, callback.Method);
        }

        public static IMethodBind BindMethod<T1, T2>(this IContainer container, string method,
            Func<T1, T2, object> callback)
        {
            Guard.Requires<ArgumentNullException>(method != null);
            Guard.Requires<ArgumentNullException>(callback != null);
            return container.BindMethod(method, callback.Target, callback.Method);
        }

        public static IMethodBind BindMethod<T1, T2, T3>(this IContainer container, string method,
            Func<T1, T2, T3, object> callback)
        {
            Guard.Requires<ArgumentNullException>(method != null);
            Guard.Requires<ArgumentNullException>(callback != null);
            return container.BindMethod(method, callback.Target, callback.Method);
        }

        public static IMethodBind BindMethod<T1, T2, T3, T4>(this IContainer container, string method,
            Func<T1, T2, T3, T4, object> callback)
        {
            Guard.Requires<ArgumentNullException>(method != null);
            Guard.Requires<ArgumentNullException>(callback != null);
            return container.BindMethod(method, callback.Target, callback.Method);
        }

        #endregion
    }
}