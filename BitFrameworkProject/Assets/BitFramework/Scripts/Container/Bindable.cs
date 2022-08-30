using System;
using System.Collections.Generic;

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

        /// <inheritdoc cref="Unbind"/>
        protected abstract void ReleaseBind();
    }
}