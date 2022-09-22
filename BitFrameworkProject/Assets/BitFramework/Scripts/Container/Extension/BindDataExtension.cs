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

        #region OnAfterResolving

        public static IBindData OnAfterResolving(this IBindData bindData, Action closure)
        {
            Guard.Requires<ArgumentNullException>(closure != null);

            return bindData.OnAfterResolving((_, instance) => { closure(); });
        }

        public static IBindData OnAfterResolving(this IBindData bindData, Action<object> closure)
        {
            Guard.Requires<ArgumentNullException>(closure != null);
            return bindData.OnAfterResolving((_, instance) => { closure(instance); });
        }

        public static IBindData OnAfterResolving<T>(this IBindData bindData, Action<T> closure)
        {
            Guard.Requires<ArgumentNullException>(closure != null);
            return bindData.OnAfterResolving((_, instance) =>
            {
                if (instance is T)
                {
                    closure((T)instance);
                }
            });
        }

        public static IBindData OnAfterResolving<T>(this IBindData bindData, Action<IBindData, T> closure)
        {
            Guard.Requires<ArgumentNullException>(closure != null);
            return bindData.OnAfterResolving((bind, instance) =>
            {
                if (instance is T)
                {
                    closure(bind, (T)instance);
                }
            });
        }

        #endregion

        #region OnRelease

        public static IBindData OnRelease(this IBindData bindData, Action closure)
        {
            Guard.Requires<ArgumentNullException>(closure != null);

            return bindData.OnRelease((_, __) => { closure(); });
        }

        public static IBindData OnRelease(this IBindData bindData, Action<object> closure)
        {
            Guard.Requires<ArgumentNullException>(closure != null);
            return bindData.OnRelease((_, instance) => { closure(instance); });
        }

        public static IBindData OnRelease<T>(this IBindData bindData, Action<T> closure)
        {
            Guard.Requires<ArgumentNullException>(closure != null);
            return bindData.OnRelease((_, instance) =>
            {
                if (instance is T)
                {
                    closure((T)instance);
                }
            });
        }

        public static IBindData OnRelease<T>(this IBindData bindData, Action<IBindData, T> closure)
        {
            Guard.Requires<ArgumentNullException>(closure != null);
            return bindData.OnRelease((bind, instance) =>
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