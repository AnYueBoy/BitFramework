using System;

namespace BitFramework.Container
{
    /// <summary>
    /// 表示类的构造函数允许传入基元类型（Include字符串）以转换为当前类
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class VariantAttribute : Attribute
    {
    }
}