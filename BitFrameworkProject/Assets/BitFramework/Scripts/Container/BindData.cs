using System;
using System.Collections.Generic;
using BitFramework.Exception;
using BitFramework.Util;
using BContainer = BitFramework.Container.Container;

namespace BitFramework.Container
{
    public sealed class BindData : Bindable<IBindData>, IBindData
    {
        private List<Action<IBindData, object>> resolving;
        private List<Action<IBindData, object>> afterResolving;
        private List<Action<IBindData, object>> release;


        public BindData(BContainer container, string service, Func<IContainer, object[], object> concrete,
            bool isStatic)
            : base(container, service)
        {
            Concrete = concrete;
            IsStatic = isStatic;
        }

        public Func<IContainer, object[], object> Concrete { get; }
        public bool IsStatic { get; }

        public IBindData Alias(string alias)
        {
            AssertDestroyed();
            Guard.ParameterNotNull(alias);
            Container.Alias(alias, Service);
            return this;
        }

        public IBindData Tag(string tag)
        {
            AssertDestroyed();
            Guard.ParameterNotNull(tag);
            Container.Tag(tag, Service);
            return this;
        }

        public IBindData OnResolving(Action<IBindData, object> closure)
        {
            AddClosure(closure, ref resolving);
            return this;
        }

        public IBindData OnAfterResolving(Action<IBindData, object> closure)
        {
            AddClosure(closure, ref afterResolving);
            return this;
        }

        public IBindData OnRelease(Action<IBindData, object> closure)
        {
            if (!IsStatic)
            {
                throw new LogicException(
                    $"Service [{Service}] is not Singleton Bind, Can not call {nameof(OnRelease)} Function");
            }

            AddClosure(closure, ref release);
            return this;
        }

        internal object TriggerResolving(object instance)
        {
            return BContainer.Trigger(this, instance, resolving);
        }

        internal object TriggerAfterResolving(object instance)
        {
            return BContainer.Trigger(this, instance, afterResolving);
        }

        internal object TriggerRelease(object instance)
        {
            return BContainer.Trigger(this, instance, release);
        }

        protected override void ReleaseBind()
        {
            ((BContainer)Container).Unbind(this);
        }

        private void AddClosure(Action<IBindData, object> closure, ref List<Action<IBindData, object>> collection)
        {
            Guard.Requires<ArgumentException>(closure != null);
            if (collection == null)
            {
                collection = new List<Action<IBindData, object>>();
            }

            collection.Add(closure);
        }
    }
}