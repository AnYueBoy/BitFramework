using System.Reflection;

namespace BitFramework.Container
{
    internal sealed class MethodBind : Bindable<IMethodBind>, IMethodBind
    {
        private readonly MethodContainer methodContainer;

        public MethodBind(MethodContainer methodContainer, Container container, string service, object target,
            MethodInfo call) : base(container, service)
        {
            this.methodContainer = methodContainer;
            Target = target;
            MethodInfo = call;
            ParameterInfos = call.GetParameters();
        }

        /// <summary>
        /// 获取要调用该方法的实例
        /// </summary>
        public object Target { get; }

        /// <summary>
        /// 获取方法信息
        /// </summary>
        public MethodInfo MethodInfo { get; }

        /// <summary>
        /// 获取方法参数的数组
        /// </summary>
        public ParameterInfo[] ParameterInfos { get; }

        protected override void ReleaseBind()
        {
            methodContainer.Unbind(this);
        }
    }
}