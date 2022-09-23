using BitFramework.Container;

namespace BitFramework.Core
{
    /// <summary>
    /// 所有门面的基类
    /// </summary>
    public abstract class Facade<TService>
    {
        private static readonly string Service;
        private static TService that;
        private static IBindData binder;
        private static bool inited;
        private static bool released;

        static Facade()
        {
            Service = App.TypeConvertToService(typeof(TService));
            App.OnNewApplication += app =>
            {
                that = default;
                binder = null;
                inited = false;
                released = false;
            };
        }

        public static TService That => HasInstance ? that : Resolve();

        /// <summary>
        /// 该值指示已解析实例是否存在于外观中
        /// <para>如果它是非静态绑定，则返回永久false</para>
        /// </summary>
        internal static bool HasInstance => binder != null && binder.IsStatic && !released && that != null;

        internal static TService Make(params object[] userParams)
        {
            return HasInstance ? that : Resolve(userParams);
        }

        private static TService Resolve(params object[] userParams)
        {
            released = false;
            if (!inited && (App.IsResolved(Service) || App.CanMake(Service)))
            {
                App.Watch<TService>(ServiceRebound);
                inited = true;
            }
            else if (binder != null && !binder.IsStatic)
            {
                //如果已初始化，则绑定器已初始化。那么预先判断可以优化性能,而无需通过哈希表进行查找
                return Build(userParams);
            }

            var newBinder = App.GetBind(Service);
            if (newBinder == null || !newBinder.IsStatic)
            {
                binder = newBinder;
                return Build(userParams);
            }

            Rebind(newBinder);
            return that = Build(userParams);
        }

        /// <summary>
        /// 当解析对象重绑定
        /// </summary>
        private static void ServiceRebound(TService newService)
        {
            var newBinder = App.GetBind(Service);
            Rebind(newBinder);
            that = (newBinder == null || !newBinder.IsStatic) ? default : newService;
        }

        /// <summary>
        /// 将绑定数据重新绑定到给定的绑定器
        /// </summary>
        private static void Rebind(IBindData newBinder)
        {
            if (newBinder != null && binder != newBinder && newBinder.IsStatic)
            {
                newBinder.OnRelease(OnRelease);
            }

            binder = newBinder;
        }

        /// <summary>
        /// 释放解析对象时
        /// </summary>
        private static void OnRelease(IBindData oldBinder, object instance)
        {
            if (oldBinder != binder)
            {
                return;
            }

            that = default;
            released = true;
        }

        /// <summary>
        /// 从容器中解析外观对象
        /// </summary>
        private static TService Build(params object[] userParams)
        {
            return (TService)App.Make(Service, userParams);
        }
    }
}