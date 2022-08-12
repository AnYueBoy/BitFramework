using System;

namespace BitFramework.Conatiner
{
    public interface IBindData
    {
        string Service { get; }
       Func<object[],object> Concrete { get; }
       
       bool IsStatic { get; }
    }
}