using System;
using BitFramework.Util;

namespace BitFramework.Container
{
    public static class BindDataExtension
    {
        public static IBindData Alias<T>(this IBindData bindData)
        {
            return bindData.Alias(bindData.Container.TypeConvertToService(typeof(T)));
        }

        #region OnResolving

        public static IBindData OnResolving(this IBindData bindData, Action closure)
        {
            Guard.Requires<ArgumentNullException>(closure != null);
            return bindData.OnResolving((_, instance) => { closure(); });
        }

        public static IBindData OnResolving(this IBindData bindData, Action<object> closure)
        {
            Guard.Requires<ArgumentNullException>(closure != null);
            return bindData.OnResolving((_, instance) => { closure(instance); });
        }

        public static IBindData OnResolving<T>(this IBindData bindData, Action<T> closure)
        {
            Guard.Requires<ArgumentNullException>(closure != null);
            return bindData.OnResolving((_, instance) =>
            {
                if (instance is T)
                {
                    closure((T)instance);
                }
            });
        }

        public static IBindData OnResolving<T>(this IBindData bindData, Action<IBindData, T> closure)
        {
            Guard.Requires<ArgumentNullException>(closure != null);
            return bindData.OnResolving((bind, instance) =>
            {
                if (instance is T)
                {
                    closure(bind, (T)instance);
                }
            });
        }

        #endregion
    }
}