using System;

namespace BitFramework.Conatiner
{
    public class BindData : IBindData
    {
        public string Service { get; }
        public Func<object[], object> Concrete { get; }
        public bool IsStatic { get; }

        public BindData(string service, Func<object[], object> concrete, bool isStatic)
        {
            Service = service;
            Concrete = concrete;
            IsStatic = isStatic;
        }
    }
}