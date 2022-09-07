using System;
using BitFramework.Util;

namespace BitFramework.Container
{
    public class GivenData<T> : IGivenData<T>
        where T : class, IBindable<T>
    {
        private readonly Bindable<T> bindable;
        private readonly Container container;
        private string needs;

        public GivenData(Container container, Bindable<T> bindable)
        {
            this.container = container;
            this.bindable = bindable;
        }

        public T Given(string service)
        {
            Guard.ParameterNotNull(service);

            bindable.AddContextual(needs, service);
            return bindable as T;
        }

        public T Given<TService>()
        {
            return Given(container.TypeConvertToService(typeof(TService)));
        }

        public T Given(Func<object> closure)
        {
            Guard.Requires<ArgumentException>(closure != null);

            bindable.AddContextual(needs, closure);
            return bindable as T;
        }

        internal IGivenData<T> Needs(string needs)
        {
            this.needs = needs;
            return this;
        }
    }
}