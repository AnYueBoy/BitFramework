using System;

namespace BitFramework.Container
{
    public interface IBindData
    {
        string Service { get; }
       Func<object[],object> Concrete { get; }
       
       bool IsStatic { get; }
    }
}