using System;
using System.Collections.Generic;
using BitFramework.Exception;
using BitFramework.Util;

namespace BitFramework.Container
{
    public abstract class Bindable : IBindable
    {
        private readonly Container container;

        // 上下文
        private Dictionary<string, string> contextual;

        // 上下文闭包
        private Dictionary<string, Func<object>> contextualClosure;
        private bool isDestroy;

        protected Bindable(Container container, string service)
        {
            this.container = container;
            Service = service;
            isDestroy = false;
        }

        public string Service { get; }

        public IContainer Container => container;

        public void Unbind()
        {
            isDestroy = true;
            ReleaseBind();
        }

        /// <summary>
        /// 通过服务添加上下文
        /// </summary>
        /// <param name="needs">指定服务需求</param>
        /// <param name="given">给定的指定的服务或别名</param>
        internal void AddContextual(string needs, string given)
        {
            AssertDestroyed();
            if (contextual == null)
            {
                contextual = new Dictionary<string, string>();
            }

            if (contextual.ContainsKey(needs) || (contextualClosure != null && contextualClosure.ContainsKey(needs)))
            {
                throw new LogicException($"Needs [{needs}] is already exists.");
            }

            contextual.Add(needs, given);
        }

        /// <inheritdoc cref="AddContextual(string,string)"/>
        /// <param name="given">闭包返回给定的服务实例</param>
        internal void AddContextual(string needs, Func<object> given)
        {
            AssertDestroyed();
            if (contextualClosure == null)
            {
                contextualClosure = new Dictionary<string, Func<object>>();
            }

            if (contextualClosure.ContainsKey(needs) || (contextual != null && contextual.ContainsKey(needs)))
            {
                throw new LogicException($"Needs [{needs}] is already exist.");
            }

            contextualClosure.Add(needs, given);
        }

        /// <summary>
        /// 获取服务的需求上下文
        /// </summary>
        /// <param name="needs">需要的服务</param>
        /// <returns>给定的服务或别名</returns>
        internal string GetContextual(string needs)
        {
            if (contextual == null)
            {
                return null;
            }

            return contextual.TryGetValue(needs, out string contextualNeeds) ? contextualNeeds : null;
        }

        /// <inheritdoc cref="GetContextual"/>
        /// <returns>闭包返回给定的服务实例</returns>
        internal Func<object> GetContextualClosure(string needs)
        {
            if (contextualClosure == null)
            {
                return null;
            }

            return contextualClosure.TryGetValue(needs, out var closure) ? closure : null;
        }

        /// <inheritdoc cref="Unbind"/>
        protected abstract void ReleaseBind();

        /// <summary>
        /// 检查当前实例是否已被释放
        /// </summary>
        protected void AssertDestroyed()
        {
            if (isDestroy)
            {
                throw new LogicException("The current instance is destroyed.");
            }
        }
    }

    public abstract class Bindable<T> : Bindable, IBindable<T>
        where T : class, IBindable<T>
    {
        private GivenData<T> given;

        protected Bindable(Container container, string service) : base(container, service)
        {
        }

        public IGivenData<T> Needs(string service)
        {
            Guard.ParameterNotNull(service);
            AssertDestroyed();

            if (given == null)
            {
                given = new GivenData<T>((Container)Container, this);
            }

            given.Needs(service);
            return given;
        }

        public IGivenData<T> Needs<TService>()
        {
            return Needs(Container.TypeConvertToService(typeof(TService)));
        }
    }
}