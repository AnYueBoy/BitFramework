using System;
using BitFramework.Container;
using BitFramework.Exception;
using BitFramework.Scripts;
using UnityEngine;
using IServiceProvider = BitFramework.Core.IServiceProvider;

namespace BitFramework.Runtime
{
    public class UnityApplication : BitApplication
    {
        /// <summary>
        /// 初始化UnityApplication的新实例
        /// </summary>
        /// <param name="behaviour">驱动器</param>
        public UnityApplication(MonoBehaviour behaviour)
        {
            if (behaviour == null)
            {
                return;
            }

            this.Singleton<MonoBehaviour>(() => behaviour).Alias<Component>();
        }

        public override void Register(IServiceProvider provider, bool force = false)
        {
            var component = provider as Component;
            // 从MonoBehaviour 基础的服务提供者只能挂载在GameObject上.
            if (component != null && !component)
            {
                throw new LogicException(
                    "Service providers inherited from MonoBehaviour only be registered mounting on the GameObject.");
            }

            base.Register(provider, force);
        }

        protected override bool IsUnableType(Type type)
        {
            return typeof(Component).IsAssignableFrom(type) || base.IsUnableType(type);
        }
    }
}